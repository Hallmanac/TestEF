using System;

namespace TestEf.Console.Core
{
    public interface IBaseEntity
    {
        int Id { get; set; } 
        DateTimeOffset LastModifiedOn { get; set; }
    }
}