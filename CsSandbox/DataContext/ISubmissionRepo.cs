using System;
using System.Reflection;
using System.Security;
using CsSandbox.Models;
using CsSandbox.Sandbox;
using CsSandboxApi;

namespace CsSandbox.DataContext
{
	public interface ISubmissionRepo
	{
		SubmissionDetails AddSubmission(string userId, SubmissionModel submission);
		SubmissionStatus GetStatus(string userId, string id);
		SubmissionDetails FindDetails(string id);
		void SetCompilationInfo(string id, bool isCompilationError, string compilationOutput);
		void SetRunInfo(string id, string stdout, string stderr);
		void SetExceptionResult(string id, SolutionException ex);
		void SetExceptionResult(string id, OutOfMemoryException ex);
		void SetExceptionResult(string id, SecurityException ex);
		void SetExceptionResult(string id, Exception ex);
		void SetExceptionResult(string id, TargetInvocationException exception);
		void SetDone(string id);
		void SetSandboxException(string id, string message);
	}
}
