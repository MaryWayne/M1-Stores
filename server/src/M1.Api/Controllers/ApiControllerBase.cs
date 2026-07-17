using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace M1.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? throw new InvalidOperationException("No user id claim"));
}
