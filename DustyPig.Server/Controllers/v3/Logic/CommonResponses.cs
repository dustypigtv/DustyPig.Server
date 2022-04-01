#nullable enable

using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DustyPig.Server.Controllers.v3.Logic
{
    /// <summary>
    /// This mostly exists because the ForbidResult throws an unhandled 500 exception to the client (?)
    /// </summary>
    public class CommonResponses
    {
        public static StatusCodeResult BuildResult(HttpStatusCode code) => new StatusCodeResult((int)code);
        public static ObjectResult BuildResult(HttpStatusCode code, object? value) => new ObjectResult(value) { StatusCode = (int)code };

        public static StatusCodeResult Ok => BuildResult(HttpStatusCode.OK);
        
        public static ObjectResult NotFoundObject(string msg) => BuildResult(HttpStatusCode.NotFound, msg);

        public static StatusCodeResult Created => BuildResult(HttpStatusCode.Created);
        public static ObjectResult CreatedObject(object? value) => BuildResult(HttpStatusCode.Created, value);

        public static StatusCodeResult Forbid => BuildResult(HttpStatusCode.Forbidden);
        public static ObjectResult ForbidObject(object? value) => BuildResult(HttpStatusCode.Forbidden, value);
        
        public static ObjectResult ProhibitTestUser => BuildResult(HttpStatusCode.Forbidden, "Test account is not authorized to to perform this action");

        public static ObjectResult RequireMainProfile => BuildResult(HttpStatusCode.Forbidden, "You must be logged in with the main profile to perform this action");

        public static ObjectResult ProfileIsLocked => BuildResult(HttpStatusCode.Forbidden, "Your profile is locked");
    }
}
