using System.Net;
using System.Web.Http;
using CsSandbox.Models;
using CsSandboxApi;

namespace CsSandbox.Controllers
{
    public class SandboxController : ApiController
    {
	    private readonly DataManager _dataManager = new DataManager();

		/// <summary> Create and place submission to queue. </summary>
		/// <returns> Return SubmissionID used in determining status and details of submission. </returns>
		[HttpPost]
		[Route("CreateSubmission")]
		public string CreateSubmission(SubmissionModel model)
		{
			if (!ModelState.IsValid)
				throw new HttpResponseException(HttpStatusCode.BadRequest);

			return _dataManager.CreateSandbox(model);
		}

		/// <param name="id"> SubmissionID</param>
		/// <param name="token"> Autherization token </param>
	    [HttpGet]
		[Route("GetSubmissionStatus")]
		public SubmissionStatus GetSubmissionStatus(string id, string token)
	    {
			var details = _dataManager.GetDetails(id, token);
			return details.Status;
		}

		/// <param name="id"> SubmissionID</param>
		/// <param name="token"> Autherization token </param>
		[HttpGet]
		[Route("GetSubmissionDetails")]
		public PublicSubmissionDetails GetSubmissionDetails(string id, string token)
		{
			var details = _dataManager.GetDetails(id, token);
			return details.ToPublic();
		}

    }
}
