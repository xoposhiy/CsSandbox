using System;

namespace CsSandbox.Sandbox
{
	public class TimeLimitException : Exception
	{
		public TimeLimitException(string message = "Программа превысила максимальное время работы") : base(message)
		{
		}
	}
}