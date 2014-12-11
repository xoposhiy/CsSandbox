using System;
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
				return null;

			var userId = _users.GetUser(model.Token);
			if (userId == null)
				return null;
			try
			{
				var submission = _submissions.AddSubmission(userId, model);
				Task.Run((Action)new Worker(submission.Id, model).Run);
				return submission.Id;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		[HttpGet]
		[Route("GetSubmissionStatus")]
		public SubmissionStatus GetSubmissionStatus(string id, string token)
		{
			if (!ModelState.IsValid)
				return SubmissionStatus.Error;

			return _submissions.GetStatus(_users.GetUser(token), id);
		}

		[HttpGet]
		[Route("GetSubmissionDetails")]
		public PublicSubmissionDetails GetSubmissionDetails(string id, string token)
		{
			if (!ModelState.IsValid)
				return null;

			var userId = _users.GetUser(token);
			var details = _submissions.GetDetails(userId, id);
			return details.ToPublic();
		}
    }
}
