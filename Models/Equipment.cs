using System.ComponentModel.DataAnnotations;

namespace HelpdeskApp.Models;

public class Equipment
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = "";
    [Required]
    public string InventoryNumber { get; set; } = "";
    public string Location { get; set; } = "";
}