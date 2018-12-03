using Domain.Interop;
using Domain.V1.Messages;
using Domain.V1.Messages.UserRepoSearch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceDiscovery;
using ServiceInterfaces;
using System.Fabric;
using System.Threading.Tasks;

namespace WebUIService.Controllers
{
    public class SearchController : ControllerBase
    {
        private StatelessServiceContext _context;

        public SearchController(StatelessServiceContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Perform([FromBody] SearchInput input)
        {
            string gitHubAuthToken = HttpContext.Session
                .GetString(Constants.GIT_HUB_AUTH_TOKEN_SESSION_KEY);

            string gitHubLogin = HttpContext.Session
                .GetString(Constants.GIT_HUB_LOGIN_KEY);


            if (CanSearch(gitHubAuthToken, gitHubLogin))
            {
                if (string.IsNullOrWhiteSpace(input.Query))
                    return BadRequest();

                IUserRepoSearchActor userRepoSearchActor = ServiceFinder
                    .GetUserRepoSearchActor(_context, gitHubLogin);

                Result<SearchOutput> result = await userRepoSearchActor.SearchAsync(new SearchInput
                {
                    AuthToken = gitHubAuthToken,
                    Query = input.Query
                });

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, result);
                }
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AutoComplete([FromBody] SearchInput input)
        {
            string gitHubAuthToken = HttpContext.Session
                .GetString(Constants.GIT_HUB_AUTH_TOKEN_SESSION_KEY);

            string gitHubLogin = HttpContext.Session
                .GetString(Constants.GIT_HUB_LOGIN_KEY);


            if (CanSearch(gitHubAuthToken, gitHubLogin))
            {
                if (string.IsNullOrWhiteSpace(input.Query))
                    return BadRequest();

                IUserRepoSearchActor userRepoSearchActor = ServiceFinder
                    .GetUserRepoSearchActor(_context, gitHubLogin);

                AutoCompleteOutput output = await userRepoSearchActor
                    .AutoCompleteAsync(input);

                return Ok(output);
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Suggest([FromBody] SearchInput input)
        {
            string gitHubAuthToken = HttpContext.Session
                .GetString(Constants.GIT_HUB_AUTH_TOKEN_SESSION_KEY);

            string gitHubLogin = HttpContext.Session
                .GetString(Constants.GIT_HUB_LOGIN_KEY);


            if (CanSearch(gitHubAuthToken, gitHubLogin))
            {
                if (string.IsNullOrWhiteSpace(input.Query))
                    return BadRequest();

                IUserRepoSearchActor userRepoSearchActor = ServiceFinder
                    .GetUserRepoSearchActor(_context, gitHubLogin);

                SearchOutput output = await userRepoSearchActor
                    .GetSuggestionsAsync(input);

                return Ok(output);
            }
            else
            {
                return Unauthorized();
            }
        }

        private bool CanSearch(string gitHubAuthToken, string gitHubLogin)
        {
            return !string.IsNullOrEmpty(gitHubAuthToken) &&
                !string.IsNullOrEmpty(gitHubLogin);
        }
    }
}
