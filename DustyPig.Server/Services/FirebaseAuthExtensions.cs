// https://firebase.google.com/docs/reference/rest/auth
namespace DustyPig.Server.Services
{
    public enum FirebaseMethods
    {
        PasswordSignin,
        PasswordSignup,
        OauthSignin,
        GetUserData,
        SendVerificationEmail,
        ConfirmPasswordResetCode,
        PasswordReset,
        ConfirmEmailVerification
    }

    public static class FirebaseAuthExtensions
    {
        public static string TranslateFirebaseError(this Firebase.Auth.Models.ErrorData error, FirebaseMethods method)
        {
            switch (method)
            {
                case FirebaseMethods.PasswordSignup:
                    switch (error.Message)
                    {
                        case "EMAIL_EXISTS":
                            return "Account already exists";

                        case "OPERATION_NOT_ALLOWED":
                            return "Password sign-in is disabled";
                    }
                    break;

                case FirebaseMethods.ConfirmEmailVerification:
                case FirebaseMethods.ConfirmPasswordResetCode:
                case FirebaseMethods.PasswordReset:
                    switch(error.Message)
                    {
                        case "EMAIL_NOT_FOUND":
                            return "There is no user record corresponding to this identifier. The user may have been deleted";

                        case "OPERATION_NOT_ALLOWED":
                            return "Password sign-in is disabled";
                    }
                    break;
            }

            return error.Message switch
            {
                "EXPIRED_OOB_CODE" => "The action code is expired",
                "INVALID_OOB_CODE" => "The action code is invalid. This can happen if the token is malformed, expired, or has already been used.",
                "USER_DISABLED" => "The user account has been disabled by an administrator",
                "WEAK_PASSWORD : Password should be at least 6 characters" => "Password should be at least 6 characters",
                _ => TranslateFirebaseErrorMessage(error.Message),
            };
        }

        private static string TranslateFirebaseErrorMessage(string message)
        {
            var ret = message.Split('_');
            for (int i = 0; i < ret.Length; i++)
                ret[i] = ret[i].ToLower();
            ret[0] = ret[0][0].ToString().ToUpper() + ret[0][1..];
            return string.Join(' ', ret);
        }
    }
}
