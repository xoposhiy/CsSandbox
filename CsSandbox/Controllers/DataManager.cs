using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web;
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

		private string GetUserId(HttpCookie cookie)
		{
			var userId = FindUserId(cookie);
			if (userId == null)
				throw new HttpResponseException(HttpStatusCode.Unauthorized);
			return userId;
		}

		public string FindUserId(string token)
		{
			return _users.FindUser(token);
		}

		public string FindUserId(HttpCookie cookie)
		{
			if (cookie == null)
				return null;
			var token = cookie.Value;
			return string.IsNullOrWhiteSpace(token) ? null : _users.FindUser(token);
		}

		public SubmissionDetails GetDetails(string id, string token)
		{
			var userId = GetUserId(token);
			return GetDetailsByUserId(id, userId);
		}

		public SubmissionDetails GetDetails(string id, HttpCookie cookie)
		{
			var userId = GetUserId(cookie);
			return GetDetailsByUserId(id, userId);
		}

		private SubmissionDetails GetDetailsByUserId(string id, string userId)
		{
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

		public IEnumerable<SubmissionDetails> GetAllSubmission(HttpCookie cookie, int max, int skip)
		{
			var userId = GetUserId(cookie);
			return _sandbox.GetAllSubmissions(userId, max, skip);
		}
	}
}