using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    /// <summary>
    /// The account needs to be able to list available profiles to sign into, so separate this from the main ProfilesController that requires 
    /// the profile to already be signed in
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "Profiles")]
    [Route("api/v{version:apiVersion}/profiles/[action]")]
    [Produces("application/json")]
    [ExceptionLogger(typeof(ProfilesListController))]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
    public class ProfilesListController : _BaseAccountController
    {
        public ProfilesListController(AppDbContext db) : base(db) { }

        /// <summary>
        /// Level 1
        /// </summary>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public ActionResult<List<BasicProfile>> List()
        {
            var ret = UserAccount.Profiles
                .Select(item => item.ToBasicProfileInfo())
                .ToList();
            ret.Sort();

            return ret;
        }
    }



    [ApiController]
    [ApiExplorerSettings(GroupName = "Profiles")]
    [ExceptionLogger(typeof(ProfilesController))]
    public class ProfilesController : _BaseProfileController
    {
        public ProfilesController(AppDbContext db) : base(db) { }

        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Only the profile owner or the main profile for the account may view this information</remarks>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DetailedProfile>> Details(int id)
        {
            bool allowed = UserProfile.IsMain && UserAccount.Profiles.Select(item => item.Id).Contains(id);
            if (!allowed)
                allowed = id == UserProfile.Id;
            if (!allowed)
                return CommonResponses.Forbid;
                

            var profile = UserAccount.Profiles.SingleOrDefault(item => item.Id == id);
            if (profile == null)
                return NotFound();

            var ret = new DetailedProfile
            {
                AllowedRatings = profile.AllowedRatings,
                AvatarUrl = profile.AvatarUrl,
                Id = id,
                IsMain = profile.IsMain,
                Locked = profile.Locked,
                Name = profile.Name,
                NotificationMethods = profile.NotificationMethods,
                Pin = profile.PinNumber,
                TitleRequestPermissions = profile.TitleRequestPermission,
                WeeklySummary = profile.WeeklySummary
            };

            //Get all owned libraries the profile has access to
            var libs = await DB.ProfileLibraryShares
                .AsNoTracking()
                .Include(item => item.Library)
                .Where(item => item.ProfileId == id)
                .Select(item => item.Library)
                .Where(item => item.AccountId == UserAccount.Id)
                .ToListAsync();

            foreach (var lib in libs)
                ret.AvailableLibraries.Add(lib.ToBasicLibraryInfo());



            //Libs shared with account
            var ownedLibraryIds = libs.Select(item => item.Id).ToList();
            var shares = await DB.ProfileLibraryShares

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
                .ThenInclude(item => item.FriendLibraryShares)
                .ThenInclude(item => item.Library)

                .Where(item => item.ProfileId == id)
                .Where(item => !ownedLibraryIds.Contains(item.LibraryId))

                .Select(item => item.Library)
                .SelectMany(item => item.FriendLibraryShares)

                .Where(item => item.Friendship.Accepted)

                .Distinct()
                .ToListAsync();




            foreach (var share in shares)
                ret.AvailableLibraries.Add(share.ToBasicLibraryInfo(UserAccount.Id));

            return ret;
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Only the main profile on the account or the profile owner can update a profile.
        /// If the profile owner is not the main profile, then they can only update: Name, PinNumber and AvatarUrl. 
        /// If the profile being updated is the main profile, it cannot be locked</remarks>
        [HttpPost]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public async Task<ActionResult> Update(UpdateProfile info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }



            bool allowed = UserProfile.IsMain && UserAccount.Profiles.Select(item => item.Id).Contains(info.Id);
            if (!allowed)
                allowed = info.Id == UserProfile.Id;
            if (!allowed)
                return CommonResponses.Forbid;

            info.Name = Utils.EnsureNotNull(info.Name);
            if (string.IsNullOrWhiteSpace(info.Name))
                return BadRequest("Name is missing");

            bool nameExists = UserAccount.Profiles
                .Where(item => item.Id != info.Id)
                .Where(item => item.Name.ICEquals(info.Name))
                .Any();

            if (nameExists)
                return BadRequest("There is already another profile with the specified name on this account");


            var profile = UserAccount.Profiles.Single(item => item.Id == info.Id);

            profile.Name = info.Name.Trim();
            profile.PinNumber = info.Pin;
            profile.AvatarUrl = info.AvatarUrl;

            if (UserProfile.IsMain)
            {
                //Update restricted fields
                profile.AllowedRatings = info.AllowedRatings;
                profile.Locked = !profile.IsMain && info.Locked;
                profile.TitleRequestPermission = info.TitleRequestPermissions;
            }

            DB.Entry(profile).State = EntityState.Modified;
            await DB.SaveChangesAsync();

            return Ok();
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.Created)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public async Task<ActionResult<SimpleValue<int>>> Create(CreateProfile info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }



            bool nameExists = UserAccount.Profiles
                .Where(item => item.Name.ICEquals(info.Name))
                .Any();

            if (nameExists)
                return BadRequest("There is already another profile with the specified name on this account");


            var profile = new Profile
            {
                AccountId = UserAccount.Id,
                AllowedRatings = info.AllowedRatings,
                AvatarUrl = info.AvatarUrl,
                Locked = info.Locked,
                Name = info.Name,
                NotificationMethods = info.NotificationMethods,
                PinNumber = info.Pin,
                TitleRequestPermission = info.TitleRequestPermissions,
                WeeklySummary = info.WeeklySummary
            };

            DB.Profiles.Add(profile);
            await DB.SaveChangesAsync();

            return CommonResponses.CreatedObject(new SimpleValue<int>(profile.Id));
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Warning! This will remove all subscriptions, overrides, notifications, watchlists and playlists for this profile</remarks>
        [HttpDelete("{id}")]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public async Task<ActionResult> Delete(int id)
        {
            var profile = UserAccount.Profiles.SingleOrDefault(item => item.Id == id);
            if (profile == null)
                return Ok();

            if (profile.IsMain)
                return BadRequest("Cannot delete main profile");

            DB.Entry(profile).State = EntityState.Deleted;
            await DB.SaveChangesAsync();

            return Ok();
        }





        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> LinkToLibrary(ProfileLibraryLink lnk) => ProfileLibraryLinks.LinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public Task<ActionResult> UnLinkFromLibrary(ProfileLibraryLink lnk) => ProfileLibraryLinks.UnLinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);

    }
}
