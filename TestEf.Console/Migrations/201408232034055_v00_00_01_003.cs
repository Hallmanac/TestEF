using System.Data.Entity.Migrations;

namespace TestEf.ConsoleMain.Migrations
{
    public partial class v00_00_01_003 : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Emails", "EmailIndex");
            AddColumn("dbo.Emails", "TenantId", c => c.Int(nullable: false));
            AddColumn("dbo.PhoneNumbers", "TenantId", c => c.Int(nullable: false));
            CreateIndex("dbo.Emails", new[] { "TenantId", "EmailAddress" }, unique: true, name: "EmailIndex");
            CreateIndex("dbo.PhoneNumbers", new[] { "TenantId", "FormattedNumber" }, name: "IX_TenantFormattedNumber");
        }
        
        public override void Down()
        {
            DropIndex("dbo.PhoneNumbers", "IX_TenantFormattedNumber");
            DropIndex("dbo.Emails", "EmailIndex");
            DropColumn("dbo.PhoneNumbers", "TenantId");
            DropColumn("dbo.Emails", "TenantId");
            CreateIndex("dbo.Emails", "EmailAddress", unique: true, name: "EmailIndex");
        }
    }
}
