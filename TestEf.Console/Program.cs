using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Threading.Tasks;
using Nito.AsyncEx;
using TestEf.Console.Migrations;
using TestEf.Console.Repo;

namespace TestEf.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<MainDbContext, Configuration>());
            var mainMigrator = new DbMigrator(new Configuration());
            mainMigrator.Update();

            AsyncContext.Run(() => MainAsync(args));
        }

        private static async Task MainAsync(string[] args)
        {
            var runtime = new Runtime();

            // Get the number of users
            System.Console.WriteLine("How many users would you like to add?");
            var userQtyValue = System.Console.ReadLine();
            int userQty;
            Int32.TryParse(userQtyValue, out userQty);
            userQty = userQty == 0 ? 20 : userQty;

            // Get the number of PhoneNumbers
            System.Console.WriteLine("\nHow many phone numbers would you like to add?");
            var phoneQtyValue = System.Console.ReadLine();
            int phoneQty;
            Int32.TryParse(phoneQtyValue, out phoneQty);
            phoneQty = phoneQty == 0 ? 20 : phoneQty;

            await runtime.InitializeTenantsAsync();
            System.Console.WriteLine("\nTenants Initialized");

            await runtime.InitializeUsersAsync(userQty, phoneQty);
            System.Console.WriteLine("\nUsers Initialized\n");

            await runtime.TestUserInteractionAsync();
        }
    }
}