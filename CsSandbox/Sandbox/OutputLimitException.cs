using System;
using System.Runtime.Serialization;

namespace CsSandbox.Sandbox
{
	[Serializable]
	public class OutputLimitException : Exception
	{
		public OutputLimitException(string message = "Too much output") : base(message)
		{
		}

		protected OutputLimitException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}