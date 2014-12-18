using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Text;

namespace CsSandbox.Sandbox
{
	public abstract class IRunningResult
	{
		public readonly bool IsCompilationError;
		public readonly string CompilationOutput;

		protected IRunningResult(bool isCompilationError, string compilationOutput)
		{
			IsCompilationError = isCompilationError;
			CompilationOutput = compilationOutput;
		}

		public abstract IRunningResult AddCompilationInfo(CompilationOnly compilationInfo);
	}

	public class CompilationOnly : IRunningResult
	{
		private CompilationOnly(bool isCompilationError, string compilationOutput) : base(isCompilationError, compilationOutput)
		{
		}

		public static CompilationOnly Create(CompilerResults assembly)
		{
			if (assembly.Errors.Count == 0) 
				return new CompilationOnly(false, null);

			var sb = new StringBuilder();
			var errors = assembly.Errors
				.Cast<CompilerError>()
				.ToList();
			foreach (var error in errors)
			{
				sb.Append(String.Format("{0} ({1}): {2}\n", error.IsWarning ? "Warning" : "Error", error.ErrorNumber, error.ErrorText));
			}

			return new CompilationOnly(assembly.Errors.HasErrors, sb.ToString());

		}

		public override IRunningResult AddCompilationInfo(CompilationOnly compilationInfo)
		{
			return this;
		}
	}

	public class HasException : IRunningResult
	{
		public readonly Exception Exception;

		private HasException(bool isCompilationError, string compilationOutput, Exception exception)
			: base(isCompilationError, compilationOutput)
		{
			Exception = exception;
		}

		public HasException(Exception exception) : this(false, null, exception)
		{
		}

		public override IRunningResult AddCompilationInfo(CompilationOnly compilationInfo)
		{
			return new HasException(compilationInfo.IsCompilationError, compilationInfo.CompilationOutput, Exception);
		}
	}

	public class NormalRun : IRunningResult
	{
		public readonly String Stdout;
		public readonly String Stderr;

		private NormalRun(bool isCompilationError, string compilationOutput, string stdout, string stderr) : base(isCompilationError, compilationOutput)
		{
			Stdout = stdout;
			Stderr = stderr;
		}

		public NormalRun(string stdout, string stderr) : this(false, null, stdout, stderr)
		{
		}

		public override IRunningResult AddCompilationInfo(CompilationOnly compilationInfo)
		{
			return new NormalRun(compilationInfo.IsCompilationError, compilationInfo.CompilationOutput, Stdout, Stderr);
		}
	}
}