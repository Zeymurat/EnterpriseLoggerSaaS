namespace EnterpriseLogger.Domain.Entities;

public class Package
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AllowedLogLevels { get; set; } = string.Empty; // "ERROR" veya "INFO,WARN,ERROR"
    public bool IsMailEnabled { get; set; }
    public bool IsSmsEnabled { get; set; }
    public int MonthlyRequestLimit { get; set; }
}