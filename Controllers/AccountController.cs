using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebSchoolPlanner.Controllers;

[Authorize]
[Route("account/")]
[Controller]
public sealed class AccountController : Controller
{
    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Route("settings")]
    public IActionResult Settings()
    {
        return View();
    }
}
