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
using CsSandbox.DataContext;
using CsSandboxApi;
using Microsoft.CSharp;

namespace CsSandbox.Sandbox
{
	public class Worker 
	{
		private readonly string id;
		private readonly SubmissionModel submission;
		private readonly SubmissionRepo submissions = new SubmissionRepo();

		public Worker(string id, SubmissionModel submission)
		{
			this.id = id;
			this.submission = submission;
		}

		public void Run()
		{
			var provider = new CSharpCodeProvider(new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
			var compilerParameters = new CompilerParameters
			{
				GenerateExecutable = true
			};
			var assembly = provider.CompileAssemblyFromSource(compilerParameters, submission.Code);
			if (WasError(assembly)) 
				return;

			if (!submission.NeedRun)
				return;

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

			var domain = AppDomain.CreateDomain(id, evidence, adSetup, permSet, fullTrustAssembly);

			var handle = Activator.CreateInstanceFrom(
				domain, 
				typeof (Sandboxer).Assembly.ManifestModule.FullyQualifiedName,
				typeof (Sandboxer).FullName
				);
			var sandboxer = (Sandboxer)handle.Unwrap();
			var stdin = new StringReader(submission.Input ?? "");
			var stdout = new LimitedStringWriter();
			var stderr = new LimitedStringWriter();
			try
			{
				sandboxer.ExecuteUntrustedCode(assembly.CompiledAssembly.EntryPoint, stdin, stdout, stderr);
			}
			catch (TargetInvocationException ex)
			{
				submissions.SetExceptionResult(id, ex);
				return;
			}
			catch (Exception ex)
			{
				submissions.SetSandboxException(id, ex.ToString());
				return;
			}
			submissions.SetRunInfo(id, stdout.ToString(), stderr.ToString());
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

			submissions.SetCompilationInfo(id, results.Errors.HasErrors, sb.ToString());

			return results.Errors.HasErrors;
		}
	}
}