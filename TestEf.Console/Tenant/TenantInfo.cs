using System;
using TestEf.Console.Core;

namespace TestEf.Console.Tenant
{
    public class TenantInfo : IBaseEntity
    {
        public int Id { get; set; }
        public DateTimeOffset LastModifiedOn { get; set; }

        public string TenantName { get; set; }
    }
}