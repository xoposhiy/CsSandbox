using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using CsSandbox.DataContext;
using CsSandbox.Models;
using CsSandbox.Sandbox;
using CsSandboxApi;

namespace CsSandbox.Controllers
{
    public class SandboxController : ApiController
    {
		private readonly SubmissionRepo _submissions = new SubmissionRepo();
		private readonly UserRepo _users = new UserRepo();

		[HttpPost]
		[Route("CreateSubmission")]
		public string CreateSubmission(SubmissionModel model)
		{
			if (!ModelState.IsValid)
				throw new HttpResponseException(HttpStatusCode.BadRequest);

			var userId = GetUserId(model.Token);

			var submission = _submissions.AddSubmission(userId, model);
			Task.Run(() => new Worker(submission.Id, model).Run());
			return submission.Id;
		}

	    [HttpGet]
		[Route("GetSubmissionStatus")]
		public SubmissionStatus GetSubmissionStatus(string id, string token)
		{
			var status = _submissions.GetStatus(GetUserId(token), id);

		    if (status == SubmissionStatus.NotFound)
			    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
			    {
				    ReasonPhrase = "Посылка с указанным ID не найдена."
			    });

			if (status == SubmissionStatus.AccessDeny)
				throw new HttpResponseException(HttpStatusCode.Forbidden);

		    return status;
		}

	    [HttpGet]
		[Route("GetSubmissionDetails")]
		public PublicSubmissionDetails GetSubmissionDetails(string id, string token)
		{
			var userId = GetUserId(token);
			var details = _submissions.FindDetails(id);

			if (details == null)
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
				{
					ReasonPhrase = "Посылка с указанным ID не найдена."
				});

		    if (details.UserId != userId)
				throw new HttpResponseException(HttpStatusCode.Forbidden);

			return details.ToPublic();
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
