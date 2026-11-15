using System.ComponentModel.DataAnnotations;

namespace HelpdeskApp.Models;

public class Ticket
{
    public int Id { get; set; }
    [Required, StringLength(150)]
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? ReporterId { get; set; }
    public string? AssignedToId { get; set; }
}