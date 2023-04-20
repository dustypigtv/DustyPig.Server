using DustyPig.API.v3;
using DustyPig.API.v3.Models;
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

            try
            {
                //Check if they already exist
                var existingUser = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(info.Email);
                return BadRequest("Account already exists");
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
                        AllowedRatings = API.v3.MPAA.Ratings.All,
                        AvatarUrl = Utils.EnsureProfilePic(info.AvatarUrl),
                        IsMain = true,
                        Name = Utils.Coalesce(info.DisplayName, signupResponse.Data.Email[..signupResponse.Data.Email.IndexOf("@")]),
                        TitleRequestPermission = TitleRequestPermissions.Enabled
                    }).Entity;

                    await DB.SaveChangesAsync();
                }

                //Send verification mail
                var dataResponse = await _client.GetUserDataAsync(signupResponse.Data.IdToken);
                if (!dataResponse.Success)
                    return BadRequest(dataResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.GetUserData));

                bool emailVerificationRequired = !dataResponse.Data.Users.Where(item => item.Email.ICEquals(signupResponse.Data.Email)).Any(item => item.EmailVerified);
                if (emailVerificationRequired)
                {
                    var sendVerificationEmailResponse = await _client.SendEmailVerificationAsync(signupResponse.Data.IdToken);
                    if (!sendVerificationEmailResponse.Success)
                        return BadRequest(sendVerificationEmailResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.SendVerificationEmail));
                }

                return CommonResponses.CreatedObject(new CreateAccountResponse { EmailVerificationRequired = emailVerificationRequired });
            }
            else
            {
                return BadRequest(signupResponse.FirebaseError().TranslateFirebaseError(FirebaseMethods.PasswordSignup));
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

            if (account.Id == TestAccount.AccountId)
                return CommonResponses.ProhibitTestUser;

            if (profile == null)
                return CommonResponses.RequireMainProfile;

            if (!profile.IsMain)
                return CommonResponses.RequireMainProfile;

            await _client.DeleteAccountAsync(account.FirebaseId);
            DB.Entry(UserAccount).State = EntityState.Deleted;
            await DB.SaveChangesAsync();

            return Ok();
        }
    }
}
