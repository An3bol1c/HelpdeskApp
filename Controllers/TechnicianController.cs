using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskApp.Controllers;

[Authorize(Roles = "Technician,Admin")]
public class TechnicianController : Controller
{
    public IActionResult Index() => View();
}