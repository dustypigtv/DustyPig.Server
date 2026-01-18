using DustyPig.Firebase.Auth;
using DustyPig.Server.Data;
using DustyPig.Server.Extensions;
using DustyPig.Server.Services;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Pages.FirebaseActions;

internal class IndexModel : PageModel
{
    private readonly FirebaseAuthService _client;

    public IndexModel(FirebaseAuthService client) => _client = client;

    [BindProperty]
    public FirebaseActionModel FAM { get; set; }


    //public async Task OnGetAsync(string mode, string oobCode, string apiKey, string continueUrl, string lang)
    public async Task OnGetAsync(string mode, string oobCode)
    {
        FAM = new FirebaseActionModel();

        if (string.IsNullOrWhiteSpace(oobCode))
        {
            FAM.Title = "Error";
            FAM.ErrorMessage = "Invalid action code";
            return;
        }

        switch (mode)
        {
            case "resetPassword":
                var checkCodeResponse = await _client.VerifyPasswordResetCodeAsync(oobCode);
                if (checkCodeResponse.Success)
                {
                    FAM.Title = "Reset Password";
                    FAM.Code = oobCode;
                    FAM.ShowPasswordReset = true;
                }
                else
                {
                    FAM.Title = "Error";
                    FAM.ErrorMessage = FirebaseErrorMessage(checkCodeResponse, FirebaseMethods.ConfirmPasswordResetCode);
                }
                break;


            case "verifyEmail":
                var confirmResponse = await _client.ConfirmEmailVerificationAsync(oobCode);
                if (confirmResponse.Success)
                {
                    FAM.Title = "Success";
                    FAM.Message = "Your email has been verified, and you can now sign in to Dusty Pig";
                }
                else
                {
                    FAM.Title = "Error";
                    FAM.ErrorMessage = FirebaseErrorMessage(confirmResponse, FirebaseMethods.ConfirmEmailVerification);
                }
                break;


            case "recoverEmail":
            default:
                FAM.Title = "Invalid Operation";
                FAM.ErrorMessage = "This server only supports resetPassword and verifyEmail";
                break;

        }
    }

    public async Task OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(FAM.Code))
        {
            FAM.Title = "Error";
            FAM.ErrorMessage = "Invalid action code";
            return;
        }

        var ret = await _client.ConfirmPasswordResetAsync(FAM.Code, FAM.NewPassword);
        if (ret.Success)
        {
            //Reset pin
            bool pinWasReset = false;
            var fbUser = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(ret.Data.Email);
            var db = new AppDbContext();
            var acct = await db.Accounts
                .AsNoTracking()
                .Include(a => a.Profiles.Where(p => p.IsMain))
                .Where(a => a.FirebaseId == fbUser.Uid)
                .FirstOrDefaultAsync();

            if (acct != null)
            {
                var prof = acct.Profiles.FirstOrDefault(p => p.IsMain);
                if (prof != null && prof.PinNumber != null)
                {
                    prof.PinNumber = null;
                    db.Profiles.Update(prof);
                    await db.SaveChangesAsync();
                    pinWasReset = true;
                }
            }

            FAM.Title = "Success";
            if (pinWasReset)
                FAM.Message = "Your password has been reset, and your PIN cleared. You can now sign in to Dusty Pig";
            else
                FAM.Message = "Your password has been reset. You can now sign in to Dusty Pig";


        }
        else
        {
            FAM.Title = "Error";
            FAM.ErrorMessage = FirebaseErrorMessage(ret, FirebaseMethods.PasswordReset);

            //Form validation should prevent this, but just in case
            if (FAM.ErrorMessage == "Password should be at least 6 characters")
                FAM.ShowPasswordReset = true;
        }
    }

    static string FirebaseErrorMessage(REST.Response response, FirebaseMethods method)
    {
        try
        {
            return response.FirebaseError().TranslateFirebaseError(method);
        }
        catch
        {
            return response.Error.Message;
        }
    }
}
