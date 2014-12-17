using System;
using System.Threading.Tasks;
using CsSandboxApi;
using NUnit.Framework;

namespace CsSandbox.Tests
{
	[EnableApplicationDomainResourceMonitoring]
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
		public static async void TestTimeLimitError(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.TimeLimit, details.Verdict);
			Assert.IsNotNullOrEmpty(details.Error);
		}

		[TestCase(@"using System; class Program { static void Main() { var a = new byte[65 * 1024 * 1024]; Console.WriteLine(a); }}")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { static List<byte> mem = new List<byte>(65 * 1024 * 1024); static void Main() { Console.WriteLine(mem); }}")] // throw TypeInitializationException
		[TestCase(@"using System; using System.Collections.Generic; class Program { static void Main() { var mem = new List<byte>(65 * 1024 * 1024); Console.WriteLine(mem); }}")]
		public static async void TestMemoryLimitError(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.MemoryLimit, details.Verdict);
			Assert.IsNotNullOrEmpty(details.Error);
		}

		[TestCase(@"using System; class Program { static void Main() { var a = new byte[63 * 1024 * 1024]; Console.WriteLine(a); }}")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { static List<byte> mem = new List<byte>(63 * 1024 * 1024); static void Main() { Console.WriteLine(mem); }}")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { static void Main() { var mem = new List<byte>(63 * 1024 * 1024); Console.WriteLine(mem); }}")]
		public static async void TestMemoryLimit(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.IsNullOrEmpty(details.Error);
			Assert.IsNotNullOrEmpty(details.Output);
		}
		
		private static async Task<PublicSubmissionDetails> GetDetails(string code, string input)
		{
			const string userId = "tester";
			var client = new Sandbox.SandboxHandler(new DictionarySubmissionRepo());
			var id = client.Create(userId, new SubmissionModel
			{
				Code = code,
				Input = input,
				NeedRun = true,
			});
			Assert.NotNull(id);

			var count = 50;
			var lastStatus = client.GetStatus(userId, id);
			while (lastStatus != SubmissionStatus.Done && count >= 0)
			{
				await Task.Delay(100);
				--count;
				lastStatus = client.GetStatus(userId, id);
			}
			Assert.GreaterOrEqual(count, 0, "too slow...");
			var details = client.FindDetails(userId, id);

			Assert.NotNull(details);
			Assert.AreEqual(SubmissionStatus.Done, details.Status);
			Assert.AreEqual(code, details.Code);
			Assert.AreEqual(input, details.Input);
			Assert.True(details.NeedRun);

			return details;
		}
	}

	internal class EnableApplicationDomainResourceMonitoring : Attribute, ITestAction
	{
		public void BeforeTest(TestDetails testDetails)
		{
			AppDomain.MonitoringIsEnabled = true;
		}

		public void AfterTest(TestDetails testDetails)
		{
		}

		public ActionTargets Targets
		{
			get { return ActionTargets.Test; }
		}
	}
}

