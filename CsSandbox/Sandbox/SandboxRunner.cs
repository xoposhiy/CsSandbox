using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading;
using CsSandboxApi;
using Microsoft.CSharp;

namespace CsSandbox.Sandbox
{
	public class SandboxRunner 
	{
		private readonly string _id;
		private readonly SubmissionModel _submission;

		private const int TimeLimitInSeconds = 1;
		private static readonly TimeSpan TimeLimit = new TimeSpan(0, 0, 0, TimeLimitInSeconds);
		private static readonly TimeSpan IdleTimeLimit = new TimeSpan(0, 0, 0, TimeLimitInSeconds);

		private const int MemoryLimit = 64*1024*1024;

		private bool _hasTimeLimit;
		private bool _hasMemoryLimit;

		private static readonly string[] UsesAssemblies =
		{
			"System.dll", 
			"System.Core.dll",
			"System.Linq.dll", 
			"mscorlib.dll",
		};

		public SandboxRunner(string id, SubmissionModel submission)
		{
			_id = id;
			_submission = submission;
		}

		public IRunningResult Run()
		{
			var assembly = CreateAssemby();

			var compilationInfo = CompilationOnly.Create(assembly);

			if (compilationInfo.IsCompilationError)
				return compilationInfo;

			if (!_submission.NeedRun)
				return compilationInfo;

			var domain = CreateDomain(assembly);
			var sandboxer = CreateSandboxer(domain);

			var result = RunSandboxer(domain, sandboxer, assembly);
			return result.AddCompilationInfo(compilationInfo);
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
			var compilerParameters = new CompilerParameters(UsesAssemblies)
			{
				GenerateExecutable = true
			};

			var assembly = provider.CompileAssemblyFromSource(compilerParameters, _submission.Code);

			return assembly;
		}

		private IRunningResult RunSandboxer(AppDomain domain, Sandboxer sandboxer, CompilerResults assembly)
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
				return new HasException(new TimeLimitException());

			if (_hasMemoryLimit)
				return new HasException(new MemoryLimitException());

			if (res.Item1 != null)
				return new HasException(res.Item1);

			var stdout = res.Item2;
			var stderr = res.Item3;
			if (stdout.HasOutputLimit || stderr.HasOutputLimit)
				return new HasException( new OutputLimitException());

			return new NormalRun(stdout.ToString(), stderr.ToString());
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
	}
}