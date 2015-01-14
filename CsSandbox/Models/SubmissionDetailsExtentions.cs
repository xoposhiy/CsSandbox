using CsSandboxApi;

namespace CsSandbox.Models
{
	public static class SubmissionDetailsExtentions
	{
		public static PublicSubmissionDetails ToPublic(this SubmissionDetails details)
		{
			return new PublicSubmissionDetails(
				details.Id,
				details.Status,
				details.Code,
				details.Input,
				details.Timestamp,
				details.NeedRun,
				details.Verdict,
				details.CompilationOutput,
				details.Output,
				details.Error,
				details.HumanName);
		}
	}
}