using System;
using System.Threading.Tasks;
using CsSandbox.DataContext;
using CsSandbox.Models;
using CsSandboxApi;

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

			Task.Run(() => StartSandbox(id, model));

			return id;
		}

		private void StartSandbox(string id, SubmissionModel model)
		{
			IRunningResult result;
			try
			{
				result = new SandboxRunner(id, model).Run();
			}
			catch (Exception ex)
			{
				_submissions.SetSandboxException(id, ex.ToString());
				return;
			}

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

	}
}