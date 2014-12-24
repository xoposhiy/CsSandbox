namespace CsSandbox.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ExtractWorker : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Roles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        Role = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserId, t.Role })
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Roles", "UserId", "dbo.Users");
            DropIndex("dbo.Roles", new[] { "UserId" });
            DropTable("dbo.Roles");
        }
    }
}
