using Microsoft.AspNetCore.Mvc;

namespace WebSchoolPlanner.Controllers;

[Controller]
[Route(RoutePrefix + "Test")]
public class TestController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
