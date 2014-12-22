namespace CsSandbox.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IncreaseLimits : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.SubmissionDetails", "Code", c => c.String(nullable: false));
            AlterColumn("dbo.SubmissionDetails", "Input", c => c.String());
            AlterColumn("dbo.SubmissionDetails", "CompilationOutput", c => c.String());
            AlterColumn("dbo.SubmissionDetails", "Output", c => c.String());
            AlterColumn("dbo.SubmissionDetails", "Error", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.SubmissionDetails", "Error", c => c.String(maxLength: 4000));
            AlterColumn("dbo.SubmissionDetails", "Output", c => c.String(maxLength: 4000));
            AlterColumn("dbo.SubmissionDetails", "CompilationOutput", c => c.String(maxLength: 4000));
            AlterColumn("dbo.SubmissionDetails", "Input", c => c.String(maxLength: 4000));
            AlterColumn("dbo.SubmissionDetails", "Code", c => c.String(nullable: false, maxLength: 4000));
        }
    }
}
