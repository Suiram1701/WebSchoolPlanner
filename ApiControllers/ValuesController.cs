using Microsoft.AspNetCore.Mvc;

namespace WebSchoolPlanner.ApiControllers;

[Route(ApiPrefix + "values")]
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
public class ValuesController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public IActionResult Get_V1()
    {
        return Ok("V1");
    }

    [HttpGet]
    [MapToApiVersion("2.0")]
    public IActionResult Get_V2()
    {
        return Ok("V2");
    }
}
