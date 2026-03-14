using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;
using Api2.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api2.WebApi.Controllers;

/// <summary>
/// Controller for column operations within a retro board.
/// </summary>
[ApiController]
[Route("api/retros/{retroId:guid}/columns")]
public class ColumnsController : ControllerBase
{
    private readonly IColumnService _columnService;

    /// <summary>
    /// Initializes a new instance of <see cref="ColumnsController"/>.
    /// </summary>
    /// <param name="columnService">The column service.</param>
    public ColumnsController(IColumnService columnService)
    {
        _columnService = columnService;
    }

    /// <summary>Creates a new column in a retro board.</summary>
    /// <param name="retroId">The retro board ID.</param>
    /// <param name="request">The column creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created column.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ColumnResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid retroId, CreateColumnRequest request, CancellationToken cancellationToken)
    {
        ColumnResponse response = await _columnService.CreateAsync(retroId, request, cancellationToken);
        return CreatedAtAction(nameof(Create), new { retroId, columnId = response.Id }, response);
    }

    /// <summary>Updates an existing column's name.</summary>
    /// <param name="retroId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="request">The column update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated column.</returns>
    [HttpPut("{columnId:guid}")]
    [ProducesResponseType(typeof(ColumnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid retroId, Guid columnId, UpdateColumnRequest request, CancellationToken cancellationToken)
    {
        ColumnResponse response = await _columnService.UpdateAsync(retroId, columnId, request, cancellationToken);
        return Ok(response);
    }

    /// <summary>Soft-deletes a column.</summary>
    /// <param name="retroId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{columnId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid retroId, Guid columnId, CancellationToken cancellationToken)
    {
        await _columnService.DeleteAsync(retroId, columnId, cancellationToken);
        return NoContent();
    }
}
