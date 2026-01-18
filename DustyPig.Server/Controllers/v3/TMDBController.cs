using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
using DustyPig.Server.Services;
using DustyPig.TMDB.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DustyPig.Server.Services.TMDBClient;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ProhibitTestUser]
    public class TMDBController : _BaseProfileController
    {
        public TMDBController(AppDbContext db) : base(db) { }

        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedTMDB>))]
        public async Task<Result<DetailedTMDB>> GetMovie(int id)
        {
            if (!(UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled))
                return CommonResponses.Forbid();

            var movieResponse = await TMDBClient.DefaultInstance.GetMovieAsync(id);
            if (!movieResponse.Success)
                return movieResponse.Error.Message;

            var movie = movieResponse.Data;

            var ret = new DetailedTMDB
            {
                ArtworkUrl = GetPosterPath(movie.PosterPath),
                BackdropUrl = GetBackdropPath(movie.BackdropPath),
                Description = movie.Overview,
                MediaType = TMDB_MediaTypes.Movie,
                Rated = TryMapMovieRatings(movie),
                Title = movie.Title,
                TMDB_ID = movie.Id
            };

            ret.Year = TryGetMovieDate(movie)?.Year ?? 0;

            if (movie.Genres != null)
                ret.Genres = string.Join(",", movie.Genres.Select(item => item.Name)).ToGenres();

            FillCredits(GetCommonCredits(movie), ret);
            ret.Credits?.ForEach(c =>
            {
                if (string.IsNullOrWhiteSpace(c.AvatarUrl))
                    c.AvatarUrl = Constants.DEFAULT_PROFILE_IMAGE_GREY;
            });

            if (UserProfile.IsMain)
            {
                var getRequests = await DB.GetRequests
                    .AsNoTracking()
                    .Include(g => g.NotificationSubscriptions)
                    .ThenInclude(n => n.Profile)
                    .Where(g => g.AccountId == UserAccount.Id)
                    .Where(g => g.TMDB_Id == id)
                    .Where(g => g.EntryType == TMDB_MediaTypes.Movie)
                    .Where(g => g.Status == RequestStatus.RequestSentToAccount || g.Status == RequestStatus.RequestSentToMain)
                    .ToListAsync();

                if (getRequests.Count > 0)
                {
                    ret.RequestedOfThisProfile = true;
                    ret.Requestors = getRequests.SelectMany(g => g.NotificationSubscriptions.Select(n => n.Profile.Name)).ToList();
                }
            }


            var available = await DB.MediaEntries
                .AsNoTracking()
                .Include(m => m.Library)
                .ThenInclude(l => l.FriendLibraryShares.Where(f => f.Friendship.Account1Id == UserAccount.Id || f.Friendship.Account2Id == UserAccount.Id))
                .Include(m => m.Library)
                .ThenInclude(l => l.ProfileLibraryShares.Where(p => p.ProfileId == UserProfile.Id))
                .Include(m => m.TitleOverrides
                    .Where(o => o.ProfileId == UserProfile.Id)
                    .Where(o => new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(o.State))
                )
                .Where(m => m.EntryType == MediaTypes.Movie)
                .Where(m => m.TMDB_Id.HasValue)
                .Where(m => m.TMDB_Id == id)
                .Where(m =>
                    m.TitleOverrides.Any(o => o.State == OverrideState.Allow)
                    ||
                    (
                        UserProfile.IsMain
                        &&
                        (
                            m.Library.AccountId == UserAccount.Id
                            ||
                            (
                                m.Library.FriendLibraryShares.Any()
                                && !m.TitleOverrides.Any(o => o.State == OverrideState.Block)
                            )
                        )
                    )
                    ||
                    (
                        m.Library.ProfileLibraryShares.Any()
                        && UserProfile.MaxMovieRating >= (m.MovieRating ?? MovieRatings.Unrated)
                        && !m.TitleOverrides.Any(o => o.State == OverrideState.Block)
                    )
                )
                .Distinct()
                .ToListAsync();

            ret.Available = available
                .Select(item => item.ToBasicMedia())
                .ToList();

            var reqPerm = await CalculateTitleRequestStatusAsync(id, TMDB_MediaTypes.Movie);
            ret.RequestPermission = reqPerm.Permission;
            ret.RequestStatus = reqPerm.Status;

            return ret;
        }



        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedTMDB>))]
        public async Task<Result<DetailedTMDB>> GetSeries(int id)
        {
            if (!(UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled))
                return CommonResponses.Forbid();

            var seriesResponse = await TMDBClient.DefaultInstance.GetSeriesAsync(id);
            if (!seriesResponse.Success)
                return seriesResponse.Error.Message;
            var series = seriesResponse.Data;

            // Response
            var ret = new DetailedTMDB
            {
                ArtworkUrl = GetPosterPath(series.PosterPath),
                BackdropUrl = GetBackdropPath(series.BackdropPath),
                Description = series.Overview,
                MediaType = TMDB_MediaTypes.Series,
                Rated = TryMapTVRatings(series),
                Title = series.Name,
                TMDB_ID = series.Id
            };


            if (series.FirstAirDate.HasValue)
                ret.Year = series.FirstAirDate.Value.Year;

            if (series.Genres != null)
                ret.Genres = string.Join(",", series.Genres.Select(item => item.Name)).ToGenres();

            FillCredits(GetCommonCredits(series), ret);
            ret.Credits?.ForEach(c =>
            {
                if (string.IsNullOrWhiteSpace(c.AvatarUrl))
                    c.AvatarUrl = Constants.DEFAULT_PROFILE_IMAGE_GREY;
            });


            if (UserProfile.IsMain)
            {
                var getRequests = await DB.GetRequests
                    .AsNoTracking()
                    .Include(g => g.NotificationSubscriptions)
                    .ThenInclude(n => n.Profile)
                    .Where(g => g.AccountId == UserAccount.Id)
                    .Where(g => g.TMDB_Id == id)
                    .Where(g => g.EntryType == TMDB_MediaTypes.Series)
                    .Where(g => g.Status == RequestStatus.RequestSentToAccount || g.Status == RequestStatus.RequestSentToMain)
                    .ToListAsync();

                if (getRequests.Count > 0)
                {
                    ret.RequestedOfThisProfile = true;
                    ret.Requestors = getRequests.SelectMany(g => g.NotificationSubscriptions.Select(n => n.Profile.Name)).ToList();
                }
            }

            var available = await DB.MediaEntries
                .AsNoTracking()
                .Include(m => m.Library)
                .ThenInclude(l => l.FriendLibraryShares.Where(f => f.Friendship.Account1Id == UserAccount.Id || f.Friendship.Account2Id == UserAccount.Id))
                .Include(m => m.Library)
                .ThenInclude(l => l.ProfileLibraryShares.Where(p => p.ProfileId == UserProfile.Id))
                .Include(m => m.TitleOverrides
                    .Where(o => o.ProfileId == UserProfile.Id)
                    .Where(o => new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(o.State))
                )
                .Where(m => m.EntryType == MediaTypes.Series)
                .Where(m => m.TMDB_Id.HasValue)
                .Where(m => m.TMDB_Id == id)
                .Where(m =>
                    m.TitleOverrides.Any(o => o.State == OverrideState.Allow)
                    ||
                    (
                        UserProfile.IsMain
                        &&
                        (
                            m.Library.AccountId == UserAccount.Id
                            ||
                            (
                                m.Library.FriendLibraryShares.Any()
                                && !m.TitleOverrides.Any(o => o.State == OverrideState.Block)
                            )
                        )
                    )
                    ||
                    (
                        m.Library.ProfileLibraryShares.Any()
                        && UserProfile.MaxTVRating >= (m.TVRating ?? TVRatings.NotRated)
                        && !m.TitleOverrides.Any(o => o.State == OverrideState.Block)
                    )
                )
                .Distinct()
                .ToListAsync();

            ret.Available = available
                .Select(item => item.ToBasicMedia())
                .ToList();


            var reqPerm = await CalculateTitleRequestStatusAsync(id, TMDB_MediaTypes.Series);
            ret.RequestPermission = reqPerm.Permission;
            ret.RequestStatus = reqPerm.Status;

            return ret;
        }



        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<API.v3.Models.TMDB_Person>))]
        public async Task<Result<API.v3.Models.TMDB_Person>> GetPerson(int id)
        {
            var response = await TMDBClient.DefaultInstance.Endpoints.People.GetDetailsAsync(id, TMDB.Models.People.AppendToResponse.CombinedCredits);
            if (!response.Success)
                return response.Error.Message;

            var ret = new API.v3.Models.TMDB_Person
            {
                AvatarUrl = TMDBClient.GetAvatarPath(response.Data.ProfilePath),
                Biography = response.Data.Biography,
                Birthday = response.Data.Birthday,
                Deathday = response.Data.Deathday,
                KnownFor = response.Data.KnownForDepartment,
                Name = response.Data.Name,
                PlaceOfBirth = response.Data.PlaceOfBirth,
                TMDB_ID = id
            };

            CommonMediaTypes[] allowed = [CommonMediaTypes.Movie, CommonMediaTypes.TvSeries];

            var castList = new List<TmdbTitleDTO>();
            if (response.Data.CombinedCredits != null)
            {
                var castQ = response.Data.CombinedCredits.Cast
                    .Where(c => !c.Adult)
                    .Where(c => allowed.Contains(c.MediaType));

                foreach (var item in castQ)
                {
                    var mt = item.MediaType == TMDB.Models.Common.CommonMediaTypes.Movie ? MediaTypes.Movie : MediaTypes.Series;

                    bool exists = castList
                        .Where(_ => _.Id == item.Id)
                        .Where(_ => _.MediaType == mt)
                        .Any();
                    if (!exists)
                    {
                        castList.Add(new TmdbTitleDTO
                        {
                            Id = item.Id,
                            MediaType = mt
                        });

                        ret.OtherTitles.Add(new BasicTMDB
                        {
                            ArtworkUrl = TMDBClient.GetPosterPath(item.PosterPath),
                            BackdropUrl = TMDBClient.GetBackdropPath(item.BackdropPath),
                            MediaType = item.MediaType == TMDB.Models.Common.CommonMediaTypes.Movie ? TMDB_MediaTypes.Movie : TMDB_MediaTypes.Series,
                            Title = item.Title,
                            TMDB_ID = item.Id
                        });
                    }
                }

                var crewQ = response.Data.CombinedCredits.Crew
                    .Where(c => !c.Adult)
                    .Where(c => allowed.Contains(c.MediaType));
                foreach (var item in crewQ)
                {
                    if (TMDBClient.CrewJobs.ICContains(item.Job))
                    {
                        var mt = item.MediaType == TMDB.Models.Common.CommonMediaTypes.Movie ? MediaTypes.Movie : MediaTypes.Series;
                        bool exists = castList
                            .Where(_ => _.Id == item.Id)
                            .Where(_ => _.MediaType == mt)
                            .Any();
                        if (!exists)
                        {
                            castList.Add(new TmdbTitleDTO
                            {
                                Id = item.Id,
                                MediaType = mt
                            });

                            ret.OtherTitles.Add(new BasicTMDB
                            {
                                ArtworkUrl = TMDBClient.GetPosterPath(item.PosterPath),
                                BackdropUrl = TMDBClient.GetBackdropPath(item.BackdropPath),
                                MediaType = item.MediaType == TMDB.Models.Common.CommonMediaTypes.Movie ? TMDB_MediaTypes.Movie : TMDB_MediaTypes.Series,
                                Title = item.Title,
                                TMDB_ID = item.Id
                            });
                        }
                    }
                }
            }


            //Put in tmdb order
            foreach (var mediaType in castList.Select(item => item.MediaType).Distinct())
            {
                var subLst = castList
                    .Where(item => item.MediaType == mediaType)
                    .Select(item => item.Id)
                    .Distinct()
                    .ToList();

                var entries = await DB.TopLevelWatchableMediaByProfileQuery(UserProfile)
                    .AsNoTracking()
                    .Where(item => item.TMDB_Id.HasValue)
                    .Where(item => subLst.Contains(item.TMDB_Id.Value))
                    .Where(item => item.EntryType == mediaType)
                    .ToListAsync();

                foreach (var tmdbId in subLst)
                {
                    var entry = entries.FirstOrDefault(m => m.TMDB_Id == tmdbId);
                    if (entry != null)
                    {
                        var bm = entry.ToBasicMedia();
                        ret.Available.Add(bm);
                    }
                }
            }

            return ret;
        }



        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<TitleRequestPermissions>))]
        public async Task<Result<TitleRequestPermissions>> GetRequestTitlePermission()
        {
            var ret = await CalculateTitleRequestPermissionsAsync();
            return ret;
        }



        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> RequestTitle(TitleRequest data)
        {
            //Validate
            try { data.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            //Check for existing request
            var existingSubscription = await DB.GetRequestSubscriptions
                .AsNoTracking()
                .Include(item => item.GetRequest)
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.GetRequest.TMDB_Id == data.TMDB_Id)
                .Where(item => item.GetRequest.EntryType == data.MediaType)
                .AnyAsync();

            if (existingSubscription)
                return "You have already requested this title";



            var targetAcct = UserAccount;

            if (UserProfile.IsMain)
            {
                if (data.FriendId == null)
                    return "You cannot request a title from yourself";

                if (data.FriendId.Value <= 0)
                    return CommonResponses.InvalidValue(nameof(data.FriendId));

                var friend = await DB.Friendships
                    .AsNoTracking()
                    .Include(item => item.Account1)
                    .ThenInclude(item => item.Profiles)
                    .Include(item => item.Account2)
                    .ThenInclude(item => item.Profiles)
                    .Where(item => item.Id == data.FriendId)
                    .FirstOrDefaultAsync();

                if (friend == null)
                    return CommonResponses.ValueNotFound(nameof(data.FriendId));

                targetAcct = friend.Account1Id == UserAccount.Id
                    ? friend.Account2
                    : friend.Account1;
            }
            else
            {
                if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                    return "You are not authorized to request titles";

                if (UserProfile.TitleRequestPermission != TitleRequestPermissions.RequiresAuthorization)
                {
                    //data.FriendId == null means requesting from main profile
                    if (data.FriendId != null)
                    {
                        if (data.FriendId.Value <= 0)
                            return CommonResponses.InvalidValue(nameof(data.FriendId));

                        var friend = await DB.Friendships
                            .AsNoTracking()
                            .Include(item => item.Account1)
                            .ThenInclude(item => item.Profiles)
                            .Include(item => item.Account2)
                            .ThenInclude(item => item.Profiles)
                            .Where(item => item.Id == data.FriendId)
                            .FirstOrDefaultAsync();

                        if (friend == null)
                            return CommonResponses.ValueNotFound(nameof(data.FriendId));

                        targetAcct = friend.Account1Id == UserAccount.Id
                            ? friend.Account2
                            : friend.Account1;
                    }
                }
            }

            //Validate TMDB Id
            string title = null;
            if (data.MediaType == TMDB_MediaTypes.Movie)
            {
                var response = await TMDBClient.DefaultInstance.GetMovieAsync(data.TMDB_Id);
                if (!response.Success)
                    return CommonResponses.ValueNotFound(nameof(data.TMDB_Id));
                title = response.Data.Title;
                var dt = TryGetMovieDate(response.Data);
                if (dt.HasValue)
                    title += $" ({dt.Value.Year})";
            }
            else
            {
                var response = await TMDBClient.DefaultInstance.GetSeriesAsync(data.TMDB_Id);
                if (!response.Success)
                    return CommonResponses.ValueNotFound(nameof(data.TMDB_Id));
                title = response.Data.Name;
            }


            //Check if the target account already has a request to subscribe to
            var existingReq = await DB.GetRequests
                .AsNoTracking()
                .Where(item => item.AccountId == targetAcct.Id)
                .Where(item => item.TMDB_Id == data.TMDB_Id)
                .Where(item => item.EntryType == data.MediaType)
                .FirstOrDefaultAsync();

            bool notifyProfile = false;
            if (existingReq != null)
            {
                //Subscribe
                DB.GetRequestSubscriptions.Add(new GetRequestSubscription
                {
                    GetRequestId = existingReq.Id,
                    ProfileId = UserProfile.Id
                });

                string msg = existingReq.Status switch
                {
                    RequestStatus.Denied => "has been denied",
                    RequestStatus.Fulfilled => "has been fulfilled",
                    RequestStatus.Pending => "has been granted and is pending fulfillment",
                    _ => null
                };

                //Notification
                if (msg != null)
                {
                    var nType = existingReq.Status switch
                    {
                        RequestStatus.Denied => NotificationTypes.NewMediaRejected,
                        RequestStatus.Fulfilled => NotificationTypes.NewMediaFulfilled,
                        RequestStatus.Pending => NotificationTypes.NewMediaPending,
                        _ => throw new Exception("Imposible value for existingReq.Status")
                    };

                    notifyProfile = true;
                    DB.Notifications.Add(new Data.Models.Notification
                    {
                        GetRequestId = existingReq.Id,
                        Message = $"Your requested {data.MediaType.ToString().ToLower()} \"" + title + "\" " + msg,
                        NotificationType = nType,
                        ProfileId = targetAcct.Profiles.First(item => item.IsMain).Id,
                        Timestamp = DateTime.UtcNow,
                        Title = data.MediaType.ToString() + " Requested"
                    });
                }
            }
            else
            {
                //Create the request
                var newReq = new Data.Models.GetRequest
                {
                    AccountId = targetAcct.Id,
                    EntryType = data.MediaType,
                    TMDB_Id = data.TMDB_Id
                };

                if (targetAcct.Id == UserAccount.Id)
                    newReq.Status = RequestStatus.RequestSentToMain;
                else
                    newReq.Status = RequestStatus.RequestSentToAccount;

                //Subscription
                DB.GetRequestSubscriptions.Add(new GetRequestSubscription
                {
                    GetRequest = newReq,
                    ProfileId = UserProfile.Id
                });

                //Notification
                notifyProfile = true;
                DB.Notifications.Add(new Data.Models.Notification
                {
                    GetRequest = newReq,
                    Message = UserProfile.Name + $" has requested the {data.MediaType.ToString().ToLower()} \"" + title + "\"",
                    NotificationType = NotificationTypes.NewMediaRequested,
                    ProfileId = targetAcct.Profiles.First(item => item.IsMain).Id,
                    Timestamp = DateTime.UtcNow,
                    Title = data.MediaType.ToString() + " Requested"
                });

                DB.GetRequests.Add(newReq);
            }


            await DB.SaveChangesAsync();

            if (notifyProfile)
                FirebaseNotificationsManager.QueueProfileForNotifications(targetAcct.Profiles.First(item => item.IsMain).Id);

            return Result.BuildSuccess();
        }



        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> CancelTitleRequest(TitleRequest data)
        {
            //Validate
            try { data.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            var req = await DB.GetRequestSubscriptions
                .AsNoTracking()
                .Where(item => item.GetRequest.TMDB_Id == data.TMDB_Id)
                .Where(item => item.GetRequest.EntryType == data.MediaType)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (req != null)
            {
                DB.GetRequestSubscriptions.Remove(req);
                await DB.SaveChangesAsync();
            }

            return Result.BuildSuccess();
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> DenyTitleRequest(TitleRequest data) => HandleTitleRequestAsync(data, false);





        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> GrantTitleRequest(TitleRequest data) => HandleTitleRequestAsync(data, true);



        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<TitleRequestSource>>))]
        [SwaggerResponse(StatusCodes.Status403Forbidden)]
        public async Task<Result<List<TitleRequestSource>>> ListTitleRequestSources()
        {
            if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                return CommonResponses.Forbid();

            var mainProfile = UserAccount.Profiles.First(p => p.IsMain);

            var ret = new List<TitleRequestSource>();

            if (!UserProfile.IsMain)
                ret.Add(new TitleRequestSource
                {
                    Name = mainProfile.Name,
                    AvatarUrl = mainProfile.AvatarUrl
                });

            if (UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.RequiresAuthorization)
            {
                var friends = await DB.Friendships
                    .AsNoTracking()

                    .Include(item => item.Account1)
                    .ThenInclude(item => item.Profiles)

                    .Include(item => item.Account2)
                    .ThenInclude(item => item.Profiles)

                    .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                    .Where(Item => Item.Accepted == true)
                    .ToListAsync();

                var bfl = friends.Select(item => item.ToBasicFriendInfo(UserAccount.Id)).ToList();
                bfl.Sort();

                foreach (var bf in bfl)
                    ret.Add(new TitleRequestSource
                    {
                        FriendId = bf.Id,
                        Name = bf.DisplayName,
                        AvatarUrl = bf.AvatarUrl
                    });
            }

            return ret;
        }






        private async Task<Result> HandleTitleRequestAsync(TitleRequest data, bool granted)
        {
            //Validate
            try { data.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            var requests = await DB.GetRequests
                .Include(item => item.NotificationSubscriptions)
                .ThenInclude(item => item.Profile)
                .Where(item => item.TMDB_Id == data.TMDB_Id)
                .Where(item => item.EntryType == data.MediaType)
                .Where(item => item.AccountId == UserAccount.Id)
                .ToListAsync();

            if (requests.Count == 0)
                return CommonResponses.ValueNotFound(nameof(data.TMDB_Id));

            string title = null;
            if (data.MediaType == TMDB_MediaTypes.Movie)
            {
                var response = await TMDBClient.DefaultInstance.GetMovieAsync(data.TMDB_Id);
                if (!response.Success)
                    return CommonResponses.ValueNotFound(nameof(data.TMDB_Id));
                title = response.Data.Title;
                var dt = TryGetMovieDate(response.Data);
                if (dt.HasValue)
                    title += $" ({dt.Value.Year})";
            }
            else
            {
                var response = await TMDBClient.DefaultInstance.GetSeriesAsync(data.TMDB_Id);
                if (!response.Success)
                    return CommonResponses.ValueNotFound(nameof(data.TMDB_Id));
                title = response.Data.Name;
            }

            //Profiles requesting the title from this acccount
            var notifyProfileIds = requests.SelectMany(r => r.NotificationSubscriptions).Select(n => n.ProfileId).Distinct().ToList();


            //Get all relevent friends at once
            var friends = new List<Friendship>();
            bool loadFriends = false;
            foreach (var n in notifyProfileIds)
                if (!UserAccount.Profiles.Any(p => p.Id == n))
                {
                    loadFriends = true;
                    break;
                }
            if (loadFriends)
            {
                var friendProfileIds = notifyProfileIds.Where(p => !UserAccount.Profiles.Any(mp => mp.Id == p)).ToList();
                friends = await DB.Friendships
                    .AsNoTracking()
                    .Include(f => f.Account1)
                    .ThenInclude(a => a.Profiles)
                    .Include(f => f.Account2)
                    .ThenInclude(a => a.Profiles)
                    .Where(f => f.Account1Id == UserAccount.Id || f.Account2Id == UserAccount.Id)
                    .Where(f => f.Account1.Profiles.Any(p => friendProfileIds.Contains(p.Id)) || f.Account2.Profiles.Any(p => friendProfileIds.Contains(p.Id)))
                    .ToListAsync();
            }


            //Update status and send notifications
            foreach (var req in requests)
            {
                req.Status = granted ? RequestStatus.Pending : RequestStatus.Denied;
                foreach (var s in req.NotificationSubscriptions)
                {
                    string msg = UserProfile.Name;
                    if (!UserAccount.Profiles.Any(p => p.Id == s.ProfileId))
                    {
                        var friend = friends
                            .Where(f => f.Account1.Profiles.Any(p => p.Id == s.ProfileId) || f.Account2.Profiles.Any(p => p.Id == s.ProfileId))
                            .FirstOrDefault();

                        if (friend != null)
                        {
                            //Display name from other person's pov
                            var accountId = friend.Account1Id == UserAccount.Id ? friend.Account2Id : friend.Account1Id;
                            msg = friend.GetFriendDisplayNameForAccount(accountId);
                        }
                    }

                    var action = granted ? "granted" : "denied";
                    msg += $" has {action} your request for {title}";
                    if (granted)
                        msg += ". You will be notified again when it's available";

                    action = granted ? "Granted" : "Denied";
                    DB.Notifications.Add(new Data.Models.Notification
                    {
                        GetRequestId = req.Id,
                        Message = msg,
                        NotificationType = granted ? NotificationTypes.NewMediaPending : NotificationTypes.NewMediaRejected,
                        ProfileId = s.ProfileId,
                        Timestamp = DateTime.UtcNow,
                        Title = $"Request {action}"
                    });

                }
            }

            await DB.SaveChangesAsync();
            notifyProfileIds.ForEach(p => FirebaseNotificationsManager.QueueProfileForNotifications(p));

            return Result.BuildSuccess();
        }


        private async Task<TitleRequestPermissions> CalculateTitleRequestPermissionsAsync()
        {

            if (UserProfile.IsMain)
            {
                var hasFriends = await DB.Friendships
                    .AsNoTracking()
                    .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                    .AnyAsync();

                return hasFriends ? TitleRequestPermissions.Enabled : TitleRequestPermissions.Disabled;
            }
            else
            {
                if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Enabled)
                {
                    var hasFriends = await DB.Friendships
                        .AsNoTracking()
                        .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                        .AnyAsync();

                    if (hasFriends)
                        return TitleRequestPermissions.Enabled;
                    else
                        return TitleRequestPermissions.RequiresAuthorization;
                }

                return UserProfile.TitleRequestPermission;
            }
        }


        private async Task<(RequestStatus Status, TitleRequestPermissions Permission)> CalculateTitleRequestStatusAsync(int id, TMDB_MediaTypes mediaType)
        {
            RequestStatus status = RequestStatus.NotRequested;
            TitleRequestPermissions permission = await CalculateTitleRequestPermissionsAsync();

            //Get request status
            var existingRequest = await DB.GetRequestSubscriptions
                .AsNoTracking()
                .Include(item => item.GetRequest)
                .Where(item => item.GetRequest.TMDB_Id == id)
                .Where(item => item.GetRequest.EntryType == mediaType)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();


            if (existingRequest == null)
                status = RequestStatus.NotRequested;
            else
                status = existingRequest.GetRequest.Status;

            return (status, permission);
        }


        private static void FillCredits(CreditsDTO credits, DetailedTMDB ret)
        {
            if (credits != null)
            {
                if (credits.CastMembers?.Count > 0)
                {
                    ret.Credits ??= new();
                    foreach (var castMember in credits.CastMembers.OrderBy(item => item.Order))
                        AddPersonToCredits(ret.Credits, castMember);
                }

                if (credits.CrewMembers != null)
                {
                    foreach (var director in credits.CrewMembers.Where(item => item.Job.ICEquals("Director")))
                    {
                        ret.Credits ??= new();
                        AddPersonToCredits(ret.Credits, director, CreditRoles.Director);
                    }

                    foreach (var producer in credits.CrewMembers.Where(item => item.Job.ICEquals("Producer")))
                    {
                        ret.Credits ??= new();
                        AddPersonToCredits(ret.Credits, producer, CreditRoles.Producer);
                    }

                    foreach (var executiveProducer in credits.CrewMembers.Where(item => item.Job.ICEquals("Executive Producer")))
                    {
                        ret.Credits ??= new();
                        AddPersonToCredits(ret.Credits, executiveProducer, CreditRoles.ExecutiveProducer);
                    }

                    foreach (string writerJob in new string[] { "Writer", "Screenplay" })
                        foreach (var writer in credits.CrewMembers.Where(item => item.Job.ICEquals(writerJob)))
                        {
                            ret.Credits ??= new();
                            AddPersonToCredits(ret.Credits, writer, CreditRoles.Writer);
                        }
                }
            }
        }


        private static void AddPersonToCredits(List<BasicPerson> credits, CastDTO castMember)
        {
            if (!credits.Any(item => item.TMDB_Id == castMember.Id && item.Role == CreditRoles.Cast))
                credits.Add(new BasicPerson
                {
                    TMDB_Id = castMember.Id,
                    AvatarUrl = castMember.FullImagePath,
                    Name = castMember.Name,
                    Initials = castMember.Name.GetInitials(),
                    Role = CreditRoles.Cast,
                    Order = credits.Count(item => item.Role == CreditRoles.Cast)
                });
        }


        private static void AddPersonToCredits(List<BasicPerson> credits, CrewDTO crewMember, CreditRoles role)
        {
            if (!credits.Any(item => item.TMDB_Id == crewMember.Id && item.Role == role))
                credits.Add(new BasicPerson
                {
                    TMDB_Id = crewMember.Id,
                    AvatarUrl = crewMember.FullImagePath,
                    Name = crewMember.Name,
                    Initials = crewMember.Name.GetInitials(),
                    Role = role,
                    Order = credits.Count(item => item.Role == role)
                });
        }



        class TmdbTitleDTO
        {
            public int Id { get; set; }
            public MediaTypes MediaType { get; set; }
        }

    }
}
