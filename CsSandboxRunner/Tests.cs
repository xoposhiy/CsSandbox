using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CsSandboxApi;
using CsSandboxRunnerApi;
using NUnit.Framework;

namespace CsSandboxRunner
{
	internal static class Tests
	{
		private const int OutputLimit = 10 * 1024 * 1024;

		[TestCase(@"namespace Test { public class Program { static public void Main() { return ; } } }",
			"", "", "",
			TestName = "Public class and Main")]
		[TestCase(@"using System; public class M{static public void Main(){Console.WriteLine(42);}}",
			"", "42\r\n", "",
			TestName = "Output")]
		[TestCase(@"using System; public class M{static public void Main(){Console.WriteLine(4.2);}}",
			"", "4.2\r\n", "",
			TestName = "Output invariant culture")]
		[TestCase(@"using System; public class M{static public void Main(){Console.Error.WriteLine(4.2);}}",
			"", "", "4.2\r\n",
			TestName = "Error invariant culture")]
		[TestCase(@"using System; using System.Globalization; public class M{static public void Main(){var a = 4.2; Console.WriteLine(a.ToString(CultureInfo.InvariantCulture));}}",
			"", "4.2\r\n", "",
			TestName = "Set Invariant Culture in ToString")]
		[TestCase(@"using System; using System.Globalization; class A { private static void Main() { Console.WriteLine(CultureInfo.CurrentCulture.EnglishName); } }",
			"", "Invariant Language (Invariant Country)\r\n", "",
			TestName = "Get Globlal CultureInfo")]
		[TestCase(@"using System; class M{static void Main(){System.Console.WriteLine(Tuple.Create(1, 2));}}", "",
			"(1, 2)\r\n", "",
			TestName = "Tuple")]
		[TestCase(@"using System; public class M{static public void Main(){System.Console.Error.WriteLine(42);}}", "", "",
			"42\r\n",
			TestName = "Output error")]
		[TestCase(@"using System; public class M{static public void Main(){System.Console.WriteLine(Console.ReadLine());}}",
			"asdfasdf", "asdfasdf\r\n", "",
			TestName = "Read")]
		[TestCase(@"using System; class M{static void Main(){ try{throw new Exception();}catch{Console.WriteLine('!');}}}", "",
			"!\r\n", "",
			TestName = "try/catch")]
		[TestCase("using System; using System.Linq; using System.Collections.Generic; class A { static void Main() { var a = new List<String>{\"Str2\"}; foreach(var b in a.Select(s => s.ToLower())) Console.WriteLine(b); } }",
			"", "str2\r\n", "",
			TestName = "Collections and LINQ")]
		[TestCase(@"using System; enum A {a} class B { static void Main() { Console.Write(A.a); } }",
			"", "a", "",
			TestName = "Write enum")]
		[TestCase(@"using System; enum A {a} class B { static void Main() { Console.Write(A.a.ToString()); } }",
			"", "a", "",
			TestName = "Write enum ToString")]
		[TestCase("using System; class A { public override string ToString() { return \"a\"; }} class B { static void Main() { Console.Write(new A()); } }",
			"", "a", "",
			TestName = "Write ToString")]
		[TestCase("using System; class A { public override string ToString() { return \"a\"; }} class B { static void Main() { Console.Write(new A().ToString()); } }",
			"", "a", "",
			TestName = "Write ToString directly")]
		[TestCase("using System; enum A {a} class B { static void Main() { Console.Write(new string(new[] {(char) 9608, (char) 8212})); } }",
			"", "█—", "",
			TestName = "output stdout Unicode")]
		[TestCase("using System; enum A {a} class B { static void Main() { Console.Error.Write(new string(new[] {(char) 9608, (char) 8212})); } }",
			"", "", "█—", 
			TestName = "output stderr Unicode")]
		[TestCase("using System; enum A {a} class B { static void Main() { var input = Console.ReadLine(); foreach (var ch in input){ Console.Out.WriteLine((int)ch); }} }",
			"█—", "9608\r\n8212\r\n", "",
			TestName = "read Unicode")]
		[TestCase("using System; using System.Threading; class B { static void Main() { var t = new Thread(() => Console.WriteLine(4)); t.Start(); t.Join(); } }",
			"", "4\r\n", "",
			TestName = "Thread")]
		[TestCase(@"class A { static void Main(string[] args) {} }",
			"", "", "",
			TestName = "args")]
		public static void TestOk(string code, string input, string output, string error)
		{
			var details = GetDetails(code, input);
			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.AreEqual(output, details.Output);
			Assert.AreEqual(error, details.Error);
		}

