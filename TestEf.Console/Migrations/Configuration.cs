using TestEf.Console.Repo;

namespace TestEf.Console.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<MainDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            MigrationsDirectory = "Migrations";
            AutomaticMigrationDataLossAllowed = true;
        }
    }
}
