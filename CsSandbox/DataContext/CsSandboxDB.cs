using System.Data.Entity;
using CsSandbox.Migrations;
using CsSandbox.Models;

namespace CsSandbox.DataContext
{
	public class CsSandboxDb : DbContext
	{
		public CsSandboxDb()
			: base("DefaultConnection")
		{
			Database.SetInitializer(new MigrateDatabaseToLatestVersion<CsSandboxDb, Configuration>());
		}

		public DbSet<SubmissionDetails> Submission { get; set; }
		public DbSet<User> Users { get; set; }
		public DbSet<Roles> Roles { get; set; }
	}
}