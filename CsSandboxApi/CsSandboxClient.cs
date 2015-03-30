using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace CsSandboxApi
{
	public partial class CsSandboxClient
	{
		private static readonly string DefaultToken;
		private static readonly string DefaultAdress;

		private readonly string _token;
		private readonly HttpClient _httpClient;
		private readonly int _executionTimeout;

		public CsSandboxClient(TimeSpan httpTimeout, string token = null, string baseAddress = null, int executionTimeout = 30)
		{
			_token = token ?? DefaultToken;
			var address = baseAddress ?? DefaultAdress;
			_executionTimeout = executionTimeout;
			_httpClient = new HttpClient
			{
				BaseAddress = new Uri(address), 
				Timeout = httpTimeout
			};
			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}

		public async Task<Submission> CreateSubmit(string code, string input, string displayName = null, bool needRun = true)
		{
			var model = new SubmissionModel
			{
				Code = code,
				Input = input,
				NeedRun = needRun,
				Token = _token,
				DisplayName = displayName
			};

			HttpResponseMessage response;
			try
			{
				response = await _httpClient.PostAsJsonAsync("CreateSubmission", model);
			}
			catch (TaskCanceledException)
			{
				throw new RequestTimeLimit();
			}

			if (!response.IsSuccessStatusCode)
			{
				throw CsSandboxClientException.Create(response);
			}

			var submissionId = await response.Content.ReadAsStringAsync();
			return new Submission(submissionId, this);
		}

		public async Task<SubmissionStatus> GetSubmissionStatus(string submissionId)
		{
			var uri = GetUriForSubmission("GetSubmissionStatus", submissionId);

			HttpResponseMessage response;
			try
			{
				response = await _httpClient.GetAsync(uri);
			}
			catch (TaskCanceledException)
			{
				return SubmissionStatus.RequestTimeLimit;
			}

			if (!response.IsSuccessStatusCode)
			{
				throw CsSandboxClientException.Create(response);
			}

			return await response.Content.ReadAsAsync<SubmissionStatus>();
		}

		public async Task<PublicSubmissionDetails> GetSubmissionDetails(string submissionId)
		{
			var uri = GetUriForSubmission("GetSubmissionDetails", submissionId);

			HttpResponseMessage response;
			try
			{
				response = await _httpClient.GetAsync(uri);
			}
			catch (TaskCanceledException)
			{
				throw new RequestTimeLimit();
			}

			if (!response.IsSuccessStatusCode)
			{
				throw CsSandboxClientException.Create(response);
			}

			return await response.Content.ReadAsAsync<PublicSubmissionDetails>();
		}

		public async Task<PublicSubmissionDetails> Submit(string code, string input, string displayName = null)
		{
			var submission = await CreateSubmit(code, input, displayName);

			var count = _executionTimeout;
			var lastStatus = await submission.GetStatus();
			while (lastStatus != SubmissionStatus.Done && count >= 0)
			{
				await Task.Delay(1000);
				--count;
				lastStatus = await submission.GetStatus();
			}
			if (lastStatus != SubmissionStatus.Done)
				return null;

			return await submission.GetDetails();
		}

		private string GetUriForSubmission(string path, string submissionId)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["id"] = submissionId;
			query["token"] = _token;
			return path + "/?" + query;
		}
	}
}
