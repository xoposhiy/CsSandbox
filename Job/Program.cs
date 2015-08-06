using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using CsSandboxApi;
using CsSandboxApi.Runner;

namespace CsSandboxRunner
{
	internal class Program
	{
		private readonly string address;
		private readonly ConcurrentQueue<RunningResults> results = new ConcurrentQueue<RunningResults>();
		private readonly int threadsCount;
		private readonly string token;

		private readonly BlockingCollection<InternalSubmissionModel> unhandled =
			new BlockingCollection<InternalSubmissionModel>();

		public Program()
		{
			address = ConfigurationManager.AppSettings["url"];
			token = ConfigurationManager.AppSettings["token"];
			if (!int.TryParse(ConfigurationManager.AppSettings["threadsCount"], out threadsCount))
				threadsCount = Environment.ProcessorCount - 1;
		}

		public static void Main()
		{
			new Program().Run();
		}

		private void Run()
		{
			AppDomain.MonitoringIsEnabled = true;

			Console.Error.WriteLine("Listen {0} with {1} threads", address, threadsCount);

			StartHandleThreads();

			var client = new Client(address, token);
			while (true)
			{
				ReceiveNewUnhandled(client);
				SendNewResults(client);
				Thread.Sleep(100);
			}
			// ReSharper disable once FunctionNeverReturns
		}

		private void StartHandleThreads()
		{
			for (var i = 0; i < threadsCount; ++i)
				new Thread(Handle).Start();
		}

		private void Handle()
		{
			foreach (var submission in unhandled.GetConsumingEnumerable())
			{
				RunningResults result;
				try
				{
					result = new SandboxRunner(submission).Run();
				}
				catch (Exception ex)
				{
					result = new RunningResults
					{
						Id = submission.Id,
						Verdict = Verdict.SandboxError,
						Error = ex.ToString()
					};
				}
				results.Enqueue(result);
			}
		}

		private void ReceiveNewUnhandled(Client client)
		{
			if (unhandled.Count < (threadsCount + 1)/2)
			{
				var newUnhandled = client.TryGetSubmissions(threadsCount).Result;
				foreach (var submission in newUnhandled)
				{
					unhandled.Add(submission);
					Console.WriteLine("Received " + submission);
				}
			}
		}

		private void SendNewResults(Client client)
		{
			if (!results.IsEmpty)
			{
				var newResults = new List<RunningResults>();
				RunningResults result;
				while (results.TryDequeue(out result))
				{
					newResults.Add(result);
					Console.WriteLine("Finished " + result);
				}
				client.SendResults(newResults);
			}
		}
	}
}