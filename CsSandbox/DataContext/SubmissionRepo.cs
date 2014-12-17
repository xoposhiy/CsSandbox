using System.Data.Entity.Migrations;
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

	}
}