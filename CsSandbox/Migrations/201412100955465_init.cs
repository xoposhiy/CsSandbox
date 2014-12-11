namespace CsSandbox.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SubmissionDetails",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 64),
                        Code = c.String(nullable: false, maxLength: 4000),
                        UserId = c.String(nullable: false, maxLength: 128),
                        Input = c.String(maxLength: 4000),
                        Timestamp = c.DateTime(nullable: false),
                        Status = c.Int(nullable: false),
                        Verdict = c.Int(nullable: false),
                        CompilationOutput = c.String(maxLength: 4000),
                        Output = c.String(maxLength: 4000),
                        Error = c.String(maxLength: 4000),
                        NeedRun = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Token = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.SubmissionDetails", "UserId", "dbo.Users");
            DropIndex("dbo.SubmissionDetails", new[] { "UserId" });
            DropTable("dbo.Users");
            DropTable("dbo.SubmissionDetails");
        }
    }
}
