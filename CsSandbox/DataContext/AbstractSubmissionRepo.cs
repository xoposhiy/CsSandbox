using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using CsSandbox.Models;
using CsSandbox.Sandbox;
using CsSandboxApi;

namespace CsSandbox.DataContext
{
	public abstract class AbstractSubmissionRepo : ISubmissionRepo
	{
		protected abstract SubmissionDetails Find(string id);
		protected abstract void Save(SubmissionDetails submission);

		public SubmissionDetails AddSubmission(string userId, SubmissionModel submission)
		{
			var submissionDetail = new SubmissionDetails
			{
				Id = Guid.NewGuid().ToString(),
				UserId = userId,
				Code = submission.Code,
				Input = submission.Input,
				Status = SubmissionStatus.Waiting,
				Verdict = Verdict.NA,
				Timestamp = DateTime.Now,
				NeedRun = submission.NeedRun,
			};
			Save(submissionDetail);

			return submissionDetail;
		}

		public SubmissionStatus GetStatus(string userId, string id)
		{
			var submission = Find(id);
			if (submission == null)
				return SubmissionStatus.NotFound;
			if (submission.UserId != userId)
				return SubmissionStatus.AccessDeny;
			return submission.Status;
		}

		public SubmissionDetails FindDetails(string id)
		{
			return Find(id);
		}

		public void SetCompilationInfo(string id, bool isCompilationError, string compilationOutput)
		{
			var submission = Find(id);
			if (isCompilationError)
				submission.Verdict = Verdict.ComplationError;
			submission.CompilationOutput = compilationOutput;
			Save(submission);
		}

		public void SetRunInfo(string id, string stdout, string stderr)
		{
			var submission = Find(id);
			submission.Output = stdout;
			submission.Error = stderr;
			submission.Verdict = Verdict.Ok;
			Save(submission);
		}

		public void SetExceptionResult(string id, SolutionException ex)
		{
			SetExceptionResult(id, ex.Verdict, ex.Message);
		}

		public void SetExceptionResult(string id, OutOfMemoryException ex)
		{
			SetExceptionResult(id, (Exception)ex);
		}

		public void SetExceptionResult(string id, SecurityException ex)
		{
			SetExceptionResult(id, Verdict.SecurityException, null);
		}

		public void SetExceptionResult(string id, Exception ex)
		{
			SetExceptionResult(id, Verdict.RuntimeError, ex.ToString());
		}

		public void SetExceptionResult(string id, TargetInvocationException exception)
		{
			SetExceptionResult(id, (dynamic)exception.InnerException);
		}

		public void SetDone(string id)
		{
			var submission = Find(id);
			submission.Status = SubmissionStatus.Done;
			Save(submission);
		}

		public void SetSandboxException(string id, string message)
		{
			var submission = Find(id);
			submission.Verdict = Verdict.SandboxError;
			submission.Error = message;
			Save(submission);
		}

		public abstract IEnumerable<SubmissionDetails> GetAllSubmissions(string userId, int max, int skip);

		private void SetExceptionResult(string id, Verdict verdict, string message)
		{
			var submission = Find(id);
			submission.Verdict = verdict;
			submission.Error = message;
			Save(submission);
		}
	}
}