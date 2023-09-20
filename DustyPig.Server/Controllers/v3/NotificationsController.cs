using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private const int LIST_SIZE = 100;

        public NotificationsController(AppDbContext db) : base(db) { }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{start}")]
        public async Task<ResponseWrapper<List<APINotification>>> List(int start)
        {

            var notifications = await DB.Notifications
                .AsNoTracking()
                .Include(item => item.MediaEntry)
                .Where(item => item.ProfileId == UserProfile.Id)
                .OrderBy(item => item.Timestamp)
                .Skip(start)
                .Take(LIST_SIZE)
                .ToListAsync();

            return new ResponseWrapper<List<APINotification>>(notifications.Select(item => new APINotification
            {
                Id = item.Id,
                Message = item.Message,
                Seen = item.Seen,
                Timestamp = item.Timestamp,
                Title = item.Title,
                DeepLink = DeepLinks.Create(item)
            }).ToList());
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Marks a notification as seen</remarks>
        [HttpGet("{id}")]
        public async Task<ResponseWrapper> MarkAsRead(int id)
        {
            var dbNotification = await DB.Notifications
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (dbNotification == null)
                return CommonResponses.NotFound("Notification");

            //Don't throw an error, just return
            if (dbNotification.Seen)
                return CommonResponses.Ok();

            dbNotification.Seen = true;
            dbNotification.Timestamp = DateTime.UtcNow;
            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ResponseWrapper> Delete(int id)
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
            return CommonResponses.Ok();
        }
    }
}
