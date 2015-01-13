using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
		private static readonly TimeSpan IdleTimeLimit = new TimeSpan(0, 0, 0, 5 * TimeLimitInSeconds);

		private const int MemoryLimit = 64*1024*1024;
		private const int OutputLimit = 10*1024*1024;

		private bool _hasTimeLimit;
		private bool _hasMemoryLimit;
		private bool _hasOutputLimit;

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
			var startInfo = new ProcessStartInfo("CsSandboxer", String.Format("\"{0}\" {1}", Path.GetFullPath(assembly.PathToAssembly), _submission.Id))
			{
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			var sandboxer = Process.Start(startInfo);

			if (sandboxer == null)
			{
				_result.Verdict = Verdict.SandboxError;
				return;
			}

			while (sandboxer.StandardOutput.ReadLine() != "Ready")
			{
			}

			sandboxer.Refresh();
			var startUsedMemory = sandboxer.PeakWorkingSet64;
			var startUsedTime = sandboxer.TotalProcessorTime;
			var startTime = DateTime.Now;

			sandboxer.StandardInput.WriteLine("Run");
			sandboxer.StandardInput.WriteLineAsync(_submission.Input);

			var stdout = new char[OutputLimit + 1];
			var stdoutReader = sandboxer.StandardOutput.ReadBlockAsync(stdout, 0, OutputLimit + 1);
			var stderr = new char[OutputLimit + 1];
			var stderrReader = sandboxer.StandardError.ReadBlockAsync(stderr, 0, OutputLimit + 1);

			while (!sandboxer.HasExited
			       && !IsTimeLimitExpected(sandboxer, startTime, startUsedTime)
			       && !IsMemoryLimitExpected(sandboxer, startUsedMemory) 
				   && !IsOutputLimit(stdoutReader)
				   && !IsOutputLimit(stderrReader))
			{
			}

			if (!sandboxer.HasExited)
				sandboxer.Kill();

			if (_hasOutputLimit)
			{
				_result.Verdict = Verdict.OutputLimit;
				return;
			}

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

			sandboxer.WaitForExit();
			if (sandboxer.ExitCode != 0) // TODO: correct RE
			{
				stderrReader.Wait();
				var jsonSettings = new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All
				};
				object obj;
				try
				{
					obj = JsonConvert.DeserializeObject(new string(stderr, 0, stderrReader.Result), jsonSettings);
				}
				catch
				{
					_result.Verdict = Verdict.SandboxError;
					return;
				}
				if (obj is Exception)
					_result.HandleException(obj as Exception);
				else
					_result.Verdict = Verdict.SandboxError;
				return;
			}


//			var jsonSettings = new JsonSerializerSettings
//			{
//				TypeNameHandling = TypeNameHandling.All
//			};

//			object obj;
//			try
//			{
//				obj = JsonConvert.DeserializeObject(stdout.Result, jsonSettings);
//			}
//			catch
//			{
//				_result.Verdict = Verdict.SandboxError;
//				return;
//			}
//
//			if (obj is Exception)
//				_result.HandleException(obj as Exception);

			stdoutReader.Wait();
			stderrReader.Wait();
			_result.Output = new string(stdout, 0, stdoutReader.Result);
			_result.Error = new string(stderr, 0, stderrReader.Result);
		}

		private bool IsOutputLimit(Task<int> reader)
		{
			return _hasOutputLimit = _hasOutputLimit
			                         || (reader.IsCompleted && reader.Result > OutputLimit);
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