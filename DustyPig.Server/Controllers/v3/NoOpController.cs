using Asp.Versioning;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;

namespace DustyPig.Server.Controllers.v3
{
    [ApiVersion("3")]
    [Produces("application/json")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "NoOp")]
    [Route("api/v{version:apiVersion}/NoOp/[action]")]
    public class NoOpAnyController : Controller
    {
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<string>))]
        public Result<string> HelloEveryone()
        {
            return Result<string>.BuildSuccess("Hello Everyone");
        }
    }


    [ApiVersion("3")]
    [Produces("application/json")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "NoOp")]
    [Route("api/v{version:apiVersion}/NoOp/[action]")]
    public class NoOpAccountController : _BaseAccountController
    {
        public NoOpAccountController(AppDbContext db) : base(db) { }

        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<string>))]
        public Result<string> HelloAccount()
        {
            return Result<string>.BuildSuccess("Hello Account");
        }
    }


    [ApiVersion("3")]
    [Produces("application/json")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "NoOp")]
    [Route("api/v{version:apiVersion}/NoOp/[action]")]
    public class NoOpProfileController : _BaseProfileController
    {
        public NoOpProfileController(AppDbContext db) : base(db) { }


        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<string>))]
        public Result<string> HelloProfile()
        {
            return Result<string>.BuildSuccess("Hello Profile");
        }


        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<string>))]
        [RequireMainProfile]
        public Result<string> HelloMainProfile()
        {
            return Result<string>.BuildSuccess("Hello Main Profile");
        }

    }
}
