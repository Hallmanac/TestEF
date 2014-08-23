using TestEf.Console.Identity;
using TestEf.Console.Repo;

namespace TestEf.Console
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using Newtonsoft.Json;

    internal class Program
    {
        private static void Main(string[] args)
        {
            //var usersCount = 0;
            //using(var context = new MainDbContex())
            //{
            //    usersCount = context.Users.Count();
            //}
            //if(usersCount > 0)
            //{
            //    using(var context = new MainDbContex())
            //    {
            //        var phoneNumbersToDelete = context.PhoneNumbers.ToList();
            //        var usersList = context.Users.ToList();
            //        usersList.ForEach(usr => context.Entry(usr).State = EntityState.Deleted);
            //        phoneNumbersToDelete.ForEach(ph => context.Entry(ph).State = EntityState.Deleted);
            //        context.SaveChanges();
            //    }
            //}
            //var phoneNumbers = new List<PhoneNumber>();
            //for(var i = 0; i < 20; i++)
            //{
            //    phoneNumbers.Add(new PhoneNumber
            //    {
            //        AreaCode = 407,
            //        PrefixNumber = 616,
            //        LineNumber = 9600 + (20 - i)
            //    });
            //}
            //var users = new List<User>();
            //var phoneCount = 2;
            //for(var i = 0; i < 100; i++)
            //{
            //    var user = new User
            //    {
            //        FirstName = string.Format("Brian{0:0000}", i),
            //        LastName = string.Format("Hall{0:0000}", i)
            //    };
            //    user.Emails.Add(new Email
            //    {
            //        EmailAddress = string.Format("Brian_{0:0000}@Hallmanac.com", i)
            //    });
            //    var lineNumber1 = 9600 + (20 - (phoneCount));
            //    var lineNumber2 = lineNumber1 + 1;
            //    user.PhoneNumbers.Add(phoneNumbers.FirstOrDefault(ph => ph.LineNumber == lineNumber1));
            //    user.PhoneNumbers.Add(phoneNumbers.FirstOrDefault(ph => ph.LineNumber == lineNumber2));
            //    phoneCount += 2;
            //    if(phoneCount % 20 == 0)
            //    {
            //        phoneCount = 2;
            //    }
            //    users.Add(user);
            //}
            //using(var context = new MainDbContex())
            //{
            //    context.Set<User>().AddRange(users);
            //    //foreach(var newUser in users)
            //    //{
            //    //    context.Users.Add(newUser);
            //    //}
            //    context.SaveChanges();
            //}
            
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
                Console.WriteLine("The current user is:\n");
                Console.WriteLine(JsonConvert.SerializeObject(currentUser, Formatting.Indented,
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
            //using(var context = new MainDbContex())
            //{
            //    retrievedUser.PhoneNumbers.Clear();
            //    retrievedUser.Emails.ToList().ForEach(eml => context.Entry(eml).State = EntityState.Deleted);
            //    retrievedUser.Emails.Clear();
            //    context.Entry(retrievedUser).State = EntityState.Deleted;
            //    context.SaveChanges();
            //}
        }
    }
}