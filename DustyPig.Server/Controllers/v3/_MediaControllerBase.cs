using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    public abstract class _MediaControllerBase : _BaseProfileController
    {
        internal const int DEFAULT_LIST_SIZE = Constants.SERVER_RESULT_SIZE;
        internal const int ADMIN_LIST_SIZE = 100;
        internal const int MIN_GENRE_LIST_SIZE = 10;
        internal const int MAX_DB_LIST_SIZE = 1000; //This should be approximately # of Genres flags x DEFAULT_LIST_SIZE, which is currently 950

        internal _MediaControllerBase(AppDbContext db) : base(db) { }


        internal async Task<TMDB_Entry> GetTMDBInfoAsync(int? tmdbId, TMDB_MediaTypes tmdbMediaType)
        {
            if (tmdbId == null)
                return null;

            return await DB.TMDB_Entries
                .AsNoTracking()
                .Where(item => item.TMDB_Id == tmdbId.Value)
                .Where(item => item.MediaType == tmdbMediaType)
                .FirstOrDefaultAsync();
        }

        internal async Task<Result> DeleteMedia(int id)
        {
            if (!UserProfile.IsMain)
                return CommonResponses.RequireMainProfile();

            //Get the object, making sure it's owned
            var mediaEntry = await DB.MediaEntries
                .Include(item => item.Library)
                .Where(item => item.Id == id)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return Result.BuildSuccess();

            // Flag playlist artwork for updates
            var playlists = mediaEntry.EntryType == MediaTypes.Series ?
                await DB.MediaEntries
                    .Where(item => item.LinkedToId == id)
                    .Include(item => item.PlaylistItems)
                    .ThenInclude(item => item.Playlist)
                    .SelectMany(item => item.PlaylistItems)
                    .Select(item => item.Playlist)
                    .Distinct()
                    .ToListAsync() :

                await DB.PlaylistItems
                    .Where(item => item.MediaEntryId == id)
                    .Include(item => item.Playlist)
                    .Select(item => item.Playlist)
                    .Distinct()
                    .ToListAsync();

            playlists.ForEach(item => item.ArtworkUpdateNeeded = true);


            DB.MediaEntries.Remove(mediaEntry);
            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }

        internal async Task<DetailedSeries> GetSeriesDetailsAsync(int id)
        {
            var media = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Library)
                .ThenInclude(item => item.Account)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares.Where(item2 => item2.Friendship.Account1Id == UserAccount.Id || item2.Friendship.Account2Id == UserAccount.Id))
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account1)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares.Where(item2 => item2.Friendship.Account1Id == UserAccount.Id || item2.Friendship.Account2Id == UserAccount.Id))
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account2)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)
                .ThenInclude(item => item.ProfileLibraryShares.Where(item2 => item2.Profile.Id == UserProfile.Id))

                .Include(item => item.TitleOverrides.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Include(item => item.TMDB_Entry)
                .ThenInclude(item => item.People)
                .ThenInclude(item => item.TMDB_Person)

                .Include(item => item.WatchlistItems.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Include(item => item.ProfileMediaProgress.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Include(item => item.Subscriptions.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Where(item => item.Id == id)
                .Where(item => item.EntryType == MediaTypes.Series)
                .FirstOrDefaultAsync();

            if (media == null)
                return null;

            if (media.Library.AccountId != UserAccount.Id)
                if (!media.Library.FriendLibraryShares.Any())
                    if (!media.TitleOverrides.Any())
                        if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                            return null;


            bool playable = (UserProfile.IsMain)
                || media.TitleOverrides.Any(item => item.State == OverrideState.Allow)
                ||
                (
                    !media.TitleOverrides.Any(item => item.State == OverrideState.Block)
                    &&
                    (
                        media.Library.ProfileLibraryShares.Any()
                        && UserProfile.MaxTVRating >= (media.TVRating ?? TVRatings.NotRated)
                    )
                );



            //Build the response
            var ret = new DetailedSeries
            {
                Id = id,
                Added = media.Added,
                ArtworkUrl = media.ArtworkUrl,
                BackdropUrl = media.BackdropUrl,
                CanPlay = playable,
                Credits = media.GetPeople(),
                Description = media.Description,
                Genres = media.GetGenreFlags(),
                LibraryId = media.LibraryId,
                Rated = media.TVRating ?? TVRatings.None,
                Title = media.Title,
                TitleRequestPermission = TitleRequestLogic.GetTitleRequestPermissions(UserAccount, UserProfile, media.Library.FriendLibraryShares.Any()),
                TMDB_Id = media.TMDB_Id,
                Subscribed = media.Subscriptions.Any(),
            };

            ret.CanManage = UserProfile.IsMain
                &&
                (
                    UserAccount.Profiles.Count > 1
                    ||
                    (
                        media.Library.AccountId == UserAccount.Id
                        && media.Library.FriendLibraryShares.Count > 0
                    )
                );


            if (playable)
                ret.InWatchlist = media.WatchlistItems.Any();

            //Get the media owner
            if (media.Library.AccountId == UserAccount.Id)
            {
                ret.Owner = UserAccount.Profiles.Single(item => item.IsMain).Name;
            }
            else
            {
                ret.Owner = media.Library.FriendLibraryShares
                    .Select(item => item.Friendship)
                    .First()
                    .GetFriendDisplayNameForAccount(UserAccount.Id);
            }

            var progress = media.ProfileMediaProgress.FirstOrDefault(item => item.ProfileId == UserProfile.Id);
            if (progress != null && progress.Played < 1)
                progress = null;


            //Get the episodes
            var dbEps = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Subtitles)
                .Where(item => item.LinkedToId == id)
                .OrderBy(item => item.Xid)
                .ToListAsync();

            if (dbEps.Count > 0)
            {
                ret.Episodes ??= new();
                foreach (var dbEp in dbEps)
                {
                    var ep = new DetailedEpisode
                    {
                        Added = dbEp.Added,
                        ArtworkUrl = dbEp.ArtworkUrl,
                        BifUrl = playable ? dbEp.BifUrl : null,
                        CreditsStartTime = dbEp.CreditsStartTime,
                        Date = dbEp.Date.Value,
                        Description = dbEp.Description,
                        EpisodeNumber = (ushort)dbEp.Episode.Value,
                        SRTSubtitles = playable ? dbEp.Subtitles.ToSRTSubtitleList() : null,
                        Id = dbEp.Id,
                        IntroEndTime = dbEp.IntroEndTime,
                        IntroStartTime = dbEp.IntroStartTime,
                        Length = dbEp.Length.Value,
                        SeasonNumber = (ushort)dbEp.Season.Value,
                        SeriesId = id,
                        Title = dbEp.Title,
                        TMDB_Id = dbEp.TMDB_Id,
                        VideoUrl = playable ? dbEp.VideoUrl : null,
                    };

                    ret.Episodes.Add(ep);
                }
            }

            if (playable)
            {
                if (ret.Episodes != null && ret.Episodes.Count > 0)
                {
                    if (progress != null)
                    {
                        var dbEp = dbEps.FirstOrDefault(item => item.Xid == progress.Xid);
                        if (dbEp != null)
                        {
                            var upNextEp = ret.Episodes.First(item => item.Id == dbEp.Id);
                            if (progress.Played < (upNextEp.CreditsStartTime ?? dbEp.Length.Value - 30))
                            {
                                //Partially played episode
                                upNextEp.UpNext = true;
                                upNextEp.Played = progress.Played;
                            }
                            else
                            {
                                //Fully played episode, find the next one
                                var nextDBEp = dbEps.FirstOrDefault(item => item.Xid > dbEp.Xid);
                                if (nextDBEp == null)
                                {
                                    //Progress was on last episode
                                    upNextEp.UpNext = true;
                                    upNextEp.Played = progress.Played;
                                }
                                else
                                {
                                    //Next episode after progress
                                    upNextEp = ret.Episodes.First(item => item.Id == nextDBEp.Id);
                                    upNextEp.UpNext = true;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var overrideRequest = media.TitleOverrides.FirstOrDefault(item => item.ProfileId == UserProfile.Id);
                if (overrideRequest != null)
                    ret.AccessRequestedStatus = overrideRequest.Status;
            }

            return ret;
        }
    }
}
