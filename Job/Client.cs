using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using CsSandboxApi.Runner;

namespace CsSandboxRunner
{
	internal class Client
	{
		private readonly string token;
		private readonly HttpClient httpClient;

		public Client(string address, string token)
		{
			this.token = token;
			httpClient = new HttpClient {BaseAddress = new Uri(address + "/")};
			httpClient.DefaultRequestHeaders.Accept.Clear();
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}


		public async Task<InternalSubmissionModel> TryGetSubmission()
		{
			var uri = GetUri("TryGetSubmission");
			var response = await httpClient.GetAsync(uri);
			if (response.IsSuccessStatusCode) 
				return await response.Content.ReadAsAsync<InternalSubmissionModel>();

			Console.Out.WriteLine(DateTime.Now.ToString("HH:mm:ss"));
			Console.Out.WriteLine(response.ToString());
			return null;
		}

		public async Task<List<InternalSubmissionModel>> TryGetSubmissions(int threadsCount)
		{
			var uri = GetUri("GetSubmissions", new[] {"count", threadsCount.ToString(CultureInfo.InvariantCulture)});
			try
			{
				var response = await httpClient.GetAsync(uri);
				if (response.IsSuccessStatusCode)
					return await response.Content.ReadAsAsync<List<InternalSubmissionModel>>();
			}
			catch (Exception e)
			{
				Console.WriteLine("Cant connect to {0}. {1}", uri, e.Message);
			}
			return new List<InternalSubmissionModel>();
		}

		public async void SendResult(RunningResults result)
		{
			var uri = GetUri("PostResult");
			var responce = await httpClient.PostAsJsonAsync(uri, result);

			if (responce.IsSuccessStatusCode) return;

			Console.Out.WriteLine(DateTime.Now.ToString("HH:mm:ss"));
			Console.Out.WriteLine(responce.ToString());
			Console.Out.WriteLine(result);
		}

		public async void SendResults(List<RunningResults> results)
		{
			var uri = GetUri("PostResults");
			var responce = await httpClient.PostAsJsonAsync(uri, results);

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
			query["token"] = token;
			foreach (var parameter in parameters)
			{
				query[parameter[0]] = parameter[1];
			}
			return path + "/?" + query;
		}
	}
}