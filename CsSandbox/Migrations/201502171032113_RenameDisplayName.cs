namespace CsSandbox.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenameDisplayName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SubmissionDetails", "DisplayName", c => c.String(maxLength: 1024));
			Sql("update dbo.SubmissionDetails set DisplayName = HumanName");
            DropColumn("dbo.SubmissionDetails", "HumanName");
        }
        
        public override void Down()
        {
            AddColumn("dbo.SubmissionDetails", "HumanName", c => c.String(maxLength: 1024));
			Sql("update dbo.SubmissionDetails set HumanName = DisplayName");
			DropColumn("dbo.SubmissionDetails", "DisplayName");
        }
    }
}
