using System;
using System.Collections.Generic;
using System.Globalization;
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
			if (response.IsSuccessStatusCode) 
				return await response.Content.ReadAsAsync<InternalSubmissionModel>();

			Console.Out.WriteLine(DateTime.Now.ToString("HH:mm:ss"));
			Console.Out.WriteLine(response.ToString());
			return null;
		}

		public async Task<List<InternalSubmissionModel>> TryGetSubmissions(int threadsCount)
		{
			var uri = GetUri("/GetSubmissions", new[] {"count", threadsCount.ToString(CultureInfo.InvariantCulture)});
			var response = await _httpClient.GetAsync(uri);
			if (response.IsSuccessStatusCode) 
				return await response.Content.ReadAsAsync<List<InternalSubmissionModel>>();

			return new List<InternalSubmissionModel>();
		}

		public async void SendResult(RunningResults result)
		{
			var uri = GetUri("/PostResult");
			var responce = await _httpClient.PostAsJsonAsync(uri, result);

			if (responce.IsSuccessStatusCode) return;

			Console.Out.WriteLine(DateTime.Now.ToString("HH:mm:ss"));
			Console.Out.WriteLine(responce.ToString());
			Console.Out.WriteLine(result);
		}

		public async void SendResults(List<RunningResults> results)
		{
			var uri = GetUri("/PostResults");
			var responce = await _httpClient.PostAsJsonAsync(uri, results);

			if (responce.IsSuccessStatusCode) return;

			Console.Out.WriteLine(DateTime.Now.ToString("HH:mm:ss"));
			Console.Out.WriteLine(responce.ToString());
			foreach (var result in results)
			{
				Console.Out.WriteLine(result);
			}
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