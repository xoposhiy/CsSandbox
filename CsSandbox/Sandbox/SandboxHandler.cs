using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
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

		public SubmissionStatus GetStatus(string userId, string id)
		{
			var status = _submissions.GetStatus(userId, id);

			if (status == SubmissionStatus.NotFound)
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
				{
					ReasonPhrase = "Посылка с указанным ID не найдена."
				});

			if (status == SubmissionStatus.AccessDeny)
				throw new HttpResponseException(HttpStatusCode.Forbidden);

			return status;
		}

		public PublicSubmissionDetails FindDetails(string userId, string id)
		{
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
	}
}