using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebSchoolPlanner.Controllers;

[Authorize]
[Route("account/")]
[Controller]
public sealed class AccountController : Controller
{
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    [Route("settings")]
    public IActionResult Settings()
    {
        return View();
    }
}
