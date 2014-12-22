using System.Threading.Tasks;
using CsSandboxApi;
using NUnit.Framework;

namespace CsSandboxTests
{
	static class Tests
	{
		private const string Code = @"class Program { static void Main() { } }";
		private const string Adress = "http://localhost:62992/";

		[Test]
		public static async void TestOk()
		{
			var details = await GetDetails(Code, "");
			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.IsEmpty(details.Output);
			Assert.IsEmpty(details.Error);
		}

		[Test]
		[ExpectedException(typeof(CsSandboxClientException.Unauthorized))]
		public static async void TestUnauthorized()
		{
			var client = new CsSandboxClient("testUnauthorized", Adress);
			await client.CreateSubmit(Code, "");
		}

		[Test]
		[ExpectedException(typeof(CsSandboxClientException.SubmissionNotFound))]
		public static async void TestSubmissionNotFound()
		{
			var client = new CsSandboxClient("tester", Adress);
			await client.GetSubmissionStatus("incorrect");
		}

		[Test]
		[ExpectedException(typeof(CsSandboxClientException.Forbidden))]
		public static async void TestForbidden()
		{
			var client = new CsSandboxClient("tester", Adress);
			var submission = await client.CreateSubmit(Code, "");
			var details = await submission.GetDetails();
			client = new CsSandboxClient("tester2", Adress);
			await client.GetSubmissionStatus(details.Id);
		}


		private static async Task<PublicSubmissionDetails> GetDetails(string code, string input)
		{
			var client = new CsSandboxClient("tester", Adress);
			var submission = await client.CreateSubmit(code, input);
			Assert.NotNull(submission);

			var count = 0;
			var lastStatus = await submission.GetStatus();
			while (lastStatus != SubmissionStatus.Done && count < 30)
			{
				await Task.Delay(1000);
				++count;
				lastStatus = await submission.GetStatus();
			}
			Assert.Less(count, 30, "too slow...");
			var details = await submission.GetDetails();

			Assert.NotNull(details);
			Assert.AreEqual(SubmissionStatus.Done, details.Status);
			Assert.AreEqual(code, details.Code);
			Assert.AreEqual(input, details.Input);
			Assert.True(details.NeedRun);

			return details;
		}
	}
}

