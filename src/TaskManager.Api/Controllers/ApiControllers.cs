using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskManager.Application;

namespace TaskManager.Api.Controllers;

[ApiController]
[Authorize]
public abstract class AuthenticatedController : ControllerBase
{
    protected Guid UserId => Guid.Parse(User.FindFirstValue("sub")!);
}

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(Credentials request, CancellationToken cancellationToken)
    {
        var user = await auth.RegisterAsync(request, cancellationToken); return Created("/api/users/me", user);
    }
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(Credentials request, CancellationToken cancellationToken) =>
        Ok(await auth.LoginAsync(request, cancellationToken));
}

[Route("api/users")]
public sealed class UsersController(AuthService auth) : AuthenticatedController
{
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken) => Ok(await auth.GetUserAsync(UserId, cancellationToken));
}

[ApiController]
[Route("api/public")]
[AllowAnonymous]
public sealed class PublicController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult Info() => Ok(new { name = "TaskManager", version = "1.0", statuses = new[] { "Todo", "InProgress", "Completed" } });
}

[Route("api/tasks")]
public sealed class TasksController(TaskService tasks) : AuthenticatedController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> List(CancellationToken cancellationToken) => Ok(await tasks.ListAsync(UserId, cancellationToken));
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> Get(Guid id, CancellationToken cancellationToken) => Ok(await tasks.GetAsync(id, UserId, cancellationToken));
    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(TaskInput request, CancellationToken cancellationToken)
    {
        var task = await tasks.CreateAsync(UserId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, TaskInput request, CancellationToken cancellationToken)
    {
        await tasks.UpdateAsync(id, UserId, request, cancellationToken); return NoContent();
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await tasks.DeleteAsync(id, UserId, cancellationToken); return NoContent();
    }
}
