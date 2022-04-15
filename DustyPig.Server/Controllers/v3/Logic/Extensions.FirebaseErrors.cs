// https://firebase.google.com/docs/reference/rest/auth
namespace DustyPig.Server.Controllers.v3.Logic
{
    public enum FirebaseMethods
    {
        PasswordSignin,
        PasswordSignup,
        OauthSignin,
        GetUserData,
        SendVerificationEmail,
        PasswordReset
    }

    public static partial class Extensions
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
