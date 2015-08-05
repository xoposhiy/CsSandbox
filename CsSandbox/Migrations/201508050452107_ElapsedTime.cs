namespace CsSandbox.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ElapsedTime : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.SubmissionDetails", "ViewAll");
            AddColumn("dbo.SubmissionDetails", "Elapsed", c => c.Time(precision: 7));
            CreateIndex("dbo.SubmissionDetails", new[] { "UserId", "Timestamp", "Elapsed" }, name: "ViewAll");
        }
        
        public override void Down()
        {
            DropIndex("dbo.SubmissionDetails", "ViewAll");
            DropColumn("dbo.SubmissionDetails", "Elapsed");
            CreateIndex("dbo.SubmissionDetails", new[] { "UserId", "Timestamp" }, name: "ViewAll");
        }
    }
}
