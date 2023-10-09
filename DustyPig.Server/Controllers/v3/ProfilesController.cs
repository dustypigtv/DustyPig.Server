using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        /// If the profile owner is not the main profile, then they can only update: Name, PinNumber and Avatar. 
        /// If the profile being updated is the main profile, it cannot be locked</remarks>
        [HttpPost]
        [ProhibitTestUser]
        public async Task<ResponseWrapper> Update(UpdateProfile info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }

            if (info.Id != UserProfile.Id)
                if (UserProfile.IsMain)
                    if (!UserAccount.Profiles.Any(item => item.Id == info.Id))
                        return CommonResponses.Forbid();


            var profile = UserAccount.Profiles.Single(item => item.Id == info.Id);
            profile.PinNumber = info.Pin;


            info.Name = Utils.EnsureNotNull(info.Name);
            if (!string.IsNullOrWhiteSpace(info.Name))
            {
                bool nameExists = UserAccount.Profiles
                    .Where(item => item.Id != info.Id)
                    .Where(item => item.Name.ICEquals(info.Name))
                    .Any();

                if (nameExists)
                    return new ResponseWrapper("There is already another profile with the specified name on this account");

                profile.Name = info.Name;
            }

            if (!string.IsNullOrWhiteSpace(info.AvatarUrl))
                profile.AvatarUrl = info.AvatarUrl;


            if (UserProfile.IsMain)
            {
                //Update restricted fields
                if (info.AllowedRatings != API.v3.MPAA.Ratings.None)
                {
                    profile.MaxTVRating = info.AllowedRatings.ToTVRatings();
                    profile.MaxMovieRating = info.AllowedRatings.ToMovieRatings();
                }
                profile.Locked = !profile.IsMain && info.Locked;
                profile.TitleRequestPermission = info.TitleRequestPermissions;
            }

            DB.Profiles.Update(profile);
            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Only the main profile on the account or the profile owner can set the avatar. 
        /// The body of this request should be a jpeg file, no more than 1 MB in size. 
        /// This method uses a the entire body of the request as a binary file</remarks>
        [HttpPut("{id}")]
        [ProhibitTestUser]
        [RequestSizeLimit(1048576)] //Set to 1 MB
        public async Task<ResponseWrapper> SetProfileAvatarBinary(int id)
        {
            if (id != UserProfile.Id)
                if (UserProfile.IsMain)
                    if (!UserAccount.Profiles.Any(item => item.Id == id))
                        return CommonResponses.Forbid();

            var profile = UserAccount.Profiles.Single(item => item.Id == id);

            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            
            if(!IsJpeg(ms))
                return CommonResponses.BadRequest("File does not appear to be a jpeg file");

            await S3.UploadFileAsync(ms, Profile.CalculateS3Key(profile.Id), default);
            profile.AvatarUrl = Profile.CalculateS3Url(profile.Id);
            DB.Profiles.Update(profile);
            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Only the main profile on the account or the profile owner can set the avatar. 
        /// The body of this request should be a jpeg file, no more than 1 MB in size. 
        /// This method uses the multipart upload</remarks>
        [HttpPut("{id}")]
        [ProhibitTestUser]
        [RequestSizeLimit(1048676)] //Set to 1 MB, with an extra 100 kb leeway for multipart encoding
        public async Task<ResponseWrapper> SetProfileAvatarMultipart(int id)
        {
            if (id != UserProfile.Id)
                if (UserProfile.IsMain)
                    if (!UserAccount.Profiles.Any(item => item.Id == id))
                        return CommonResponses.Forbid();

            var profile = UserAccount.Profiles.Single(item => item.Id == id);

            if (!Request.Form.Files.Any())
                return CommonResponses.BadRequest("Missing File");

            if (Request.Form.Files.Count > 1)
                return CommonResponses.BadRequest("Only 1 file allowed");

            var file = Request.Form.Files[0];
            if(!string.IsNullOrWhiteSpace(file.ContentType))
                if (file.ContentType != "image/jpeg")
                    return CommonResponses.BadRequest("Content-Type does not match image/jpeg");

            var stream = file.OpenReadStream();
            if (!IsJpeg(stream))
                return CommonResponses.BadRequest("File does not appear to be a jpeg file");

            await S3.UploadFileAsync(stream, Profile.CalculateS3Key(profile.Id), default);
            profile.AvatarUrl = Profile.CalculateS3Url(profile.Id);
            DB.Profiles.Update(profile);
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
                AvatarUrl = Utils.EnsureProfilePic(info.AvatarUrl),
                MaxMovieRating = info.AllowedRatings.ToMovieRatings(),
                MaxTVRating = info.AllowedRatings.ToTVRatings(),
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
                if (!string.IsNullOrWhiteSpace(url))
                    DB.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = url });

            DB.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = Profile.CalculateS3Url(id) });
            DB.Profiles.Remove(profile);

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


        static bool IsJpeg(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ReadByte() == 0xFF && stream.ReadByte() == 0xD8;
        }


    }
}
