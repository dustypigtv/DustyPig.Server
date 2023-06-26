﻿using DustyPig.API.v3;
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
        public ResponseWrapper<List<BasicProfile>> List()
        {
            var ret = UserAccount.Profiles
                .Select(item => item.ToBasicProfileInfo())
                .ToList();
            ret.Sort();

            return new ResponseWrapper<List<BasicProfile>>(ret);
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
        public async Task<ResponseWrapper<DetailedProfile>> Details(int id)
        {
            bool allowed = UserProfile.IsMain && UserAccount.Profiles.Select(item => item.Id).Contains(id);
            if (!allowed)
                allowed = id == UserProfile.Id;
            if (!allowed)
                return CommonResponses.Forbid<DetailedProfile>();


            var profile = UserAccount.Profiles.SingleOrDefault(item => item.Id == id);
            if (profile == null)
                return CommonResponses.NotFound<DetailedProfile>();

            var ret = new DetailedProfile
            {
                AllowedRatings = profile.MaxMovieRating.ToRatings() | profile.MaxTVRating.ToRatings(),
                AvatarUrl = profile.AvatarUrl,
                Id = id,
                IsMain = profile.IsMain,
                Locked = profile.Locked,
                Name = profile.Name,
                Pin = profile.PinNumber,
                TitleRequestPermissions = profile.TitleRequestPermission
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

            return new ResponseWrapper<DetailedProfile>(ret);
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Only the main profile on the account or the profile owner can update a profile.
        /// If the profile owner is not the main profile, then they can only update: Name, PinNumber and AvatarUrl. 
        /// If the profile being updated is the main profile, it cannot be locked</remarks>
        [HttpPost]
        [ProhibitTestUser]
        public async Task<ResponseWrapper> Update(UpdateProfile info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }



            bool allowed = UserProfile.IsMain && UserAccount.Profiles.Select(item => item.Id).Contains(info.Id);
            if (!allowed)
                allowed = info.Id == UserProfile.Id;
            if (!allowed)
                return CommonResponses.Forbid();

            info.Name = Utils.EnsureNotNull(info.Name);
            if (string.IsNullOrWhiteSpace(info.Name))
                return new ResponseWrapper("Name is missing");

            bool nameExists = UserAccount.Profiles
                .Where(item => item.Id != info.Id)
                .Where(item => item.Name.ICEquals(info.Name))
                .Any();

            if (nameExists)
                return new ResponseWrapper("There is already another profile with the specified name on this account");


            var profile = UserAccount.Profiles.Single(item => item.Id == info.Id);

            profile.Name = info.Name.Trim();
            profile.PinNumber = info.Pin;
            profile.AvatarUrl = info.AvatarUrl;

            if (UserProfile.IsMain)
            {
                //Update restricted fields
                profile.MaxTVRating = info.AllowedRatings.ToTVRatings();
                profile.MaxMovieRating = info.AllowedRatings.ToMovieRatings();
                profile.Locked = !profile.IsMain && info.Locked;
                profile.TitleRequestPermission = info.TitleRequestPermissions;
            }

            DB.Entry(profile).State = EntityState.Modified;
            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        public async Task<ResponseWrapper<SimpleValue<int>>> Create(CreateProfile info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<SimpleValue<int>>(ex.ToString()); }



            bool nameExists = UserAccount.Profiles
                .Where(item => item.Name.ICEquals(info.Name))
                .Any();

            if (nameExists)
                return new ResponseWrapper<SimpleValue<int>>("There is already another profile with the specified name on this account");


            var profile = new Profile
            {
                AccountId = UserAccount.Id,
                MaxMovieRating = info.AllowedRatings.ToMovieRatings(),
                MaxTVRating = info.AllowedRatings.ToTVRatings(),
                AvatarUrl = info.AvatarUrl,
                Locked = info.Locked,
                Name = info.Name,
                PinNumber = info.Pin,
                TitleRequestPermission = info.TitleRequestPermissions
            };

            DB.Profiles.Add(profile);
            await DB.SaveChangesAsync();

            return new ResponseWrapper<SimpleValue<int>>(new SimpleValue<int>(profile.Id));
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Warning! This will remove all subscriptions, overrides, notifications, watchlists and playlists for this profile</remarks>
        [HttpDelete("{id}")]
        [ProhibitTestUser]
        [RequireMainProfile]
        public async Task<ResponseWrapper> Delete(int id)
        {
            var profile = UserAccount.Profiles.SingleOrDefault(item => item.Id == id);
            if (profile == null)
                return CommonResponses.Ok();

            if (profile.IsMain)
                return new ResponseWrapper("Cannot delete main profile");

            var playlistArtworkUrls = await DB.Playlists
                .AsNoTracking()
                .Where(item => item.ProfileId == id)
                .Select(item => item.ArtworkUrl)
                .ToListAsync();

            foreach (string url in playlistArtworkUrls)
                if(!string.IsNullOrWhiteSpace(url))
                    DB.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = url });

            DB.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = Profile.CalculateS3Url(id) });
            DB.Entry(profile).State = EntityState.Deleted;
            
            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }





        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        public Task<ResponseWrapper> LinkToLibrary(ProfileLibraryLink lnk) => ProfileLibraryLinks.LinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        public Task<ResponseWrapper> UnLinkFromLibrary(ProfileLibraryLink lnk) => ProfileLibraryLinks.UnLinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);

    }
}
