using System.Collections.Generic;
using CsSandboxApi;

namespace CsSandbox.Models
{
	public class SubmissionsListModel
	{
		public string Token ;
		public IEnumerable<PublicSubmissionDetails> Submissions;
	}
}