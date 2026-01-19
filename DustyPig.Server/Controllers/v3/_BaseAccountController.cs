using DustyPig.Server.Data;
using DustyPig.Server.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3;

/// <summary>
/// This base class ensure the user is logged in with an acocunt, and optionally a profile
/// </summary>
[Authorize]
[SwaggerResponse((int)HttpStatusCode.Unauthorized)]
public abstract class _BaseAccountController : _BaseController
{
    protected _BaseAccountController(AppDbContext db) : base(db) { }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //All apis inheriting from this must be logged in with an account

        var (account, profile) = await User.VerifyAsync(DB);

        if (account == null)
        {
            context.Result = Unauthorized();
            return;
        }

        UserAccount = account;
        UserProfile = profile;

        await base.OnActionExecutionAsync(context, next);
    }
}
