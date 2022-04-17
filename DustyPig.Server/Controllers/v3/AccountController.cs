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
        /// Level 0
        /// </summary>
        /// <remarks>This will create the Firebase account and send a confirmation email</remarks>
        [HttpPost]
        [SwaggerOperation(OperationId = "Create")]
        [SwaggerResponse((int)HttpStatusCode.Created)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> Create(CreateAccount info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }

            var signinResponse = await _client.SignUpWithEmailPasswordAsync(info.Email, info.Password);
            if (signinResponse.Success)
            {
                var account = await DB.Accounts
                    .AsNoTracking()
                    .Include(item => item.Profiles)
                    .Where(item => item.FirebaseId == signinResponse.Data.LocalId)
                    .FirstOrDefaultAsync();


                int profileId = 0;
                if (account == null)
                {
                    account = DB.Accounts.Add(new Data.Models.Account { FirebaseId = signinResponse.Data.LocalId }).Entity;
                    var profile = DB.Profiles.Add(new Data.Models.Profile
                    {
                        Account = account,
                        AllowedRatings = API.v3.MPAA.Ratings.All,
                        AvatarUrl = info.AvatarUrl,
                        IsMain = true,
                        Name = Utils.Coalesce(info.DisplayName, signinResponse.Data.Email[..signinResponse.Data.Email.IndexOf("@")]),
                        TitleRequestPermission = TitleRequestPermissions.Enabled
                    }).Entity;

                    await DB.SaveChangesAsync();
                    profileId = profile.Id;
                }
                
                //Send verification mail

                var dataResponse = await _client.GetUserDataAsync(signinResponse.Data.IdToken);
                if (!dataResponse.Success)
                    return BadRequest(dataResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.GetUserData));

                bool emailVerificationRequired = !dataResponse.Data.Users.Where(item => item.Email.ICEquals(signinResponse.Data.Email)).Any(item => item.EmailVerified);

                if (emailVerificationRequired)
                {
                    var sendVerificationEmailResponse = await _client.SendEmailVerificationAsync(signinResponse.Data.IdToken);
                    if (!sendVerificationEmailResponse.Success)
                        return BadRequest(sendVerificationEmailResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.SendVerificationEmail));

                    return CommonResponses.CreatedObject(new CreateAccountResponse { EmailVerificationRequired = true });
                }
                else
                {
                    if (profileId == 0)
                    {
                        //Account already existed
                        if (account.Profiles.Count == 1 && account.Profiles[0].PinNumber == null)
                        {
                            var profile = account.Profiles.First();
                            if(!string.IsNullOrWhiteSpace(info.DeviceToken))
                            {
                                var deviceToken = await DB.DeviceTokens
                                    .Where(item => item.Token == info.DeviceToken)
                                    .FirstOrDefaultAsync();

                                if (deviceToken == null)
                                    deviceToken = DB.DeviceTokens.Add(new DeviceToken { Token = info.DeviceToken }).Entity;

                                //Change the device token to the last profile to login to that device
                                deviceToken.ProfileId = profile.Id;
                                deviceToken.LastSeen = DateTime.UtcNow;

                                await DB.SaveChangesAsync();
                            }

                            var token = await _jwtProvider.CreateTokenAsync(account.Id, profile.Id, info.DeviceToken);
                            return CommonResponses.CreatedObject(new CreateAccountResponse { Token = token, LoginType = LoginResponseType.Profile });
                        }
                        else
                        {
                            var token = await _jwtProvider.CreateTokenAsync(account.Id, null, null);
                            return CommonResponses.CreatedObject(new CreateAccountResponse { Token = token, LoginType = LoginResponseType.Account });
                        }
                    }
                    else
                    {
                        //Account created
                        var token = await _jwtProvider.CreateTokenAsync(account.Id, profileId, null);
                        return CommonResponses.CreatedObject(new CreateAccountResponse { Token = token, LoginType = LoginResponseType.Account });
                    }
                }
                
            }
            else
            {
                return BadRequest(signinResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.PasswordSignup));
            }
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>WARNING: This will permanently delete the account and ALL data. This is not recoverable!</remarks>
        [HttpDelete]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public async Task<ActionResult> Delete()
        {
            var (account, profile) = await User.VerifyAsync();

            if (account.Id == TestCredentials.AccountId)
                return CommonResponses.ProhibitTestUser;

            if (profile == null)
                return CommonResponses.RequireMainProfile;

            if (!profile.IsMain)
                return CommonResponses.RequireMainProfile;

            DB.Entry(UserAccount).State = EntityState.Deleted;
            await DB.SaveChangesAsync();

            return Ok();
        }
    }
}
