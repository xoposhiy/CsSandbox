using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CsSandboxApi;
using CsSandboxRunnerApi;

namespace CsSandboxRunner
{
	static class Program
	{
		private static readonly BlockingCollection<InternalSubmissionModel> Unhandled = new BlockingCollection<InternalSubmissionModel>();
		private static readonly ConcurrentQueue<RunningResults> Results = new ConcurrentQueue<RunningResults>();

		static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.Error.WriteLine("Format: <address> <token> [<threads count>]");
				return;
			}

			AppDomain.MonitoringIsEnabled = true;

			var address = args[0];
			var token = args[1];
			int threadsCount;
			if (args.Length < 3 || !int.TryParse(args[2], out threadsCount))
				threadsCount = Environment.ProcessorCount - 1;

			Console.Error.WriteLine("Start with {0} threads", threadsCount);

			for (var i = 0; i < threadsCount; ++i)
			{
				new Thread(Handle).Start();
			}

			var client = new Client(address, token);
			while (true)
			{
				if (Unhandled.Count < (threadsCount + 1) / 2)
				{
					var unhandled = client.TryGetSubmissions(threadsCount).Result;
					foreach (var submission in unhandled)
					{
						Unhandled.Add(submission);
					}
				}
				if (!Results.IsEmpty)
				{
					var results = new List<RunningResults>();
					RunningResults result;
					while (Results.TryDequeue(out result))
						results.Add(result);
					client.SendResults(results);
				}
				Thread.Sleep(100);
			}
		}

		private static void Handle()
		{	
			foreach (var submission in Unhandled.GetConsumingEnumerable())
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
				Results.Enqueue(result);
			}
		}
	}
}
