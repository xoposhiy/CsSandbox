using System;

namespace CsSandboxApi
{
	public class PublicSubmissionDetails
	{
		public readonly string Id;
		public readonly SubmissionStatus Status;

		public readonly string Code;
		public readonly string Input;
		public readonly DateTime Timestamp;
		public readonly bool NeedRun;

		public readonly Verdict Verdict;

		public readonly string CompilationInfo;

		public readonly string Output;
		public readonly string Error;

		public PublicSubmissionDetails(string id, SubmissionStatus status, string code, string input, DateTime timestamp, bool needRun, Verdict verdict, string compilationInfo, string output, string error)
		{
			Id = id;
			Status = status;
			Code = code;
			Input = input;
			Timestamp = timestamp;
			NeedRun = needRun;
			Verdict = verdict;
			CompilationInfo = compilationInfo;
			Output = output;
			Error = error;
		}
	}
}