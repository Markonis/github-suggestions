using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using GitHubAPI;
using Domain.Interop;
using System.Web;
using System;

namespace WebUIService.Controllers
{
    public class OAuthController : Controller
    {
        private ConfigSettings _settings;
        private IGitHubClient _gitHubClient;

        public OAuthController(ConfigSettings settings, IGitHubClient gitHubClient)
        {
            _settings = settings;
            _gitHubClient = gitHubClient;
        }

        [HttpPost]
        public IActionResult CreateUrl()
        {
            string state = Guid.NewGuid().ToString();
            HttpContext.Session.SetString(Constants.GIT_HUB_AUTH_STATE_SESSION_KEY, state);

            string result = Constants.GIT_HUB_AUTHORIZE_URL;
            result += $"?client_id={HttpUtility.UrlEncode(_settings.GitHubClientId)}";
            result += $"&redirect_uri={HttpUtility.UrlEncode(_settings.GitHubRedirectUri)}";
            result += $"&state={HttpUtility.UrlEncode(state)}";

            return Ok(new { Url = result });
        }

        [HttpGet]
        public async Task<IActionResult> Callback(string code, string state)
        {
            string sessionState = HttpContext.Session.GetString(Constants.GIT_HUB_AUTH_STATE_SESSION_KEY);
            if (sessionState != null && sessionState == state)
            {
                string token = await GetAuthToken(code, state);

                HttpContext.Session.SetString(
                    Constants.GIT_HUB_AUTH_TOKEN_SESSION_KEY, token);

                Result<string> loginResult = await _gitHubClient.GetLoginAsync(token);

                if (loginResult.Success)
                    HttpContext.Session.SetString(Constants.GIT_HUB_LOGIN_KEY, loginResult.Data);
            }
            return Redirect("/");
        }

        private async Task<string> GetAuthToken(string code, string state)
        {
            using (var client = new HttpClient())
            {
                var requestParams = new Dictionary<string, string>();
                requestParams.Add("client_id", _settings.GitHubClientId);
                requestParams.Add("client_secret", _settings.GitHubClientSecret);
                requestParams.Add("code", code);
                requestParams.Add("state", state);
                requestParams.Add("redirect_uri", _settings.GitHubRedirectUri);

                var content = new FormUrlEncodedContent(requestParams);
                var response = await client.PostAsync(Constants.GIT_HUB_TOKEN_URL, content);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsFormDataAsync();
                    return data.Get("access_token");
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
