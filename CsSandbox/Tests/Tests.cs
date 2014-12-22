using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CsSandbox.Models;
using CsSandboxApi;
using NUnit.Framework;

namespace CsSandbox.Tests
{
	[EnableApplicationDomainResourceMonitoring]
	static class Tests
	{
		private const int OutputLimit = 10*1024*1024;

		[TestCase(@"namespace Test { public class Program { static public void Main() { return ; } } }", "", "", "", TestName = "Public class and Main")]
		[TestCase(@"using System; public class M{static public void Main(){Console.WriteLine(42);}}", "", "42\r\n", "", TestName = "Output")]
		[TestCase(@"using System; public class M{static public void Main(){Console.WriteLine(4.2);}}", "", "4.2\r\n", "", TestName = "Output invariant culture")]
		[TestCase(@"using System; public class M{static public void Main(){Console.Error.WriteLine(4.2);}}", "", "", "4.2\r\n", TestName = "Error invariant culture")]
		[TestCase(@"using System; using System.Globalization; public class M{static public void Main(){var a = 4.2; Console.WriteLine(a.ToString(CultureInfo.InvariantCulture));}}", "", "4.2\r\n", "", TestName = "Set Invariant Culture in ToString")]
		[TestCase(@"using System; using System.Globalization; class A { private static void Main() { Console.WriteLine(CultureInfo.CurrentCulture.EnglishName); } }", "", "Invariant Language (Invariant Country)\r\n", "", TestName = "Get Globlal CultureInfo")]
		[TestCase(@"using System; class M{static void Main(){System.Console.WriteLine(Tuple.Create(1, 2));}}", "", "(1, 2)\r\n", "", TestName = "Tuple")]
		[TestCase(@"using System; public class M{static public void Main(){System.Console.Error.WriteLine(42);}}", "", "", "42\r\n", TestName = "Output error")]
		[TestCase(@"using System; public class M{static public void Main(){System.Console.WriteLine(Console.ReadLine());}}", "asdfasdf", "asdfasdf\r\n", "", TestName = "Read")]
		[TestCase(@"using System; class M{static void Main(){ try{throw new Exception();}catch{Console.WriteLine('!');}}}", "", "!\r\n", "", TestName = "try/catch")]
		[TestCase("using System; using System.Linq; using System.Collections.Generic; class A { static void Main() { var a = new List<String>{\"Str2\"}; foreach(var b in a.Select(s => s.ToLower())) Console.WriteLine(b); } }", "", "str2\r\n", "", TestName = "Collections and LINQ")]
		public static async void TestOk(string code, string input, string output, string error)
		{
			var details = await GetDetails(code, input);

			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.AreEqual(output, details.Output);
			Assert.AreEqual(error, details.Error);
		}

		[TestCase("namespace Test { public class Program { static public void Main() { return 0; } } }", TestName = "Return int in void Main")]
		public static async void TestCompilationError(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.CompilationError, details.Verdict);
			Assert.IsNotNullOrEmpty(details.CompilationInfo);
		}

