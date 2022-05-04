using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    public abstract class _MediaControllerBase : _BaseProfileController
    {
        internal const int LIST_SIZE = 100;

        internal const long ID_CONTINUE_WATCHING = -1;
        internal const long ID_WATCHLIST = -2;
        internal const long ID_PLAYLISTS = -3;
        internal const long ID_RECENTLY_ADDED = -4;

        internal readonly Services.TMDBClient _tmdbClient;


        internal _MediaControllerBase(AppDbContext db, Services.TMDBClient tmdbClient) : base(db)
        {
            _tmdbClient = tmdbClient;        }


        internal async Task<ActionResult> DeleteMedia(int id)
        {
            //Get the object, making sure it's owned
            var mediaEntry = await DB.MediaEntries
                .Include(item => item.Library)
                .Include(item => item.Subtitles)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (mediaEntry == null || mediaEntry.Library.AccountId != UserAccount.Id)
                return NotFound("Either the specified item does not exist or is not owned by this account");

            DB.MediaEntries.Remove(mediaEntry);
            await DB.SaveChangesAsync();

            return Ok();
        }


        internal async Task UpdatePopularity(MediaEntry me)
        {
            if (!(me.EntryType == MediaTypes.Movie || me.EntryType == MediaTypes.Series))
                return;

            if (me.TMDB_Id == null)
                return;

            if (me.TMDB_Id <= 0)
                return;

            try
            {
                me.Popularity = await DB.MediaEntries
                    .Where(item => item.TMDB_Id == me.TMDB_Id)
                    .Where(item => item.EntryType == me.EntryType)
                    .Where(item => item.Popularity != null)
                    .Where(item => item.Popularity > 0)
                    .Select(item => item.Popularity)
                    .FirstOrDefaultAsync();

                if (me.Popularity == null)
                {
                    if (me.EntryType == MediaTypes.Movie)
                    {
                        var movie = await _tmdbClient.GetMovieAsync(me.TMDB_Id.Value);
                        me.Popularity = movie.Success ? movie.Data.Popularity : 0;
                    }
                    else //Series
                    {
                        var series = await _tmdbClient.GetSeriesAsync(me.TMDB_Id.Value);
                        me.Popularity = series.Success ? series.Data.Popularity : 0;
                    }
                }
            }
            catch
            {
                me.Popularity = 0;
            }
        }


        internal static List<string> GetSearchTerms(MediaEntry me, List<string> extraSearchTerms)
        {
            var ret = me.Title.Tokenize();

            //foreach (string name in me.Cast)
            //    ret.AddRange(name.Tokenize());

            //foreach (string name in me.Directors)
            //    ret.AddRange(name.Tokenize());

            //foreach (string name in me.Writers)
            //    ret.AddRange(name.Tokenize());

            //foreach (string name in me.Producers)
            //    ret.AddRange(name.Tokenize());

            if (extraSearchTerms != null)
                ret.AddRange(extraSearchTerms.Select(item => (item + string.Empty).Trim().NormalizeMiscCharacters()));

            ret.RemoveAll(item => string.IsNullOrWhiteSpace(item));

            return ret.Distinct().ToList();
        }


        internal async Task<ActionResult> RequestAccessOverride(int id, MediaTypes mediaType)
        {
            if (!UserProfile.IsMain)
                if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                    return CommonResponses.Forbid;


            //Get the media entry
            var meQ =
                from mediaEntry in DB.MediaEntries
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
                    .Include(item => item.OverrideRequests)

                where mediaEntry.Id == id && mediaEntry.EntryType == mediaType
                select mediaEntry;

            var data = await meQ.SingleOrDefaultAsync();
            if (data == null)
                return NotFound();


            //See if the media searchable and playable by profile
            bool searchable = await DB.MediaEntriesSearchableByProfile(UserAccount, UserProfile)
                .Where(item => item.Id == id)
                .Where(item => item.EntryType == mediaType)
                .AnyAsync();

            if (!searchable)
                return NotFound();


            //Check if already playable
            bool playable = await DB.MediaEntriesPlayableByProfile(UserProfile)
                .Where(item => item.Id == id)
                .Where(item => item.EntryType == mediaType)
                .AnyAsync();

            if (playable)
                return BadRequest($"You already have access to this {mediaType.ToString().ToLower()}");


            //Check if already requested
            if (data.OverrideRequests.Any(item => item.ProfileId == UserProfile.Id))
                return BadRequest($"You have already requested access to this {mediaType.ToString().ToLower()}");


            var request = DB.OverrideRequests.Add(new OverrideRequest
            {
                MediaEntryId = id,
                ProfileId = UserProfile.Id,
                Status = RequestStatus.Requested,
                Timestamp = DateTime.UtcNow
            }).Entity;

            await DB.SaveChangesAsync();

            return Ok();
        }


        internal async Task<ActionResult> SetAccessOverride(API.v3.Models.TitleOverride info, MediaTypes mediaType)
        {
            // Check the profile
            if (!UserAccount.Profiles.Any(item => item.Id == info.ProfileId))
                return NotFound("Profile not found");

            //Get the media entry
            var meQ =
                from mediaEntry in DB.MediaEntries
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

                    .Include(item => item.Library)
                    .ThenInclude(item => item.ProfileLibraryShares)
                    .ThenInclude(item => item.Profile)

                    .Include(item => item.OverrideRequests)

                where mediaEntry.Id == info.MediaEntryId && mediaEntry.EntryType == mediaType
                select mediaEntry;

            var data = await meQ.SingleOrDefaultAsync();
            if (data == null)
                return NotFound($"{mediaType} not found");

            //Make sure media is accessable to the account
            if (data.Library.AccountId != UserAccount.Id)
                if (!data.Library.FriendLibraryShares.Any(item => item.Friendship.Account1Id == data.Library.AccountId))
                    if (!data.Library.FriendLibraryShares.Any(item => item.Friendship.Account2Id == data.Library.AccountId))
                        return NotFound($"{mediaType} not found");

            var existingOverride = await DB.TitleOverrides
                .Where(item => item.MediaEntryId == info.MediaEntryId)
                .Where(item => item.ProfileId == info.ProfileId)
                .FirstOrDefaultAsync();

            if (info.State == OverrideState.Default)
            {
                //Delete override if exists
                if (existingOverride != null)
                    DB.TitleOverrides.Remove(existingOverride);
            }
            else
            {
                //Add or update
                if (existingOverride == null)
                    existingOverride = DB.TitleOverrides.Add(new Data.Models.TitleOverride
                    {
                        ProfileId = info.ProfileId,
                        MediaEntryId = info.MediaEntryId
                    }).Entity;

                existingOverride.State = info.State;
            }


            // Update any requests
            var overrideRequest = await DB.OverrideRequests
                .Where(item => item.MediaEntryId == info.MediaEntryId)
                .Where(item => item.ProfileId == info.ProfileId)
                .FirstOrDefaultAsync();

            if (overrideRequest != null)
            {
                if (info.State == OverrideState.Allow)
                {
                    overrideRequest.Status = RequestStatus.Fufilled;
                }
                else if (info.State == OverrideState.Block)
                {
                    overrideRequest.Status = RequestStatus.Denied;
                }
                else
                {
                    //Default
                    var profile = UserAccount.Profiles.Single(item => item.Id == info.ProfileId);
                    if (profile.IsMain)
                    {
                        overrideRequest.Status = RequestStatus.Fufilled;
                    }
                    else if (data.Library.ProfileLibraryShares.Any(item => item.ProfileId == profile.Id))
                    {
                        if (profile.AllowedRatings == Ratings.All)
                            overrideRequest.Status = RequestStatus.Fufilled;

                        else if (data.Rated.HasValue && ((profile.AllowedRatings & data.Rated) == data.Rated))
                            overrideRequest.Status = RequestStatus.Fufilled;

                        else
                            overrideRequest.Status = RequestStatus.Denied;
                    }
                    else
                    {
                        overrideRequest.Status = RequestStatus.Denied;
                    }
                }

                overrideRequest.NotificationCreated = false;
                overrideRequest.Timestamp = DateTime.UtcNow;
            }

            await DB.SaveChangesAsync();

            return Ok();
        }


        internal async Task<ActionResult> UpdateMediaPlaybackProgress(PlaybackProgress hist, IQueryable<MediaEntry> q)
        {
            if (hist == null)
                return NotFound();

            if (hist.Id <= 0)
                return NotFound();

            hist.Seconds = Math.Max(hist.Seconds, 0);

            var query =
                from mediaEntry in q
                    .Where(item => item.Id == hist.Id)

                join progress in MediaProgress on mediaEntry.Id equals progress.MediaEntryId into progressLJ
                from progress in progressLJ.DefaultIfEmpty()

                select new { mediaEntry, progress };


            var data = await query.SingleOrDefaultAsync();
            if (data == null || data.mediaEntry == null)
                return NotFound();

            var prog = data.progress;
            if (prog == null)
            {
                //Add
                prog = DB.ProfileMediaProgresses.Add(new ProfileMediaProgress
                {
                    MediaEntryId = hist.Id,
                    ProfileId = UserProfile.Id,
                    Played = hist.Seconds,
                    Timestamp = DateTime.UtcNow
                }).Entity;
            }
            else
            {
                //Update
                prog.Played = hist.Seconds;
                prog.Timestamp = DateTime.UtcNow;
                DB.Entry(prog).State = EntityState.Modified;
            }

            await DB.SaveChangesAsync();

            return Ok();
        }
                

        internal IQueryable<ProfileMediaProgress> MediaProgress =>
            DB.ProfileMediaProgresses
            .AsNoTracking()
            .Include(item => item.Profile)
            .Where(item => item.ProfileId == UserProfile.Id);


        internal static IOrderedQueryable<MediaEntry> ApplySortOrder(IQueryable<MediaEntry> q, SortOrder sortOrder)
        {
            if (sortOrder == SortOrder.Alphabetical)
                return q.OrderBy(item => item.SortTitle);

            if (sortOrder == SortOrder.Alphabetical_Descending)
                return q.OrderByDescending(item => item.SortTitle);

            if (sortOrder == SortOrder.Added)
                return q.OrderBy(item => item.Added);

            if (sortOrder == SortOrder.Added_Descending)
                return q.OrderByDescending(item => item.Added);

            if (sortOrder == SortOrder.Released)
                return q.OrderBy(item => item.Date);

            if (sortOrder == SortOrder.Released_Descending)
                return q.OrderByDescending(item => item.Date);

            if (sortOrder == SortOrder.Popularity)
                return q.OrderBy(item => item.Popularity);

            if (sortOrder == SortOrder.Popularity_Descending)
                return q.OrderByDescending(item => item.Popularity);

            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        
    }
}
