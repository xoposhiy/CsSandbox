using System;
using System.Collections.Generic;
using System.Linq;
using CsSandbox.DataContext;
using CsSandbox.Models;

namespace CsSandbox.Tests
{
	public class DictionarySubmissionRepo : AbstractSubmissionRepo
	{
		private static readonly Dictionary<String, SubmissionDetails> db = new Dictionary<string, SubmissionDetails>();

		protected override SubmissionDetails Find(string id)
		{
			SubmissionDetails submission;
			return !db.TryGetValue(id, out submission) ? null : submission;
		}

		protected override void Save(SubmissionDetails submission)
		{
			db[submission.Id] = submission;
		}

		public override IEnumerable<SubmissionDetails> GetAllSubmissions(string userId, int max, int skip)
		{
			return db.Select(pair => pair.Value)
				.Where(details => details.UserId == userId)
				.OrderByDescending(details => details.Timestamp)
				.Skip(skip)
				.Take(max);
		}
	}
}