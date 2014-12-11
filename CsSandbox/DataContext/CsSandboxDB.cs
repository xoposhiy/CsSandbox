using System.Data.Entity;
using CsSandbox.Models;

namespace CsSandbox.DataContext
{
	public class CsSandboxDb : DbContext
	{
		public DbSet<SubmissionDetails> Submission { get; set; }
		public DbSet<User> Users { get; set; }
	}
}