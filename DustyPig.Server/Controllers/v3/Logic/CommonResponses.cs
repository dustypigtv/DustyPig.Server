using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public class CommonResponses
    {
        public static BadRequestObjectResult RequiredValueMissing(string name) => new BadRequestObjectResult($"Validation failed: {name} must be specified");

        public static BadRequestObjectResult ValueNotFound(string name) => new BadRequestObjectResult($"{name} not found");

        public static BadRequestObjectResult InvalidValue(string name) => new BadRequestObjectResult($"Invalid {name}");


        public static ForbidResult RequireMainProfile() => Forbid("You must be logged in with the main profile to perform this action");

        public static ForbidResult ProhibitTestUser() => Forbid("Test account is not authorized to to perform this action");

        public static ForbidResult ProfileIsLocked() => Forbid("Your profile is locked");

        public static ForbidResult Forbid(string msg) => new ForbidResult(msg);

        public static ForbidResult Forbid() => new ForbidResult();

        public static StatusCodeResult InternalServerError() => new StatusCodeResult((int)HttpStatusCode.InternalServerError);
    }
}
