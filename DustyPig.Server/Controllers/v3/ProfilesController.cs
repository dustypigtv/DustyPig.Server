﻿using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
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
        /// Requires account
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
        /// Requires profile
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
                Initials = profile.Name.GetInitials(),
                HasPin = profile.PinNumber != null,
                TitleRequestPermissions = profile.TitleRequestPermission
            };

            //Get all owned libraries the profile has access to
            List<Library> libs;
            if (profile.IsMain)
            {
                libs = await DB.Libraries
                    .AsNoTracking()
                    .Where(item => item.AccountId == profile.AccountId)
                    .ToListAsync();
            }
            else
            {
                libs = await DB.ProfileLibraryShares
                    .AsNoTracking()
                    .Include(item => item.Library)
                    .Where(item => item.ProfileId == id)
                    .Select(item => item.Library)
                    .Where(item => item.AccountId == UserAccount.Id)
                    .ToListAsync();
            }

            if (libs.Count > 0)
            {
                ret.AvailableLibraries ??= new();
                foreach (var lib in libs)
                    ret.AvailableLibraries.Add(lib.ToBasicLibraryInfo());
            }


            //Libs shared with account
            var ownedLibraryIds = libs.Select(item => item.Id).ToList();
            List<FriendLibraryShare> shares;
            if (profile.IsMain)
            {
                shares = await DB.FriendLibraryShares
                    .AsNoTracking()

                    .Include(item => item.Library)
                    .ThenInclude(item => item.FriendLibraryShares)
                    .ThenInclude(item => item.Friendship)
                    .ThenInclude(item => item.Account1)

                    .Include(item => item.Library)
                    .ThenInclude(item => item.FriendLibraryShares)
                    .ThenInclude(item => item.Friendship)
                    .ThenInclude(item => item.Account2)

                    .Include(item => item.Library)
                    .ThenInclude(item => item.FriendLibraryShares)
                    .ThenInclude(item => item.Library)

                    .Where(item => item.Friendship.Account1Id == UserAccount.Id || item.Friendship.Account2Id == UserAccount.Id)
                    .Where(item => !ownedLibraryIds.Contains(item.LibraryId))

                    .Select(item => item.Library)
                    .SelectMany(item => item.FriendLibraryShares)

                    .Where(item => item.Friendship.Accepted)

                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                shares = await DB.ProfileLibraryShares
                    .AsNoTracking()

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
            }


            if (shares.Count > 0)
            {
                ret.AvailableLibraries ??= new();
                foreach (var share in shares)
                    ret.AvailableLibraries.Add(share.ToBasicLibraryInfo(UserAccount.Id));
            }

            if (ret.AvailableLibraries != null)
                ret.AvailableLibraries.Sort();

            return ret;
        }


        /// <summary>
        /// Requires profile
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
            profile.AvatarUrl = LogicUtils.EnsureProfilePic(info.AvatarUrl);


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


            info.Name = LogicUtils.EnsureNotNull(info.Name);
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

                if (info.LibraryIds != null && info.LibraryIds.Count > 0)
                {
                    var profLibShares = DB.ProfileLibraryShares
                        .AsNoTracking()
                        .Where(item => item.ProfileId == info.Id)
                        .ToList();

                    foreach (var pls in profLibShares)
                        if (!info.LibraryIds.Contains(pls.LibraryId))
                            DB.ProfileLibraryShares.Remove(pls);

                    var libs = await DB.GetLibraryIdsAccessableByAccount(UserAccount.Id);
                    foreach (var libId in info.LibraryIds)
                        if (libs.Contains(libId))
                            DB.ProfileLibraryShares.Add(new ProfileLibraryShare
                            {
                                Profile = profile,
                                LibraryId = libId
                            });
                }
            }


            DB.Profiles.Update(profile);
            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }


        /// <summary>
        /// Requires profile
        /// </summary>
        /// <returns>Url to the new avatar</returns>
        /// <remarks>Only the main profile on the account or the profile owner can set the avatar. 
        /// The body of this request should be a jpeg file, no more than 1 MB in size. 
        /// This method uses a the entire body of the request as a binary file</remarks>
        [HttpPut("{id}")]
        [ProhibitTestUser]
        [RequestSizeLimit(5242880)] //Set to 5MB
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<string>))]
        public async Task<Result<string>> SetProfileAvatarBinary(int id, [FromBody] byte[] data)
        {
            if (id != UserProfile.Id)
                if (UserProfile.IsMain)
                    if (!UserAccount.Profiles.Any(item => item.Id == id))
                        return CommonResponses.ValueNotFound(nameof(id));

            var profile = UserAccount.Profiles.Single(item => item.Id == id);

            using var ms = new MemoryStream();
            ms.Write(data, 0, data.Length);


            bool jpg = IsJpeg(ms);
            if (!(jpg || IsPng(ms)))
                return Result<string>.BuildError("File does not appear to be a jpeg or png file");

            var ext = jpg ? "jpg" : "png";
            string fileName = $"{id}.{Guid.NewGuid().ToString("N")}.{ext}";
            string keyPath = $"{Constants.DEFAULT_PROFILE_PATH}/{fileName}";
            string urlPath = $"{Constants.DEFAULT_PROFILE_URL_ROOT}{fileName}";

            await S3.UploadAvatarAsync(ms, keyPath, default);

            //Swap
            await ArtworkUpdater.SetNeedsDeletionAsync(profile.AvatarUrl);
            profile.AvatarUrl = urlPath;
            DB.Profiles.Update(profile);

            await DB.SaveChangesAsync();

            return Result<string>.BuildSuccess(urlPath);
        }


        /// <summary>
        /// Requires profile
        /// </summary>
        /// <returns>Url to the new avatar</returns>
        /// <remarks>Only the main profile on the account or the profile owner can set the avatar. 
        /// The body of this request should be a jpeg file, no more than 1 MB in size. 
        /// This method uses the multipart upload</remarks>
        [HttpPut("{id}")]
        [ProhibitTestUser]
        [RequestSizeLimit(5242980)] //Set to 5MB, with an extra 100 kb leeway for multipart encoding
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
            {
                if (file.ContentType != "image/jpeg" && file.ContentType != "image/png")
                    return Result<string>.BuildError("Content-Type does not match image/jpeg or image/png");
            }
            var stream = file.OpenReadStream();
            
            bool jpg = IsJpeg(stream);
            if (!(jpg || IsPng(stream)))
                return Result<string>.BuildError("File does not appear to be a jpeg or png file");

            var ext = jpg ? "jpg" : "png";
            string fileName = $"{id}.{Guid.NewGuid().ToString("N")}.{ext}";
            string keyPath = $"{Constants.DEFAULT_PROFILE_PATH}/{fileName}";
            string urlPath = $"{Constants.DEFAULT_PROFILE_URL_ROOT}{fileName}";

            await S3.UploadAvatarAsync(stream, keyPath, default);

            //Swap
            await ArtworkUpdater.SetNeedsDeletionAsync(profile.AvatarUrl);
            profile.AvatarUrl = urlPath;
            DB.Profiles.Update(profile);

            await DB.SaveChangesAsync();

            return Result<string>.BuildSuccess(urlPath);
        }



        /// <summary>
        /// Requires main profile
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
                AvatarUrl = LogicUtils.EnsureProfilePic(info.AvatarUrl),
                MaxMovieRating = info.MaxMovieRating,
                MaxTVRating = info.MaxTVRating,
                Locked = info.Locked,
                Name = info.Name,
                PinNumber = info.Pin,
                TitleRequestPermission = info.TitleRequestPermissions
            };

            if (info.LibraryIds != null && info.LibraryIds.Count > 0)
            {
                var libs = await DB.GetLibraryIdsAccessableByAccount(UserAccount.Id);

                foreach (var libId in info.LibraryIds)
                    if (libs.Contains(libId))
                        DB.ProfileLibraryShares.Add(new ProfileLibraryShare
                        {
                            Profile = profile,
                            LibraryId = libId
                        });
            }

            DB.Profiles.Add(profile);
            await DB.SaveChangesAsync();

            return profile.Id;
        }


        /// <summary>
        /// Requires main profile
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

            var artworkToDelete = await DB.Playlists
                .AsNoTracking()
                .Where(item => item.ProfileId == id)
                .Select(item => item.ArtworkUrl)
                .ToListAsync();

            artworkToDelete.Add(profile.AvatarUrl);
            
            DB.Profiles.Remove(profile);

            await DB.SaveChangesAsync();

            await ArtworkUpdater.SetNeedsDeletionAsync(artworkToDelete);

            return Result.BuildSuccess();
        }








        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> LinkToLibrary(ProfileLibraryLink lnk) => ProfileLibraryLinks.LinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);


        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> UnLinkFromLibrary(ProfileLibraryLink lnk) => ProfileLibraryLinks.UnLinkLibraryAndProfile(UserAccount, lnk.ProfileId, lnk.LibraryId);


        static bool IsJpeg(Stream stream)
        {
            const int MIN_JPG_LENGTH = 107;

            if (stream.Length < MIN_JPG_LENGTH)
                return false;

            var pos = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            
            var ret = 
                stream.ReadByte() == 0xFF && 
                stream.ReadByte() == 0xD8;
            
            stream.Seek(pos, SeekOrigin.Begin);
            return ret;
        }


        static bool IsPng(Stream stream)
        {
            const int MIN_PNG_LENGTH = 67;

            if (stream.Length < MIN_PNG_LENGTH)
                return false;

            var pos = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            
            var ret = 
                stream.ReadByte() == 0x89 &&
                stream.ReadByte() == 0x50 &&
                stream.ReadByte() == 0x4E &&
                stream.ReadByte() == 0x47 &&
                stream.ReadByte() == 0x0D &&
                stream.ReadByte() == 0x0A &&
                stream.ReadByte() == 0x1A &&
                stream.ReadByte() == 0x0A;
            
            stream.Seek(pos, SeekOrigin.Begin);
            return ret;
        }

    }
}
