namespace CsSandbox.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class HumanNames : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SubmissionDetails", "HumanName", c => c.String(maxLength: 1024));
        }
        
        public override void Down()
        {
            DropColumn("dbo.SubmissionDetails", "HumanName");
        }
    }
}
