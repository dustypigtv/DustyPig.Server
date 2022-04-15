﻿using DustyPig.Server.Controllers.v3.Logic;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace DustyPig.Server.Controllers.v3.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ProhibitTestUserAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.GetAccountId() == TestCredentials.AccountId)
                context.Result = CommonResponses.ProhibitTestUser;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            //throw new System.NotImplementedException();
        }
    }
}