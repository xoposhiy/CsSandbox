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
				sb.Append(String.Format("{0} ({1}): {2}\n", error.IsWarning ? "Warning" : "Error", error.ErrorNumber,
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

		public static void HandleOutput(this RunningResults results, LimitedStringWriter stdout, LimitedStringWriter stderr)
		{
			if (stdout.HasOutputLimit || stderr.HasOutputLimit)
			{
				results.Verdict = Verdict.OutputLimit;
			}
			else
			{
				results.Output = stdout.ToString();
				results.Error = stderr.ToString();
			}
		}

		public static void Finalize(this RunningResults results)
		{
			if (results.Verdict == Verdict.NA)
				results.Verdict = Verdict.Ok;
		}

		private static void HandleException(ref RunningResults results, Exception ex)
		{
			results.Verdict = Verdict.SandboxError;
		}

		private static void HandleException(ref RunningResults results, TargetInvocationException ex)
		{
			HandleInnerException(ref results, (dynamic)ex.InnerException);
		}

		private static void HandleInnerException(ref RunningResults results, SecurityException ex)
		{
			results.Verdict = Verdict.SecurityException;
		}

		private static void HandleInnerException(ref RunningResults results, MemberAccessException ex)
		{
			results.Verdict = Verdict.SecurityException;
		}

		private static void HandleInnerException(ref RunningResults results, Exception ex)
		{
			results.Verdict = Verdict.RuntimeError;
			results.Error = ex.ToString();
		}
	}
}
