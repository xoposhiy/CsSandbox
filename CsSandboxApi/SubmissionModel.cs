using System.ComponentModel.DataAnnotations;

namespace CsSandboxApi
{
	public class SubmissionModel
	{
		/// <summary>
		/// Autherization token
		/// </summary>
		[Required]
		[StringLength(512)]
		public string Token { get; set; }

		[Required]
		[StringLength(64 * 1024, ErrorMessage = "Code length is too large")]
		public string Code { get; set; }

		[StringLength(10 * 1024 * 1024, ErrorMessage = "Input length is too large")]
		public string Input { get; set; }

		/// <summary>
		/// Submission name displayed in submission list.
		/// </summary>
		[StringLength(1024, ErrorMessage = "Info length is too large")]
		public string HumanName { get; set; }

		/// <summary>
		/// Only compile if false.
		/// </summary>
		public bool NeedRun { get; set; }
	}
}