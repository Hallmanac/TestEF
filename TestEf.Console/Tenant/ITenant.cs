namespace TestEf.ConsoleMain.Tenant
{
    public interface ITenant
    {
        /// <summary>
        /// Tenant Id for hosting multi-tenant applications
        /// </summary>
        int TenantId { get; set; }
    }
}