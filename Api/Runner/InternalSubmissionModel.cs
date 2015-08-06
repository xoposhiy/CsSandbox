namespace CsSandboxApi.Runner
{
	public class InternalSubmissionModel
	{
		public string Id;
		public string Code;
		public string Input;
		public bool NeedRun;

		public override string ToString()
		{
			return string.Format("Id: {0}, NeedRun: {1}", Id, NeedRun);
		}
	}
}