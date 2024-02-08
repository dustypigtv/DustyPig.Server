using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    /// <summary>
    /// This base class ensures the user is logged in with a profile
    /// </summary>
    [Authorize]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    [SwaggerResponse((int)HttpStatusCode.Forbidden)]
    public abstract class _BaseProfileController : _BaseController
    {
        protected _BaseProfileController(AppDbContext db) : base(db) { }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var (account, profile) = await User.VerifyAsync();

            if (account == null)
            {
                context.Result = Unauthorized();
                return;
            }

            if (profile == null)
            {
                context.Result = Unauthorized();
                return;
            }

            if (profile.Locked)
            {
                context.Result = new OkObjectResult(CommonResponses.ProfileIsLocked());
                return;
            }

            UserAccount = account;
            UserProfile = profile;

            await base.OnActionExecutionAsync(context, next);
        }
    }
}
