using System.Net;
using System.Net.Http;
using System.Web.Http;
using CsSandbox.DataContext;
using CsSandbox.Models;
using CsSandbox.Sandbox;
using CsSandboxRunnerApi;

namespace CsSandbox.Controllers
{
    public class RunnerController : ApiController
    {
		private readonly UserRepo _user = new UserRepo();
		private readonly SubmissionRepo _submissionsRepo = new SubmissionRepo();
		private readonly SandboxHandler _sandboxHandler = new SandboxHandler();

		[HttpGet]
		[Route("TryGetSubmission")]
	    public InternalSubmissionModel TryGetSubmission(string token)
	    {
			CheckRunner(token);

		    var submission = _submissionsRepo.FindUnhandled();
		    if (submission == null)
			    return null;
			return new InternalSubmissionModel
			{
				Id = submission.Id,
				Code = submission.Code,
				Input = submission.Input,
				NeedRun = submission.NeedRun
			};
	    }

	    [HttpPost]
		[Route("PostResult")]
	    public void PostResult([FromUri]string token, [FromUri]string id, RunningResults result)
	    {
			if (!ModelState.IsValid)
				throw new HttpResponseException(HttpStatusCode.BadRequest);
			CheckRunner(token);

			_sandboxHandler.SaveResult(id, result);
	    }

	    private void CheckRunner(string token)
	    {
			var roles = _user.FindRoles(token);
			if (!roles.Contains(Role.Runner))
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Forbidden));
	    }
    }
}
