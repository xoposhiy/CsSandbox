﻿using System;
using System.IO;
using System.Text;

namespace CsSandboxer
{
	public class LimitedStringWriter : StringWriter
	{
		private readonly int _maxLength;
		private const int DefaultLength = 10 * 1024 * 1024;

		private bool HasOutputLimit { get; set; }

		public LimitedStringWriter(int maxLength = DefaultLength)
		{
			_maxLength = maxLength;
			base.GetStringBuilder().Capacity = maxLength;
		}

		public LimitedStringWriter(IFormatProvider formatProvider, int maxLength = DefaultLength) : base(formatProvider)
		{
			_maxLength = maxLength;
			base.GetStringBuilder().Capacity = maxLength;
		}

		public LimitedStringWriter(StringBuilder sb, int maxLength = DefaultLength) : base(sb)
		{
			_maxLength = maxLength;
			base.GetStringBuilder().Capacity = maxLength;
		}

		public LimitedStringWriter(StringBuilder sb, IFormatProvider formatProvider, int maxLength = DefaultLength)
			: base(sb, formatProvider)
		{
			_maxLength = maxLength;
			base.GetStringBuilder().Capacity = maxLength;
		}

		public override void Write(char value)
		{
			if (GetStringBuilder().Length + 1 > _maxLength)
			{
				HasOutputLimit = true;
				return;
			}

			base.Write(value);
		}

		public override void Write(char[] buffer, int index, int count)
		{
			if (GetStringBuilder().Length + count > _maxLength)
			{
				HasOutputLimit = true;
				return;
			}

			base.Write(buffer, index, count);
		}

		public override void Write(string value)
		{
			if (value != null && GetStringBuilder().Length + value.Length > _maxLength)
			{
				HasOutputLimit = true;
				return;
			}

			base.Write(value);
		}

		public string GetData()
		{
			return HasOutputLimit ? null : base.ToString();
		}
	}
}