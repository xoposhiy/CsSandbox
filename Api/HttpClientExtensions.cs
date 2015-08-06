using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CsSandboxApi
{
	public static class HttpClientExtensions
	{
		public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string uri, T payload)
		{
			var serializedPayload = JsonConvert.SerializeObject(payload, Formatting.Indented);
			return await client.PostAsync(
				uri,
				new StringContent(serializedPayload, Encoding.UTF8, "application/json"),
				CancellationToken.None);
		}

		public static async Task<T> ReadAsAsync<T>(this HttpContent content)
		{
			var s = await content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<T>(s);
		}
	}
}