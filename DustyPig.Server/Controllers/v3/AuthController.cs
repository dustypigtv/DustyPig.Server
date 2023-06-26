using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Firebase.Auth;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
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
        /// Level 0
        /// </summary>
        /// <remarks>Returns an account level bearer token</remarks>
        [HttpPost]
        public async Task<ResponseWrapper<LoginResponse>> PasswordLogin(PasswordCredentials credentials)
        {
            //Validate
            try { credentials.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<LoginResponse>(ex.ToString()); }

            Account account;

            if (credentials.Email.ToLower().Trim() == TestAccount.Email)
            {
                if (credentials.Password != TestAccount.Password)
                    return new ResponseWrapper<LoginResponse>("Invalid Password");

                account = await GetOrCreateAccountAsync(TestAccount.FirebaseId, null, TestAccount.Email, null);
            }
            else
            {
                var signInResponse = await _firebaseClient.SignInWithEmailPasswordAsync(credentials.Email, credentials.Password);
                if (!signInResponse.Success)
                    return new ResponseWrapper<LoginResponse>(signInResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.PasswordSignin));

                var dataResponse = await _firebaseClient.GetUserDataAsync(signInResponse.Data.IdToken);
                if (!dataResponse.Success)
                    return new ResponseWrapper<LoginResponse>(signInResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.GetUserData));

                var users = dataResponse.Data.Users.Where(item => item.Email.ICEquals(signInResponse.Data.Email));
                if (!users.Any(item => item.EmailVerified))
                    return new ResponseWrapper<LoginResponse>("You must verify your email address before you can sign in");

                account = await GetOrCreateAccountAsync(signInResponse.Data.LocalId, null, signInResponse.Data.Email, null);
            }

            if (account.Profiles.Count == 1)
            {
                int? fcmId = string.IsNullOrWhiteSpace(credentials.FCMToken) ?
                    null :
                    await EnsureFCMTokenAssociatedWithProfile(account.Profiles.First(), credentials.FCMToken);

                return new ResponseWrapper<LoginResponse>(new LoginResponse
                {
                    LoginType = LoginType.MainProfile,
                    ProfileId = account.Profiles.First().Id,
                    Token = await _jwtProvider.CreateTokenAsync(account.Id, account.Profiles.First().Id, fcmId)
                });
            }
            else
            {
                return new ResponseWrapper<LoginResponse>(new LoginResponse
                {
                    LoginType = LoginType.Account,
                    Token = await _jwtProvider.CreateTokenAsync(account.Id, null, null)
                });
            }
        }


        /// <summary>
        /// Level 0
        /// </summary>
        /// <remarks>Sends a new account verification email</remarks>
        [HttpPost]
        public async Task<ResponseWrapper> SendVerificationEmail(PasswordCredentials credentials)
        {
            //Validate
            try { credentials.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }

            if (credentials.Email == TestAccount.Email)
                return CommonResponses.ProhibitTestUser();

            var signInResponse = await _firebaseClient.SignInWithEmailPasswordAsync(credentials.Email, credentials.Password);
            if (!signInResponse.Success)
                return new ResponseWrapper(signInResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.PasswordSignin));

            var sendVerificationEmailResponse = await _firebaseClient.SendEmailVerificationAsync(signInResponse.Data.IdToken);
            if (!sendVerificationEmailResponse.Success)
                return new ResponseWrapper(sendVerificationEmailResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.SendVerificationEmail));

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 0
        /// </summary>
        /// <remarks>Sends a password reset email</remarks>
        [HttpPost]
        public async Task<ResponseWrapper> SendPasswordResetEmail(SimpleValue<string> email)
        {
            if (string.IsNullOrWhiteSpace(email.Value))
                return new ResponseWrapper(nameof(email) + " must be specified");

            if (email.Value.ToLower().Trim() == TestAccount.Email)
                return CommonResponses.ProhibitTestUser();


            var ret = await _firebaseClient.SendPasswordResetEmailAsync(email.Value);
            if (!ret.Success)
                return new ResponseWrapper(ret.FirebaseError().TranslateFirebaseError(FirebaseMethods.PasswordReset));

            return CommonResponses.Ok();
        }


        ///// <summary>
        ///// Level 0
        ///// </summary>
        ///// <remarks>Logs into the account using an OAuth token, and returns an account level bearer token</remarks>
        //[HttpPost]
        //public async Task<LoginResponse> OAuthLogin(OAuthCredentials credentials)
        //{
        //    //Validate
        //    try { credentials.Validate(); }
        //    catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }

        //    var response = await _firebaseClient.SignInWithOAuthAsync("http://localhost", credentials.Token, credentials.Provider.ToString().ToLower() + ".com");
        //    if (!response.Success)
        //        return new ResponseWrapper(response.FirebaseError().TranslateFirebaseError(FirebaseMethods.OauthSignin));

        //    var account = await GetOrCreateAccountAsync(response.Data.LocalId, Utils.Coalesce(response.Data.FirstName, response.Data.FullName), response.Data.Email, response.Data.PhotoUrl);

        //    if (account.Profiles.Count == 1)
        //        return new ResponseWrapper(new LoginResponse
        //        {
        //            LoginType = LoginType.MainProfile,
        //            Token = await _jwtProvider.CreateTokenAsync(account.Id, account.Profiles.First().Id, credentials.FCMToken)
        //        });
        //    else
        //        return new ResponseWrapper(new LoginResponse
        //        {
        //            LoginType = LoginType.Account,
        //            Token = await _jwtProvider.CreateTokenAsync(account.Id, null, null)
        //        });
        //}



        /// <summary>
        /// Level 0
        /// </summary>
        /// <remarks>Returns a code that can be used to login a device with no keyboard (streaming devices, smart tvs, etc)</remarks>
        [HttpGet]
        public async Task<ResponseWrapper<SimpleValue<string>>> GenerateDeviceLoginCode()
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
                    return new ResponseWrapper<SimpleValue<string>>(new SimpleValue<string>(activationCode.Code));
                }
                catch
                {
                    DB.Entry(activationCode).State = EntityState.Detached;
                }
            }
        }


        /// <summary>
        /// Level 0
        /// </summary>
        /// <remarks>Check the generated code to see if it has been authorized, and if so returns an account level bearer token. Once this returns true, the generated code will be deleted</remarks>
        [HttpPost]
        public async Task<ResponseWrapper<DeviceCodeStatus>> VerifyDeviceLoginCode(SimpleValue<string> code)
        {
            if (string.IsNullOrWhiteSpace(code.Value))
                return new ResponseWrapper<DeviceCodeStatus>("value must be specified");

            code.Value = code.Value.Trim();
            if (code.Value.Length != Constants.DEVICE_ACTIVATION_CODE_LENGTH)
                return CommonResponses.BadRequest<DeviceCodeStatus>($"Invalid {nameof(code)}");

            var rec = await DB.ActivationCodes
                .AsNoTracking()
                .Include(item => item.Account)
                .ThenInclude(item => item.Profiles)
                .Where(item => item.Code == code.Value)
                .SingleOrDefaultAsync();

            if (rec == null)
                return CommonResponses.NotFound<DeviceCodeStatus>(nameof(code));


            var ret = new DeviceCodeStatus { Activated = rec.AccountId != null };
            if (ret.Activated)
            {
                DB.ActivationCodes.Remove(rec);
                await DB.SaveChangesAsync();

                if (rec.Account.Profiles.Count == 1)
                {
                    ret.Token = await new JWTProvider(DB).CreateTokenAsync(rec.AccountId.Value, rec.Account.Profiles[0].Id, null);
                    ret.LoginType = LoginType.MainProfile;
                }
                else
                {
                    ret.Token = await new JWTProvider(DB).CreateTokenAsync(rec.AccountId.Value, null, null);
                    ret.LoginType = LoginType.Account;
                }
            }

            return new ResponseWrapper<DeviceCodeStatus>(ret);
        }




        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Associates the generated code with logged in user account, allowing a subsequent call to VerifyLoginCode by the device to get an account token</remarks>
        [HttpPost]
        [Authorize]
        public async Task<ResponseWrapper> LoginDeviceWithCode(SimpleValue<string> code)
        {
            var (account, profile) = await User.VerifyAsync();
            if (profile == null)
                return CommonResponses.Unauthorized();

            if (profile.Locked)
                return CommonResponses.ProfileIsLocked();


            if (string.IsNullOrWhiteSpace(code.Value))
                return CommonResponses.BadRequest($"Invalid {nameof(code)}");

            code.Value = code.Value.Trim();
            if (code.Value.Length != Constants.DEVICE_ACTIVATION_CODE_LENGTH)
                return CommonResponses.BadRequest($"Invalid {nameof(code)}");

            var rec = await DB.ActivationCodes
                .Where(item => item.Code == code.Value)
                .Where(item => item.AccountId == null)
                .SingleOrDefaultAsync();

            if (rec == null)
                return CommonResponses.BadRequest($"Invalid {nameof(code)}");

            rec.AccountId = account.Id;
            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }





        /// <summary>
        /// Level 1
        /// </summary>
        /// <remarks>Returns a profile level bearer token</remarks>
        [HttpPost]
        [Authorize]
        public async Task<ResponseWrapper<LoginResponse>> ProfileLogin(ProfileCredentials credentials)
        {
            //Validate
            try { credentials.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<LoginResponse>(ex.ToString()); }

            var (account, _) = await User.VerifyAsync();

            if (account == null)
                return CommonResponses.Unauthorized<LoginResponse>();

            using var db = new AppDbContext();
            var profile = await db.Profiles
                .AsNoTracking()
                .Where(item => item.AccountId == account.Id)
                .Where(item => item.Id == credentials.Id)
                .FirstOrDefaultAsync();

            if (profile == null)
                return CommonResponses.BadRequest<LoginResponse>("Profile does not exist");

            if (profile.PinNumber != null)
            {
                if (credentials.Pin == null || credentials.Pin != profile.PinNumber)
                    return CommonResponses.BadRequest<LoginResponse>("Invalid pin");
            }

            if (!profile.IsMain && profile.Locked)
                return CommonResponses.ProfileIsLocked<LoginResponse>();

            int? fcmId = string.IsNullOrWhiteSpace(credentials.FCMToken) ?
                null :
                await EnsureFCMTokenAssociatedWithProfile(profile, credentials.FCMToken);

            return new ResponseWrapper<LoginResponse>(new LoginResponse
            {
                ProfileId = profile.Id,
                LoginType = profile.IsMain ? LoginType.MainProfile : LoginType.SubProfile,
                Token = await _jwtProvider.CreateTokenAsync(account.Id, profile.Id, fcmId)
            });
        }

        /// <summary>
        /// Level 1
        /// </summary>
        /// <remarks>If presenting an account level token, will sign out of the account. If presenting a profile level token,will sign out of the profile</remarks>
        [HttpGet]
        [Authorize]
        public async Task<ResponseWrapper> Signout()
        {
            var (account, profile) = await User.VerifyAsync();

            if (account == null)
                return CommonResponses.Unauthorized();

            var acctToken = account.AccountTokens.FirstOrDefault(item => item.Id == User.GetAuthTokenId());
            if (acctToken == null)
                return CommonResponses.Unauthorized();

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

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Sign all users out of all devices</remarks>
        [HttpGet]
        [Authorize]
        public async Task<ResponseWrapper> SignoutEverywhere()
        {
            var acctId = User.GetAccountId();
            var authTokenId = User.GetAuthTokenId();
            var profId = User.GetProfileId();
            var fcmTokenId = User.GetFCMTokenId();

            if (acctId == null || authTokenId == null)
                return CommonResponses.Unauthorized();

            var account = await DB.Accounts
                .AsNoTracking()
                .Where(item => item.Id == acctId)
                .Include(item => item.Profiles)
                .ThenInclude(item => item.FCMTokens)
                .Include(item => item.AccountTokens)
                .FirstOrDefaultAsync();

            if (account == null)
                return CommonResponses.Unauthorized();

            var profile = account.Profiles
                .Where(item => item.Id == profId)
                .FirstOrDefault();
           
            if(profile == null)
                return CommonResponses.Unauthorized();

            if (!profile.IsMain)
                return CommonResponses.RequireMainProfile();

            if (profile.Id == TestAccount.ProfileId)
                return CommonResponses.ProhibitTestUser();

            DB.AccountTokens.RemoveRange(account.AccountTokens);
            DB.FCMTokens.RemoveRange(account.Profiles.SelectMany(item => item.FCMTokens));

            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }



        /// <summary>
        /// Level 1
        /// </summary>
        /// <returns>Verifies the current auth token (and if not null, the FirebaseCloudMessaging token) and returns the type. This may include a new AuthToken</returns>
        [HttpPost]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        public async Task<ResponseWrapper<LoginResponse>> UpdateAuthToken(SimpleValue<string> fcmToken)
        {
            var (account, profile) = await User.VerifyAsync();

            if (account == null)
                return CommonResponses.Unauthorized<LoginResponse>();

            if (profile == null)
            {
                return new ResponseWrapper<LoginResponse>(new LoginResponse { LoginType = LoginType.Account });
            }
            else
            {
                int? newFCMId = null;
                string newFCMVal = fcmToken == null ? null : fcmToken.Value;               

                if (string.IsNullOrWhiteSpace(newFCMVal))
                    await DeleteCurrentFCMToken(profile);
                else
                    newFCMId = await EnsureFCMTokenAssociatedWithProfile(profile, newFCMVal);

                var authId = User.GetAuthTokenId();
                var oldAuthToken = await DB.AccountTokens
                    .AsNoTracking()
                    .Where(item => item.Id == authId)
                    .FirstOrDefaultAsync();

                if (oldAuthToken != null)
                {
                    DB.AccountTokens.Remove(oldAuthToken);
                    await DB.SaveChangesAsync();
                }

                return new ResponseWrapper<LoginResponse>(new LoginResponse
                {
                    LoginType = profile.IsMain ? LoginType.MainProfile : LoginType.SubProfile,
                    ProfileId = profile.Id,
                    Token = await _jwtProvider.CreateTokenAsync(account.Id, account.Profiles.First().Id, newFCMId)
                });
            }
        }


        /// <summary>
        /// Level 1
        /// </summary>
        /// <returns>Updates the device token for Firebase Cloud Messaging, and returns a new JWT</returns>
        [HttpPost]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        public async Task<ResponseWrapper<SimpleValue<string>>> UpdateFCMToken(SimpleValue<string> newFCMToken)
        {
            var (account, profile) = await User.VerifyAsync();

            if (account == null || profile == null)
                return CommonResponses.Unauthorized<SimpleValue<string>>();

            string newJWT;

            if (string.IsNullOrWhiteSpace(newFCMToken.Value))
            {
                await DeleteCurrentFCMToken(profile);
                newJWT = await _jwtProvider.CreateTokenAsync(account.Id, account.Profiles.First().Id, null);
            }
            else
            {
                int newId = await EnsureFCMTokenAssociatedWithProfile(profile, newFCMToken.Value);
                newJWT = await _jwtProvider.CreateTokenAsync(account.Id, account.Profiles.First().Id, newId);
            }

            return new ResponseWrapper<SimpleValue<string>>(new SimpleValue<string>(newJWT));
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
                    MaxMovieRating = MovieRatings.NotRated,
                    MaxTVRating = TVRatings.NotRated,
                    AvatarUrl = Utils.EnsureProfilePic(photoUrl),
                    IsMain = true,
                    Name = Utils.Coalesce(name, email[..email.IndexOf("@")]),
                    TitleRequestPermission = TitleRequestPermissions.Enabled
                });

                await DB.SaveChangesAsync();
            }

            return account;
        }
    }
}