		[TestCase("using System; using System.IO; namespace UntrustedCode { public class UntrustedClass { public static void Main() { Directory.GetFiles(@\"c:\\\"); }}}", TestName = "Get files list")]
		[TestCase("using System; class A { static void Main() { foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) Console.WriteLine(assembly.GetName().Name); }}", TestName = "Loaded assemblies list")]
		[TestCase("using System; using System.Threading; using System.Linq; using System.Reflection; using System.Security; [SecurityCritical] class A { static void Main() { var assemblies = Thread.GetDomain().GetAssemblies(); var ass = assemblies.FirstOrDefault(assembly => assembly.ToString().Contains(\"CsSandbox\")); var type = ass.GetType(\"CsSandbox.Sandbox.Sandboxer\", true, true); if(type == null) Console.WriteLine(\"lol\"); else type.InvokeMember(\"MustDontWork\", BindingFlags.InvokeMethod, null, null, null); }}", TestName = "Method in sandboxer")]
		[TestCase("using System; using System.Threading; using System.Linq; using System.Reflection; using System.Security; [SecurityCritical] class A { static void Main() { var assemblies = Thread.GetDomain().GetAssemblies(); var ass = assemblies.FirstOrDefault(assembly => assembly.ToString().Contains(\"CsSandbox\")); var type = ass.GetType(\"CsSandbox.Sandbox.Sandboxer\", true, true); if(type == null) Console.WriteLine(\"lol\"); else type.InvokeMember(\"Secret\", BindingFlags.GetField, null, null, null);; }}", TestName = "Field in sandboxer")]
		public static async void TestSecurityException(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.SecurityException, details.Verdict);
			Assert.IsNullOrEmpty(details.Error);
		}

		[TestCase("using System; namespace Test { public class Program { static public void Main() { throw new Exception(); }}}", TestName = "throw")]
		public static async void TestRuntimeError(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.RuntimeError, details.Verdict);
			Assert.IsNotNullOrEmpty(details.Error);
		}

		[TestCase("using System; class Program { static void Main() { var s = new string('*', $limit + 1); Console.Write(s); }}", TestName = "Output")]
		[TestCase("using System; class Program { static void Main() { var s = new string('*', $limit); Console.WriteLine(s); }}", TestName = "Output + newline")]
		[TestCase("using System; class Program { static void Main() { var s = new string('*', $limit); Console.Write(s); Console.WriteLine(); }}", TestName = "Output + newline explicit")]
		public static async void TestOutputLimitError(string code)
		{
			var details = await GetDetails(code.Replace("$limit", OutputLimit.ToString(CultureInfo.InvariantCulture)), "");

			Assert.AreEqual(Verdict.OutputLimit, details.Verdict);
			Assert.IsNotNullOrEmpty(details.Error);
		}

		[TestCase(@"using System; class Program { static void Main() { var s = new string('*', $limit); Console.Write(s); }}", TestName = "Output")]
		public static async void TestOutputLimit(string code)
		{
			var details = await GetDetails(code.Replace("$limit", OutputLimit.ToString(CultureInfo.InvariantCulture)), "");

			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.AreEqual(new string('*', OutputLimit), details.Output);
			Assert.IsNullOrEmpty(details.Error);
			Assert.IsNullOrEmpty(details.CompilationInfo);
		}

		[TestCase(@"using System; class Program { static void Main() { int a = 0; while(true) { ++a; } }}", TestName = "Infinty loop")]
		[TestCase(@"using System.Threading; class Program{ private static void Main() { Thread.Sleep(3000); }}", TestName = "Thread.Sleep")]
		public static async void TestTimeLimitError(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.TimeLimit, details.Verdict);
			Assert.IsNotNullOrEmpty(details.Error);
		}

		[TestCase(@"using System; class Program { static void Main() { var a = new byte[65 * 1024 * 1024]; Console.WriteLine(a); }}", TestName = "Local array")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { static List<byte> mem = new List<byte>(65 * 1024 * 1024); static void Main() { Console.WriteLine(mem); }}", TestName = "List field")] // throw TypeInitializationException
		[TestCase(@"using System; using System.Collections.Generic; class Program { static void Main() { var mem = new List<byte>(65 * 1024 * 1024); Console.WriteLine(mem); }}", TestName = "Local List")]
		public static async void TestMemoryLimitError(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.MemoryLimit, details.Verdict);
			Assert.IsNotNullOrEmpty(details.Error);
		}

		[TestCase(@"using System; class Program { static void Main() { var a = new byte[63 * 1024 * 1024]; Console.WriteLine(a); }}", TestName = "Local array")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { static List<byte> mem = new List<byte>(63 * 1024 * 1024); static void Main() { Console.WriteLine(mem); }}", TestName = "List field")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { static void Main() { var mem = new List<byte>(63 * 1024 * 1024); Console.WriteLine(mem); }}", TestName = "Local List")]
		public static async void TestMemoryLimit(string code)
		{
			var details = await GetDetails(code, "");

			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.IsNullOrEmpty(details.Error);
			Assert.IsNotNullOrEmpty(details.Output);
		}

		[Test]
		[Explicit]
		public static async void TestAbort()
		{
			const string code = @"class A { static void Main() { try { while(true) {} } finally { A.Main(); } } }";
			const int threads = 10;
			var a = Process.GetCurrentProcess().Threads.Count;
			for (var i = 0; i < threads; ++i)
			{
				var details = await GetDetails(code, "");
				Console.Out.WriteLine("{0}: {1}", i + 1, details.Verdict);
			}
			Console.Out.WriteLine("{0}", Process.GetCurrentProcess().Threads.Count - a);
			for (var i = 0; i < threads; ++i)
			{
				Thread.Sleep(1000);
				Console.Out.WriteLine("{0}", Process.GetCurrentProcess().Threads.Count - a);
			}
			Assert.AreEqual(0, Process.GetCurrentProcess().Threads.Count - a);
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
			var lastStatus = client.FindDetails(id).Status;
			while (lastStatus != SubmissionStatus.Done && count >= 0)
			{
				await Task.Delay(100);
				--count;
				lastStatus = client.FindDetails(id).Status;
			}
			Assert.GreaterOrEqual(count, 0, "too slow...");
			var details = client.FindDetails(id).ToPublic();

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

