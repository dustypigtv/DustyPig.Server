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
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    /// <summary>
    /// The account needs to be able to list available profiles to sign into, so separate this from the main ProfilesController that requires 
    /// the profile to already be signed in
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "Profiles")]
    [Route("api/v{version:apiVersion}/Profiles/[action]")]
    [Produces("application/json")]
    [ExceptionLogger(typeof(ProfilesListController))]
    public class ProfilesListController : _BaseAccountController
    {
        public ProfilesListController(AppDbContext db) : base(db) { }

        /// <summary>
        /// Level 1
        /// </summary>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicProfile>>))]
        public Result<List<BasicProfile>> List()
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
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedProfile>))]
        public async Task<Result<DetailedProfile>> Details(int id)
        {
            bool allowed = UserProfile.IsMain && UserAccount.Profiles.Select(item => item.Id).Contains(id);
            if (!allowed)
                allowed = id == UserProfile.Id;
            if (!allowed)
                return CommonResponses.Forbid();


            var profile = UserAccount.Profiles.SingleOrDefault(item => item.Id == id);
            if (profile == null)
                return CommonResponses.ValueNotFound(nameof(id));

            var ret = new DetailedProfile
            {
                AvatarUrl = profile.AvatarUrl,
                Id = id,
                IsMain = profile.IsMain,
                Locked = profile.Locked,
                MaxMovieRating = profile.MaxMovieRating,
                MaxTVRating = profile.MaxTVRating,
                Name = profile.Name,
                HasPin = profile.PinNumber != null,
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

            if (libs.Count > 0)
            {
                ret.AvailableLibraries ??= new();
                foreach (var lib in libs)
                    ret.AvailableLibraries.Add(lib.ToBasicLibraryInfo());
            }


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



            if (shares.Count > 0)
            {
                ret.AvailableLibraries ??= new();
                foreach (var share in shares)
                    ret.AvailableLibraries.Add(share.ToBasicLibraryInfo(UserAccount.Id));
            }

            return ret;
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Only the main profile on the account or the profile owner can update a profile.
        /// If the profile owner is not the main profile, then they can only update: Name, PinNumber and Avatar. 
        /// If the profile being updated is the main profile, it cannot be locked</remarks>
        [HttpPost]
        [ProhibitTestUser]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Update(UpdateProfile info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            if (info.Id != UserProfile.Id)
                if (UserProfile.IsMain)
                    if (!UserAccount.Profiles.Any(item => item.Id == info.Id))
                        return CommonResponses.ValueNotFound(nameof(info.Id));


            var profile = UserAccount.Profiles.Single(item => item.Id == info.Id);
            profile.AvatarUrl = Utils.EnsureProfilePic(info.AvatarUrl);


            if (info.Pin == null)
            {
                //Only set to null if client specifically wants to delete the pin number
                if (info.ClearPin)
                    profile.PinNumber = null;
            }
            else
            {
                //Set to value supplied by client
                profile.PinNumber = info.Pin;
            }


            info.Name = Utils.EnsureNotNull(info.Name);
            bool nameExists = UserAccount.Profiles
                .Where(item => item.Id != info.Id)
                .Where(item => item.Name.ICEquals(info.Name))
                .Any();

            if (nameExists)
                return "There is already another profile with the specified name on this account";

            profile.Name = info.Name;



            if (UserProfile.IsMain && UserProfile.Id != info.Id)
            {
                //Update restricted fields
                profile.MaxTVRating = info.MaxTVRating;
                profile.MaxMovieRating = info.MaxMovieRating;
                profile.Locked = info.Locked;
                profile.TitleRequestPermission = info.TitleRequestPermissions;
            }

            DB.Profiles.Update(profile);
            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <returns>Url to the new avatar</returns>
        /// <remarks>Only the main profile on the account or the profile owner can set the avatar. 
        /// The body of this request should be a jpeg file, no more than 1 MB in size. 
        /// This method uses a the entire body of the request as a binary file</remarks>
        [HttpPut("{id}")]
        [ProhibitTestUser]
        [RequestSizeLimit(1048576)] //Set to 1 MB
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<string>))]
        public async Task<Result<string>> SetProfileAvatarBinary(int id)
        {
            if (id != UserProfile.Id)
                if (UserProfile.IsMain)
                    if (!UserAccount.Profiles.Any(item => item.Id == id))
                        return CommonResponses.ValueNotFound(nameof(id));

            var profile = UserAccount.Profiles.Single(item => item.Id == id);

            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);

            if (!IsJpeg(ms))
                return Result<string>.BuildError("File does not appear to be a jpeg file");

            string fileName = $"{id}.{Guid.NewGuid().ToString("N")}.jpg";
            string keyPath = $"{Constants.DEFAULT_PROFILE_PATH}/{fileName}";
            string urlPath = $"{Constants.DEFAULT_PROFILE_URL_ROOT}{fileName}";

            await S3.UploadAvatarAsync(ms, keyPath, default);

            //Swap
            DB.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = profile.AvatarUrl });
            profile.AvatarUrl = urlPath;
            DB.Profiles.Update(profile);

            await DB.SaveChangesAsync();

            return Result<string>.BuildSuccess(urlPath);
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <returns>Url to the new avatar</returns>
        /// <remarks>Only the main profile on the account or the profile owner can set the avatar. 
        /// The body of this request should be a jpeg file, no more than 1 MB in size. 
        /// This method uses the multipart upload</remarks>
        [HttpPut("{id}")]
        [ProhibitTestUser]
        [RequestSizeLimit(1048676)] //Set to 1 MB, with an extra 100 kb leeway for multipart encoding
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<string>))]
        public async Task<Result<string>> SetProfileAvatarMultipart(int id)
        {
            if (id != UserProfile.Id)
                if (UserProfile.IsMain)
                    if (!UserAccount.Profiles.Any(item => item.Id == id))
                        return CommonResponses.ValueNotFound(nameof(id));

            var profile = UserAccount.Profiles.Single(item => item.Id == id);

            if (!Request.Form.Files.Any())
                return Result<string>.BuildError("Missing File");

            if (Request.Form.Files.Count > 1)
                return Result<string>.BuildError("Only 1 file allowed");

            var file = Request.Form.Files[0];
            if (!string.IsNullOrWhiteSpace(file.ContentType))
                if (file.ContentType != "image/jpeg")
                    return Result<string>.BuildError("Content-Type does not match image/jpeg");

            var stream = file.OpenReadStream();
            if (!IsJpeg(stream))
                return Result<string>.BuildError("File does not appear to be a jpeg file");


            string fileName = $"{id}.{Guid.NewGuid().ToString("N")}.jpg";
            string keyPath = $"{Constants.DEFAULT_PROFILE_PATH}/{fileName}";
            string urlPath = $"{Constants.DEFAULT_PROFILE_URL_ROOT}{fileName}";

            await S3.UploadAvatarAsync(stream, keyPath, default);

            //Swap
            DB.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = profile.AvatarUrl });
            profile.AvatarUrl = urlPath;
            DB.Profiles.Update(profile);

            await DB.SaveChangesAsync();

            return Result<string>.BuildSuccess(urlPath);
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<int>))]
        public async Task<Result<int>> Create(CreateProfile info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return ex; }



            bool nameExists = UserAccount.Profiles
                .Where(item => item.Name.ICEquals(info.Name))
                .Any();

            if (nameExists)
                return "There is already another profile with the specified name on this account";

            var profile = new Profile
            {
                AccountId = UserAccount.Id,
                AvatarUrl = Utils.EnsureProfilePic(info.AvatarUrl),
                MaxMovieRating = info.MaxMovieRating,
                MaxTVRating = info.MaxTVRating,
                Locked = info.Locked,
                Name = info.Name,
                PinNumber = info.Pin,
                TitleRequestPermission = info.TitleRequestPermissions
            };

            DB.Profiles.Add(profile);
            await DB.SaveChangesAsync();

            return profile.Id;
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Warning! This will remove all subscriptions, overrides, notifications, watchlists and playlists for this profile</remarks>
        [HttpDelete("{id}")]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Delete(int id)
        {
            var profile = UserAccount.Profiles.SingleOrDefault(item => item.Id == id);
            if (profile == null)
                return Result.BuildSuccess();

            if (profile.IsMain)
                return "Cannot delete main profile";

            var playlistArtworkUrls = await DB.Playlists
                .AsNoTracking()
                .Where(item => item.ProfileId == id)
                .Select(item => item.ArtworkUrl)
                .ToListAsync();

            foreach (string url in playlistArtworkUrls)
                if (!string.IsNullOrWhiteSpace(url))
                    DB.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = url });

            DB.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = profile.AvatarUrl });
            DB.Profiles.Remove(profile);

            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }








        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> LinkToLibrary(ProfileLibraryLink lnk) => ProfileLibraryLinks.LinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> UnLinkFromLibrary(ProfileLibraryLink lnk) => ProfileLibraryLinks.UnLinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);


        static bool IsJpeg(Stream stream)
        {
            var pos = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            var ret = stream.ReadByte() == 0xFF && stream.ReadByte() == 0xD8;
            stream.Seek(pos, SeekOrigin.Begin);
            return ret;
        }


    }
}
