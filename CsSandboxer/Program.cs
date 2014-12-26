using System;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading;
using CsSandboxRunner;

namespace CsSandboxer
{
	static class Program
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			var assemblyPath = args[0];
			var id = args[1];

			var pipe = new NamedPipeClientStream(id);
			pipe.Connect();

			var stream = new StringStream(pipe);

			var assembly = Assembly.LoadFile(assemblyPath);
			var domain = CreateDomain(id, assemblyPath);
			var sandboxer = CreateSandboxer(domain);

			var stdin = new StringReader(stream.Read());
			var stdout = new LimitedStringWriter();
			var stderr = new LimitedStringWriter();

			GC.Collect();

			stream.Write("Ready");
			while (stream.Read() != "Run")
			{
			}

			var wasException = false;

			try
			{
				sandboxer.ExecuteUntrustedCode(assembly.EntryPoint, stdin, stdout, stderr);
			}
			catch (Exception ex)
			{
				stream.Write(ex);
				wasException = true;
			}

			if (wasException) return;

			stream.Write(Tuple.Create(stdout.GetData(), stderr.GetData()));
		}

		private static AppDomain CreateDomain(string id, string assemblyPath)
		{
			var permSet = new PermissionSet(PermissionState.None);
			permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
			var evidence = new Evidence();
			evidence.AddHostEvidence(new Zone(SecurityZone.Untrusted));
			var fullTrustAssembly = typeof(Sandboxer).Assembly.Evidence.GetHostEvidence<StrongName>();

			var adSetup = new AppDomainSetup
			{
				ApplicationBase = Path.GetDirectoryName(assemblyPath),
			};

			var domain = AppDomain.CreateDomain(id, evidence, adSetup, permSet, fullTrustAssembly);
			return domain;
		}

		private static Sandboxer CreateSandboxer(AppDomain domain)
		{
			var handle = Activator.CreateInstanceFrom(
				domain,
				typeof(Sandboxer).Assembly.ManifestModule.FullyQualifiedName,
				typeof(Sandboxer).FullName
				);
			var sandboxer = (Sandboxer)handle.Unwrap();
			return sandboxer;
		}
	}
}
