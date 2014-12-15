using System.Threading.Tasks;
using CsSandboxApi;
using NUnit.Framework;

namespace CsSandboxTests
{
	static class Tests
	{
		[TestCase(@"namespace Test { public class Program { static public void Main() { return ; } } }", "", "", "")]
		[TestCase(@"using System; public class M{static public void Main(){System.Console.WriteLine(42);}}", "", "42\r\n", "")]
		[TestCase(@"using System; class M{static void Main(){System.Console.WriteLine(Tuple.Create(1, 2));}}", "", "(1, 2)\r\n", "")]
		[TestCase(@"using System; public class M{static public void Main(){System.Console.Error.WriteLine(42);}}", "", "", "42\r\n")]
		[TestCase(@"using System; public class M{static public void Main(){System.Console.WriteLine(Console.ReadLine());}}", "asdfasdf", "asdfasdf\r\n", "")]
		[TestCase(@"using System; class M{static void Main(){ try{throw new Exception();}catch{Console.WriteLine('!');}}}", "", "!\r\n", "")]
		public static async void TestOk(string code, string input, string output, string error)
		{
			var details = await GetDetails(code, input);
			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.AreEqual(output, details.Output);
			Assert.AreEqual(error, details.Error);
		}

		[TestCase("namespace Test { public class Program { static public void Main() { return 0; } } }")]
		public static async void TestCompilationError(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.ComplationError, details.Verdict);
			Assert.IsNotNullOrEmpty(details.CompilationError);
		}

		[TestCase("using System; using System.IO; namespace UntrustedCode { public class UntrustedClass { public static void Main() { Directory.GetFiles(@\"c:\\\"); }}}")]
		public static async void TestSecurityException(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.SecurityException, details.Verdict);
			Assert.IsNullOrEmpty(details.Error);
		}

		[TestCase("using System; namespace Test { public class Program { static public void Main() { if (1 + 1 == 2) throw new Exception();}}}")]
		public static async void TestRuntimeError(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.RuntimeError, details.Verdict);
			Assert.IsNotNullOrEmpty(details.Error);
		}

		[TestCase("using System; class Program { static void Main() { var s = new string('*', 4000); Console.WriteLine(s); }}")]
		[TestCase("using System; class Program { static void Main() { var s = new string('*', 4000); Console.Write(s); Console.WriteLine(); }}")]
		public static async void TestOutputLimitError(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.OutputLimit, details.Verdict);
			Assert.IsNotNullOrEmpty(details.Error);
		}

		[TestCase(@"using System; class Program { static void Main() { var s = new string('*', 4000); Console.Write(s); }}")]
		public static async void TestOutputLimit(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.AreEqual(new string('*', 4000), details.Output);
			Assert.IsNullOrEmpty(details.Error);
			Assert.IsNullOrEmpty(details.CompilationError);
		}

		[TestCase(@"using System; class Program { static void Main() { int a = 0; while(true) { ++a; } }}")]
		[TestCase(@"using System.Threading; class Program{ private static void Main() { Thread.Sleep(3000); }}")]
		public static async void TestTimeLimit(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.TimeLimit, details.Verdict);
			Assert.IsNotNullOrEmpty(details.Error);
		}

		private static async Task<PublicSubmissionDetails> GetDetails(string code, string input)
		{
			var client = new CsSandboxClient("tester");
			var submission = await client.Submit(code, input);
			Assert.NotNull(submission);

			var count = 0;
			var lastStatus = await submission.GetStatus();
			while (lastStatus != SubmissionStatus.Done && count < 30)
			{
				await Task.Delay(1000);
				++count;
				lastStatus = await submission.GetStatus();
			}
			Assert.Less(count, 30, "too slow...");
			var details = await submission.GetDetails();

			Assert.NotNull(details);
			Assert.AreEqual(SubmissionStatus.Done, details.Status);
			Assert.AreEqual(code, details.Code);
			Assert.AreEqual(input, details.Input);
			Assert.True(details.NeedRun);

			return details;
		}
	}
}

