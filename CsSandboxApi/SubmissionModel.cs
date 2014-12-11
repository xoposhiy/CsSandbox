using System.ComponentModel.DataAnnotations;

namespace CsSandboxApi
{
	public class SubmissionModel
	{
		[Required]
		[StringLength(512)]
		public string Token { get; set; }

		[Required]
		[StringLength(4000, ErrorMessage = "Code length is too large")]
		public string Code { get; set; }

		[StringLength(4000, ErrorMessage = "Input length is too large")]
		public string Input { get; set; }

		public bool NeedRun { get; set; }
	}
}