using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CsSandbox.DataContext;
using CsSandbox.Models;
using CsSandbox.Sandbox;
using CsSandboxApi;

namespace CsSandbox.Controllers
{
	public class DataManager
	{
		private readonly UserRepo _users = new UserRepo();
		private readonly SandboxHandler _sandbox = new SandboxHandler();

		private string GetUserId(string token)
		{
			var userId = _users.FindUser(token);
			if (userId == null)
				throw new HttpResponseException(HttpStatusCode.Unauthorized);
			return userId;
		}

		public SubmissionDetails GetDetails(string id, string token)
		{
			var userId = GetUserId(token);
			var details = _sandbox.FindDetails(id);

			if (details == null)
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
				{
					ReasonPhrase = "There isn't submission with given Id."
				});

			if (details.UserId != userId)
				throw new HttpResponseException(HttpStatusCode.Forbidden);

			return details;
		}

		public string CreateSandbox(SubmissionModel model)
		{
			var userId = GetUserId(model.Token);
			return _sandbox.Create(userId, model);
		}

		public IEnumerable<SubmissionDetails> GetAllSubmission(string token, int max, int skip)
		{
			var userId = GetUserId(token);
			return _sandbox.GetAllSubmissions(userId, max, skip);
		}
	}
}