		[TestCase("namespace Test { public class Program { static public void Main() { return 0; } } }",
			TestName = "Return int in void Main")]
		public static void TestCompilationError(string code)
		{
			var details = GetDetails(code, "");
			Assert.AreEqual(Verdict.CompilationError, details.Verdict);
			Assert.IsNotNullOrEmpty(details.CompilationOutput);
		}

		[TestCase("using System; using System.IO; namespace UntrustedCode { public class UntrustedClass { public static void Main() { Directory.GetFiles(@\"c:\\\"); }}}",
			TestName = "Get files list")]
		[TestCase("using System; class A { static void Main() { foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) Console.WriteLine(assembly.GetName().Name); }}",
			TestName = "Loaded assemblies list")]
		[TestCase("using System; using System.Threading; using System.Linq; using System.Reflection; using System.Security; [SecurityCritical] class A { static void Main() { var assemblies = Thread.GetDomain().GetAssemblies(); var ass = assemblies.FirstOrDefault(assembly => assembly.ToString().Contains(\"CsSandbox\")); var type = ass.GetType(\"CsSandboxer.Sandboxer\", true, true); if(type == null) Console.WriteLine(\"lol\"); else type.InvokeMember(\"MustDontWork\", BindingFlags.InvokeMethod, null, null, null); }}",
			TestName = "Method in sandboxer")]
		[TestCase("using System; using System.Threading; using System.Linq; using System.Reflection; using System.Security; [SecurityCritical] class A { static void Main() { var assemblies = Thread.GetDomain().GetAssemblies(); var ass = assemblies.FirstOrDefault(assembly => assembly.ToString().Contains(\"CsSandbox\")); var type = ass.GetType(\"CsSandboxer.Sandboxer\", true, true); if(type == null) Console.WriteLine(\"lol\"); else type.InvokeMember(\"Secret\", BindingFlags.GetField, null, null, null);; }}",
			TestName = "Field in sandboxer")]
		public static void TestSecurityException(string code)
		{
			var details = GetDetails(code, "");
			Assert.AreEqual(Verdict.SecurityException, details.Verdict);
			Assert.IsNullOrEmpty(details.Error);
		}

		[TestCase("using System; namespace Test { public class Program { static public void Main() { throw new Exception(); }}}",
			TestName = "throw")]
		[TestCase("using System; namespace Test { public class Program { static public void Main() { Console.Error.Write('a'); throw new Exception(); }}}",
			TestName = "write stderr + throw")]
		public static void TestRuntimeError(string code)
		{
			var details = GetDetails(code, "");
			Assert.AreEqual(Verdict.RuntimeError, details.Verdict);
			Assert.IsNotNullOrEmpty(details.Error);
		}

		[TestCase("using System; class Program { static void Main() { var s = new string('*', $limit + 1); Console.Write(s); }}",
			TestName = "Output")]
		[TestCase("using System; class Program { static void Main() { var s = new string('*', $limit); Console.WriteLine(s); }}",
			TestName = "Output + newline")]
		[TestCase("using System; class Program { static void Main() { var s = new string('*', $limit); Console.Write(s); Console.WriteLine(); }}",
			TestName = "Output + newline explicit")]
		public static void TestOutputLimitError(string code)
		{
			var details = GetDetails(code.Replace("$limit", OutputLimit.ToString(CultureInfo.InvariantCulture)), "");
			Assert.AreEqual(Verdict.OutputLimit, details.Verdict);
		}

		[TestCase(@"using System; class Program { static void Main() { var s = new string('*', $limit); Console.Write(s); }}",
			TestName = "Output")]
		public static void TestOutputLimit(string code)
		{
			var details = GetDetails(code.Replace("$limit", OutputLimit.ToString(CultureInfo.InvariantCulture)), "");
			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.AreEqual(new string('*', OutputLimit), details.Output);
		}

