using System.Data.Entity.Migrations;
using TestEf.ConsoleMain.Repo;

namespace TestEf.ConsoleMain.Migrations
{
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
