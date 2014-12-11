using System;
using System.Data.Entity.Migrations;
using System.Reflection;
using System.Security;
using CsSandbox.Models;
using CsSandboxApi;

namespace CsSandbox.DataContext
{
	public class SubmissionRepo
	{
		private readonly CsSandboxDb db;

		public SubmissionRepo() : this(new CsSandboxDb())
		{
			
		}

		private SubmissionRepo(CsSandboxDb db)
		{
			this.db = db;
		}


		public SubmissionDetails AddSubmission(string userId, SubmissionModel submission)
		{
			var submissionDetails = db.Submission.Add(new SubmissionDetails
			{
				Id = Guid.NewGuid().ToString(),
				UserId = userId,
				Code = submission.Code,
				Input = submission.Input,
				Status = SubmissionStatus.Waiting,
				Verdict = Verdict.NA,
				Timestamp = DateTime.Now,
				NeedRun = submission.NeedRun,
			});

			db.SaveChanges(); 

			return submissionDetails;
		}

		public SubmissionStatus GetStatus(string userId, string id)
		{
			var submission = db.Submission.Find(id);
			if (submission == null)
				return SubmissionStatus.NotFound;
			if (submission.UserId != userId)
				return SubmissionStatus.AccessDeny;
			return submission.Status;
		}

		public SubmissionDetails GetDetails(string userId, string id)
		{
			var submission = db.Submission.Find(id);
			if (submission == null || submission.UserId != userId)
				return null;
			return submission;
		}

		public void SetCompilationInfo(string id, bool isCompilationError, string compilationOutput)
		{
			var submittion = db.Submission.Find(id);
			if (isCompilationError)
				submittion.Verdict = Verdict.ComplationError;
			submittion.CompilationOutput = compilationOutput;
			submittion.Status = SubmissionStatus.Done;
			db.Submission.AddOrUpdate(submittion);
			db.SaveChanges();
		}

		public void SetRunInfo(string id, string stdout, string stderr)
		{
			var submittion = db.Submission.Find(id);
			submittion.Output = stdout;
			submittion.Error = stderr;
			submittion.Verdict = Verdict.Ok;
			submittion.Status = SubmissionStatus.Done;
			db.Submission.AddOrUpdate(submittion);
			db.SaveChanges();
		}

		private void SetSecurityException(string id)
		{
			var submittion = db.Submission.Find(id);
			submittion.Status = SubmissionStatus.Done;
			submittion.Verdict = Verdict.SecurityException;
			db.Submission.AddOrUpdate(submittion);
			db.SaveChanges();
		}

		private void SetRuntimeError(string id, string message)
		{
			var submittion = db.Submission.Find(id);
			submittion.Status = SubmissionStatus.Done;
			submittion.Verdict = Verdict.RuntimeError;
			submittion.Error = message;
			db.Submission.AddOrUpdate(submittion);
			db.SaveChanges();
		}

		public void SetTargetInvocationException(string id, TargetInvocationException exception)
		{
			var innerException = exception.InnerException;
			if (innerException is SecurityException)
				SetSecurityException(id);
			else
				SetRuntimeError(id, innerException.ToString());
		}

		public void SetException(string id, string message)
		{
			var submittion = db.Submission.Find(id);
			submittion.Status = SubmissionStatus.Done;
			submittion.Verdict = Verdict.SandboxError;
			submittion.Error = message;
			db.Submission.AddOrUpdate(submittion);
			db.SaveChanges();			
		}
	}
}