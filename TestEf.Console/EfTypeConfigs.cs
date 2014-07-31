namespace TestEf.Console
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Infrastructure.Annotations;
    using System.Data.Entity.ModelConfiguration;

    public class UserTypeConfig : EntityTypeConfiguration<User>
    {
        public UserTypeConfig()
        {
            Property(usr => usr.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).HasColumnOrder(1);
            Property(usr => usr.FirstName).IsRequired().HasColumnOrder(2).HasMaxLength(64);
            Property(usr => usr.LastName).IsRequired().HasColumnOrder(3).HasMaxLength(64);
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
            Property(eml => eml.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).HasColumnOrder(1);
            Property(eml => eml.EmailAddress)
                .IsRequired()
                .HasColumnOrder(2)
                .HasMaxLength(256)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("EmailIndex") {IsUnique = true}));
        }
    }

    public class PhoneNumberConfig : EntityTypeConfiguration<PhoneNumber>
    {
        public PhoneNumberConfig()
        {
            Property(ph => ph.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).HasColumnOrder(1);
            Property(ph => ph.FormattedNumber).IsRequired().HasColumnOrder(2).HasMaxLength(32);
            
        }
    }
}