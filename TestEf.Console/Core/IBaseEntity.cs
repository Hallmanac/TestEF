using System;

namespace TestEf.ConsoleMain.Core
{
    public interface IBaseEntity
    {
        int Id { get; set; } 
        DateTimeOffset LastModifiedOn { get; set; }
    }
}