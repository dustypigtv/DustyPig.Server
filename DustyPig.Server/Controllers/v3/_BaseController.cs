using Asp.Versioning;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DustyPig.Server.Controllers.v3
{
    [ApiVersion("3")]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [Produces("application/json")]
    [SwaggerResponse(500)]
    public abstract class _BaseController : Controller
    {
        public _BaseController(AppDbContext db) => DB = db;

        public Account UserAccount { get; set; }

        public Profile UserProfile { get; set; }

        public AppDbContext DB { get; private set; }
    }
}
