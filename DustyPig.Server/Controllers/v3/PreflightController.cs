//using DustyPig.REST;
//using DustyPig.Server.Controllers.v3.Filters;
//using Microsoft.AspNetCore.Mvc;

//namespace DustyPig.Server.Controllers.v3
//{
//    /// <summary>
//    /// Handles Preflight requests from Cast Receiver
//    /// </summary>
//    [ApiController]
//    [Produces("application/json")]
//    [ExceptionLogger(typeof(PreflightController))]
//    public class PreflightController : Controller
//    {
//        [HttpOptions]
//        [Route("api/v{version:apiVersion}/Movies/Details")]
//        [Route("api/v{version:apiVersion}/Playlists/Details")]
//        [Route("api/v{version:apiVersion}/Series/Details")]
//        [Route("api/v{version:apiVersion}/Media/UpdatePlaybackProgress")]
//        [Route("api/v{version:apiVersion}/Playlists/SetPlaylistProgress")]
//        public IActionResult Preflight()
//        {
//            Response.Headers.AccessControlAllowOrigin = "*";
//            Response.Headers.AccessControlAllowMethods = "GET, OPTIONS, POST";
//            Response.Headers.AccessControlAllowHeaders = "*";
//            Response.Headers.AccessControlAllowCredentials = "true";
//            Response.Headers.AccessControlMaxAge = (30 * 24 * 60 * 60).ToString(); //1 month - far surpasses any browser caps
            
//            return NoContent();
//        }
//    }
//}
