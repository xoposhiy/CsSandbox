namespace CsSandbox.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IndexForView : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.SubmissionDetails", new[] { "UserId" });
            CreateIndex("dbo.SubmissionDetails", new[] { "UserId", "Timestamp" }, name: "ViewAll");
        }
        
        public override void Down()
        {
            DropIndex("dbo.SubmissionDetails", "ViewAll");
            CreateIndex("dbo.SubmissionDetails", "UserId");
        }
    }
}
