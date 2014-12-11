using CsSandbox.Sandbox;
using CsSandboxApi;
using NUnit.Framework;

namespace CsSandbox.Tests
{
	public static class RunTests
	{
		[TestCase(@"namespace Test { public class Program { static public void Main() { return ; } } }", "", "")]
		[TestCase(@"namespace Test { public class Program { static public void Main() { return 0; } } }", "", "")]
		[TestCase(@"using System; public class M{static public void Main(){System.Console.WriteLine(42);}}", "", "42\n")]
		[TestCase(@"using System; class M{static void Main(){System.Console.WriteLine(Tuple.Create(1, 2));}}", "", "(1, 2)\n")]
		[TestCase("using System; using System.IO; namespace UntrustedCode { public class UntrustedClass { public static void Main() { System.Console.WriteLine(File.ReadAllText(\"D:\\\\bulat\\\\log\"));}}}", "", "")]
		public static void TestRun(string code, string input, string output)
		{
			new Worker("test", new SubmissionModel
			{
				Code = code,
				Input = input,
				NeedRun = true
			}).Run();
		}
	}
}