using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
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
			SetErrorMode(ErrorModes.SEM_NOGPFAULTERRORBOX); // WinOnly StackOverflow handling fix
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Console.InputEncoding = Encoding.UTF8;
			Console.OutputEncoding = Encoding.UTF8;

			var assemblyPath = args[0];
			var id = args[1];
			Assembly assembly = null;
			Sandboxer sandboxer = null;

			try
			{
				assembly = Assembly.LoadFile(assemblyPath);
				var domain = CreateDomain(id, assemblyPath);
				sandboxer = CreateSandboxer(domain);
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}

			if (assembly == null || sandboxer == null)
				Environment.Exit(1);

			GC.Collect();

			Console.Out.WriteLine("Ready");
			var runCommand = Console.In.ReadLineAsync();
			if (!runCommand.Wait(1000) || runCommand.Result != "Run")
				Environment.Exit(1);

			try
			{
				sandboxer.ExecuteUntrustedCode(assembly.EntryPoint);
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
		}

		private static void HandleException(Exception ex)
		{
			Console.Error.WriteLine();
			Console.Error.Write(JsonConvert.SerializeObject(ex, Settings));
			Console.Error.Close();
			Environment.Exit(1);
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

		[DllImport("kernel32.dll")]
		static extern ErrorModes SetErrorMode(ErrorModes uMode);

		[Flags]
		private enum ErrorModes : uint
		{
			SYSTEM_DEFAULT = 0x0,
			SEM_FAILCRITICALERRORS = 0x0001,
			SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
			SEM_NOGPFAULTERRORBOX = 0x0002,
			SEM_NOOPENFILEERRORBOX = 0x8000
		}
	}
}
