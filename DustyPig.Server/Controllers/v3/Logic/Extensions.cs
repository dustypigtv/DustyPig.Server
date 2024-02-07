using DustyPig.API.v3;
using Microsoft.AspNetCore.Mvc;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BadRequestObjectResult ValidationFailed(this ModelValidationException ex) => new BadRequestObjectResult(ex.ToString());
    }
}
