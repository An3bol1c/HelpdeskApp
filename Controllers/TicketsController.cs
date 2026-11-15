using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskApp.Controllers;

[Authorize]
public class TicketsController : Controller
{
    public IActionResult Index() => View();
}