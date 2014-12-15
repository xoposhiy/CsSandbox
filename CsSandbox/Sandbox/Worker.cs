using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
			catch (AggregateException ex)
			{
				_submissionsRepo.SetExceptionResult(_id, ex);
			}
			catch (SolutionException ex)
			{
				_submissionsRepo.SetExceptionResult(_id, (dynamic)ex);
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
			var task =
				new Task<Tuple<LimitedStringWriter, LimitedStringWriter>>(
					() => sandboxer.ExecuteUntrustedCode(assembly.CompiledAssembly.EntryPoint, stdin));
			task.Start();

			var finishTime = DateTime.Now.Add(IdleTimeLimit);
			while (!task.IsCompleted 
				&& TimeLimit.CompareTo(domain.MonitoringTotalProcessorTime) >= 0 
				&& finishTime.CompareTo(DateTime.Now) >= 0)
			{
				if (task.IsFaulted)
					throw task.Exception;
				Thread.Sleep(100);
			}

			if (!task.IsCompleted)
				throw new TimeLimitException();

			var stdout = task.Result.Item1;
			var stderr = task.Result.Item2;
			if (stdout.HasOutputLimit || stderr.HasOutputLimit)
				throw new OutputLimitException();

			_submissionsRepo.SetRunInfo(_id, stdout.ToString(), stderr.ToString());
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