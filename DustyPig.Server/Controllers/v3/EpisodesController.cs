﻿using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(EpisodesController))]
    public class EpisodesController : _MediaControllerBase
    {
        public EpisodesController(AppDbContext db, TMDBClient tmdbClient, IMemoryCache memoryCache) : base(db, tmdbClient, memoryCache)
        {
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DetailedEpisode>> Details(int id)
        {
            //Get the media entry
            var data = await DB.MediaEntries
                .AsNoTracking()
                .Include(Item => Item.Library)
                .ThenInclude(item => item.Account)
                .ThenInclude(item => item.Profiles)
                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares)
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account1)
                .ThenInclude(item => item.Profiles)
                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares)
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account2)
                .ThenInclude(item => item.Profiles)
                .Include(item => item.Subtitles)
                .Where(item => item.Id == id)
                .Where(item => item.EntryType == MediaTypes.Episode)
                .SingleOrDefaultAsync();

            if (data == null)
                return NotFound();


            //See if the movie or series is searchable and playable by profile
            bool searchable = await DB.MediaEntriesSearchableByProfile(UserAccount, UserProfile)
                .Where(item => item.Id == data.LinkedToId.Value)
                .AnyAsync();

            if (!searchable)
                return NotFound();

            bool playable = await DB.EpisodesPlayableByProfile(UserProfile)
                .Where(item => item.Id == id)
                .AnyAsync();




            //Build the response
            var ret = new DetailedEpisode
            {
                ArtworkUrl = data.ArtworkUrl,
                BifAsset = playable ? Utils.GetAsset(data.BifServiceCredential, _memoryCache, data.BifUrl) : null,
                CreditsStartTime = data.CreditsStartTime,
                Date = data.Date.Value,
                Description = data.Description,
                EpisodeNumber = (ushort)data.Episode.Value,
                Id = data.Id,
                IntroEndTime = data.IntroEndTime,
                IntroStartTime = data.IntroStartTime,
                Length = data.Length.Value,
                SeasonNumber = (ushort)data.Season.Value,
                SeriesId = data.LinkedToId.Value,
                Title = data.Title,
                TMDB_Id = data.TMDB_Id,
                VideoAsset = playable ? Utils.GetAsset(data.VideoServiceCredential, _memoryCache, data.VideoUrl) : null
            };


            if (playable)
            {
                foreach (var dbSub in data.Subtitles)
                {
                    var asset = Utils.GetAsset(dbSub.ServiceCredential, _memoryCache, dbSub.Url);
                    ret.ExternalSubtitles.Add(new ExternalSubtitle
                    {
                        Name = dbSub.Name,
                        ExpiresUTC = asset.ExpiresUTC,
                        ServiceCredentialId = asset.ServiceCredentialId,
                        Token = asset.Token,
                        Url = asset.Url
                    });
                }

                if (ret.ExternalSubtitles.Count == 0)
                    ret.ExternalSubtitles = null;



                //Get all episodes
                var epQ =
                    from mediaEntry in DB.MediaEntries
                        .AsNoTracking()
                        .Include(item => item.BifServiceCredential)
                        .Include(item => item.VideoServiceCredential)
                        .Include(item => item.Subtitles)
                        .ThenInclude(item => item.ServiceCredential)
                        .Include(item => item.People)
                        .ThenInclude(item => item.Person)
                        .Where(item => item.LinkedToId == id)

                    join progress in MediaProgress on mediaEntry.Id equals progress.MediaEntryId into progressLJ
                    from progress in progressLJ.DefaultIfEmpty()

                    where mediaEntry.Id == id

                    orderby mediaEntry.Xid

                    select new { mediaEntry, progress };

                var dbEps = await epQ.ToListAsync();

                DetailedEpisode upnext = null;
                var lastTS = DateTime.MinValue;

                foreach (var dbEp in dbEps)
                {
                    var ep = new DetailedEpisode
                    {
                        Id = dbEp.mediaEntry.Id,
                    };

                    if (upnext == null)
                        upnext = ep;

                    if (dbEp.progress != null)
                    {
                        if (dbEp.progress.Played > 1000 && dbEp.progress.Played < (dbEp.mediaEntry.CreditsStartTime ?? dbEp.mediaEntry.Length.Value - 30))
                            ep.Played = dbEp.progress.Played;

                        if (dbEp.progress.Timestamp > lastTS)
                        {
                            upnext = ep;
                            lastTS = dbEp.progress.Timestamp;
                        }
                    }
                }

                if (upnext != null)
                {
                    ret.UpNext = upnext.Id == ret.Id;
                    ret.Played = ret.UpNext ? upnext.Played : null;
                }
            }

            return ret;
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.Created)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<SimpleValue<int>>> Create(CreateEpisode episodeInfo)
        {
            // ***** Tons of validation *****
            try { episodeInfo.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }



            //Make sure any credential ids are owned
            if (episodeInfo.VideoAsset.ServiceCredentialId != null || (episodeInfo.BifAsset != null && episodeInfo.BifAsset.ServiceCredentialId != null))
            {
                var acctCredentialIds = await DB.EncryptedServiceCredentials
                    .AsNoTracking()
                    .Where(item => item.AccountId == UserAccount.Id)
                    .Select(item => item.Id)
                    .ToListAsync();

                if (episodeInfo.BifAsset != null && episodeInfo.BifAsset.ServiceCredentialId != null)
                    if (!acctCredentialIds.Contains(episodeInfo.BifAsset.ServiceCredentialId.Value))
                        return NotFound($"{nameof(CreateMovie.BifAsset)}.{nameof(episodeInfo.BifAsset.ServiceCredentialId)}");

                if (episodeInfo.VideoAsset.ServiceCredentialId != null)
                    if (!acctCredentialIds.Contains(episodeInfo.VideoAsset.ServiceCredentialId.Value))
                        return NotFound(nameof(episodeInfo.VideoAsset.ServiceCredentialId));

                if (episodeInfo.ExternalSubtitles != null)
                    foreach (var subtitle in episodeInfo.ExternalSubtitles)
                        if (subtitle.ServiceCredentialId != null)
                            if (!acctCredentialIds.Contains(subtitle.ServiceCredentialId.Value))
                                return NotFound($"{nameof(CreateExternalSubtitle)}.{nameof(subtitle.ServiceCredentialId)}");
            }




            //Make sure the series is owned
            var ownedSeries = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Library)
                .Where(item => item.Id == episodeInfo.SeriesId)
                .SingleOrDefaultAsync();
            if (ownedSeries == null)
                return NotFound(nameof(episodeInfo.SeriesId));


            if (ownedSeries.Library.AccountId != UserAccount.Id)
                return NotFound(nameof(episodeInfo.SeriesId));


            // ***** Ok at this point the mediaInfo has all required data, build the new entry *****
            var newItem = new MediaEntry
            {
                Added = DateTime.UtcNow,
                ArtworkUrl = episodeInfo.ArtworkUrl,
                BifUrl = episodeInfo.BifAsset?.Url,
                BifServiceCredentialId = episodeInfo.BifAsset?.ServiceCredentialId,
                CreditsStartTime = episodeInfo.CreditsStartTime,
                Date = episodeInfo.Date,
                Description = episodeInfo.Description,
                EntryType = MediaTypes.Episode,
                Episode = episodeInfo.EpisodeNumber,
                IntroEndTime = episodeInfo.IntroEndTime,
                IntroStartTime = episodeInfo.IntroStartTime,
                Length = episodeInfo.Length,
                LibraryId = ownedSeries.LibraryId,
                LinkedToId = episodeInfo.SeriesId,
                Rated = ownedSeries.Rated,
                Season = episodeInfo.SeasonNumber,
                Title = episodeInfo.Title,
                TMDB_Id = episodeInfo.TMDB_Id,
                VideoServiceCredentialId = episodeInfo.VideoAsset.ServiceCredentialId,
                VideoUrl = episodeInfo.VideoAsset.Url
            };

            newItem.Hash = newItem.ComputeHash();
            newItem.Xid = newItem.ComputeXid();

            //Dup check
            var existingItem = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.LibraryId == newItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Episode)
                .Where(item => item.TMDB_Id == newItem.TMDB_Id)
                .Where(item => item.Hash == newItem.Hash)
                .AnyAsync();

            if (existingItem)
                return BadRequest($"An episode already exists with the following parameters: {nameof(ownedSeries.LibraryId)}, {nameof(episodeInfo.TMDB_Id)}, {nameof(episodeInfo.Title)}");


            //Add the new item
            DB.MediaEntries.Add(newItem);


            //Add Subtitles
            if (episodeInfo.ExternalSubtitles != null)
                foreach (var srt in episodeInfo.ExternalSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntry = newItem,
                        Name = srt.Name,
                        ServiceCredentialId = srt.ServiceCredentialId,
                        Url = srt.Url
                    });

            //Moment of truth!
            await DB.SaveChangesAsync();

            return CommonResponses.CreatedObject(new SimpleValue<int>(newItem.Id));
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> Update(UpdateEpisode episodeInfo)
        {
            try { episodeInfo.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            //Make sure any credential ids are owned
            if (episodeInfo.VideoAsset.ServiceCredentialId != null || (episodeInfo.BifAsset != null && episodeInfo.BifAsset.ServiceCredentialId != null))
            {
                var acctCredentialIds = await DB.EncryptedServiceCredentials
                    .AsNoTracking()
                    .Where(item => item.AccountId == UserAccount.Id)
                    .Select(item => item.Id)
                    .ToListAsync();

                if (episodeInfo.BifAsset != null && episodeInfo.BifAsset.ServiceCredentialId != null)
                    if (!acctCredentialIds.Contains(episodeInfo.BifAsset.ServiceCredentialId.Value))
                        return NotFound($"{nameof(CreateMovie.BifAsset)}.{nameof(episodeInfo.BifAsset.ServiceCredentialId)}");

                if (episodeInfo.VideoAsset.ServiceCredentialId != null)
                    if (!acctCredentialIds.Contains(episodeInfo.VideoAsset.ServiceCredentialId.Value))
                        return NotFound(nameof(episodeInfo.VideoAsset.ServiceCredentialId));

                if (episodeInfo.ExternalSubtitles != null)
                    foreach (var subtitle in episodeInfo.ExternalSubtitles)
                        if (subtitle.ServiceCredentialId != null)
                            if (!acctCredentialIds.Contains(subtitle.ServiceCredentialId.Value))
                                return NotFound($"{nameof(CreateExternalSubtitle)}.{nameof(subtitle.ServiceCredentialId)}");
            }


            //Update
            var existingEpisode = await DB.MediaEntries
                .Include(item => item.LinkedTo)
                .ThenInclude(item => item.Library)
                .Where(item => item.Id == episodeInfo.Id)
                .SingleOrDefaultAsync();

            if (existingEpisode == null)
                return NotFound(nameof(episodeInfo.Id));

            if (existingEpisode.Library.AccountId != UserAccount.Id)
                return NotFound(nameof(episodeInfo.Id));


            existingEpisode.Added = DateTime.UtcNow;
            existingEpisode.ArtworkUrl = episodeInfo.ArtworkUrl;
            existingEpisode.BifUrl = episodeInfo.BifAsset?.Url;
            existingEpisode.BifServiceCredentialId = episodeInfo.BifAsset?.ServiceCredentialId;
            existingEpisode.CreditsStartTime = episodeInfo.CreditsStartTime;
            existingEpisode.Date = episodeInfo.Date;
            existingEpisode.Description = episodeInfo.Description;
            existingEpisode.EntryType = MediaTypes.Episode;
            existingEpisode.Episode = episodeInfo.EpisodeNumber;
            existingEpisode.IntroEndTime = episodeInfo.IntroEndTime;
            existingEpisode.IntroStartTime = episodeInfo.IntroStartTime;
            existingEpisode.Length = episodeInfo.Length;
            existingEpisode.Season = episodeInfo.SeasonNumber;
            existingEpisode.Title = episodeInfo.Title;
            existingEpisode.TMDB_Id = episodeInfo.TMDB_Id;
            existingEpisode.VideoServiceCredentialId = episodeInfo.VideoAsset.ServiceCredentialId;
            existingEpisode.VideoUrl = episodeInfo.VideoAsset.Url;

            existingEpisode.Hash = existingEpisode.ComputeHash();
            existingEpisode.Xid = existingEpisode.ComputeXid();

            //Dup check
            var existingItem = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.Id != existingEpisode.Id)
                .Where(item => item.LibraryId == existingEpisode.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Episode)
                .Where(item => item.TMDB_Id == existingEpisode.TMDB_Id)
                .Where(item => item.Hash == existingEpisode.Hash)
                .AnyAsync();

            if (existingItem)
                return BadRequest($"An episode already exists with the following parameters: {nameof(existingEpisode.LibraryId)}, {nameof(episodeInfo.TMDB_Id)}, {nameof(episodeInfo.Title)}");



            //Redo Subtitles
            var subLst = await DB.Subtitles
                .Where(item => item.MediaEntryId == existingEpisode.Id)
                .ToListAsync();
            subLst.ForEach(item => DB.Subtitles.Remove(item));
            if (episodeInfo.ExternalSubtitles != null)
                foreach (var srt in episodeInfo.ExternalSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntryId = existingEpisode.Id,
                        Name = srt.Name,
                        ServiceCredentialId = srt.ServiceCredentialId,
                        Url = srt.Url
                    });

            //Moment of truth!
            await DB.SaveChangesAsync();

            return Ok();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> Delete(int id) => DeleteMedia(id);


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> UpdatePlaybackProgress(PlaybackProgress hist) => UpdateMediaPlaybackProgress(hist, DB.EpisodesPlayableByProfile(UserProfile));

    }
}