namespace TestEf.Console.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<MainDbContex>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            MigrationsDirectory = "Migrations";
            AutomaticMigrationDataLossAllowed = true;
        }
    }
}
