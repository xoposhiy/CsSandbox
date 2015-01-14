using System;
using System.ComponentModel.DataAnnotations;
using CsSandboxApi;

namespace CsSandbox.Models
{
	public class SubmissionDetails
	{
		[Required]
		[Key]
		[StringLength(64)]
		public string Id { get; set; }

		[Required]
		[StringLength(64 * 1024)]
		public string Code { get; set; }

		public virtual User User { get; set; }

		[Required]
		public string UserId { get; set; }

		[StringLength(10 * 1024 * 1024)]
		public string Input { get; set; }

		[Required]
		public DateTime Timestamp { get; set; }

		[Required]
		public SubmissionStatus Status { get; set; }

		public Verdict Verdict { get; set; }

		[StringLength(10 * 1024 * 1024)]
		public string CompilationOutput { get; set; }

		[StringLength(10 * 1024 * 1024)]
		public string Output { get; set; }

		[StringLength(10 * 1024 * 1024)]
		public string Error { get; set; }

		[StringLength(1024)]
		public string HumanName { get; set; }

		public bool NeedRun { get; set; }
	}
}