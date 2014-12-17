using System.Net;
using System.Web.Http;
using CsSandbox.DataContext;
using CsSandbox.Sandbox;
using CsSandboxApi;

namespace CsSandbox.Controllers
{
    public class SandboxController : ApiController
    {
		private readonly UserRepo _users = new UserRepo();
		private readonly SandboxHandler _sandbox = new SandboxHandler();

		[HttpPost]
		[Route("CreateSubmission")]
		public string CreateSubmission(SubmissionModel model)
		{
			if (!ModelState.IsValid)
				throw new HttpResponseException(HttpStatusCode.BadRequest);

			var userId = GetUserId(model.Token);
			return _sandbox.Create(userId, model);
		}

	    [HttpGet]
		[Route("GetSubmissionStatus")]
		public SubmissionStatus GetSubmissionStatus(string id, string token)
	    {
		    var userId = GetUserId(token);
			return _sandbox.GetStatus(userId, id);
		}

	    [HttpGet]
		[Route("GetSubmissionDetails")]
		public PublicSubmissionDetails GetSubmissionDetails(string id, string token)
		{
			var userId = GetUserId(token);
			return _sandbox.FindDetails(userId, id);
		}
	    private string GetUserId(string token)
	    {
		    var userId = _users.FindUser(token);
		    if (userId == null)
			    throw new HttpResponseException(HttpStatusCode.Unauthorized);
		    return userId;
	    }
    }
}
