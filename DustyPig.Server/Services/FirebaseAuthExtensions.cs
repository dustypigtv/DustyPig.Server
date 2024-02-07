// https://firebase.google.com/docs/reference/rest/auth

using DustyPig.Server.Controllers.v3.Logic;
using Microsoft.AspNetCore.Mvc;

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
        public class ErrorInfo
        {
            public ErrorInfo(ActionResult result, string text)
            {
                Result = result;
                Text = text;
            }

            public ActionResult Result { get; set; }
            public string Text { get; set; }
        }


        private static ErrorInfo GetBadRequest(string text) => new(new BadRequestObjectResult(text), text);

        private static ErrorInfo GetForbid(string text) => new(CommonResponses.Forbid(text), text);

        private static ErrorInfo GetInternalServerError() => new(CommonResponses.InternalServerError(), "Internal server error");

        private static ErrorInfo GetFirebaseErrorInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return GetInternalServerError();

            var ret = message.ToLower().Split('_');
            ret[0] = ret[0][0].ToString().ToUpper() + ret[0][1..];
            return GetBadRequest(string.Join(' ', ret));
        }

        /// <summary>
        /// Produces BAD_REQUEST and FORBID
        /// </summary>
        /// <returns></returns>
        public static ErrorInfo GetFirebaseErrorInfo(this Firebase.Auth.Models.ErrorData error, FirebaseMethods method)
        {
            if (error == null || string.IsNullOrWhiteSpace(error.Message))
            {
                //switch (method)
                //{
                //    case FirebaseMethods.PasswordSignin:
                //        return "Invalid email or password";

                //    case FirebaseMethods.PasswordReset:
                //        return "Invalid email";
                //}

                return GetInternalServerError();
            }

            var split = error.Message.Split(':');
            if (split.Length > 1)
                return new(new BadRequestObjectResult(split[1].Trim()), split[1].Trim());


            switch (method)
            {
                case FirebaseMethods.PasswordSignup:
                    switch (error.Message)
                    {
                        case "EMAIL_EXISTS":
                            return GetBadRequest("Account already exists");

                        case "OPERATION_NOT_ALLOWED":
                            return GetForbid("Password sign-in is disabled");

                        case "TOO_MANY_ATTEMPTS_TRY_LATER":
                            return GetForbid("Too many attempts, try later");
                    }
                    break;

                case FirebaseMethods.ConfirmEmailVerification:
                case FirebaseMethods.ConfirmPasswordResetCode:
                case FirebaseMethods.PasswordReset:
                    switch (error.Message)
                    {
                        case "EMAIL_NOT_FOUND":
                            return GetBadRequest("There is no user record corresponding to this identifier. The user may have been deleted");

                        case "OPERATION_NOT_ALLOWED":
                            return GetForbid("Password sign-in is disabled");
                    }
                    break;

                case FirebaseMethods.OauthSignin:
                    if (error.Message.StartsWith("INVALID_IDP_RESPONSE : Bad access token:"))
                        return GetBadRequest("Bad access token");
                    break;
            }

            return error.Message switch
            {
                "EXPIRED_OOB_CODE" => GetForbid("The action code is expired"),
                "INVALID_OOB_CODE" => GetForbid("The action code is invalid. This can happen if the token is malformed, expired, or has already been used."),
                "USER_DISABLED" => GetForbid("The user account has been disabled by an administrator"),
                _ => GetFirebaseErrorInfo(error.Message),
            };
        }


        public static ActionResult GetFirebaseErrorActionResult(this Firebase.Auth.Models.ErrorData error, FirebaseMethods method) =>
            error.GetFirebaseErrorInfo(method).Result;

    }
}
