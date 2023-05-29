using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
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
        public async Task<ResponseWrapper<List<BasicLibrary>>> List()
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
            return new ResponseWrapper<List<BasicLibrary>>(ret);
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet]
        [RequireMainProfile]
        public async Task<ResponseWrapper<List<DetailedLibrary>>> AdminList()
        {
            var ret = new List<DetailedLibrary>();
            
            var libs = await DB.Libraries
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
                .ToListAsync();

            foreach(var lib in libs)
            {
                var dl = new DetailedLibrary
                {
                    Id = lib.Id,
                    IsTV = lib.IsTV,
                    Name = lib.Name,
                };

                foreach (var share in lib.ProfileLibraryShares)
                    if (UserAccount.Profiles.Select(item => item.Id).Contains(share.ProfileId))
                        dl.Profiles.Add(share.Profile.ToBasicProfileInfo());

                foreach (var friendship in lib.FriendLibraryShares.Select(item => item.Friendship))
                    dl.SharedWith.Add(friendship.ToBasicFriendInfo(UserAccount.Id));

                ret.Add(dl);
            }

            ret.Sort();
            return new ResponseWrapper<List<DetailedLibrary>>(ret);
        }


        [HttpGet("{id}")]
        public async Task<ResponseWrapper<BasicLibrary>> GetBasic(int id)
        {
            //Libs owned by the account
            var lib = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (lib != null)
                return new ResponseWrapper<BasicLibrary>(lib.ToBasicLibraryInfo());

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
                return new ResponseWrapper<BasicLibrary>(share.ToBasicLibraryInfo(UserAccount.Id));

            return CommonResponses.NotFound<BasicLibrary>("Library");
        }

        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet("{id}")]
        [RequireMainProfile]
        public async Task<ResponseWrapper<DetailedLibrary>> Details(int id)
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
                    return CommonResponses.NotFound<DetailedLibrary>();

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

                return new ResponseWrapper<DetailedLibrary>(ret);
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

                return new ResponseWrapper<DetailedLibrary>(ret);
            }
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <returns>Id of newly created library</returns>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        public async Task<ResponseWrapper<SimpleValue<int>>> Create(CreateLibrary info)
        {
            //Validate object
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<SimpleValue<int>>(ex.ToString()); }



            //Ensure unique
            var alreadyExists = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Name == info.Name)
                .AnyAsync();
            if (alreadyExists)
                return new ResponseWrapper<SimpleValue<int>>("A library with the specified name already exists in this account");

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

            return new ResponseWrapper<SimpleValue<int>>(new SimpleValue<int>(lib.Id));
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        public async Task<ResponseWrapper> Update(UpdateLibrary info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }



            var allLibs = await DB.Libraries
                .Where(item => item.AccountId == UserAccount.Id)
                .ToListAsync();

            var lib = allLibs.SingleOrDefault(item => item.Id == info.Id);
            if (lib == null)
                CommonResponses.NotFound("Library");

            //Make sure the new name is unique
            if (lib.Name != info.Name)
            {
                var nameExists = allLibs
                    .Where(item => item.Id != lib.Id)
                    .Where(item => item.Name == info.Name)
                    .Any();

                if (nameExists)
                    return new ResponseWrapper("A library with the specified name already exists in this account");
            }

            lib.IsTV = info.IsTV;
            lib.Name = info.Name;

            await DB.SaveChangesAsync();
            return new ResponseWrapper();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Warning! This will delete all media in the lib, which will in turn delete all subscriptions, overrides and watch progress for the media, and remove all deleted media from watchlists and playlists</remarks>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        public async Task<ResponseWrapper> Delete(int id)
        {
            var lib = await DB.Libraries
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (lib != null)
            {
                var q =
                    from me in DB.MediaEntries.Where(item => item.LibraryId == id)
                    join pi in DB.PlaylistItems on me.Id equals pi.MediaEntryId
                    select pi.PlaylistId;

                var playlistIds = await q.Distinct().ToListAsync();

                DB.Libraries.Remove(lib);
                await DB.SaveChangesAsync();

                await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);
            }

            return new ResponseWrapper();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        public Task<ResponseWrapper> LinkToProfile(ProfileLibraryLink lnk) => ProfileLibraryLinks.LinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        public Task<ResponseWrapper> UnLinkFromProfile(ProfileLibraryLink lnk) => ProfileLibraryLinks.UnLinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        public Task<ResponseWrapper> ShareWithFriend(LibraryFriendLink lnk) => FriendLibraryLinkLogic.LinkLibraryAndFriend(UserAccount, lnk.FriendId, lnk.LibraryId);


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        public Task<ResponseWrapper> UnShareWithFriend(LibraryFriendLink lnk) => FriendLibraryLinkLogic.UnLinkLibraryAndFriend(UserAccount, lnk.FriendId, lnk.LibraryId);

    }
}
