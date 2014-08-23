namespace TestEf.Console.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class v00_00_01_002 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Tenants",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LastModifiedOn = c.DateTimeOffset(nullable: false, precision: 7),
                        TenantName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.TenantName, unique: true);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.Tenants", new[] { "TenantName" });
            DropTable("dbo.Tenants");
        }
    }
}
