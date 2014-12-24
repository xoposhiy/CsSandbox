using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using CsSandbox.Models;
using CsSandboxApi;
using CsSandboxRunnerApi;

namespace CsSandbox.DataContext
{
	public interface ISubmissionRepo
	{
		SubmissionDetails AddSubmission(string userId, SubmissionModel submission);
		SubmissionDetails FindDetails(string id);
		void SetCompilationInfo(string id, bool isCompilationError, string compilationOutput);
		void SetRunInfo(string id, string stdout, string stderr);
		void SetExceptionResult(string id, SolutionException ex);
		void SetExceptionResult(string id, OutOfMemoryException ex);
		void SetExceptionResult(string id, SecurityException ex);
		void SetExceptionResult(string id, MemberAccessException ex);
		void SetExceptionResult(string id, Exception ex);
		void SetExceptionResult(string id, TargetInvocationException exception);
		void SetDone(string id);
		void SetSandboxException(string id, string message);
		IEnumerable<SubmissionDetails> GetAllSubmissions(string userId, int max, int skip);
	}
}
