using System;
using TestEf.Console.Core;

namespace TestEf.Console.Identity
{
    public class Email : IBaseEntity
    {
        public string EmailAddress { get; set; }

        public bool IsVerified { get; set; }

        public int UserId { get; set; }
        public int Id { get; set; }

        public DateTimeOffset LastModifiedOn { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Email other)
        {
            if(ReferenceEquals(null, other))
            {
                return false;
            }
            if(ReferenceEquals(this, other))
            {
                return true;
            }
            return Id == other.Id && string.Equals(EmailAddress, other.EmailAddress) && IsVerified.Equals(other.IsVerified) && UserId == other.UserId &&
                   LastModifiedOn.Equals(other.LastModifiedOn);
        }

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
                hashCode = (hashCode * 397) ^ (EmailAddress != null ? EmailAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsVerified.GetHashCode();
                hashCode = (hashCode * 397) ^ UserId;
                hashCode = (hashCode * 397) ^ LastModifiedOn.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Email left, Email right) { return Equals(left, right); }
        public static bool operator !=(Email left, Email right) { return !Equals(left, right); }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj))
            {
                return false;
            }
            if(ReferenceEquals(this, obj))
            {
                return true;
            }
            if(obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((Email)obj);
        }
    }
}