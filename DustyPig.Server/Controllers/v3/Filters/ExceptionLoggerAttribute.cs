using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace DustyPig.Server.Controllers.v3.Filters
{
    public class ExceptionLoggerAttribute : ExceptionFilterAttribute
    {
        private readonly NLog.ILogger _logger;

        public ExceptionLoggerAttribute(Type classType)
        {
            _logger = NLog.LogManager.GetLogger(classType.FullName);
        }

        public override void OnException(ExceptionContext context)
        {
            if (context.Exception.GetType() != typeof(OperationCanceledException))
                _logger.Error(context.Exception, context.Exception.Message);

            context.Result = new StatusCodeResult(500);
            context.ExceptionHandled = true;
        }
    }
}
