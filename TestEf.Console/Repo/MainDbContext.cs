﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using TestEf.Console.Identity;
using TestEf.Console.Tenant;

namespace TestEf.Console.Repo
{
    public class MainDbContext : DbContext
    {
        public MainDbContext()
            : base("name=TestEfConnection")
        {
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Email> Emails { get; set; }

        public DbSet<PhoneNumber> PhoneNumbers { get; set; }

        public DbSet<TenantInfo> Tenants { get; set; }

        /// <summary>
        /// This method is called when the model for a derived context has been initialized, but
        ///             before the model has been locked down and used to initialize the context.  The default
        ///             implementation of this method does nothing, but it can be overridden in a derived class
        ///             such that the model can be further configured before it is locked down.
        /// </summary>
        /// <remarks>
        /// Typically, this method is called only once when the first instance of a derived context
        ///             is created.  The model for that context is then cached and is for all further instances of
        ///             the context in the app domain.  This caching can be disabled by setting the ModelCaching
        ///             property on the given ModelBuidler, but note that this can seriously degrade performance.
        ///             More control over caching is provided through use of the DbModelBuilder and DbContextFactory
        ///             classes directly.
        /// </remarks>
        /// <param name="modelBuilder">The builder that defines the model for the context being created. </param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Properties<int>().Where(p => p.Name == "Id")
                        .Configure(p => p.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                                         .HasColumnOrder(1)
                                         .IsKey());

            modelBuilder.Configurations.Add(new TenantInfoConfig());
            modelBuilder.Configurations.Add(new UserTypeConfig());
            modelBuilder.Configurations.Add(new EmailTypeConfig());
            modelBuilder.Configurations.Add(new PhoneNumberConfig());
        }
    }
}