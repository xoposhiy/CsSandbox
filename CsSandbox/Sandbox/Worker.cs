using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using CsSandbox.DataContext;
using CsSandboxApi;
using Microsoft.CSharp;

namespace CsSandbox.Sandbox
{
	public class Worker 
	{
		private readonly string _id;
		private readonly SubmissionModel _submission;
		private readonly SubmissionRepo _submissionsRepo = new SubmissionRepo();

		private const int TimeLimitInSeconds = 1;
		private static readonly TimeSpan TimeLimit = new TimeSpan(0, 0, 0, TimeLimitInSeconds);
		private static readonly TimeSpan IdleTimeLimit = new TimeSpan(0, 0, 0, TimeLimitInSeconds);

		private const int MemoryLimit = 64*1024*1024;

		private bool _hasTimeLimit;
		private bool _hasMemoryLimit;

		public Worker(string id, SubmissionModel submission)
		{
			_id = id;
			_submission = submission;
		}

		public void Run()
		{
			var assembly = CreateAssemby();

			if (assembly == null)
				return;

			if (!_submission.NeedRun)
				return;

			var domain = CreateDomain(assembly);
			var sandboxer = CreateSandboxer(domain);

			try
			{
				RunSandboxer(domain, sandboxer, assembly);
			}
			catch (TargetInvocationException ex)
			{
				_submissionsRepo.SetExceptionResult(_id, ex);
			}
			catch (SolutionException ex)
			{
				_submissionsRepo.SetExceptionResult(_id, ex);
			}
			catch (Exception ex)
			{
				_submissionsRepo.SetSandboxException(_id, ex.ToString());
			}
		}

		private static Sandboxer CreateSandboxer(AppDomain domain)
		{
			var handle = Activator.CreateInstanceFrom(
				domain,
				typeof (Sandboxer).Assembly.ManifestModule.FullyQualifiedName,
				typeof (Sandboxer).FullName
				);
			var sandboxer = (Sandboxer) handle.Unwrap();
			return sandboxer;
		}

		private AppDomain CreateDomain(CompilerResults assembly)
		{
			var assemblyPath = Path.GetFullPath(assembly.PathToAssembly);
			var permSet = new PermissionSet(PermissionState.None);
			permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
			var evidence = new Evidence();
			evidence.AddHostEvidence(new Zone(SecurityZone.Untrusted));
			var fullTrustAssembly = typeof (Sandboxer).Assembly.Evidence.GetHostEvidence<StrongName>();

			var adSetup = new AppDomainSetup
			{
				ApplicationBase = Path.GetDirectoryName(assemblyPath),
			};

			var domain = AppDomain.CreateDomain(_id, evidence, adSetup, permSet, fullTrustAssembly);
			return domain;
		}

		private CompilerResults CreateAssemby()
		{
			var provider = new CSharpCodeProvider(new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
			var compilerParameters = new CompilerParameters
			{
				GenerateExecutable = true
			};

			var assembly = provider.CompileAssemblyFromSource(compilerParameters, _submission.Code);

			return WasError(assembly) ? null : assembly;
		}

		private void RunSandboxer(AppDomain domain, Sandboxer sandboxer, CompilerResults assembly)
		{
			var stdin = new StringReader(_submission.Input ?? "");
			Tuple<Exception, LimitedStringWriter, LimitedStringWriter> res = null;
			var task = new Thread(() => { res = sandboxer.ExecuteUntrustedCode(assembly.CompiledAssembly.EntryPoint, stdin); }, MemoryLimit);

			var maxMemory = domain.MonitoringSurvivedMemorySize + MemoryLimit;
			var finishTime = DateTime.Now.Add(IdleTimeLimit);

			task.Start();

			while (task.IsAlive
				&& !IsTimeLimitExpected(domain, finishTime)
				&& !IsMemoryLimitExpected(domain, maxMemory))
			{
				Thread.Sleep(100);
			}

			task.Abort();

			if (_hasTimeLimit)
			{
				throw new TimeLimitException();
			}

			if (_hasMemoryLimit)
			{
				throw new MemoryLimitException();
			}

			if (res.Item1 != null)
				throw res.Item1;

			var stdout = res.Item2;
			var stderr = res.Item3;
			if (stdout.HasOutputLimit || stderr.HasOutputLimit)
				throw new OutputLimitException();

			_submissionsRepo.SetRunInfo(_id, stdout.ToString(), stderr.ToString());
		}

		private bool IsMemoryLimitExpected(AppDomain domain, long maxMemory)
		{
			return _hasMemoryLimit = _hasMemoryLimit
			                         || maxMemory < domain.MonitoringSurvivedMemorySize;
		}

		private bool IsTimeLimitExpected(AppDomain domain, DateTime finishTime)
		{
			return _hasTimeLimit = _hasTimeLimit
			                       || TimeLimit.CompareTo(domain.MonitoringTotalProcessorTime) < 0
			                       || finishTime.CompareTo(DateTime.Now) < 0;
		}

		private bool WasError(CompilerResults results)
		{
			if (results.Errors.Count == 0) return false;
			var sb = new StringBuilder();
			var errors = results.Errors
				.Cast<CompilerError>()
				.ToList();
			foreach (var error in errors)
			{
				sb.Append(String.Format("{0} ({1}): {2}", error.IsWarning ? "Warning" : "Error", error.ErrorNumber, error.ErrorText));
			}

			_submissionsRepo.SetCompilationInfo(_id, results.Errors.HasErrors, sb.ToString());

			return results.Errors.HasErrors;
		}
	}
}