using System;
using System.Collections.Generic;
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
	}
}