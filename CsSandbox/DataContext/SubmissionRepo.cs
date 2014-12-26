using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using CsSandbox.Models;

namespace CsSandbox.DataContext
{
	public class SubmissionRepo : AbstractSubmissionRepo
	{
		private readonly CsSandboxDb db;

		public SubmissionRepo() : this(new CsSandboxDb())
		{
			
		}

		private SubmissionRepo(CsSandboxDb db)
		{
			this.db = db;
		}

		protected override SubmissionDetails Find(string id)
		{
			return db.Submission.Find(id);
		}

		protected override void Save(SubmissionDetails submission)
		{
			db.Submission.AddOrUpdate(submission);
			db.SaveChanges();
		}

		public override IEnumerable<SubmissionDetails> GetAllSubmissions(string userId, int max, int skip)
		{
			return db.Submission.Where(details => details.UserId == userId)
				.OrderByDescending(details => details.Timestamp)
				.Skip(skip)
				.Take(max);
		}
	}
}