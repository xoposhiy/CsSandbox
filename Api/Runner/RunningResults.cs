namespace CsSandboxApi.Runner
{
	public class RunningResults
	{
		public string Id { get; set; }
		public string CompilationOutput { get; set; }
		public Verdict Verdict { get; set; }
		public string Output { get; set; }
		public string Error { get; set; }

		public override string ToString()
		{
			return string.Format("Id: {0}, Verdict: {1}: {2}", Id, Verdict, 
				Verdict == Verdict.SandboxError ? Error : Output);
		}
	}
}