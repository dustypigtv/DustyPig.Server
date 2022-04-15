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
        PasswordReset,
        ConfirmEmailVerification
    }

    public static class FirebaseAuthExtensions
    {
        public static string TranslateFirebaseError(this Firebase.Auth.Models.ErrorData error, FirebaseMethods method)
        {
            switch(method)
            {
                case FirebaseMethods.PasswordSignup:
                    switch (error.Message)
                    {
                        case "EMAIL_EXISTS":
                            return "Account already exists";

                        case "OPERATION_NOT_ALLOWED":
                            return "Password sign-in is disabled for this app";
                    }
                    break;

                case FirebaseMethods.ConfirmEmailVerification:
                    switch(error.Message)
                    {
                        case "EXPIRED_OOB_CODE":
                            return "The token is expired";

                        case "INVALID_OOB_CODE":
                            return "The token is invalid. This can happen if the token is malformed, expired, or has already been used.";

                    }
                    break;
            }

            return TranslateFirebaseErrorMessage(error.Message);
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
