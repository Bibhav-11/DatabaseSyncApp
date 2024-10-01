using System.ComponentModel.DataAnnotations;

namespace DatabaseSyncApp.Models;

public class Customer
{
    [Key]
    public int CustomerId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }

    public ICollection<Location> Locations { get; } = new List<Location>();
}