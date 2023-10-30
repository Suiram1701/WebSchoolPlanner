using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebSchoolPlanner.Controllers;

[Authorize]
[Route("Account/")]
[Controller]
public sealed class AccountController : Controller
{
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    [Route("Settings")]
    public IActionResult Settings()
    {
        return View();
    }
}
