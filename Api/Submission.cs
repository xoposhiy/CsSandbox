using System.Threading.Tasks;

namespace CsSandboxApi
{
	public class Submission
	{
		private readonly string _submissionId;
		private readonly CsSandboxClient _csSandboxClient;

		public Submission(string submissionId, CsSandboxClient csSandboxClient)
		{
			_submissionId = submissionId.Trim('"');
			_csSandboxClient = csSandboxClient;
		}

		public async Task<SubmissionStatus> GetStatus()
		{
			return await _csSandboxClient.GetSubmissionStatus(_submissionId);
		}

		public async Task<PublicSubmissionDetails> GetDetails()
		{
			return await _csSandboxClient.GetSubmissionDetails(_submissionId);
		}
	}
}