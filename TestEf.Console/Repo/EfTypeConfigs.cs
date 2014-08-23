using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using TestEf.Console.Identity;
using TestEf.Console.Tenant;

namespace TestEf.Console.Repo
{
    public class TenantInfoConfig : EntityTypeConfiguration<TenantInfo>
    {
        public TenantInfoConfig()
        {
            ToTable("Tenants");
            Property(tenant => tenant.TenantName)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_TenantName") {IsUnique = true}));
        }
    }

    public class UserTypeConfig : EntityTypeConfiguration<User>
    {
        public UserTypeConfig()
        {
            Property(usr => usr.FirstName).IsRequired().HasColumnOrder(2).HasMaxLength(64);
            Property(usr => usr.LastName).IsRequired().HasColumnOrder(3).HasMaxLength(64);
            Property(usr => usr.Username)
                .IsRequired()
                .HasColumnOrder(4)
                .HasMaxLength(256)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("TenantUsername", 2)
                {
                    IsClustered = false,
                    IsUnique = true
                }));
            Property(usr => usr.TenantId)
                .HasColumnOrder(5)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("TenantUsername", 1)
                {
                    IsClustered = false,
                    IsUnique = true
                }));
            HasMany(usr => usr.Emails).WithRequired().HasForeignKey(eml => eml.UserId);
            HasMany(usr => usr.PhoneNumbers).WithMany(ph => ph.Users).Map(m =>
            {
                m.ToTable("UserPhoneNumber");
                m.MapLeftKey("UserId");
                m.MapRightKey("PhoneNumberId");
            });
        }
    }

    public class EmailTypeConfig : EntityTypeConfiguration<Email>
    {
        public EmailTypeConfig()
        {
            Property(eml => eml.EmailAddress)
                .IsRequired()
                .HasColumnOrder(2)
                .HasMaxLength(256)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("EmailIndex", 2) {IsUnique = true}));
            Property(eml => eml.TenantId)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("EmailIndex", 1) {IsUnique = true}));
        }
    }

    public class PhoneNumberConfig : EntityTypeConfiguration<PhoneNumber>
    {
        public PhoneNumberConfig()
        {
            Property(ph => ph.FormattedNumber)
                .IsRequired()
                .HasColumnOrder(2)
                .HasMaxLength(32)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_TenantFormattedNumber", 2) {IsUnique = false}));
            Property(ph => ph.TenantId)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_TenantFormattedNumber", 1) {IsUnique = false}));
        }
    }
}