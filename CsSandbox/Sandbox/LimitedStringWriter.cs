using System;
using System.IO;
using System.Text;

namespace CsSandbox.Sandbox
{
	public class LimitedStringWriter : StringWriter
	{
		private readonly int _maxLength;
		private const int DefaultLength = 4000;

		public LimitedStringWriter(int maxLength = DefaultLength)
		{
			_maxLength = maxLength;
		}

		public LimitedStringWriter(IFormatProvider formatProvider, int maxLength) : base(formatProvider)
		{
			_maxLength = maxLength;
		}

		public LimitedStringWriter(StringBuilder sb, int maxLength = DefaultLength) : base(sb)
		{
			_maxLength = maxLength;
		}

		public LimitedStringWriter(StringBuilder sb, IFormatProvider formatProvider, int maxLength = DefaultLength)
			: base(sb, formatProvider)
		{
			_maxLength = maxLength;
		}

		public override void Write(char value)
		{
			if (GetStringBuilder().Length + 1 > _maxLength)
			{
				return;
				throw new OutputLimitException();
			}

			base.Write(value);
		}

		public override void Write(char[] buffer, int index, int count)
		{
			if (GetStringBuilder().Length + count > _maxLength)
			{
				return;
				throw new OutputLimitException();
			}

			base.Write(buffer, index, count);
		}

		public override void Write(string value)
		{
			if (value != null && GetStringBuilder().Length + value.Length > _maxLength)
			{
				return;
				throw new OutputLimitException();
			}
			base.Write(value);
		}
	}
}