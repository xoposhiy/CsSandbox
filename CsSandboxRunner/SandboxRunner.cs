using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using CsSandboxApi;
using CsSandboxRunnerApi;
using Microsoft.CSharp;
using Newtonsoft.Json;

namespace CsSandboxRunner
{
	public class SandboxRunner 
	{
		private readonly InternalSubmissionModel _submission;

		private const int TimeLimitInSeconds = 1;
		private static readonly TimeSpan TimeLimit = new TimeSpan(0, 0, 0, TimeLimitInSeconds);
		private static readonly TimeSpan IdleTimeLimit = new TimeSpan(0, 0, 0, TimeLimitInSeconds);

		private const int MemoryLimit = 128*1024*1024;
		private const int OutputLimit = 10*1024*1024;

		private bool _hasTimeLimit;
		private bool _hasMemoryLimit;

		private readonly RunningResults _result = new RunningResults();

		private static readonly string[] UsesAssemblies =
		{
			"System.dll", 
			"System.Core.dll",
			"System.Linq.dll", 
			"mscorlib.dll",
		};

		public SandboxRunner(InternalSubmissionModel submission)
		{
			_submission = submission;
			_result.Id = submission.Id;
		}

		public RunningResults Run()
		{
			var assembly = CreateAssemby();

			_result.AddCompilationInfo(assembly);

			if (_result.IsCompilationError())
				return _result;

			if (!_submission.NeedRun)
				return _result;

			
			RunSandboxer(assembly);

			_result.Finalize();

			return _result;
		}


		private CompilerResults CreateAssemby()
		{
			var provider = new CSharpCodeProvider(new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
			var compilerParameters = new CompilerParameters(UsesAssemblies)
			{
				GenerateExecutable = true
			};

			var assembly = provider.CompileAssemblyFromSource(compilerParameters, _submission.Code);

			return assembly;
		}

		private void RunSandboxer(CompilerResults assembly)
		{
			var pipe = new NamedPipeServerStream(_submission.Id, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
				PipeOptions.None, 2 * OutputLimit + 1024, 0);

			Process sandboxer = null;
			new Thread(
				() =>
					sandboxer =
						Process.Start("CsSandboxer", String.Format("{0} {1}", Path.GetFullPath(assembly.PathToAssembly), _submission.Id)))
				.Start();

			pipe.WaitForConnection();
			var stream = new StringStream(pipe);
			stream.Write(_submission.Input ?? "");

			while (stream.Read() != "Ready")
			{
			}

//			if (sandboxer == null)
//			{
//				_result.Verdict = Verdict.SandboxError;
//				return;
//			}

			sandboxer.Refresh();
			var startUsedMemory = sandboxer.PeakWorkingSet64;
			var startUsedTime = sandboxer.TotalProcessorTime;
			var startTime = DateTime.Now;

			stream.Write("Run");

			while (!sandboxer.HasExited
			       && !IsTimeLimitExpected(sandboxer, startTime, startUsedTime)
			       && !IsMemoryLimitExpected(sandboxer, startUsedMemory))
			{
			}

			if (!sandboxer.HasExited)
				sandboxer.Kill();

			if (_hasTimeLimit)
			{
				_result.Verdict = Verdict.TimeLimit;
				return;
			}

			if (_hasMemoryLimit)
			{
				_result.Verdict = Verdict.MemoryLimit;
				return;
			}

			if (sandboxer.ExitCode != 0)
			{
				_result.Verdict = Verdict.SandboxError;
				return;
			}

			var res = stream.Read();
			stream.Dispose();
			var jsonSettings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All
			};
			var obj = JsonConvert.DeserializeObject(res, jsonSettings);

			if (obj is Exception)
				_result.HandleException(obj as Exception);
			else
			{
				var tuple = obj as Tuple<string, string>;
				_result.HandleOutput(tuple.Item1, tuple.Item2);
			}
		}

		private bool IsMemoryLimitExpected(Process sandboxer, long startUsedMemory)
		{
			sandboxer.Refresh();
			long mem;
			try
			{
				mem = sandboxer.PeakWorkingSet64;
			}
			catch
			{
				return _hasMemoryLimit;
			}

			return _hasMemoryLimit = _hasMemoryLimit
			                         || startUsedMemory + MemoryLimit < mem;
		}

		private bool IsTimeLimitExpected(Process sandboxer, DateTime startTime, TimeSpan startUsedTime)
		{
			return _hasTimeLimit = _hasTimeLimit
			                       || TimeLimit.Add(startUsedTime).CompareTo(sandboxer.TotalProcessorTime) < 0
			                       || startTime.Add(IdleTimeLimit).CompareTo(DateTime.Now) < 0;
		}
	}
}