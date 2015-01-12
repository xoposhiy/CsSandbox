using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace CsSandboxer
{
	[Serializable]
	public class Sandboxer : MarshalByRefObject
	{

		public void ExecuteUntrustedCode(MethodInfo entryPoint)
		{
			(new PermissionSet(PermissionState.Unrestricted)).Assert();

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