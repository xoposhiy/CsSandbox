using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using CsSandboxRunnerApi;

namespace CsSandboxRunner
{
	internal class Client
	{
		private readonly string _token;
		private readonly HttpClient _httpClient;

		public Client(string address, string token)
		{
			_token = token;
			_httpClient = new HttpClient {BaseAddress = new Uri(address)};
			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}


		public async Task<InternalSubmissionModel> TryGetSubmission()
		{
			var uri = GetUri("/TryGetSubmission");
			var response = await _httpClient.GetAsync(uri);
			if (!response.IsSuccessStatusCode)
			{
				Console.Out.WriteLine(response.ToString());
				return null;
			}
			return await response.Content.ReadAsAsync<InternalSubmissionModel>();
		}

		public async void SendResult(string id, RunningResults result)
		{
			var uri = GetUri("/PostResult", new[] {"id", id});
			var responce = await _httpClient.PostAsJsonAsync(uri, result);

			if (responce.IsSuccessStatusCode) return;

			Console.Out.WriteLine(responce.ToString());
			Console.Out.WriteLine(result);
		}

		private string GetUri(string path, params string[][] parameters)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["token"] = _token;
			foreach (var parameter in parameters)
			{
				query[parameter[0]] = parameter[1];
			}
			return path + "/?" + query;
		}
	}
}