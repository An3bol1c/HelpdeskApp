using Microsoft.AspNetCore.Mvc.Rendering;

namespace HelpdeskApp.Models;

public class TicketBrowseViewModel
{
    public IReadOnlyList<TicketListItemViewModel> Tickets { get; set; } = [];
    public string Status { get; set; } = "active";
    public string? Search { get; set; }
    public string? ReporterId { get; set; }
    public int? CategoryId { get; set; }
    public int? EquipmentId { get; set; }
    public string? Priority { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string SortBy { get; set; } = "created";
    public string SortDirection { get; set; } = "desc";
    public int ActiveCount { get; set; }
    public int ClosedCount { get; set; }
    public IReadOnlyList<SelectListItem> Reporters { get; set; } = [];
    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
    public IReadOnlyList<SelectListItem> Equipment { get; set; } = [];
    public IReadOnlyList<SelectListItem> Priorities { get; set; } = [];

    public string SortIndicator(string column)
    {
        if (!SortBy.Equals(column, StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        return SortDirection == "asc" ? "↑" : "↓";
    }

    public string NextSortDirection(string column)
    {
        return SortBy.Equals(column, StringComparison.OrdinalIgnoreCase)
            ? SortDirection == "asc" ? "desc" : "asc"
            : "asc";
    }

    public string FilterIndicator(string column)
    {
        return column switch
        {
            "title" when !string.IsNullOrWhiteSpace(Search) => "▼",
            "reporter" when !string.IsNullOrWhiteSpace(ReporterId) => "▼",
            "status" => "▼",
            "priority" when !string.IsNullOrWhiteSpace(Priority) => "▼",
            "category" when CategoryId.HasValue => "▼",
            "equipment" when EquipmentId.HasValue => "▼",
            "created" when FromDate.HasValue || ToDate.HasValue => "▼",
            _ => ""
        };
    }
}
