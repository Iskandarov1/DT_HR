using DT_HR.Api.Contracts;
using DT_HR.Application.Users.Commands;
using DT_HR.Application.Users.Commands.UserRole;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Enumeration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DT_HR.Api.Controllers;


[Route("api/[controller]")]
public class AdminController(IMediator mediator) : ApiController(mediator)
{
    [HttpPost("users/{id}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ChangeUserRole(Guid id, [FromQuery] int roleValue, CancellationToken cancellationToken)
    {

        var roleResult = UserRole.FromValue(roleValue);
        
        if (roleResult.HasNoValue)
        {
            return BadRequest(new Error("invalid_role", $"Role with value {roleValue} does not exist"));
        }
        
        var command = new ChangeUserRoleCommand(id, roleResult.Value);
        var result = await Mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }
    



        // await Result.Success(new ChangeUserRoleCommand(id, role))
    //     .Bind(command => Mediator.Send(command, HttpContext.RequestAborted))
    //     .Match(Ok, BadRequest);




    // [HttpPost("users/{id}/role")] 
    // public async Task<IActionResult> ChangeUserRole(Guid id, [FromQuery] UserRole role, CancellationToken cancellationToken)
    // {
    //     var command = new ChangeUserRoleCommand(id, role);
    //     var result = await mediator.Send(command, cancellationToken);
    //     return result.IsSuccess ? Ok() : NotFound(result.Error);
    // }

}