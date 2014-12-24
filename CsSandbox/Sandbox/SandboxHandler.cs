using System.Collections.Generic;
using CsSandbox.DataContext;
using CsSandbox.Models;
using CsSandboxApi;
using CsSandboxRunnerApi;

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

		public void SaveResult(string id, IRunningResult result)
		{
			_submissions.SetCompilationInfo(id, result.IsCompilationError, result.CompilationOutput);
			SaveResult(id, (dynamic)result);
			_submissions.SetDone(id);
		}

		private void SaveResult(string id, CompilationOnly result)
		{
		}

		private void SaveResult(string id, HasException result)
		{
			_submissions.SetExceptionResult(id, (dynamic)result.Exception);
		}

		private void SaveResult(string id, NormalRun result)
		{
			_submissions.SetRunInfo(id, result.Stdout, result.Stderr);
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