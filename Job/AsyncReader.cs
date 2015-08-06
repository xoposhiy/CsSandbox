using System.IO;
using System.Threading.Tasks;

namespace CsSandboxRunner
{
	public class AsyncReader
	{
		private readonly char[] _buffer;
		private readonly Task<int> _readerTask;

		public AsyncReader(StreamReader reader, int length)
		{
			_buffer = new char[length];
			_readerTask = reader.ReadBlockAsync(_buffer, 0, length);
		}

		public string GetData()
		{
			_readerTask.Wait();
			return new string(_buffer, 0, _readerTask.Result);
		}

		private bool IsCompleted
		{
			get { return _readerTask.IsCompleted; }
		}

		public int ReadedLength
		{
			get { return IsCompleted ? _readerTask.Result : -1; }
		}
	}
}