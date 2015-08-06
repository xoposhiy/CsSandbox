using System.Collections.Generic;
using CsSandbox.DataContext;
using CsSandbox.Models;
using CsSandboxApi;
using CsSandboxApi.Runner;

namespace CsSandbox.Sandbox
{
	public class SandboxHandler
	{
		private readonly ISubmissionRepo _submissions;

		public SandboxHandler() : this(new SubmissionRepo())
		{
		}

		public SandboxHandler(ISubmissionRepo submissions)
		{
			_submissions = submissions;
		}

		public string Create(string userId, SubmissionModel model)
		{
			var id = _submissions.AddSubmission(userId, model).Id;
			return id;
		}

		public void SaveResult(RunningResults result)
		{
			_submissions.SaveResults(result);
		}

		public void SaveResults(List<RunningResults> results)
		{
			_submissions.SaveAllResults(results);
		}

		public SubmissionDetails FindDetails(string id)
		{
			return _submissions.FindDetails(id);
		}

		public IEnumerable<SubmissionDetails> GetAllSubmissions(string userId, int max, int skip)
		{
			return _submissions.GetAllSubmissions(userId, max, skip);
		}
	}
}