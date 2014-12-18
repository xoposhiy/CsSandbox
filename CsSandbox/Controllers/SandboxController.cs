using System.Net;
using System.Net.Http;
using System.Web.Http;
using CsSandbox.DataContext;
using CsSandbox.Models;
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
			var details = GetDetails(id, token);
			return details.Status;
		}

	    [HttpGet]
		[Route("GetSubmissionDetails")]
		public PublicSubmissionDetails GetSubmissionDetails(string id, string token)
		{
			var details = GetDetails(id, token);
			return details.ToPublic();
		}

	    private string GetUserId(string token)
	    {
		    var userId = _users.FindUser(token);
		    if (userId == null)
			    throw new HttpResponseException(HttpStatusCode.Unauthorized);
		    return userId;
	    }

		private SubmissionDetails GetDetails(string id, string token)
		{
			var userId = GetUserId(token);
			var details = _sandbox.FindDetails(id);

			if (details == null)
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
				{
					ReasonPhrase = "Посылка с указанным ID не найдена."
				});

			if (details.UserId != userId)
				throw new HttpResponseException(HttpStatusCode.Forbidden);

			return details;
		}

    }
}
