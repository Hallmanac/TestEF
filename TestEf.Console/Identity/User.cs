using System;
using System.Collections.Generic;
using TestEf.Console.Core;

namespace TestEf.Console.Identity
{
    public class User : IBaseEntity
    {
        public User()
        {
            Emails = new List<Email>();
            PhoneNumbers = new List<PhoneNumber>();
        }

        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public List<Email> Emails { get; set; } 

        public List<PhoneNumber> PhoneNumbers { get; set; } 

        public DateTimeOffset LastModifiedOn { get; set; }

        public bool IsIdenticalTo(User entity)
        {
            return entity.Id == Id
                   && entity.FirstName == FirstName
                   && entity.LastName == LastName
                   && entity.LastModifiedOn == LastModifiedOn;
        }
    }
}