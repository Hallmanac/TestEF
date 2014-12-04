using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Threading.Tasks;
using Nito.AsyncEx;
using TestEf.ConsoleMain.Migrations;
using TestEf.ConsoleMain.Repo;

namespace TestEf.ConsoleMain
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
            Console.WriteLine("How many users would you like to add?");
            var userQtyValue = Console.ReadLine();
            int userQty;
            Int32.TryParse(userQtyValue, out userQty);
            userQty = userQty == 0 ? 20 : userQty;

            // Get the number of PhoneNumbers
            Console.WriteLine("\nHow many phone numbers would you like to add?");
            var phoneQtyValue = Console.ReadLine();
            int phoneQty;
            Int32.TryParse(phoneQtyValue, out phoneQty);
            phoneQty = phoneQty == 0 ? 20 : phoneQty;

            await runtime.InitializeTenantsAsync();
            Console.WriteLine("\nTenants Initialized");

            await runtime.InitializeUsersAsync(userQty, phoneQty);
            Console.WriteLine("\nUsers Initialized\n");

            await runtime.TestUserInteractionAsync();
        }
    }
}