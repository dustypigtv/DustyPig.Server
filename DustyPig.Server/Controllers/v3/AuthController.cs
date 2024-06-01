using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Firebase.Auth;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
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
    [ExceptionLogger(typeof(AuthController))]
    public class AuthController : _BaseController
    {
        private readonly FirebaseAuthClient _firebaseClient;
        private readonly JWTProvider _jwtProvider;

        public AuthController(AppDbContext db, FirebaseAuthClient firebaseClient, JWTProvider jwtProvider) : base(db)
        {
            _firebaseClient = firebaseClient;
            _jwtProvider = jwtProvider;
        }

        



        /// <summary>
        /// Requires no authorization
        /// </summary>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<LoginResponse>))]
        public async Task<Result<LoginResponse>> PasswordLogin(PasswordCredentials credentials)
        {
            //Validate
            try { credentials.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            Account account;

            if (credentials.Email.ToLower().Trim() == TestAccount.Email)
            {
                if (credentials.Password != TestAccount.Password)
                    return "Invalid credentials";

                account = await GetOrCreateAccountAsync(TestAccount.FirebaseId, null, TestAccount.Email, null);
            }
            else
            {
                var signInResponse = await _firebaseClient.SignInWithEmailPasswordAsync(credentials.Email, credentials.Password);
                if (!signInResponse.Success)
                    return signInResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.PasswordSignin);

                var dataResponse = await _firebaseClient.GetUserDataAsync(signInResponse.Data.IdToken);
                if (!dataResponse.Success)
                    return dataResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.GetUserData);

                var user = dataResponse.Data.Users.First();
                //if (!user.EmailVerified)
                //    return "You must verify your email address before you can sign in";

                account = await GetOrCreateAccountAsync(user.LocalId, user.DisplayName, user.Email, user.PhotoUrl);
            }

            if (account.Profiles.Count == 1)
            {
                int? fcmId = string.IsNullOrWhiteSpace(credentials.FCMToken) ?
                    null :
                    await EnsureFCMTokenAssociatedWithProfile(account.Profiles.First(), credentials.FCMToken);

                return new LoginResponse
                {
                    LoginType = LoginType.MainProfile,
                    ProfileId = account.Profiles.First().Id,
                    Token = await _jwtProvider.CreateTokenAsync(account.Id, account.Profiles.First().Id, fcmId, credentials.DeviceId)
                };
            }
            else
            {
                return new LoginResponse
                {
                    LoginType = LoginType.Account,
                    Token = await _jwtProvider.CreateTokenAsync(account.Id, null, null, null)
                };
            }
        }


        ///// <summary>
        ///// Requires no authorization
        ///// </summary>
        ///// <remarks>Sends a new account verification email</remarks>
        //[HttpPost]
        //[SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        //public async Task<Result> SendVerificationEmail(PasswordCredentials credentials)
        //{
        //    //Validate
        //    try { credentials.Validate(); }
        //    catch (ModelValidationException ex) { return ex; }

        //    if (credentials.Email == TestAccount.Email)
        //        return CommonResponses.ProhibitTestUser();

        //    var signInResponse = await _firebaseClient.SignInWithEmailPasswordAsync(credentials.Email, credentials.Password);
        //    if (!signInResponse.Success)
        //        return signInResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.PasswordSignin);

        //    var sendVerificationEmailResponse = await _firebaseClient.SendEmailVerificationAsync(signInResponse.Data.IdToken);
        //    if (!sendVerificationEmailResponse.Success)
        //        return sendVerificationEmailResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.SendVerificationEmail);

        //    return Result.BuildSuccess();
        //}


        /// <summary>
        /// Requires no authorization
        /// </summary>
        /// <remarks>Sends a password reset email</remarks>
        /// <param name="email"># This _MUST_ be a JSON encoded string</param>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> SendPasswordResetEmail([FromBody] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return CommonResponses.RequiredValueMissing(nameof(email));

            if (email.ToLower().Trim() == TestAccount.Email)
                return CommonResponses.ProhibitTestUser();


            var ret = await _firebaseClient.SendPasswordResetEmailAsync(email);
            if (!ret.Success)
                return ret.FirebaseError().TranslateFirebaseError(FirebaseMethods.PasswordReset);

            return Result.BuildSuccess();
        }






        /// <summary>
        /// Requires no authorization
        /// </summary>
        /// <remarks>Returns a code that can be used to login a device with no keyboard (streaming devices, smart tvs, etc)</remarks>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<string>))]
        public async Task<Result<string>> GenerateDeviceLoginCode()
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

            var rand = new Random();

            while (true)
            {
                var activationCode = new ActivationCode
                {
                    Code = new string(Enumerable.Repeat(chars, 5).Select(s => s[rand.Next(s.Length)]).ToArray())
                };

                try
                {
                    DB.ActivationCodes.Add(activationCode);
                    await DB.SaveChangesAsync();
                    return Result<string>.BuildSuccess(activationCode.Code);
                }
                catch
                {
                    DB.Entry(activationCode).State = EntityState.Detached;
                }
            }
        }


        /// <summary>
        /// Requires no authorization
        /// </summary>
        /// <remarks>Check the generated code to see if it has been authorized, and if so returns an account level bearer token. Once this returns true, the generated code will be deleted</remarks>
        /// <param name="code"># This _MUST_ be a JSON encoded string</param>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DeviceCodeStatus>))]
        public async Task<Result<DeviceCodeStatus>> VerifyDeviceLoginCode([FromBody] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return CommonResponses.RequiredValueMissing(nameof(code));

            code = code.Trim();
            if (code.Length != Constants.DEVICE_ACTIVATION_CODE_LENGTH)
                return CommonResponses.InvalidValue(nameof(code));

            var rec = await DB.ActivationCodes
                .AsNoTracking()
                .Include(item => item.Account)
                .ThenInclude(item => item.Profiles)
                .Where(item => item.Code == code)
                .SingleOrDefaultAsync();

            if (rec == null)
                return CommonResponses.ValueNotFound(nameof(code));


            var ret = new DeviceCodeStatus { Activated = rec.AccountId != null };
            if (ret.Activated)
            {
                DB.ActivationCodes.Remove(rec);
                await DB.SaveChangesAsync();

                if (rec.Account.Profiles.Count == 1)
                {
                    ret.Token = await new JWTProvider(DB).CreateTokenAsync(rec.AccountId.Value, rec.Account.Profiles[0].Id, null, null);
                    ret.LoginType = LoginType.MainProfile;
                }
                else
                {
                    ret.Token = await new JWTProvider(DB).CreateTokenAsync(rec.AccountId.Value, null, null, null);
                    ret.LoginType = LoginType.Account;
                }
            }

            return ret;
        }




        /// <summary>
        /// Requires profile
        /// </summary>
        /// <remarks>Associates the generated code with logged in user account, allowing a subsequent call to VerifyLoginCode by the device to get an account token</remarks>
        /// <param name="code"># This _MUST_ be a JSON encoded string</param>
        [HttpPost]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<ActionResult<Result>> LoginDeviceWithCode([FromBody] string code)
        {
            var (account, profile) = await User.VerifyAsync();
            if (profile == null)
                return Unauthorized();

            if (profile.Locked)
                return CommonResponses.ProfileIsLocked();


            if (string.IsNullOrWhiteSpace(code))
                return CommonResponses.InvalidValue(nameof(code));

            code = code.Trim();
            if (code.Length != Constants.DEVICE_ACTIVATION_CODE_LENGTH)
                return CommonResponses.InvalidValue(nameof(code));

            var rec = await DB.ActivationCodes
                .Where(item => item.Code == code)
                .Where(item => item.AccountId == null)
                .SingleOrDefaultAsync();

            if (rec == null)
                return CommonResponses.ValueNotFound(nameof(code));

            rec.AccountId = account.Id;
            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }





        /// <summary>
        /// Requires account
        /// </summary>
        /// <remarks>Returns a profile level bearer token</remarks>
        [HttpPost]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<LoginResponse>))]
        public async Task<ActionResult<Result<LoginResponse>>> ProfileLogin(ProfileCredentials credentials)
        {
            //Validate
            try { credentials.Validate(); }
            catch (ModelValidationException ex) { return Result<LoginResponse>.BuildError(ex); }

            var (account, _) = await User.VerifyAsync();

            if (account == null)
                return Unauthorized();

            using var db = new AppDbContext();
            var profile = await db.Profiles
                .AsNoTracking()
                .Include(item => item.FCMTokens)
                .Where(item => item.AccountId == account.Id)
                .Where(item => item.Id == credentials.Id)
                .FirstOrDefaultAsync();

            if (profile == null)
                return (Result<LoginResponse>)CommonResponses.ValueNotFound(nameof(credentials.Id));

            if (profile.PinNumber != null && profile.PinNumber >= 1000 && profile.PinNumber <= 9999)
            {
                if (credentials.Pin == null || credentials.Pin != profile.PinNumber)
                    return (Result<LoginResponse>)Result.BuildError("Invalid pin");
            }

            if (!profile.IsMain && profile.Locked)
                return (Result<LoginResponse>)CommonResponses.ProfileIsLocked();

            int? fcmId = string.IsNullOrWhiteSpace(credentials.FCMToken) ?
                null :
                await EnsureFCMTokenAssociatedWithProfile(profile, credentials.FCMToken);

            return Result<LoginResponse>.BuildSuccess(new LoginResponse
            {
                ProfileId = profile.Id,
                LoginType = profile.IsMain ? LoginType.MainProfile : LoginType.SubProfile,
                Token = await _jwtProvider.CreateTokenAsync(account.Id, profile.Id, fcmId, credentials.DeviceId)
            });
        }

        /// <summary>
        /// Requires account
        /// </summary>
        /// <remarks>If presenting an account level token, will sign out of the account. If presenting a profile level token,will sign out of the profile</remarks>
        [HttpGet]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        public async Task<Result> Signout()
        {
            var (account, profile) = await User.VerifyAsync();

            if (account == null)
                return Result.BuildSuccess();

            var acctToken = account.AccountTokens.FirstOrDefault(item => item.Id == User.GetAuthTokenId());
            if (acctToken == null)
                return Result.BuildSuccess();

            DB.AccountTokens.Remove(acctToken);

            if (profile != null)
            {
                var fcmTokenId = User.GetFCMTokenId();
                if (fcmTokenId != null)
                {
                    var dbFCMToken = profile.FCMTokens.FirstOrDefault(item => item.Id == fcmTokenId);
                    if (dbFCMToken != null)
                        DB.FCMTokens.Remove(dbFCMToken);
                }
            }

            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Sign all users out of all devices</remarks>
        [HttpGet]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult<Result>> SignoutEverywhere()
        {
            var acctId = User.GetAccountId();
            var authTokenId = User.GetAuthTokenId();
            var profId = User.GetProfileId();
            var fcmTokenId = User.GetFCMTokenId();

            if (acctId == null || authTokenId == null)
                return Unauthorized();

            var account = await DB.Accounts
                .AsNoTracking()
                .Where(item => item.Id == acctId)
                .Include(item => item.Profiles)
                .ThenInclude(item => item.FCMTokens)
                .Include(item => item.AccountTokens)
                .FirstOrDefaultAsync();

            if (account == null)
                return Unauthorized();

            var profile = account.Profiles
                .Where(item => item.Id == profId)
                .FirstOrDefault();

            if (profile == null)
                return Unauthorized();

            if (!profile.IsMain)
                return CommonResponses.RequireMainProfile();

            if (profile.Id == TestAccount.ProfileId)
                return CommonResponses.ProhibitTestUser();

            DB.AccountTokens.RemoveRange(account.AccountTokens);
            DB.FCMTokens.RemoveRange(account.Profiles.SelectMany(item => item.FCMTokens));

            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }



        


        /// <summary>
        /// Requires account
        /// </summary>
        /// <returns>Updates the device token for Firebase Cloud Messaging, and returns a new JWT</returns>
        /// <param name="newFCMToken"># This _MUST_ be a JSON encoded string</param>
        [HttpPost]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<string>))]
        public async Task<ActionResult<Result<string>>> UpdateFCMToken([FromBody] string newFCMToken)
        {
            var (account, profile) = await User.VerifyAsync();

            if (account == null || profile == null)
                return Unauthorized();

            string newJWT;

            if (string.IsNullOrWhiteSpace(newFCMToken))
            {
                await DeleteCurrentFCMToken(profile);
                newJWT = await _jwtProvider.CreateTokenAsync(account.Id, account.Profiles.First().Id, null, null);
            }
            else
            {
                int newId = await EnsureFCMTokenAssociatedWithProfile(profile, newFCMToken);
                newJWT = await _jwtProvider.CreateTokenAsync(account.Id, account.Profiles.First().Id, newId, null);
            }

            return Result<string>.BuildSuccess(newJWT);
        }


        private async Task<int> EnsureFCMTokenAssociatedWithProfile(Profile profile, string newFCM)
        {
            if (string.IsNullOrWhiteSpace(newFCM))
                throw new ArgumentNullException(nameof(newFCM));

            int? currentFCM = User.GetFCMTokenId();


            //Check if the token value is in the db
            var dbNewToken = await DB.FCMTokens
                .AsNoTracking()
                .Where(item => item.Token == newFCM)
                .FirstOrDefaultAsync();

            if (dbNewToken == null)
            {
                //If profile still has the old token, delete it
                if (currentFCM != null)
                {
                    var dbOldToken = profile.FCMTokens.FirstOrDefault(item => item.Id == currentFCM);
                    if (dbOldToken != null)
                    {
                        DB.FCMTokens.Remove(dbOldToken);
                        await DB.SaveChangesAsync();
                    }
                }

                //New token
                dbNewToken = DB.FCMTokens.Add(new FCMToken
                {
                    ProfileId = profile.Id,
                    Token = newFCM,
                    LastSeen = DateTime.UtcNow
                }).Entity;
                dbNewToken.ComputeHash();
                await DB.SaveChangesAsync();

                return dbNewToken.Id;
            }
            else
            {
                FCMToken dbOldToken =
                    currentFCM == null ?
                    null :
                    profile.FCMTokens.FirstOrDefault(item => item.Id == currentFCM);

                if (dbOldToken != null)
                {
                    if (dbOldToken.Token == newFCM)

                        //Same token, all good
                        return dbOldToken.Id;

                    else

                        //Delete the old token
                        DB.FCMTokens.Remove(dbOldToken);
                }

                //Associate with this profile
                dbNewToken.ProfileId = profile.Id;
                DB.FCMTokens.Update(dbNewToken);
                await DB.SaveChangesAsync();

                return dbNewToken.Id;
            }
        }

        private async Task<bool> DeleteCurrentFCMToken(Profile profile)
        {
            int? currentFCM = User.GetFCMTokenId();
            if (currentFCM == null)
                return false;

            var dbToken = profile.FCMTokens.FirstOrDefault(item => item.Id == currentFCM.Value);
            if (dbToken == null)
                return false;

            //Only delete when still associated with this profile
            DB.FCMTokens.Remove(dbToken);
            await DB.SaveChangesAsync();
            return true;
        }

        private async Task<Account> GetOrCreateAccountAsync(string localId, string name, string email, string photoUrl)
        {
            var account = await DB.Accounts
                .AsNoTracking()
                .Include(item => item.Profiles)
                .Where(item => item.FirebaseId == localId)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                account = new Account { FirebaseId = localId };
                DB.Accounts.Add(account);

                DB.Profiles.Add(new Profile
                {
                    Account = account,
                    MaxMovieRating = MovieRatings.Unrated,
                    MaxTVRating = TVRatings.NotRated,
                    AvatarUrl = LogicUtils.EnsureProfilePic(photoUrl),
                    IsMain = true,
                    Name = LogicUtils.Coalesce(name, email[..email.IndexOf("@")]),
                    TitleRequestPermission = TitleRequestPermissions.Enabled
                });

                await DB.SaveChangesAsync();
            }

            return account;
        }

        static T GetFBClaim<T>(IReadOnlyDictionary<string, object> claims, string key, T defVal)
        {
            try { return (T)claims.First(item => item.Key == key).Value; }
            catch { return defVal; }
        }


    }
}
