using System;
using System.Collections.Generic;
using CsSandbox.Models;
using CsSandboxApi;
using CsSandboxRunnerApi;

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

			Unhandled.Enqueue(submissionDetail.Id);

			return submissionDetail;
		}

		public SubmissionDetails FindDetails(string id)
		{
			return Find(id);
		}

		public abstract IEnumerable<SubmissionDetails> GetAllSubmissions(string userId, int max, int skip);

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

		private static readonly Queue<string> Unhandled = new Queue<string>();

		public SubmissionDetails FindUnhandled()
		{
			if (Unhandled.Count == 0)
				return null;
			var id = Unhandled.Dequeue();
			var submission = FindDetails(id);
			submission.Status = SubmissionStatus.Running;
			Save(submission);
			return submission;
		}
	}
}