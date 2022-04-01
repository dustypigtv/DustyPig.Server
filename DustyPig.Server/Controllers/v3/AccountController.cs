using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Firebase.Auth;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
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
        public AccountController(AppDbContext db) : base(db) { }

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


            var fbAuth = new FirebaseAuthClient();

            //Check if user exists
            var signinResponse = await fbAuth.SignInWithEmailPasswordAsync(info.Email, info.Password);
            if (signinResponse.Success)
            {
                var account = await DB.Accounts
                    .AsNoTracking()
                    .Include(item => item.Profiles)
                    .Where(item => item.FirebaseId == signinResponse.Data.LocalId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    //Account exists in Firebase but not here. Create it
                    account = DB.Accounts.Add(new Data.Models.Account { FirebaseId = signinResponse.Data.LocalId }).Entity;

                    var profile = DB.Profiles.Add(new Data.Models.Profile
                    {
                        Account = account,
                        AllowedRatings = API.v3.MPAA.Ratings.All,
                        AvatarUrl = info.AvatarUrl,
                        IsMain = true,
                        Name = info.DisplayName,
                        TitleRequestPermission = TitleRequestPermissions.Enabled
                    }).Entity;

                    await DB.SaveChangesAsync();
                }
                else
                {
                    return BadRequest("Account already exists");
                }
            }
            else
            {
                var errorData = signinResponse.FirebaseError();
                if (errorData.Message == "INVALID_EMAIL")
                {
                    //Create
                    var userRec = new UserRecordArgs
                    {
                        DisplayName = info.DisplayName,
                        Email = info.Email,
                        Password = info.Password,
                        PhotoUrl = info.AvatarUrl,
                    };
                    try
                    {
                        var user = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRec);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex.Message);
                    }

                    signinResponse = await fbAuth.SignInWithEmailPasswordAsync(info.Email, info.Password);
                    if (!signinResponse.Success)
                        return BadRequest(signinResponse.FirebaseError().Message);

                    var sendEmailResponse = await fbAuth.SendEmailVerificationAsync(signinResponse.Data.IdToken);
                    if (!sendEmailResponse.Success)
                        return BadRequest(sendEmailResponse.FirebaseError().Message);
                }
                else
                {
                    return BadRequest(errorData.Message);
                }
            }

            return CommonResponses.Created;
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
