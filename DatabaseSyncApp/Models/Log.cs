using System.ComponentModel.DataAnnotations;

namespace DatabaseSyncApp.Models;

public class Log
{
    [Key]
    public int LogId { get; set; }
    public DateTime TimeStamp { get; set; }
    public string? ChangedField { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? Description { get; set; }
}