		[TestCase(@"using System; class Program { static void Main() { int a = 0; while(true) { ++a; } }}",
			TestName = "Infinty loop")]
		[TestCase(@"using System.Threading; class Program{ private static void Main() { Thread.Sleep(15000); }}",
			TestName = "Thread.Sleep")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { static void Main() { const int memory = 63 * 1024 * 1024; var a = new byte[memory]; for (var i = 0; i < 1000*1000*1000; ++i){ a[i % memory] = (byte)i; } }}",
			TestName = "many assignation")]
		public static void TestTimeLimitError(string code)
		{
			var details = GetDetails(code, "");
			Assert.AreEqual(Verdict.TimeLimit, details.Verdict);
		}

		[TestCase(@"using System; using System.Collections.Generic; class Program { static void Main() { const int memory = 63 * 1024 * 1024; var a = new byte[memory]; for (var i = 0; i < 100*1000*1000; ++i){ a[i % memory] = (byte)i; } }}",
			TestName = "many assignation")]
		public static void TestTimeLimit(string code)
		{
			var details = GetDetails(code, "");
			Assert.AreEqual(Verdict.Ok, details.Verdict);
		}

		[TestCase(@"using System; class Program { static void Main() { var a = new byte[65 * 1024 * 1024]; for (var i = 0; i < a.Length; ++i) { a[i] = (byte)i; } }}",
			TestName = "Local array")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { const int memory = 65 * 1024 * 1024; static List<byte> a = new List<byte>(memory); static void Main() { for (var i = 0; i < memory; ++i) { a.Add((byte)i); } }}",
			TestName = "List field")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { static void Main() { const int memory = 65 * 1024 * 1024; var a = new List<byte>(memory); for (var i = 0; i < memory; ++i) { a.Add((byte)i); } }}",
			TestName = "Local List")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { static void Main() { const int memory = 65 * 1024 * 1024; var a = new byte[memory]; var i = 0; while(true){ a[i] = (byte)i; i = (i + 1) % memory; } }}",
			TestName = "TL after ML")]
		public static void TestMemoryLimitError(string code)
		{
			var details = GetDetails(code, "");
			Assert.AreEqual(Verdict.MemoryLimit, details.Verdict);
		}

		[TestCase(@"using System; class Program { static void Main() { var a = new byte[63 * 1024 * 1024]; for (var i = 0; i < a.Length; ++i) { a[i] = (byte)i; } }}",
			TestName = "Local array")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { const int memory = 63 * 1024 * 1024; static List<byte> a = new List<byte>(memory); static void Main() { for (var i = 0; i < memory; ++i) { a.Add((byte)i); } }}",
			TestName = "List field")]
		[TestCase(@"using System; using System.Collections.Generic; class Program { static void Main() { const int memory = 63 * 1024 * 1024; var a = new List<byte>(memory); for (var i = 0; i < memory; ++i) { a.Add((byte)i); } }}",
			TestName = "Local List")]
		public static void TestMemoryLimit(string code)
		{
			var details = GetDetails(code, "");
			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.IsEmpty(details.Output);
		}

		[Test]
		[Explicit]
		public static void TestAbort()
		{
			const string code = @"class A { static void Main() { try { while(true) {} } finally { A.Main(); } } }";
			const int threads = 10;
			var a = Process.GetCurrentProcess().Threads.Count;
			for (var i = 0; i < threads; ++i)
			{
				new Thread(() => GetDetails(code, "")).Start();
			}
			for (var i = 0; i < threads; ++i)
			{
				Thread.Sleep(1000);
				Console.Out.WriteLine("{0}", Process.GetCurrentProcess().Threads.Count - a);
			}
		}

		private static RunningResults GetDetails(string code, string input)
		{
			var model = new InternalSubmissionModel
			{
				Id = Guid.NewGuid().ToString(),
				Code = code,
				Input = input,
				NeedRun = true
			};

			var result = new SandboxRunner(model).Run();
			Assert.IsNotNull(result);
			return result;
		}
	}
}

