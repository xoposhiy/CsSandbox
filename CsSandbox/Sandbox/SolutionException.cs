using System;
using CsSandboxApi;

namespace CsSandbox.Sandbox
{
	public class SolutionException : Exception
	{
		public readonly Verdict Verdict;
		protected SolutionException(string message, Verdict verdict) : base(message)
		{
			Verdict = verdict;
		}
	}

	public class TimeLimitException : SolutionException
	{
		public TimeLimitException()
			: base("Программа превысила максимальное время работы.", Verdict.TimeLimit)
		{
		}
	}

	public class OutputLimitException : SolutionException
	{
		public OutputLimitException()
			: base("Программа вывела слишком много символов.", Verdict.OutputLimit)
		{
		}
	}

	public class MemoryLimitException : SolutionException
	{
		public MemoryLimitException()
			: base("Программа привысила максимальный размер используемой памяти", Verdict.MemoryLimit)
		{
		}
	}

}