using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
using DustyPig.Server.Services;
using DustyPig.TMDB;
using DustyPig.TMDB.Models;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DustyPig.Server.Services.TMDBClient;
using static DustyPig.TMDB.Utils;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ProhibitTestUser]
    [ExceptionLogger(typeof(TMDBController))]
    public class TMDBController : _BaseProfileController
    {
        private readonly TMDBClient _client = new()
        {
            RetryCount = 1,
            RetryDelay = 100
        };

        public TMDBController(AppDbContext db) : base(db) { }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedTMDB>))]
        public async Task<Result<DetailedTMDB>> GetMovie(int id)
        {
            if (!(UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled))
                return CommonResponses.Forbid();

            var movieResponse = await _client.GetMovieAsync(id);
            if (!movieResponse.Success)
                return movieResponse.Error.Message;

            var movie = movieResponse.Data;

            var ret = new DetailedTMDB
            {
                ArtworkUrl = GetFullImageUrl(movie.PosterPath, "w185"),
                BackdropUrl = GetFullImageUrl(movie.BackdropPath, "w300"),
                Description = movie.Overview,
                MediaType = TMDB_MediaTypes.Movie,
                Rated = TryMapMovieRatings(movie.ReleaseDates),
                Title = movie.Title,
                TMDB_ID = movie.Id
            };

            ret.Year = TryGetMovieDate(movie)?.Year ?? 0;

            if (movie.Genres != null)
                ret.Genres = string.Join(",", movie.Genres.Select(item => item.Name)).ToGenres();

            FillCredits(GetCommonCredits(movie.Credits), ret);


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
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedTMDB>))]
        public async Task<Result<DetailedTMDB>> GetSeries(int id)
        {
            if (!(UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled))
                return CommonResponses.Forbid();

            var seriesResponse = await _client.GetSeriesAsync(id);
            if (!seriesResponse.Success)
                return seriesResponse.Error.Message;
            var series = seriesResponse.Data;
            
            // Response
            var ret = new DetailedTMDB
            {
                ArtworkUrl = GetFullImageUrl(series.PosterPath, "w185"),
                BackdropUrl = GetFullImageUrl(series.BackdropPath, "w300"),
                Description = series.Overview,
                MediaType = TMDB_MediaTypes.Series,
                Rated = TryMapTVRatings(series.ContentRatings),
                Title = series.Name,
                TMDB_ID = series.Id
            };


            if (series.FirstAirDate.HasValue)
                ret.Year = series.FirstAirDate.Value.Year;

            if (series.Genres != null)
                ret.Genres = string.Join(",", series.Genres.Select(item => item.Name)).ToGenres();

            FillCredits(GetCommonCredits(series.Credits), ret);


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
        /// Level 2
        /// </summary>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<TitleRequestPermissions>))]
        public async Task<Result<TitleRequestPermissions>> GetRequestTitlePermission()
        {
            var ret = await CalculateTitleRequestPermissionsAsync();
            return ret;
        }


        /// <summary>
        /// Level 2
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
                var response = await _client.GetMovieAsync(data.TMDB_Id);
                if (!response.Success)
                    return CommonResponses.ValueNotFound(nameof(data.TMDB_Id));
                title = response.Data.Title;
            }
            else
            {
                var response = await _client.GetSeriesAsync(data.TMDB_Id);
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

            return Result.BuildSuccess();
        }


        /// <summary>
        /// Level 2
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
                .Include(item => item.GetRequest)
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
            TitleRequestPermissions permission = TitleRequestPermissions.Disabled;

            //Get request status
            var existingRequest = await DB.GetRequestSubscriptions
                .AsNoTracking()
                .Include(item => item.GetRequest)
                .Where(item => item.GetRequest.TMDB_Id == id)
                .Where(item => item.GetRequest.EntryType == mediaType)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();


            if (existingRequest == null)
            {
                status = RequestStatus.NotRequested;
                permission = await CalculateTitleRequestPermissionsAsync();
            }
            else
            {
                status = existingRequest.GetRequest.Status;
                permission = TitleRequestPermissions.Disabled;
            }

            return (status, permission);
        }


        private static void FillCredits(CreditsDTO credits, DetailedTMDB ret)
        {
            if (credits != null)
            {
                if (credits.CastMembers.Count > 0)
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

                    foreach(string writerJob in new string[] { "Writer", "Screenplay" })
                        foreach (var writer in credits.CrewMembers.Where(item => item.Job.ICEquals(writerJob)))
                        {
                            ret.Credits ??= new();
                            AddPersonToCredits(ret.Credits, writer, CreditRoles.Writer);
                        }
                }
            }
        }


        private static void AddPersonToCredits(List<Person> credits, CastDTO castMember)
        {
            if (!credits.Any(item => item.TMDB_Id == castMember.Id && item.Role == CreditRoles.Cast))
                credits.Add(new Person
                {
                    TMDB_Id = castMember.Id,
                    AvatarUrl = castMember.FullImagePath,
                    Name = castMember.Name,
                    Initials = castMember.Name.GetInitials(),
                    Role = CreditRoles.Cast,
                    Order = credits.Count(item => item.Role == CreditRoles.Cast)
                });
        }

        private static void AddPersonToCredits(List<Person> credits, CrewDTO crewMember, CreditRoles role)
        {
            if (!credits.Any(item => item.TMDB_Id == crewMember.Id && item.Role == role))
                credits.Add(new Person
                {
                    TMDB_Id = crewMember.Id,
                    AvatarUrl = crewMember.FullImagePath,
                    Name = crewMember.Name,
                    Initials = crewMember.Name.GetInitials(),
                    Role = role,
                    Order = credits.Count(item => item.Role == role)
                });
        }
    }
}
