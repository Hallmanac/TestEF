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

            //DoSomething();

            //using(var context = new MainDbContext())
            //{
            //    retrievedUser.PhoneNumbers.Clear();
            //    retrievedUser.Emails.ToList().ForEach(eml => context.Entry(eml).State = EntityState.Deleted);
            //    retrievedUser.Emails.Clear();
            //    context.Entry(retrievedUser).State = EntityState.Deleted;
            //    context.SaveChanges();
            //}
        }

        private static async Task MainAsync(string[] args)
        {
            var runtime = new Runtime();
            await runtime.InitializeTenantsAsync();
            System.Console.WriteLine("\nTenants Initialized");

            await runtime.InitializeUsersAsync(100, 42);
            System.Console.WriteLine("\nUsers Initialized\n");

            await runtime.TestUserInteractionAsync();
        }
    }
}