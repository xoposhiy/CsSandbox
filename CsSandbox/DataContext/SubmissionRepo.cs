using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using CsSandbox.Models;
using CsSandboxApi;

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

		public override SubmissionDetails FindUnhandled()
		{
			var submission = db.Submission.FirstOrDefault(details => details.Status == SubmissionStatus.Waiting);
			if (submission == null)
				return null;
			submission.Status = SubmissionStatus.Running;
			Save(submission);
			return submission;
		}
	}
}