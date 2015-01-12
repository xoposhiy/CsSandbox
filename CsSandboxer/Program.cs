using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading;
using Newtonsoft.Json;

namespace CsSandboxer
{
	static class Program
	{
		private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All
		};

		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			var assemblyPath = args[0];
			var id = args[1];

			var assembly = Assembly.LoadFile(assemblyPath);
			var domain = CreateDomain(id, assemblyPath);
			var sandboxer = CreateSandboxer(domain);

			GC.Collect();

			Console.Out.WriteLine("Ready");
			while (Console.In.ReadLine() != "Run")
			{
			}

			try
			{
				sandboxer.ExecuteUntrustedCode(assembly.EntryPoint);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(JsonConvert.SerializeObject(ex, Settings));
				Console.Error.Close();
				Environment.Exit(1);
			}
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
