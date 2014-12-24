using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CsSandboxRunner
{
	static class Program
	{
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
				var thread = new Thread(() => Handle(address, token));
				thread.Start();
				threads.Add(thread);
			}

			while (true)
			{
				threads = threads.Where(thread => thread.IsAlive).ToList();
				if (threads.Count < threadsCount)
				{
					var thread = new Thread(() => Handle(address, token));
					thread.Start();
					threads.Add(thread);
				}
				Thread.Sleep(1000);
			}
		}

		private static void Handle(string address, string token)
		{
			var client = new Client(address, token);
			while (true)
			{
				var submission = client.TryGetSubmission().Result;
				if (submission == null)
				{
					Thread.Sleep(100);
					continue;
				}

				var result = new SandboxRunner(submission).Run();

				client.SendResult(submission.Id, result);
			}
		}
	}
}
