using System.Net;
using System.Web.Http;
using CsSandbox.Models;
using CsSandboxApi;

namespace CsSandbox.Controllers
{
    public class SandboxController : ApiController
    {
	    private readonly DataManager _dataManager = new DataManager();

		[HttpPost]
		[Route("CreateSubmission")]
		public string CreateSubmission(SubmissionModel model)
		{
			if (!ModelState.IsValid)
				throw new HttpResponseException(HttpStatusCode.BadRequest);

			return _dataManager.CreateSandbox(model);
		}

	    [HttpGet]
		[Route("GetSubmissionStatus")]
		public SubmissionStatus GetSubmissionStatus(string id, string token)
	    {
			var details = _dataManager.GetDetails(id, token);
			return details.Status;
		}

	    [HttpGet]
		[Route("GetSubmissionDetails")]
		public PublicSubmissionDetails GetSubmissionDetails(string id, string token)
		{
			var details = _dataManager.GetDetails(id, token);
			return details.ToPublic();
		}

    }
}
