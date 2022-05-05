using DustyPig.Firebase.Auth;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace DustyPig.Server.Pages.FirebaseActions
{
    public class IndexModel : PageModel
    {
        private readonly FirebaseAuthClient _client;

        public IndexModel(FirebaseAuthClient client) => _client = client;

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
                FAM.Title = "Success";
                FAM.Message = "Your password has been reset, you can now sign in to Dusty Pig";
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
}
