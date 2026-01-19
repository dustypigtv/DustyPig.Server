using DustyPig.Server.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace DustyPig.Server.Controllers.v3.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireMainProfileAttribute : Attribute, IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var profile = ((_BaseController)context.Controller).UserProfile;
        if (!profile.IsMain)
            context.Result = new OkObjectResult(CommonResponses.RequireMainProfile());
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        //throw new System.NotImplementedException();
    }
}
