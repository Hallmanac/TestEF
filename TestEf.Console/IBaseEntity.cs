namespace TestEf.Console
{
    using System;

    public interface IBaseEntity
    {
        int Id { get; set; } 
        DateTimeOffset LastModifiedOn { get; set; }
    }
}