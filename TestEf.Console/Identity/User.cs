using System;
using System.Collections.Generic;
using TestEf.Console.Core;
using TestEf.Console.Tenant;

namespace TestEf.Console.Identity
{
    public class User : IBaseEntity, ITenant
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
        /// Tenant Id for hosting multi-tenant applications
        /// </summary>
        public int TenantId { get; set; }

        public bool IsIdenticalTo(User entity)
        {
            return entity.Id == Id
                   && entity.FirstName == FirstName
                   && entity.LastName == LastName
                   && TenantId == entity.TenantId
                   && entity.LastModifiedOn == LastModifiedOn;
        }
    }
}