using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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
			"mscorlib.dll"
		};

		public SandboxRunner(InternalSubmissionModel submission)
		{
			_submission = submission;
			_result.Id = submission.Id;
		}

		public RunningResults Run()
		{
			var assembly = CreateAssemby();

			_result.Verdict = Verdict.Ok;

			_result.AddCompilationInfo(assembly);

			if (_result.IsCompilationError())
			{
				Remove(assembly);
				return _result;
			}

			if (!_submission.NeedRun)
			{
				Remove(assembly);
				return _result;
			}
			RunSandboxer(assembly);

			Remove(assembly);
			return _result;
		}

		private void Remove(CompilerResults assembly)
		{
			try
			{
				File.Delete(assembly.PathToAssembly);
			}
			catch
			{
			}
		}


		private CompilerResults CreateAssemby()
		{
			var provider = new CSharpCodeProvider(new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
			var compilerParameters = new CompilerParameters(UsesAssemblies)
			{
				GenerateExecutable = true,
				IncludeDebugInformation = true,
				WarningLevel = 4,
			};

			var assembly = provider.CompileAssemblyFromSource(compilerParameters, _submission.Code);

			return assembly;
		}

		private void RunSandboxer(CompilerResults assembly)
		{
			var inputBytes = Encoding.UTF8.GetBytes(_submission.Input); 
			var input = Encoding.Default.GetString(inputBytes);

			var startInfo = new ProcessStartInfo("CsSandboxer", String.Format("\"{0}\" {1}", Path.GetFullPath(assembly.PathToAssembly), _submission.Id))
			{
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8
			};
			var sandboxer = Process.Start(startInfo);

			if (sandboxer == null)
			{
				_result.Verdict = Verdict.SandboxError;
				_result.Error = "Can't start proces";
				return;
			}

			var stderrReader = new AsyncReader(sandboxer.StandardError, OutputLimit + 1);

			var readyState = sandboxer.StandardOutput.ReadLineAsync();
			if (!readyState.Wait(TimeLimitInSeconds * 1000) || readyState.Result != "Ready")
			{
				if (!sandboxer.HasExited)
				{
					sandboxer.Kill();
					_result.Verdict = Verdict.SandboxError;
					_result.Error = "Sandbox does not respond";
					return;
				}
				if (sandboxer.ExitCode != 0)
				{
					HandleNonZeroExitCode(stderrReader.GetData(), sandboxer.ExitCode);
					return;
				}
				_result.Verdict = Verdict.SandboxError;
				_result.Error = "Sandbox exit before respond";
				return;
			}

			sandboxer.Refresh();
			var startUsedMemory = sandboxer.WorkingSet64;
			var startUsedTime = sandboxer.TotalProcessorTime;
			var startTime = DateTime.Now;

			sandboxer.StandardInput.WriteLine("Run");
			sandboxer.StandardInput.WriteLineAsync(input);

			var stdoutReader = new AsyncReader(sandboxer.StandardOutput, OutputLimit + 1);
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
			if (sandboxer.ExitCode != 0)
			{
				HandleNonZeroExitCode(stderrReader.GetData(), sandboxer.ExitCode);
				return;
			}

			_result.Output = stdoutReader.GetData();
			_result.Error = stderrReader.GetData();
		}

		private void HandleNonZeroExitCode(string error, int exitCode)
		{
			var obj = FindSerializedException(error);

			if (obj != null)
				_result.HandleException(obj);
			else
				HandleNtStatus(exitCode, error);
		}

		private void HandleNtStatus(int exitCode, string error)
		{
			switch ((uint)exitCode)
			{
				case 0xC00000FD:
					_result.Verdict = Verdict.RuntimeError;
					_result.Error = "Stack overflow exception.";
					break;
				default:
					_result.Verdict = Verdict.SandboxError;
					_result.Error = string.IsNullOrWhiteSpace(error) ? "Non-zero exit code" : error;
					_result.Error += string.Format("\nExit code: 0x{0:X8}", exitCode);
					break;
			}
		}

		private bool IsOutputLimit(AsyncReader reader)
		{
			return _hasOutputLimit = _hasOutputLimit
			                         || (reader.ReadedLength > OutputLimit);
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

		private static Exception FindSerializedException(string str)
		{
			if (!str.EndsWith("}"))
				return null;

			var pos = str.LastIndexOf(Environment.NewLine, StringComparison.Ordinal);

			if (pos == -1)
				return null;

			var jsonSettings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All
			};

			try
			{
				var obj = JsonConvert.DeserializeObject(str.Substring(pos), jsonSettings);
				return obj as Exception;
			}
			catch
			{
				return null;
			}
		}
	}
}