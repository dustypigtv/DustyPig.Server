using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Data;
using DustyPig.Server.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using APINotification = DustyPig.API.v3.Models.Notification;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(NotificationsController))]
    public class NotificationsController : _BaseProfileController
    {
        private const int LIST_SIZE = 100;

        public NotificationsController(AppDbContext db) : base(db) { }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{start}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<APINotification>>> List(int start)
        {

            var notifications = await DB.Notifications
                .AsNoTracking()
                .Where(item => item.ProfileId == UserProfile.Id)
                .OrderBy(item => item.Timestamp)
                .Skip(start)
                .Take(LIST_SIZE)
                .ToListAsync();

            return notifications.Select(item => new APINotification
            {
                Id = item.Id,
                Message = item.Message,
                Seen = item.Seen,
                Timestamp = item.Timestamp,
                Title = item.Title,
                DeepLink = DeepLinks.Create(item)
            }).ToList();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Marks a notification as seen</remarks>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            var dbNotification = await DB.Notifications
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (dbNotification == null)
                return NotFound("Notification not found");

            //Don't throw an error, just return
            if (dbNotification.Seen)
                return Ok();

            dbNotification.Seen = true;
            dbNotification.Timestamp = DateTime.UtcNow;
            await DB.SaveChangesAsync();

            return Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult> Delete(int id)
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
            return Ok();
        }
    }
}
