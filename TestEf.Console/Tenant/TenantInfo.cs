﻿using System;
using TestEf.Console.Core;

namespace TestEf.Console.Tenant
{
    public class TenantInfo : IBaseEntity, IEquatable<TenantInfo>
    {
        public string TenantName { get; set; }
        public int Id { get; set; }
        public DateTimeOffset LastModifiedOn { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(TenantInfo other)
        {
            if(ReferenceEquals(null, other))
            {
                return false;
            }
            if(ReferenceEquals(this, other))
            {
                return true;
            }
            return Id == other.Id && LastModifiedOn.Equals(other.LastModifiedOn) && string.Equals(TenantName, other.TenantName);
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
                hashCode = (hashCode * 397) ^ (TenantName != null ? TenantName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(TenantInfo left, TenantInfo right) { return Equals(left, right); }
        public static bool operator !=(TenantInfo left, TenantInfo right) { return !Equals(left, right); }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj) { return Equals((TenantInfo)obj); }
    }
}