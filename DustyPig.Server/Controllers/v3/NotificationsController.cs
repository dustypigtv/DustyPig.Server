using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APINotification = DustyPig.API.v3.Models.Notification;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(NotificationsController))]
    public class NotificationsController : _BaseProfileController
    {
        private const int LIST_SIZE = 25;

        public NotificationsController(AppDbContext db) : base(db) { }

        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Lists the next 25 notifications based on start position</remarks>
        [HttpGet("{start}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<APINotification>>))]
        public async Task<Result<List<APINotification>>> List(int start)
        {
            if (start < 0)
                return CommonResponses.InvalidValue(nameof(start));

            var notifications = await DB.Notifications
                .AsNoTracking()
                .Include(item => item.MediaEntry)
                .Include(item => item.GetRequest)
                .Where(item => item.ProfileId == UserProfile.Id)
                .OrderBy(item => item.Timestamp)
                .Skip(start)
                .Take(LIST_SIZE)
                .ToListAsync();

            return notifications.Select(item =>
            {
                return new APINotification
                {
                    Id = item.Id,
                    Message = item.Message,
                    Seen = item.Seen,
                    Timestamp = item.Timestamp,
                    Title = item.Title,
                    FriendshipId = item.FriendshipId,
                    NotificationType = item.NotificationType,
                    ProfileId = UserProfile.Id,
                    MediaId = item.MediaEntryId,
                    MediaType = item.NotificationType switch
                    {
                        NotificationTypes.NewMediaAvailable => MediaTypes.Series,
                        NotificationTypes.NewMediaFulfilled => item.MediaEntry.EntryType,
                        NotificationTypes.NewMediaPending => item.GetRequest.EntryType == TMDB_MediaTypes.Series ? MediaTypes.Series : MediaTypes.Movie,
                        NotificationTypes.NewMediaRejected => item.GetRequest.EntryType == TMDB_MediaTypes.Series ? MediaTypes.Series : MediaTypes.Movie,
                        NotificationTypes.NewMediaRequested => item.GetRequest.EntryType == TMDB_MediaTypes.Series ? MediaTypes.Series : MediaTypes.Movie,
                        NotificationTypes.OverrideMediaGranted => item.MediaEntry.EntryType,
                        NotificationTypes.OverrideMediaRejected => item.MediaEntry.EntryType,
                        NotificationTypes.OverrideMediaRequested => item.MediaEntry.EntryType,
                        _ => null
                    }
                };
            }).ToList();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Marks a notification as seen</remarks>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> MarkAsRead(int id)
        {
            var dbNotification = await DB.Notifications
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (dbNotification == null)
                return CommonResponses.ValueNotFound(nameof(id));

            //Don't throw an error, just return
            if (dbNotification.Seen)
                return Result.BuildSuccess();

            dbNotification.Seen = true;
            dbNotification.Timestamp = DateTime.UtcNow;
            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Delete(int id)
        {
            var dbNotification = await DB.Notifications
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (dbNotification != null)
            {
                DB.Notifications.Remove(dbNotification);
                await DB.SaveChangesAsync();
            }
            return Result.BuildSuccess();
        }
    }
}
