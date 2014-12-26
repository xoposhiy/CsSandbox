using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace CsSandboxer
{
	[Serializable]
	public class Sandboxer : MarshalByRefObject
	{

		public void ExecuteUntrustedCode(MethodInfo entryPoint, TextReader stdin, TextWriter stdout, TextWriter stderr)
		{
			(new PermissionSet(PermissionState.Unrestricted)).Assert();

			Console.SetIn(stdin);
			Console.SetOut(stdout);
			Console.SetError(stderr);
			entryPoint.Invoke(null, null);

			CodeAccessPermission.RevertAssert();
		}

		#region Security Test

		public static void MustDontWork()
		{
			Console.WriteLine("Security broken!!!");
		}

		public static int Secret = 42;

		#endregion

	}
}