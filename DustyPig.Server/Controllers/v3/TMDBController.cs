using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
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

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ProhibitTestUser]
    [ExceptionLogger(typeof(TMDBController))]
    public class TMDBController : _BaseProfileController
    {
        private readonly TMDBClient _client;

        public TMDBController(AppDbContext db, TMDBClient client) : base(db)
        {
            _client = client;
        }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedTMDB>))]
        public async Task<Result<DetailedTMDB>> GetMovie(int id)
        {
            if (!(UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled))
                return CommonResponses.Forbid();

            var movie = await _client.GetMovieAsync(id);
            if (!movie.Success)
                return movie.Error.GetErrorResponse().StatusMessage;

            var ret = new DetailedTMDB
            {
                ArtworkUrl = TMDB.Utils.GetFullPosterPath(movie.Data.PosterPath, true),
                BackdropUrl = TMDB.Utils.GetFullBackdropPath(movie.Data.BackdropPath, true),
                Description = movie.Data.Overview,
                MediaType = TMDB_MediaTypes.Movie,
                Rated = HostedServices.TMDB_Updater.TryMapMovieRatings(movie.Data.Releases),
                Title = movie.Data.Title,
                TMDB_ID = movie.Data.Id
            };

            if (movie.Data.ReleaseDate.HasValue)
            {
                ret.Year = movie.Data.ReleaseDate.Value.Year;
            }
            else
            {
                if (movie.Data.Releases != null && movie.Data.Releases.Countries != null)
                {
                    movie.Data.Releases.Countries.Sort();
                    foreach (var release in movie.Data.Releases.Countries.Where(item => item.Name == "US"))
                    {
                        ret.Year = release.ReleaseDate.Year;
                        break;
                    }
                }
            }

            if (movie.Data.Genres != null)
                ret.Genres = string.Join(",", movie.Data.Genres.Select(item => item.Name)).ToGenres();

            FillCredits(movie.Data.Credits, ret);


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

            var series = await _client.GetSeriesAsync(id);
            if (!series.Success)
                return series.Error.GetErrorResponse().StatusMessage;

            // Response
            var ret = new DetailedTMDB
            {
                ArtworkUrl = TMDB.Utils.GetFullPosterPath(series.Data.PosterPath, true),
                BackdropUrl = TMDB.Utils.GetFullBackdropPath(series.Data.BackdropPath, true),
                Description = series.Data.Overview,
                MediaType = TMDB_MediaTypes.Series,
                Rated = HostedServices.TMDB_Updater.TryMapTVRatings(series.Data.ContentRatings),
                Title = series.Data.Title,
                TMDB_ID = series.Data.Id
            };


            if (series.Data.FirstAirDate.HasValue)
                ret.Year = series.Data.FirstAirDate.Value.Year;

            if (series.Data.Genres != null)
                ret.Genres = string.Join(",", series.Data.Genres.Select(item => item.Name)).ToGenres();

            FillCredits(series.Data.Credits, ret);


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
                title = response.Data.Title;
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


        private static void FillCredits(Credits credits, DetailedTMDB ret)
        {
            if (credits != null)
            {
                if (credits.Cast != null && credits.Cast.Count > 0)
                {
                    ret.Credits ??= new();
                    foreach (var castMember in credits.Cast.OrderBy(item => item.Order))
                        AddPersonToCredits(ret.Credits, castMember);
                }

                if (credits.Crew != null)
                {
                    foreach (var director in credits.Crew.Where(item => item.Job.ICEquals("Director")))
                    {
                        ret.Credits ??= new();
                        AddPersonToCredits(ret.Credits, director, CreditRoles.Director);
                    }

                    foreach (var producer in credits.Crew.Where(item => item.Job.ICEquals("Producer")))
                    {
                        ret.Credits ??= new();
                        AddPersonToCredits(ret.Credits, producer, CreditRoles.Producer);
                    }

                    foreach (var executiveProducer in credits.Crew.Where(item => item.Job.ICEquals("Executive Producer")))
                    {
                        ret.Credits ??= new();
                        AddPersonToCredits(ret.Credits, executiveProducer, CreditRoles.ExecutiveProducer);
                    }

                    foreach(string writerJob in new string[] { "Writer", "Screenplay" })
                        foreach (var writer in credits.Crew.Where(item => item.Job.ICEquals(writerJob)))
                        {
                            ret.Credits ??= new();
                            AddPersonToCredits(ret.Credits, writer, CreditRoles.Writer);
                        }
                }
            }
        }


        private static void AddPersonToCredits(List<Person> credits, Cast castMember)
        {
            if (!credits.Any(item => item.TMDB_Id == castMember.Id && item.Role == CreditRoles.Cast))
                credits.Add(new Person
                {
                    TMDB_Id = castMember.Id,
                    AvatarUrl = TMDB.Utils.GetFullBackdropPath(castMember.ProfilePath, false),
                    Name = castMember.Name,
                    Initials = castMember.Name.GetInitials(),
                    Role = CreditRoles.Cast,
                    Order = credits.Count(item => item.Role == CreditRoles.Cast)
                });
        }

        private static void AddPersonToCredits(List<Person> credits, Crew crewMember, CreditRoles role)
        {
            if (!credits.Any(item => item.TMDB_Id == crewMember.Id && item.Role == role))
                credits.Add(new Person
                {
                    TMDB_Id = crewMember.Id,
                    AvatarUrl = TMDB.Utils.GetFullBackdropPath(crewMember.ProfilePath, false),
                    Name = crewMember.Name,
                    Initials = crewMember.Name.GetInitials(),
                    Role = role,
                    Order = credits.Count(item => item.Role == role)
                });
        }
    }
}
