﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsSandboxApi;
using NUnit.Framework;

namespace CsSandboxTests
{
	static class Tests
	{
		private const string Code = @"class Program { static void Main() { } }";

		private const string TlCode =
			@"using System; using System.Collections.Generic; class Program { static void Main() { const int memory = 63 * 1024 * 1024; var a = new byte[memory]; for (var i = 0; i < 1000*1000*1000; ++i){ a[i % memory] = (byte)i; } }}";
		private const string Adress = "http://kontur-labs02/CsSandbox/";

		[Test]
		public static async void TestOk()
		{
			var details = await GetDetails(Code, "");
			Assert.AreEqual(Verdict.Ok, details.Verdict);
			Assert.IsEmpty(details.Output);
			Assert.IsEmpty(details.Error);
		}

		[Test]
		[Explicit]
		public static async void TestLatency()
		{
			for (var i = 0; i < 10; ++i)
			{
				var details = await GetDetails(TlCode, "");
				Assert.AreEqual(Verdict.TimeLimit, details.Verdict);
			}
		}

		[Test]
		[ExpectedException(typeof(CsSandboxClientException.Unauthorized))]
		public static async void TestUnauthorized()
		{
			var client = new CsSandboxClient(TimeSpan.FromSeconds(1), "testUnauthorized", Adress);
			await client.CreateSubmit(Code, "");
		}

		[Test]
		[ExpectedException(typeof(CsSandboxClientException.SubmissionNotFound))]
		public static async void TestSubmissionNotFound()
		{
			var client = new CsSandboxClient(TimeSpan.FromSeconds(1), "tester", Adress);
			await client.GetSubmissionStatus("incorrect");
		}

		[Test]
		[ExpectedException(typeof(CsSandboxClientException.Forbidden))]
		public static async void TestForbidden()
		{
			var client = new CsSandboxClient(TimeSpan.FromSeconds(1), "tester", Adress);
			var submission = await client.CreateSubmit(Code, "");
			var details = await submission.GetDetails();
			client = new CsSandboxClient(TimeSpan.FromSeconds(1), "tester2", Adress);
			await client.GetSubmissionStatus(details.Id);
		}

		[Test]
		[Explicit]
		public static async void TestManySubmissions()
		{
			var client = new CsSandboxClient(TimeSpan.FromSeconds(1), "tester", Adress, 0);
			var submissions = new List<Submission>();
			var startTime = DateTime.Now;
			for (var i = 0; i < 100; ++i)
			{
				submissions.Add(await client.CreateSubmit("class A { static void Main() { while(true) {} } }", ""));
			}
			Console.Out.WriteLine("{0}", DateTime.Now.Subtract(startTime));
			while (submissions.Any())
			{
				var tmp = new List<Submission>();
				foreach (var submission in submissions)
				{
					var status = await submission.GetStatus();
					if (status != SubmissionStatus.Done)
						tmp.Add(submission);
				}
				submissions = tmp;
				Console.Out.WriteLine("{0}: {1}", DateTime.Now.Subtract(startTime), submissions.Count);
				Thread.Sleep(100);
			}
		}

		private static async Task<PublicSubmissionDetails> GetDetails(string code, string input)
		{
			var client = new CsSandboxClient(TimeSpan.FromSeconds(1), "tester", Adress);
			var submission = await client.CreateSubmit(code, input);
			Assert.NotNull(submission);

			var count = 0;
			var lastStatus = await submission.GetStatus();
			while (lastStatus != SubmissionStatus.Done && count < 300)
			{
				await Task.Delay(100);
				++count;
				lastStatus = await submission.GetStatus();
			}
			Assert.Less(count, 300, "too slow...");
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

