using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SPEAgentWithRetrieval.Core.Models;

namespace WebApiTemp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly Microsoft365Options _microsoft365Options;

    public ConfigController(IOptions<Microsoft365Options> microsoft365Options)
    {
        _microsoft365Options = microsoft365Options.Value;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            clientId = _microsoft365Options.ClientId,
            tenantId = _microsoft365Options.TenantId
        });
    }
}
