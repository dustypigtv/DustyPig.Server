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
    [ApiController]
    [ExceptionLogger(typeof(LibrariesController))]
    public class LibrariesController : _BaseProfileController
    {
        public LibrariesController(AppDbContext db) : base(db) { }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicLibrary>>> List()
        {
            //Libs owned by the account
            var ownedLibs = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .ToListAsync();

            var ret = ownedLibs
                .Select(item => item.ToBasicLibraryInfo())
                .ToList();


            //Libs shared with the account
            var ownedLibIds = ownedLibs.Select(item => item.Id).ToList();
            var sharedLibs = await DB.FriendLibraryShares
                .AsNoTracking()

                .Include(item => item.Friendship)
                .ThenInclude(item => item.Account1)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Friendship)
                .ThenInclude(item => item.Account2)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)

                .Where(item => item.Friendship.Account1Id == UserAccount.Id || item.Friendship.Account2Id == UserAccount.Id)
                .Where(item => item.Friendship.Accepted)
                .Where(item => !ownedLibIds.Contains(item.LibraryId))

                .ToListAsync();

            foreach (var share in sharedLibs)
                ret.Add(share.ToBasicLibraryInfo(UserAccount.Id));

            ret.Sort();
            return ret;
        }

        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<BasicLibrary>> GetBasic(int id)
        {
            //Libs owned by the account
            var lib = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (lib != null)
                return lib.ToBasicLibraryInfo();

            var sharedLibs = await DB.FriendLibraryShares
                .AsNoTracking()

                .Include(item => item.Friendship)
                .ThenInclude(item => item.Account1)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Friendship)
                .ThenInclude(item => item.Account2)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)

                .Where(item => item.Friendship.Account1Id == UserAccount.Id || item.Friendship.Account2Id == UserAccount.Id)
                .Where(item => item.Friendship.Accepted)

                .ToListAsync();

            var share = sharedLibs.FirstOrDefault(item => item.LibraryId == id);
            if (share != null)
                return share.ToBasicLibraryInfo(UserAccount.Id);

            return NotFound("Library not found");
        }

        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet("{id}")]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DetailedLibrary>> Details(int id)
        {
            //Try to get owned lib
            var lib = await DB.Libraries
                .AsNoTracking()

                .Include(item => item.ProfileLibraryShares)
                .ThenInclude(item => item.Profile)

                .Include(item => item.FriendLibraryShares)
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account1)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.FriendLibraryShares)
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account2)
                .ThenInclude(item => item.Profiles)

                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (lib == null)
            {
                //Try to get shared lib
                var share = await DB.FriendLibraryShares
                    .AsNoTracking()

                    .Include(item => item.Friendship)
                    .ThenInclude(item => item.Account1)
                    .ThenInclude(item => item.Profiles)

                    .Include(item => item.Friendship)
                    .ThenInclude(item => item.Account2)
                    .ThenInclude(item => item.Profiles)

                    .Include(item => item.Library)
                    .ThenInclude(item => item.ProfileLibraryShares)

                    .Where(item => item.LibraryId == id)
                    .Where(item => item.Friendship.Account1Id == UserAccount.Id || item.Friendship.Account2Id == UserAccount.Id)
                    .SingleOrDefaultAsync();

                if (share == null)
                    return NotFound();

                var ret = new DetailedLibrary
                {
                    Id = id,
                    IsTV = share.Library.IsTV,
                    Name = Utils.Coalesce(share.LibraryDisplayName, share.Library.Name),
                    Owner = share.Friendship.GetFriendDisplayNameForAccount(UserAccount.Id)
                };

                foreach (var pls in share.Library.ProfileLibraryShares)
                    if (UserAccount.Profiles.Select(item => item.Id).Contains(pls.ProfileId))
                        ret.Profiles.Add(pls.Profile.ToBasicProfileInfo());

                return ret;
            }
            else
            {
                //This acct owns the lib
                var ret = new DetailedLibrary
                {
                    Id = id,
                    IsTV = lib.IsTV,
                    Name = lib.Name
                };

                foreach (var share in lib.ProfileLibraryShares)
                    if (UserAccount.Profiles.Select(item => item.Id).Contains(share.ProfileId))
                        ret.Profiles.Add(share.Profile.ToBasicProfileInfo());

                foreach (var friendship in lib.FriendLibraryShares.Select(item => item.Friendship))
                    ret.SharedWith.Add(friendship.ToBasicFriendInfo(UserAccount.Id));

                return ret;
            }
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <returns>Id of newly created library</returns>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.Created)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public async Task<ActionResult<SimpleValue<int>>> Create(CreateLibrary info)
        {
            //Validate object
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }



            //Ensure unique
            var alreadyExists = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Name == info.Name)
                .AnyAsync();
            if (alreadyExists)
                return BadRequest("A library with the specified name already exists in this account");

            var lib = DB.Libraries.Add(new Library
            {
                AccountId = UserAccount.Id,
                IsTV = info.IsTV,
                Name = info.Name
            }).Entity;

            //When creating a library, auto-share with the main profile
            DB.ProfileLibraryShares.Add(new ProfileLibraryShare
            {
                Library = lib,
                ProfileId = UserProfile.Id
            });

            await DB.SaveChangesAsync();

            return CommonResponses.CreatedObject(new SimpleValue<int>(lib.Id));
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> Update(UpdateLibrary info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }



            var allLibs = await DB.Libraries
                .Where(item => item.AccountId == UserAccount.Id)
                .ToListAsync();

            var lib = allLibs.SingleOrDefault(item => item.Id == info.Id);
            if (lib == null)
                NotFound("Library not found");

            //Make sure the new name is unique
            if (lib.Name != info.Name)
            {
                var nameExists = allLibs
                    .Where(item => item.Id != lib.Id)
                    .Where(item => item.Name == info.Name)
                    .Any();

                if (nameExists)
                    return BadRequest("A library with the specified name already exists in this account");
            }

            lib.IsTV = info.IsTV;
            lib.Name = info.Name;

            await DB.SaveChangesAsync();
            return Ok();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Warning! This will delete all media in the lib, which will in turn delete all subscriptions, overrides and watch progress for the media, and remove all deleted media from watchlists and playlists</remarks>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public async Task<ActionResult> Delete(int id)
        {
            var lib = await DB.Libraries
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (lib != null)
            {
                DB.Libraries.Remove(lib);
                await DB.SaveChangesAsync();
            }

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
        public Task<ActionResult> LinkToProfile(ProfileLibraryLink lnk) => ProfileLibraryLinks.LinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public Task<ActionResult> UnLinkFromProfile(ProfileLibraryLink lnk) => ProfileLibraryLinks.UnLinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> ShareWithFriend(LibraryFriendLink lnk) => FriendLibraryLinkLogic.LinkLibraryAndFriend(UserAccount, lnk.FriendId, lnk.LibraryId);


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public Task<ActionResult> UnShareWithFriend(LibraryFriendLink lnk) => FriendLibraryLinkLogic.UnLinkLibraryAndFriend(UserAccount, lnk.FriendId, lnk.LibraryId);

    }
}
