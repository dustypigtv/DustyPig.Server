using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Firebase.Auth;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    [ApiExplorerSettings(GroupName = "Account")]
    [ExceptionLogger(typeof(AccountController))]
    public class AccountController : _BaseController
    {
        private readonly FirebaseAuthClient _client;
        private readonly JWTProvider _jwtProvider;

        public AccountController(AppDbContext db, FirebaseAuthClient client, JWTProvider jwtProvider) : base(db)
        {
            _client = client;
            _jwtProvider = jwtProvider;
        }


        /// <summary>
        /// Requires no authorization
        /// </summary>
        /// <remarks>This will create the Firebase account and send a confirmation email</remarks>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Create(CreateAccount info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            try
            {
                //Check if they already exist
                var existingUser = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(info.Email);
                return "Account already exists";
            }
            catch { }


            var signupResponse = await _client.SignUpWithEmailPasswordAsync(info.Email, info.Password);
            if (signupResponse.Success)
            {
                var account = await DB.Accounts
                    .AsNoTracking()
                    .Include(item => item.Profiles)
                    .Where(item => item.FirebaseId == signupResponse.Data.LocalId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    account = DB.Accounts.Add(new Account { FirebaseId = signupResponse.Data.LocalId }).Entity;
                    var profile = DB.Profiles.Add(new Profile
                    {
                        Account = account,
                        MaxMovieRating = MovieRatings.Unrated,
                        MaxTVRating = TVRatings.NotRated,
                        AvatarUrl = LogicUtils.EnsureProfilePic(info.AvatarUrl),
                        IsMain = true,
                        Name = LogicUtils.Coalesce(info.DisplayName, signupResponse.Data.Email[..signupResponse.Data.Email.IndexOf("@")]),
                        TitleRequestPermission = TitleRequestPermissions.Enabled
                    }).Entity;

                    await DB.SaveChangesAsync();
                }

                return Result.BuildSuccess();
            }
            else
            {
                return signupResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.PasswordSignup);
            }
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>WARNING: This will permanently delete the account and ALL data. This is not recoverable!</remarks>
        [HttpDelete]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Delete()
        {
            var (account, profile) = await User.VerifyAsync();

            if (account.Id == TestAccount.AccountId)
                return CommonResponses.ProhibitTestUser();

            if (profile == null)
                return CommonResponses.RequireMainProfile();

            if (!profile.IsMain)
                return CommonResponses.RequireMainProfile();

            //Images to cleanup from Wasabi
            var profileIds = account.Profiles.Select(item => item.Id).ToList();
            var profileAvatars = account.Profiles.Select(item => item.AvatarUrl).ToList();
            var playlistArtworkUrls = new List<string>();
            try
            {
                playlistArtworkUrls = await DB.Playlists
                    .AsNoTracking()
                    .Where(item => profileIds.Contains(item.ProfileId))
                    .Where(item => item.ArtworkUrl != Constants.DEFAULT_PLAYLIST_IMAGE)
                    .Select(item => item.ArtworkUrl)
                    .ToListAsync();
            }
            catch { }


            await FirebaseAuth.DefaultInstance.DeleteUserAsync(account.FirebaseId);

            DB.Accounts.Remove(account);
            await DB.SaveChangesAsync();

            //Try to clean up, but ok if it fails
            try
            {
                foreach (var avatar in profileAvatars)
                    DB.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = avatar });

                foreach (var playlistArtworkUrl in playlistArtworkUrls.Where(item => !string.IsNullOrWhiteSpace(item)))
                    DB.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = playlistArtworkUrl });

                await DB.SaveChangesAsync();
            }
            catch { }

            return Result.BuildSuccess();
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Change the password for the account</remarks>
        /// <param name="newPassword"># This _MUST_ be a JSON encoded string</param>
        [HttpPost]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<ActionResult<Result>> ChangePassword(StringValue newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword.Value))
                return CommonResponses.RequiredValueMissing(nameof(newPassword.Value));

            var (account, profile) = await User.VerifyAsync();
            if (profile == null)
                return Unauthorized();

            if (profile.Locked)
                return CommonResponses.ProfileIsLocked();

            if (!profile.IsMain)
                return CommonResponses.RequireMainProfile();

            var fbUser = await FirebaseAuth.DefaultInstance.GetUserAsync(account.FirebaseId);
            await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
            {
                Disabled = false,
                DisplayName = fbUser.DisplayName,
                Email = fbUser.Email,
                EmailVerified = fbUser.EmailVerified,
                Password = newPassword.Value,
                PhoneNumber = fbUser.PhoneNumber,
                PhotoUrl = fbUser.PhotoUrl,
                Uid = fbUser.Uid
            });


            return Result.BuildSuccess();
        }


    }
}
