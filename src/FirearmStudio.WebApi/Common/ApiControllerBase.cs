using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Common;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected string CurrentApiVersion =>
        HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";

    protected string VersionedUrl(string path) =>
        $"/api/v{CurrentApiVersion}/{path.TrimStart('/')}";
}
