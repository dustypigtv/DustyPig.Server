using DustyPig.Firebase.Auth;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Extensions;
using DustyPig.Server.Services;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Pages.DeleteAccount;

public class IndexModel : PageModel
{
    //This class exists because the logger shouldn't be 'IndexModel'
    public class DeleteAccountPage { }

    private readonly AppDbContext _db;
    private readonly FirebaseAuthService _firebaseAuthService;
    private readonly ILogger<DeleteAccountPage> _logger;

    public IndexModel(AppDbContext db, FirebaseAuthService firebaseAuthService, ILogger<DeleteAccountPage> logger)
    {
        _db = db;
        _firebaseAuthService = firebaseAuthService;
        _logger = logger;
        Credentials = new();
    }
    

    [BindProperty]
    public DeleteAccountModel Credentials { get; set; }

    public async Task OnPostAsync()
    {
        Credentials.Email = (Credentials.Email + string.Empty).Trim().ToLower();
        Credentials.Password = (Credentials.Password + string.Empty);

        if (Credentials.Email.IsNullOrWhiteSpace() || Credentials.Password.IsNullOrWhiteSpace())
        {
            Credentials.State = DeleteAccountModel.States.None;
            return;
        }


        if (Credentials.Email.ICEquals(TestAccount.Email))
        {
            Credentials.Error = "The demo account cannot be deleted";
            Credentials.State = DeleteAccountModel.States.Error;
            return;
        }

        var signInResponse = await _firebaseAuthService.SignInWithEmailPasswordAsync(Credentials.Email, Credentials.Password);
        if (!signInResponse.Success)
        {
            _logger.LogError(signInResponse.Error, "Firebase sign in");
            Credentials.Error = FirebaseErrorMessage(signInResponse);
            Credentials.State = DeleteAccountModel.States.Error;
            return;
        }

        var user = await FirebaseAuth.DefaultInstance.GetUserAsync(signInResponse.Data.LocalId);
        var deleteResponse = await _firebaseAuthService.DeleteAccountAsync(user.Uid, default);
        if (!deleteResponse.Success)
        {
            _logger.LogError(deleteResponse.Error, "Delete account from Firebase");
            Credentials.Error = FirebaseErrorMessage(signInResponse);
            Credentials.State = DeleteAccountModel.States.Error;
            return;
        }


        try
        {
            var account = await _db.Accounts
                .Where(_ => _.FirebaseId == user.Uid)
                .FirstAsync();

            _db.Accounts.Remove(account);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete account from DB");
            Credentials.Error = FirebaseErrorMessage(signInResponse);
            Credentials.State = DeleteAccountModel.States.Error;
            return;
        }

        Credentials.State = DeleteAccountModel.States.Deleted;
        Credentials.Email = null;
        Credentials.Password = null;
    }

    static string FirebaseErrorMessage(REST.Response response)
    {
        try
        {
            return response.FirebaseError().TranslateFirebaseError(FirebaseMethods.PasswordSignin);
        }
        catch
        {
            return response.Error.Message;
        }
    }
}
