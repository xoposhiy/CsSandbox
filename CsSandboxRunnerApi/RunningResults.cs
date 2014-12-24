using CsSandboxApi;

namespace CsSandboxRunnerApi
{
	public class RunningResults
	{
		public string CompilationOutput { get; set; }
		public Verdict Verdict { get; set; }
		public string Output { get; set; }
		public string Error { get; set; }
	}
}