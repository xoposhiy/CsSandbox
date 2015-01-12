using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CsSandboxRunnerApi;

namespace CsSandboxRunner
{
	static class Program
	{
		private static readonly ConcurrentQueue<InternalSubmissionModel> Unhandled = new ConcurrentQueue<InternalSubmissionModel>();
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
			if (args.Length < 3)
				threadsCount = 1;
			else
			{
				if (!int.TryParse(args[2], out threadsCount))
					threadsCount = 1;
			}


			var threads = new List<Thread>();
			for (var i = 0; i < threadsCount; ++i)
			{
				var thread = new Thread(Handle);
				thread.Start();
				threads.Add(thread);
			}

			var client = new Client(address, token);
			while (true)
			{
				threads = threads.Where(thread => thread.IsAlive).ToList();
				if (Unhandled.Count < threadsCount / 2)
				{
					var unhandled = client.TryGetSubmissions(threadsCount).Result;
					foreach (var submission in unhandled)
					{
						Unhandled.Enqueue(submission);
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
				if (threads.Count < threadsCount)
				{
					var thread = new Thread(Handle);
					thread.Start();
					threads.Add(thread);
				}
			}
		}

		private static void Handle()
		{
			var spinWait = new SpinWait();
			while (true)
			{
				InternalSubmissionModel submission;
				while (!Unhandled.TryDequeue(out submission))
					spinWait.SpinOnce();
				var result = new SandboxRunner(submission).Run();
				Results.Enqueue(result);
			}
		}
	}
}
