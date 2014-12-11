using System.Linq;
using CsSandbox.DataContext;
using CsSandbox.Models;

namespace CsSandbox.Migrations
{
	using System.Data.Entity.Migrations;


	internal sealed class Configuration : DbMigrationsConfiguration<DataContext.CsSandboxDb>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(DataContext.CsSandboxDb context)
        {
			//  This method will be called after migrating to the latest version.

	        if (!context.Users.Any(user => user.Id == "tester"))
	        {
		        var db = new CsSandboxDb();
		        db.Users.Add(new User
		        {
					Id = "tester",
					Token = "tester"
		        });
		        db.SaveChanges();
	        }

	        //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}
