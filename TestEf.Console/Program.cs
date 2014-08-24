using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TestEf.Console.Identity;
using TestEf.Console.Migrations;
using TestEf.Console.Repo;
using TestEf.Console.Tenant;

namespace TestEf.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<MainDbContext, Configuration>());
            var mainMigrator = new DbMigrator(new Configuration());
            mainMigrator.Update();

            InitializeTenants();
            InitializeUsers(55);
            TestUserInteraction();
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

        private static void TestUserInteraction()
        {
            var usersRepo = new UserRepo();

            List<User> allUsers;
            using(var ctx = usersRepo.DbContext())
            {
                allUsers = ctx.Users.ToList();
            }
            allUsers.ForEach(usr =>
            {
                var sb = new StringBuilder();
                sb.Append(string.Format("{0}_v2", usr.FirstName));
                usr.FirstName = sb.ToString();
            });

            usersRepo.SaveFullEntitiesAsync(allUsers.ToArray()).Wait();

            System.Console.WriteLine("Users were updated to v2");
            System.Console.ReadLine();

            //var existingUsr = usersRepo.GetByUsernameAsync("userNumber0000").Result;
            //if(existingUsr == null)
            //{
            //    var newUsr = new User
            //    {
            //        Username = string.Format("UserNumber{0:0000}", 0),
            //        FirstName = "First",
            //        LastName = "Last",
            //        TenantId = 2
            //    };
            //    usersRepo.SaveFullEntitiesAsync(new[] {newUsr}).Wait();
            //}
            //System.Console.WriteLine("The new UserNumber0000 was saved properly.");
            //System.Console.ReadLine();
        }

        private static void InitializeTenants(int numberOfTenants = 5)
        {
            List<TenantInfo> tenants;
            using(var context = new MainDbContext())
            {
                tenants = context.Tenants.ToList();
            }
            if(tenants.Count > 0)
                return;
            
            for(var i = 1; i <= numberOfTenants; i++)
            {
                tenants.Add(new TenantInfo
                {
                    TenantName = string.Format("Tenant {0}", i)
                });
            }
            using(var context = new MainDbContext())
            {
                context.Tenants.AddRange(tenants);
                context.SaveChanges();
            }
        }

        public static void InitializeUsers(int numberOfUsersToCreate = 20, int numberOfPhoneNumbers = 20)
        {
            var usersRepo = new UserRepo();
            
            // --- First delete existing users --- //
            var usersCount = 0;
            using(var tempRepo = new MainDbContext())
            {
                usersCount = tempRepo.Users.Count();
            }
            
            // Delete all existing users
            if(usersCount > 0)
            {
                using(var context = new MainDbContext())
                {
                    var phoneNumbersToDelete = context.PhoneNumbers.ToList();
                    var usersList = context.Users.ToList();
                    usersList.ForEach(usr => context.Entry(usr).State = EntityState.Deleted);
                    phoneNumbersToDelete.ForEach(ph => context.Entry(ph).State = EntityState.Deleted);
                    context.SaveChanges();
                }
            }

            // --- Next create the users --- //

            // Get the tenants
            var tenants = new List<TenantInfo>();
            using(var context = new MainDbContext())
            {
                tenants.AddRange(context.Tenants.ToList());
            }

            var users = new List<User>();
            tenants.ForEach(tenant =>
            {
                // Create all the phone numbers
                var phoneNumbers = new List<PhoneNumber>();
                for (var i = 0; i < numberOfPhoneNumbers; i++)
                {
                    phoneNumbers.Add(new PhoneNumber
                    {
                        AreaCode = 407,
                        PrefixNumber = 616,
                        LineNumber = 9600 + (20 - i),
                        TenantId = tenant.Id
                    });
                }

                // Create all the users
                var phoneCount = 2;
                for (var i = 0; i < numberOfUsersToCreate; i++)
                {
                    var user = new User
                    {
                        Username = string.Format("UserNumber{0:0000}", i),
                        FirstName = string.Format("Brian{0:0000}", i),
                        LastName = string.Format("Hall{0:0000}", i)
                    };
                    user.Emails.Add(new Email
                    {
                        EmailAddress = string.Format("Brian_{0:0000}@Hallmanac.com", i),
                        TenantId = tenant.Id
                    });
                    var lineNumber1 = 9600 + (20 - (phoneCount));
                    var lineNumber2 = lineNumber1 + 1;
                    user.PhoneNumbers.Add(phoneNumbers.FirstOrDefault(ph => ph.LineNumber == lineNumber1));
                    user.PhoneNumbers.Add(phoneNumbers.FirstOrDefault(ph => ph.LineNumber == lineNumber2));
                    phoneCount += 2;
                    if (phoneCount % numberOfPhoneNumbers == 0)
                    {
                        phoneCount = 2;
                    }
                    user.TenantId = tenant.Id;
                    users.Add(user);
                }
            });
            //using(var context = usersRepo.DbContext())
            //{
            //    context.Users.AddRange(users);
            //    context.SaveChanges();
            //}
            usersRepo.InsertAsync(users.ToArray()).Wait();
            usersRepo.Dispose();

        }

        public static void DoSomething()
        {
            User currentUser;
            PhoneNumber phone;
            using(var context = new MainDbContext())
            {
                //============= Experiment ===============//
                var random = new Random();
                var searchUserId = random.Next();
                var somestring = searchUserId.ToString();
                var theUser = (from user in context.Users
                               join email in context.Emails on user.Id equals email.UserId
                               where ((searchUserId != 0 && user.Id == searchUserId)
                                      && (!string.Equals(somestring, "blah") && string.Equals(somestring, email.EmailAddress)))
                               select user).FirstOrDefault();
                //======== End Experiment ============//
                currentUser = context.Users.FirstOrDefault(usr => usr.FirstName == "Brian0052");
                if(currentUser != null)
                {
                    currentUser.Emails = context.Emails.Where(e => e.UserId == currentUser.Id).ToList();
                    //currentUser.PhoneNumbers = context.Users.Where(usr => usr.Id == currentUser.Id)
                    //                                  .SelectMany(u => u.PhoneNumbers).ToList();
                    currentUser.PhoneNumbers = (from phoneItem in context.PhoneNumbers
                                                where (from @user in phoneItem.Users
                                                       where @user.Id == currentUser.Id
                                                       select @user).Any()
                                                select phoneItem).ToList();
                }
                //currentUser.PhoneNumbers = query.ToList();
                //phone = currentUser.PhoneNumbers.FirstOrDefault();
            }
            if(currentUser != null)
            {
                currentUser.Emails.ForEach(eml => eml.LastModifiedOn = DateTimeOffset.UtcNow);
                currentUser.PhoneNumbers.ForEach(p => p.LastModifiedOn = DateTimeOffset.UtcNow);
                currentUser.LastModifiedOn = DateTimeOffset.UtcNow;
                using(var context = new MainDbContext())
                {
                    //context.Users.Attach(currentUser);
                    context.Entry(currentUser).State = EntityState.Modified;
                    currentUser.Emails.ForEach(eml => context.Entry(eml).State = EntityState.Modified);
                    currentUser.PhoneNumbers.ForEach(ph => context.Entry(ph).State = EntityState.Modified);
                    context.SaveChanges();
                }
                System.Console.WriteLine("The current user is:\n");
                System.Console.WriteLine(JsonConvert.SerializeObject(currentUser, Formatting.Indented,
                    new JsonSerializerSettings {PreserveReferencesHandling = PreserveReferencesHandling.Objects}));
            }
            var listOfUsers = new List<User>();
            User retrievedUser;
            var phoneNums = new List<PhoneNumber>();
            var firstNamesToQuery = new List<string>
            {
                "Brian0050",
                "Brian0051",
                "Brian0052"
            };
            using(var context = new MainDbContext())
            {
                retrievedUser = context.Users
                                       .Include(usr => usr.Emails)
                                       .Include(usr => usr.PhoneNumbers)
                                       .FirstOrDefault(usr => string.Equals(usr.FirstName, "Brian0000"));
                listOfUsers = context.Users
                                     .Include(usr => usr.Emails)
                                     .Include(usr => usr.PhoneNumbers)
                                     .Where(usr => firstNamesToQuery.Any(fn => string.Equals(fn, usr.FirstName)))
                                     .ToList();
                var ids = listOfUsers.Select(usr => usr.Id).ToList();
                var listOfUPhoneIds = listOfUsers.SelectMany(u => u.PhoneNumbers.Select(ph => ph.Id)).ToList();
                phoneNums = (from phNum in context.PhoneNumbers
                             where listOfUPhoneIds.Any(pid => pid == phNum.Id)
                             select phNum).Include(p => p.Users).ToList();
            }
        }
    }
}