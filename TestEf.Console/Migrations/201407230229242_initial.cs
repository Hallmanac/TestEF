namespace TestEf.Console.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Emails",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EmailAddress = c.String(nullable: false, maxLength: 256),
                        IsVerified = c.Boolean(nullable: false),
                        UserId = c.Int(nullable: false),
                        LastModifiedOn = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.EmailAddress, unique: true, name: "EmailIndex")
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.PhoneNumbers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FormattedNumber = c.String(nullable: false, maxLength: 32),
                        AreaCode = c.Int(nullable: false),
                        PrefixNumber = c.Int(nullable: false),
                        LineNumber = c.Int(nullable: false),
                        LastModifiedOn = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FirstName = c.String(nullable: false, maxLength: 64),
                        LastName = c.String(nullable: false, maxLength: 64),
                        LastModifiedOn = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.UserPhoneNumber",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        PhoneNumberId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserId, t.PhoneNumberId })
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.PhoneNumbers", t => t.PhoneNumberId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.PhoneNumberId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserPhoneNumber", "PhoneNumberId", "dbo.PhoneNumbers");
            DropForeignKey("dbo.UserPhoneNumber", "UserId", "dbo.Users");
            DropForeignKey("dbo.Emails", "UserId", "dbo.Users");
            DropIndex("dbo.UserPhoneNumber", new[] { "PhoneNumberId" });
            DropIndex("dbo.UserPhoneNumber", new[] { "UserId" });
            DropIndex("dbo.Emails", new[] { "UserId" });
            DropIndex("dbo.Emails", "EmailIndex");
            DropTable("dbo.UserPhoneNumber");
            DropTable("dbo.Users");
            DropTable("dbo.PhoneNumbers");
            DropTable("dbo.Emails");
        }
    }
}
