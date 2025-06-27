using DT_HR.Api.Contracts;
using DT_HR.Domain.Core.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT_HR.Api.Controllers;

[ApiController]

public class ApiController(IMediator mediator) : ControllerBase
{
	protected IMediator Mediator { get; } = mediator;

	/// <summary>
	/// Creates an <see cref="BadRequestObjectResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/>.
	/// response based on the specified <see cref="Result"/>.
	/// </summary>
	/// <param name="error">The error.</param>
	/// <returns>The created <see cref="BadRequestObjectResult"/> for the response.</returns>
	protected IActionResult BadRequest(Error error) => BadRequest(new ApiErrorResponse([error]));

	/// <summary>
	/// Creates an <see cref="OkObjectResult"/> that produces a <see cref="StatusCodes.Status200OK"/>.
	/// </summary>
	/// <returns>The created <see cref="OkObjectResult"/> for the response.</returns>
	/// <returns></returns>
	protected new IActionResult Ok(object value) => base.Ok(value);

	/// <summary>
	/// Creates an <see cref="OkObjectResult"/> that produces a <see cref="StatusCodes.Status200OK"/>.
	/// </summary>
	/// <returns>The created <see cref="OkObjectResult"/> for the response.</returns>
	/// <returns></returns>
	protected new IActionResult Ok(Guid value) => base.Ok(value);

	/// <summary>
	/// Creates an <see cref="OkObjectResult"/> that produces a <see cref="StatusCodes.Status200OK"/>.
	/// </summary>
	/// <returns>The created <see cref="OkObjectResult"/> for the response.</returns>
	/// <returns></returns>
	protected IActionResult Ok(bool value) => base.Ok(value);

	/// <summary>
	/// Creates an <see cref="OkObjectResult"/> that produces a <see cref="StatusCodes.Status200OK"/>.
	/// </summary>
	/// <returns>The created <see cref="OkObjectResult"/> for the response.</returns>
	/// <returns></returns>
	protected new IActionResult Ok(int value) => base.Ok(value);
		

	/// <summary>
	/// Creates an <see cref="NotFoundResult"/> that produces a <see cref="StatusCodes.Status404NotFound"/>.
	/// </summary>
	/// <returns>The created <see cref="NotFoundResult"/> for the response.</returns>
	protected new IActionResult NotFound() => base.NotFound(new ApiErrorResponse([new Error(StatusCodes.Status404NotFound.ToString(), "Item not found")]));
}
