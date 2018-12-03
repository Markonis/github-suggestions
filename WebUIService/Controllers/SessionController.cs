using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebUIService.Controllers
{
    public class SessionController : ControllerBase
    {
        [HttpGet]
        public IActionResult Status()
        {
            string gitHubAuthToken = HttpContext.Session
                .GetString(Constants.GIT_HUB_AUTH_TOKEN_SESSION_KEY);

            string gitHubLogin = HttpContext.Session
                .GetString(Constants.GIT_HUB_LOGIN_KEY);

            bool isLoggedIn = !string.IsNullOrEmpty(gitHubAuthToken) &&
                !string.IsNullOrEmpty(gitHubLogin);

            return Ok(new { IsLoggedIn = isLoggedIn });
        }
    }
}
