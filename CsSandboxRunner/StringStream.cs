using System.IO.Pipes;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CsSandboxRunner
{
	public class StringStream
	{
		private const int HeaderLength = 4;

		private readonly PipeStream _pipe;
		private readonly JsonSerializerSettings _settings;

		public StringStream(PipeStream pipe)
		{
			_pipe = pipe;
			_settings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All
			};
		}

		public string Read()
		{
			var len = 0;
			for (var i = 0; i < HeaderLength; ++i)
			{
				len *= 256;
				len += _pipe.ReadByte();
			}
			var bytes = new byte[len];
			_pipe.Read(bytes, 0, len);
			var res = Encoding.UTF8.GetString(bytes);
			return res;
		}

		public void Write(string str)
		{
			var bytes = Encoding.UTF8.GetBytes(str);
			var len = bytes.Length;
			var lenb = new byte[HeaderLength];
			for (var i = 0; i < HeaderLength; ++i)
			{
				lenb[i] = (byte)(len % 256);
				len /= 256;
			}
			_pipe.Write(lenb.Reverse().ToArray(), 0, HeaderLength);
			_pipe.Write(bytes, 0, bytes.Length);
			_pipe.Flush();
		}

		public void Write(object obj)
		{
			Write(JsonConvert.SerializeObject(obj, _settings));
		}

		public bool HasData()
		{
			return true; // TODO
		}

		public void Dispose()
		{
			_pipe.Close();
		}
	}
}