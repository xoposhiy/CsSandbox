using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using CsSandboxApi;
using CsSandboxRunnerApi;

namespace CsSandboxRunner
{
	internal static class RunningResultsExtention
	{
		public static void AddCompilationInfo(this RunningResults results, CompilerResults assembly)
		{
			if (assembly.Errors.Count == 0)
			{
				return;
			}
			var sb = new StringBuilder();
			var errors = assembly.Errors
				.Cast<CompilerError>()
				.ToList();
			foreach (var error in errors)
			{
				sb.Append(String.Format("({2},{3}): {0} {1}: {4}\n", error.IsWarning ? "warning" : "error", error.ErrorNumber,
					error.Line, error.Column,
					error.ErrorText));
			}
			if (assembly.Errors.HasErrors)
				results.Verdict = Verdict.CompilationError;
			results.CompilationOutput = sb.ToString();
		}

		public static void HandleException(this RunningResults results, Exception ex)
		{
			HandleException(ref results, (dynamic)ex);
		}

		public static bool IsCompilationError(this RunningResults results)
		{
			return results.Verdict == Verdict.CompilationError;
		}

		private static void HandleException(ref RunningResults results, Exception ex)
		{
			results.Verdict = Verdict.SandboxError;
#if DEBUG
			results.Error = ex.ToString();
#endif
		}

		private static void HandleException(ref RunningResults results, TargetInvocationException ex)
		{
			HandleInnerException(ref results, (dynamic)ex.InnerException);
		}

		private static void HandleInnerException(ref RunningResults results, SecurityException ex)
		{
			results.Verdict = Verdict.SecurityException;
#if DEBUG
			results.Error = ex.ToString();
#endif
		}

		private static void HandleInnerException(ref RunningResults results, MemberAccessException ex)
		{
			results.Verdict = Verdict.SecurityException;
#if DEBUG
			results.Error = ex.ToString();
#endif
		}

		private static void HandleInnerException(ref RunningResults results, Exception ex)
		{
			results.Verdict = Verdict.RuntimeError;
			results.Error = ex.ToString();
		}
	}
}
