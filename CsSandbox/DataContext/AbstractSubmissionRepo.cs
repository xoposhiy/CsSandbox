using System;
using System.Collections.Generic;
using CsSandbox.Models;
using CsSandboxApi;
using CsSandboxRunnerApi;

namespace CsSandbox.DataContext
{
	public abstract class AbstractSubmissionRepo : ISubmissionRepo
	{
		abstract protected SubmissionDetails Find(string id);
		abstract protected void Save(SubmissionDetails submission);
		abstract public IEnumerable<SubmissionDetails> GetAllSubmissions(string userId, int max, int skip);
		abstract public SubmissionDetails FindUnhandled();

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

		public SubmissionDetails FindDetails(string id)
		{
			return Find(id);
		}

		public void SaveResults(string id, RunningResults result)
		{
			var submission = Find(id);
			submission.CompilationOutput = result.CompilationOutput;
			submission.Verdict = result.Verdict;
			submission.Output = result.Output;
			submission.Error = result.Error;
			submission.Status = SubmissionStatus.Done;
			Save(submission);
		}

	}
}