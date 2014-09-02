using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TestEf.Console.Identity;
using TestEf.Console.Repo;
using TestEf.Console.Tenant;

namespace TestEf.Console
{
    public class Runtime
    {
        public async Task TestUserInteractionAsync()
        {
            var usersRepo = new UserRepo();

            List<User> allUsers;
            using (var ctx = usersRepo.DbContext())
            {
                allUsers = await ctx.Users.Include(usr => usr.Emails).Include(usr => usr.PhoneNumbers).ToListAsync().ConfigureAwait(false);
            }
            allUsers.ForEach(usr =>
            {
                var sb = new StringBuilder();
                sb.Append(string.Format("{0}_v2", usr.FirstName));
                usr.FirstName = sb.ToString();

            });

            await usersRepo.SaveFullEntitiesAsync(allUsers);

            System.Console.WriteLine("Users were updated to v2");
            System.Console.ReadLine();
        }

        public async Task InitializeTenantsAsync(int numberOfTenants = 5)
        {
            List<TenantInfo> tenants;
            using (var context = new MainDbContext())
            {
                tenants = await context.Tenants.ToListAsync();
            }
            if (tenants.Count > 0)
                return;

            for (var i = 1; i <= numberOfTenants; i++)
            {
                tenants.Add(new TenantInfo
                {
                    TenantName = string.Format("Tenant {0}", i)
                });
            }
            using (var context = new MainDbContext())
            {
                context.Tenants.AddRange(tenants);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task InitializeUsersAsync(int numberOfUsersToCreate = 20, int numberOfPhoneNumbers = 20)
        {
            var usersRepo = new UserRepo();

            // --- First delete existing users --- //
            var usersCount = 0;
            using (var tempRepo = new MainDbContext())
            {
                usersCount = await tempRepo.Users.CountAsync().ConfigureAwait(false);
            }

            // Delete all existing users
            if (usersCount > 0)
            {
                // Getting all user Ids as a way to test the GetByIdsAsync method
                var allUserIds = await usersRepo.GetAllUserIds().ConfigureAwait(false);

                // Getting all users by their Ids
                var existingUsers = await usersRepo.GetByIdsAsync(allUserIds.ToArray()).ConfigureAwait(false);
                await usersRepo.DeleteAsync(existingUsers).ConfigureAwait(false);
            }

            // --- Next create the users --- //

            // Get the tenants
            var tenants = new List<TenantInfo>();
            using (var context = new MainDbContext())
            {
                tenants.AddRange(await context.Tenants.ToListAsync().ConfigureAwait(false));
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

            var sw = new Stopwatch();
            sw.Start();
            await usersRepo.InsertAsync(users).ConfigureAwait(false);
            sw.Stop();
            System.Console.WriteLine("\nUsers inserted in {0} milliseconds.", sw.ElapsedMilliseconds);

            sw.Restart();
            var returnedIds = users.Select(usr => usr.Id).ToList();
            var newUsers = await usersRepo.GetByIdsAsync(returnedIds.ToArray()).ConfigureAwait(false);
            sw.Stop();
            
            System.Console.WriteLine("\nThe phone number of the first new user is {0}", newUsers.FirstOrDefault().PhoneNumbers.FirstOrDefault().FormattedNumber);
            System.Console.WriteLine("The time taken to retrieve the new users was {0} milliseconds", sw.ElapsedMilliseconds);

            usersRepo.Dispose();

        }

        public void DoSomething()
        {
            User currentUser;
            PhoneNumber phone;
            using (var context = new MainDbContext())
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
                if (currentUser != null)
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
            if (currentUser != null)
            {
                currentUser.Emails.ForEach(eml => eml.LastModifiedOn = DateTimeOffset.UtcNow);
                currentUser.PhoneNumbers.ForEach(p => p.LastModifiedOn = DateTimeOffset.UtcNow);
                currentUser.LastModifiedOn = DateTimeOffset.UtcNow;
                using (var context = new MainDbContext())
                {
                    //context.Users.Attach(currentUser);
                    context.Entry(currentUser).State = EntityState.Modified;
                    currentUser.Emails.ForEach(eml => context.Entry(eml).State = EntityState.Modified);
                    currentUser.PhoneNumbers.ForEach(ph => context.Entry(ph).State = EntityState.Modified);
                    context.SaveChanges();
                }
                System.Console.WriteLine("The current user is:\n");
                System.Console.WriteLine(JsonConvert.SerializeObject(currentUser, Formatting.Indented,
                    new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
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
            using (var context = new MainDbContext())
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