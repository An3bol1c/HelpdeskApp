using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HelpdeskApp.Models;

public class EquipmentEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [Required]
    public string Location { get; set; } = "";

    [BindNever]
    public string InventoryNumber { get; set; } = "";
}
