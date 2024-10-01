using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatabaseSyncApp.Models;

public class Location
{
    [Key]
    public int LocationId { get; set; }

    public string Address { get; set; } = null!;
    
    [ForeignKey(nameof(Customer))]
    public int CustomerId { get; set; }
    
    public Customer Customer { get; }
}