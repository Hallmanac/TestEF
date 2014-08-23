using TestEf.Console.Identity;

namespace TestEf.Console.Repo
{
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Threading.Tasks;

    public class UserEntityRepo : BaseSqlRepo<User, MainDbContext>
    {
        /// <summary>
        /// Gets an object by its Guid based ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task<User> GetByIdAsync(int id = 0)
        {
            if(id == 0)
            {
                return null;
            }
            using(Context = new MainDbContext())
            {
                var user = await Context.Users
                                    .Include(usr => usr.PhoneNumbers)
                                    .Include(usr => usr.Emails)
                                    .FirstOrDefaultAsync(usr => usr.Id == id).ConfigureAwait(false);
                return user; // We could just use the return statement without a variable, but it's easier to debug this way
            }
        }

        public override async Task UpdateFullAsync(User[] entities)
        {
            if(entities == null || entities.Length < 1)
                return;
            // Create a maximum batch count that is 100 or the number of entities to save. Which ever is less.
            // The number 100 is based on several StackOverflow posts that have indicated that their benchmarks for batch saves were best optimized at 100
            var commitCount = GetCommitCount(entities);

            // List to hold the current batch
            var commitList = new List<User>();
            foreach(var entity in entities)
            {
                commitList.Add(entity);
                if(commitList.Count % commitCount != 0)
                    continue;// Keep building the commitList until we've met the commit count and then we'll go to the database

                // Now that we've reached the max count for the current batch save, we save.
                using(Context = new MainDbContext())
                {
                    foreach(var user in commitList)
                    {
                        user.Emails.ForEach(eml => Context.Entry(eml).State = EntityState.Modified);
                        user.PhoneNumbers.ForEach(ph => Context.Entry(ph).State = EntityState.Modified);
                        Context.Entry(user).State = EntityState.Modified;
                    }
                    await Context.SaveChangesAsync().ConfigureAwait(false);
                }
                // We clear out the commitList in the event we have more entities to be saved.
                commitList.Clear();
            }
        }

        public override async Task DeleteAsync(User[] entities)
        {
            if(entities == null || entities.Length < 1)
                return;

            // Create a maximum batch count that is 100 or the number of entities to save. Which ever is less.
            // The number 100 is based on several StackOverflow posts that have indicated that their benchmarks for batch saves were best optimized at 100
            var commitCount = GetCommitCount(entities);

            // List to hold the current batch
            var commitList = new List<User>();
            foreach(var entity in entities)
            {
                // Add the current iteration entity to the current batch to be saved.
                commitList.Add(entity);
                if (commitList.Count % commitCount != 0)
                {
                    continue; // Keep building the commitList until we've met the commit count and then we'll go to the database
                }

                // Now that we've reached the max count for the current batch save, we save.
                using (Context = new MainDbContext())
                {
                    foreach (var user in commitList)
                    {
                        // We make double sure that AutoDetectChanges is off because that has a pretty significant impact on raw performance
                        Context.Configuration.AutoDetectChangesEnabled = false;

                        user.PhoneNumbers.Clear();
                        user.Emails.ForEach(eml => Context.Entry(eml).State = EntityState.Deleted);
                        user.Emails.Clear();
                        Context.Entry(user).State = EntityState.Deleted;
                    }
                    await Context.SaveChangesAsync().ConfigureAwait(false);
                }
                // We clear out the commitList in the event we have more entities to be saved.
                commitList.Clear();
            }
            
        }

        public override async Task SaveAllChildCollectionsAsync(User[] entities)
        {
            if(entities == null)
            {
                return;
            }
            if(entities.Length < 1)
            {
                return;
            }
            var emails = new List<Email>();
            var phones = new List<PhoneNumber>();
            foreach(var user in entities)
            {
                if(user.Emails.Count > 0)
                {
                    emails.AddRange(user.Emails);
                }
                if(user.PhoneNumbers.Count > 0)
                {
                    phones.AddRange(user.PhoneNumbers);
                }
            }
            await SaveEmailsAsync(emails).ConfigureAwait(false);
            await SavePhoneNumbersAsync(phones, entities.ToList()).ConfigureAwait(false);
        }

        private async Task SavePhoneNumbersAsync(List<PhoneNumber> givenItems, List<User> users)
        {
            // Get the database versions of the givenItems for comparison. We'll get them based on the UserEntityId property so we can also 
            // keep track of what email addresses have been deleted.
            List<PhoneNumber> dbItems;
            var phIds = users.SelectMany(u => u.PhoneNumbers.Select(p => p.Id)).ToList();
            using(Context = new MainDbContext())
            {
                dbItems = await (from phNum in Context.PhoneNumbers
                                 where phIds.Any(pid => pid == phNum.Id)
                                 select phNum).ToListAsync().ConfigureAwait(false);
            }
            // Get all the items that need to be updated by comparing two entities that have the same ID
            // and seeing if they have different values in other properties.
            var updates = (from givenItem in givenItems 
                           let dbItem = dbItems.FirstOrDefault(dbItemEnumerator => dbItemEnumerator.Id == givenItem.Id) 
                           where !givenItem.Equals(dbItem) 
                           select givenItem)
                           .ToList();
            // Loop through the database items and check to see if they're in the givenItems to see which ones have been deleted
            var deletes = dbItems.Where(dbItem => givenItems.All(gi => gi.Id != dbItem.Id)).ToList();
            if(deletes.Count > 0)
            {
                await SaveSqlEntitiesAsBatchAsync(deletes.ToArray(), EntityState.Deleted).ConfigureAwait(false);
            }
            if(updates.Count > 0)
            {
                await SaveSqlEntitiesAsBatchAsync(updates.ToArray(), EntityState.Modified).ConfigureAwait(false);
            }
        }

        private async Task SaveEmailsAsync(List<Email> givenItems)
        {
            // Get the database versions of the givenItems for comparison. We'll get them based on the UserEntityId property so we can also 
            // keep track of what email addresses have been deleted.
            List<Email> dbItems;
            using(Context = new MainDbContext())
            {
                dbItems = await (from eml in Context.Emails
                                 from usr in Context.Users
                                 where eml.UserId == usr.Id
                                 select eml).ToListAsync();
            }
            // Get all the items that need to be updated by comparing two entities that have the same ID
            // and seeing if they have different values in other properties.
            var updates = (from givenItem in givenItems
                           let dbItem = dbItems.FirstOrDefault(eml => eml.Id == givenItem.Id)
                           where !givenItem.Equals(dbItem)
                           select givenItem).ToList();
            // Loop through the database items and check to see if they're in the givenItems to see which ones have been deleted
            var deletes = dbItems.Where(dbItem => givenItems.All(eml => eml.Id != dbItem.Id)).ToList();
            if(deletes.Count > 0)
            {
                await SaveSqlEntitiesAsBatchAsync(deletes.ToArray(), EntityState.Deleted).ConfigureAwait(false);
            }
            if(updates.Count > 0)
            {
                await SaveSqlEntitiesAsBatchAsync(updates.ToArray(), EntityState.Modified).ConfigureAwait(false);
            }
        }
    }
}