using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
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

namespace DustyPig.Server.Controllers.v3;

[ApiController]
[ApiExplorerSettings(GroupName = "Account")]
internal class AccountController : _BaseController
{
    private readonly FirebaseAuthService _firebaseAuthService;

    public AccountController(AppDbContext db, FirebaseAuthService firebaseAuthClient) : base(db)
    {
        _firebaseAuthService = firebaseAuthClient;
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

        try
        {
            var newUserRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
            {
                Email = info.Email,
                Password = info.Password
            });

            var account = DB.Accounts.Add(new Account { FirebaseId = newUserRecord.Uid }).Entity;
            var profile = DB.Profiles.Add(new Profile
            {
                Account = account,
                MaxMovieRating = MovieRatings.Unrated,
                MaxTVRating = TVRatings.NotRated,
                AvatarUrl = LogicUtils.EnsureProfilePic(info.AvatarUrl),
                IsMain = true,
                Name = LogicUtils.Coalesce(info.DisplayName, newUserRecord.Email[..newUserRecord.Email.IndexOf("@")]),
                TitleRequestPermission = TitleRequestPermissions.Enabled
            }).Entity;

            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }


    /// <summary>
    /// Requires main profile
    /// </summary>
    /// <remarks>WARNING: This will permanently delete the account and ALL data. This is not recoverable!</remarks>
    [HttpDelete]
    [Authorize]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
    public async Task<ActionResult<Result>> Delete(DeleteAccountRequest data)
    {
        //Validate
        try { data.Validate(); }
        catch (ModelValidationException ex) { return Result.BuildError(ex); }

        var (account, profile) = await User.VerifyAsync();

        if (account.Id == TestAccount.AccountId)
            return CommonResponses.ProhibitTestUser();

        if (profile == null)
            return CommonResponses.RequireMainProfile();

        if (!profile.IsMain)
            return CommonResponses.RequireMainProfile();


        var signInResponse = await _firebaseAuthService.SignInWithEmailPasswordAsync(data.Email, data.Password);
        if (!signInResponse.Success)
            return Result.BuildError("Invalid credentials");

        var user = await FirebaseAuth.DefaultInstance.GetUserAsync(signInResponse.Data.LocalId);
        var account2 = await DB.Accounts
           .AsNoTracking()
           .Include(item => item.Profiles)
           .Where(item => item.FirebaseId == user.Uid)
           .FirstOrDefaultAsync();

        if (account.Id != account2.Id)
            return Result.BuildError("Invalid credentials");

        await FirebaseAuth.DefaultInstance.DeleteUserAsync(account.FirebaseId);

        DB.Accounts.Remove(account);
        await DB.SaveChangesAsync();

        return Result.BuildSuccess();
    }


    /// <summary>
    /// Requires main profile
    /// </summary>
    /// <remarks>Change the password for the account</remarks>
    [HttpPost]
    [Authorize]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
    public async Task<ActionResult<Result>> ChangePassword(ChangePasswordRequest data)
    {
        //Validate
        try { data.Validate(); }
        catch (ModelValidationException ex) { return Result.BuildError(ex); }

        var (account, profile) = await User.VerifyAsync();
        if (profile == null)
            return Unauthorized();

        if (account.Id == TestAccount.AccountId)
            return CommonResponses.ProhibitTestUser();

        if (!profile.IsMain)
            return CommonResponses.RequireMainProfile();

        var signInResponse = await _firebaseAuthService.SignInWithEmailPasswordAsync(data.EmailAddress, data.Password);
        if (!signInResponse.Success)
            return Result.BuildError("Invalid credentials");

        var user = await FirebaseAuth.DefaultInstance.GetUserAsync(signInResponse.Data.LocalId);
        var account2 = await DB.Accounts
           .AsNoTracking()
           .Include(item => item.Profiles)
           .Where(item => item.FirebaseId == user.Uid)
           .FirstOrDefaultAsync();

        if (account.Id != account2.Id)
            return Result.BuildError("Invalid credentials");


        var fbUser = await FirebaseAuth.DefaultInstance.GetUserAsync(account.FirebaseId);
        await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
        {
            Password = data.NewPassword,
            Uid = fbUser.Uid
        });


        return Result.BuildSuccess();
    }



    /// <summary>
    /// Requires main profile
    /// </summary>
    /// <remarks>Change the email address for the account</remarks>
    [HttpPost]
    [Authorize]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
    public async Task<ActionResult<Result>> ChangeEmailAddress(ChangeEmailAddressRequest data)
    {
        //Validate
        try { data.Validate(); }
        catch (ModelValidationException ex) { return Result.BuildError(ex); }

        var (account, profile) = await User.VerifyAsync();
        if (profile == null)
            return Unauthorized();

        if (account.Id == TestAccount.AccountId)
            return CommonResponses.ProhibitTestUser();

        if (!profile.IsMain)
            return CommonResponses.RequireMainProfile();

        var signInResponse = await _firebaseAuthService.SignInWithEmailPasswordAsync(data.EmailAddress, data.Password);
        if (!signInResponse.Success)
            return Result.BuildError("Invalid credentials");

        var user = await FirebaseAuth.DefaultInstance.GetUserAsync(signInResponse.Data.LocalId);
        var account2 = await DB.Accounts
           .AsNoTracking()
           .Include(item => item.Profiles)
           .Where(item => item.FirebaseId == user.Uid)
           .FirstOrDefaultAsync();

        if (account.Id != account2.Id)
            return Result.BuildError("Invalid credentials");

        var fbUser = await FirebaseAuth.DefaultInstance.GetUserAsync(account.FirebaseId);
        await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
        {
            Email = data.NewEmailAddress,
            Uid = fbUser.Uid
        });


        return Result.BuildSuccess();
    }
}
