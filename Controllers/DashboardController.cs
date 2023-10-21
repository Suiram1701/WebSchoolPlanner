﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebSchoolPlanner.Controllers;

[Authorize]
[Controller]
[Route("/")]
public sealed class DashboardController : Controller
{
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }
}
