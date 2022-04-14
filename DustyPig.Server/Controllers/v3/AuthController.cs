﻿using DustyPig.API.v3.Models;
using DustyPig.Firebase.Auth;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using FirebaseAdmin.Auth;
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
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<SimpleValue<string>>> PasswordLogin(PasswordCredentials credentials)
        {
            if (string.IsNullOrWhiteSpace(credentials.Email))
                return BadRequest(nameof(credentials.Email) + " must be specified");

            if (string.IsNullOrWhiteSpace(credentials.Password))
                return BadRequest(nameof(credentials.Password) + " must be specified");

            var signInResponse = await _firebaseClient.SignInWithEmailPasswordAsync(credentials.Email, credentials.Password);
            if (!signInResponse.Success)
                return BadRequest(signInResponse.Error.Message);

            var account = await GetOrCreateAccountAsync(signInResponse.Data.LocalId, signInResponse.Data.IdToken);
            var token = await _jwtProvider.CreateTokenAsync(account.Id, null, null);

            return new SimpleValue<string>(token);
        }


        /// <summary>
        /// Level 0
        /// </summary>
        /// <remarks>Logs into the account using an OAuth token, and returns an account level bearer token</remarks>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<SimpleValue<string>>> OAuthLogin(OAuthCredentials credentials)
        {
            var response = await _firebaseClient.SignInWithOAuthAsync("http://localhost", credentials.Token, credentials.Provider.ToString().ToLower() + ".com", true);
            if (!response.Success)
                return BadRequest(response.FirebaseError().Message);

            var account = await GetOrCreateAccountAsync(response.Data.LocalId, response.Data.IdToken);
            var token = await _jwtProvider.CreateTokenAsync(account.Id, null, null);

            return new SimpleValue<string>(token);
        }



        /// <summary>
        /// Level 0
        /// </summary>
        /// <remarks>Returns a code that can be used to login a device with no keyboard (streaming devices, smart tvs, etc)</remarks>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.Created)]
        public async Task<ActionResult<SimpleValue<string>>> GenerateDeviceLoginCode()
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

            var rand = new Random();

            string code;
            while (true)
            {
                code = new string(Enumerable.Repeat(chars, 5).Select(s => s[rand.Next(s.Length)]).ToArray());
                if (!await DB.ActivationCodes.AnyAsync(item => item.Code == code))
                    break;
            }

            var activationToken = new ActivationCode { Code = code };

            DB.ActivationCodes.Add(activationToken);
            await DB.SaveChangesAsync();

            return CommonResponses.CreatedObject(new SimpleValue<string>(code));
        }


        /// <summary>
        /// Level 0
        /// </summary>
        /// <remarks>Check the generated code to see if it has been authorized, and if so returns an account level bearer token. Once this returns true, the generated code will be deleted</remarks>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DeviceCodeStatus>> VerifyDeviceLoginCode(SimpleValue<string> code)
        {
            if (string.IsNullOrWhiteSpace(code.Value))
                return BadRequest($"value must be specified");

            code.Value = code.Value.Trim();
            if (code.Value.Length != 5)
                return BadRequest($"value is invalid");

            var rec = await DB.ActivationCodes
                .Where(item => item.Code == code.Value)
                .SingleOrDefaultAsync();

            if (rec == null)
                return NotFound();


            var ret = new DeviceCodeStatus { Activated = rec.AccountId != null };
            if (ret.Activated)
            {
                ret.AccountToken = await new JWTProvider(DB).CreateTokenAsync(rec.AccountId.Value, null, null);
                DB.ActivationCodes.Remove(rec);
                await DB.SaveChangesAsync();
            }

            return ret;
        }




        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Associates the generated code with logged in user account, allowing a subsequent call to VerifyLoginCode by the device to get an account token</remarks>
        [HttpPost]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public async Task<ActionResult> LoginDeviceWithCode(SimpleValue<string> code)
        {
            if (string.IsNullOrWhiteSpace(code.Value))
                return BadRequest($"You must specify value");

            code.Value = code.Value.Trim();
            if (code.Value.Length != 5)
                return BadRequest($"Invalid value");

            var (account, profile) = await User.VerifyAsync();
            if (profile == null)
                return Unauthorized();
               

            if (profile.Locked)
                return CommonResponses.ProfileIsLocked;

            var rec = await DB.ActivationCodes
                .Where(item => item.Code == code.Value)
                .SingleOrDefaultAsync();

            if (rec == null)
                return BadRequest($"Invalid value");

            if (rec.AccountId != null)
                return BadRequest($"value has already been claimed");

            rec.AccountId = account.Id;
            await DB.SaveChangesAsync();

            return Ok();
        }





        /// <summary>
        /// Level 1
        /// </summary>
        /// <remarks>Returns a profile level bearer token</remarks>
        [HttpPost]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public async Task<ActionResult<SimpleValue<string>>> ProfileLogin(ProfileCredentials credentials)
        {
            var (account, _) = await User.VerifyAsync();

            if (account == null)
                return Unauthorized();

            using var db = new AppDbContext();
            var profile = await db.Profiles
                .AsNoTracking()
                .Where(item => item.AccountId == account.Id)
                .Where(item => item.Id == credentials.Id)
                .SingleOrDefaultAsync();

            if (profile == null)
                return BadRequest("Profile does not exist");

            if (profile.PinNumber != null)
            {
                if (credentials.Pin == null || credentials.Pin != profile.PinNumber)
                    return BadRequest("Invalid pin");
            }

            if (profile.Locked)
                return CommonResponses.ProfileIsLocked;

            if (!string.IsNullOrWhiteSpace(credentials.DeviceToken))
            {
                var deviceToken = await db.DeviceTokens
                    .Where(item => item.Token == credentials.DeviceToken)
                    .FirstOrDefaultAsync();

                if (deviceToken == null)
                    deviceToken = db.DeviceTokens.Add(new DeviceToken { Token = credentials.DeviceToken }).Entity;

                //Change the device token to the last profile to login to that device
                deviceToken.ProfileId = profile.Id;
                deviceToken.LastSeen = DateTime.UtcNow;

                await db.SaveChangesAsync();
            }

            var token = await _jwtProvider.CreateTokenAsync(account.Id, profile.Id, credentials.DeviceToken);

            return new SimpleValue<string>(token);
        }

        /// <summary>
        /// Level 1
        /// </summary>
        /// <remarks>If presenting an account level token, will sign out of the account. If presenting a profile level token,will sign out of the profile</remarks>
        [HttpGet]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult> Signout()
        {
            var (account, profile) = await User.VerifyAsync();

            if (account == null)
                return Unauthorized();

            var token = account.AccountTokens.FirstOrDefault(item => item.Id == User.GetTokenId());
            if (token == null)
                return Unauthorized();

            DB.AccountTokens.Remove(token);

            if (profile != null)
            {
                var deviceToken = User.GetDeviceTokenId();
                if (deviceToken != null)
                {
                    var dbDeviceToken = profile.DeviceTokens.FirstOrDefault(item => item.Token == deviceToken);
                    if (dbDeviceToken != null)
                        DB.DeviceTokens.Remove(dbDeviceToken);
                }
            }


            await DB.SaveChangesAsync();

            return Ok();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Sign all users out of all devices</remarks>
        [HttpGet]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public async Task<ActionResult> SignoutEverywhere()
        {
            var (account, profile) = await User.VerifyAsync();

            if (account == null)
                return Unauthorized();

            if (profile == null)
                return Unauthorized();

            if (!profile.IsMain)
                return CommonResponses.RequireMainProfile;

            if (profile.Id == TestCredentials.ProfileId)
                return CommonResponses.ProhibitTestUser;

            await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.RevokeRefreshTokensAsync(account.FirebaseId);


            DB.AccountTokens.RemoveRange(account.AccountTokens);
            foreach (var deviceToken in profile.DeviceTokens)
                DB.DeviceTokens.Remove(deviceToken);

            await DB.SaveChangesAsync();

            return Ok();
        }




        private async Task<Account> GetOrCreateAccountAsync(string localId, string idToken)
        {
            var account = await DB.Accounts
                .AsNoTracking()
                .Where(item => item.FirebaseId == localId)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                //Exists in firebase but not here... shouldn't happen except in development, but go ahead and fix it
                var userResponse = await _firebaseClient.GetUserDataAsync(idToken);
                userResponse.ThrowIfError();

                account = new Account { FirebaseId = userResponse.Data.Users[0].LocalId };
                DB.Accounts.Add(account);

                DB.Profiles.Add(new Profile
                {
                    Account = account,
                    AllowedRatings = API.v3.MPAA.Ratings.All,
                    AvatarUrl = userResponse.Data.Users[0].PhotoUrl,
                    IsMain = true,
                    Name = Utils.Coalesce(userResponse.Data.Users[0].DisplayName, userResponse.Data.Users[0].Email, "New User!"),
                    TitleRequestPermission = TitleRequestPermissions.Enabled
                });

                await DB.SaveChangesAsync();
            }

            return account;
        }
    }
}
