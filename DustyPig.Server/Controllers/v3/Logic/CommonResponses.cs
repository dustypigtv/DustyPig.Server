using DustyPig.API.v3.Models;
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


        public static ResponseWrapper NotFound() => new ResponseWrapper("Not found");
        public static ResponseWrapper NotFound(string name) => new ResponseWrapper($"{name} not found");
        public static ResponseWrapper<T> NotFound<T>() => new ResponseWrapper<T> { Error = "Not found" };
        public static ResponseWrapper<T> NotFound<T>(string name) => new ResponseWrapper<T> { Error = $"{name} not found" };


        
        public static ResponseWrapper Forbid() => new ResponseWrapper("Forbidden");
        public static ResponseWrapper<T> Forbid<T>() => new ResponseWrapper<T> { Error = "Forbidden" };


        public static ResponseWrapper ProhibitTestUser() => new ResponseWrapper("Test account is not authorized to to perform this action");
        public static ResponseWrapper<T> ProhibitTestUser<T>() => new ResponseWrapper<T> { Error = "Test account is not authorized to to perform this action" };

        public static ResponseWrapper RequireMainProfile() => new ResponseWrapper("You must be logged in with the main profile to perform this action");
        public static ResponseWrapper<T> RequireMainProfile<T>() => new ResponseWrapper<T> { Error = "You must be logged in with the main profile to perform this action" };

        public static ResponseWrapper ProfileIsLocked() => new ResponseWrapper("Your profile is locked");
        public static ResponseWrapper<T> ProfileIsLocked<T>() => new ResponseWrapper<T> { Error = "Your profile is locked" };

        public static ResponseWrapper Unauthorized() => new ResponseWrapper("Unauthorized");
        public static ResponseWrapper<T> Unauthorized<T>() => new ResponseWrapper<T> { Error = "Unauthorized" };

    }
}
