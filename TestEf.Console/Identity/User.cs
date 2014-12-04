using System;
using System.Collections.Generic;
using TestEf.ConsoleMain.Core;
using TestEf.ConsoleMain.Tenant;

namespace TestEf.ConsoleMain.Identity
{
    public class User : IBaseEntity, ITenant, IEquatable<User>
    {
        public User()
        {
            Emails = new List<Email>();
            PhoneNumbers = new List<PhoneNumber>();
        }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Username { get; set; }

        public List<Email> Emails { get; set; }

        public List<PhoneNumber> PhoneNumbers { get; set; }
        public int Id { get; set; }

        public DateTimeOffset LastModifiedOn { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(User other)
        {
            if(ReferenceEquals(null, other))
            {
                return false;
            }
            if(ReferenceEquals(this, other))
            {
                return true;
            }
            return string.Equals(FirstName, other.FirstName) && string.Equals(LastName, other.LastName) && string.Equals(Username, other.Username) &&
                   Id == other.Id && LastModifiedOn.Equals(other.LastModifiedOn) && TenantId == other.TenantId;
        }

        /// <summary>
        /// Tenant Id for hosting multi-tenant applications
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ TenantId;
                return hashCode;
            }
        }

        public static bool operator ==(User left, User right) { return Equals(left, right); }
        public static bool operator !=(User left, User right) { return !Equals(left, right); }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((User)obj);
        }
    }
}