using System;
using System.Collections.Generic;
using TestEf.ConsoleMain.Core;
using TestEf.ConsoleMain.Repo;
using TestEf.ConsoleMain.Tenant;

namespace TestEf.ConsoleMain.Identity
{
    public class PhoneNumber : IBaseEntity, ITenant, IEquatable<PhoneNumber>, IUserManyToManyCollection
    {
        private string _formattedNumber;

        public int AreaCode { get; set; }

        public int PrefixNumber { get; set; }

        public int LineNumber { get; set; }

        public string FormattedNumber
        {
            get { return string.IsNullOrEmpty(_formattedNumber) ? string.Format("({0})-{1}-{2}", AreaCode, PrefixNumber, LineNumber) : _formattedNumber; }
            set { _formattedNumber = value; }
        }

        public List<User> Users { get; set; }

        public int Id { get; set; }

        public DateTimeOffset LastModifiedOn { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(PhoneNumber other)
        {
            if(ReferenceEquals(null, other))
            {
                return false;
            }
            if(ReferenceEquals(this, other))
            {
                return true;
            }
            return AreaCode == other.AreaCode
                   && TenantId == other.TenantId
                   && PrefixNumber == other.PrefixNumber
                   && LineNumber == other.LineNumber
                   && Id == other.Id
                   && LastModifiedOn.Equals(other.LastModifiedOn);
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
                hashCode = (hashCode * 397) ^ PrefixNumber;
                hashCode = (hashCode * 397) ^ LineNumber;
                hashCode = (hashCode * 397) ^ AreaCode;
                hashCode = (hashCode * 397) ^ LastModifiedOn.GetHashCode();
                hashCode = (hashCode * 397) ^ TenantId;
                return hashCode;
            }
        }

        public static bool operator ==(PhoneNumber left, PhoneNumber right) { return Equals(left, right); }
        public static bool operator !=(PhoneNumber left, PhoneNumber right) { return !Equals(left, right); }

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
            return Equals((PhoneNumber)obj);
        }
    }
}