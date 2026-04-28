using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SmartHome.Api.Contracts.Scenes;
using SmartHome.Domain.Scene;

namespace SmartHome.Api.Controller;

/// <summary>
/// API controller for managing smart home scenes — named presets that execute
/// multiple device operations together (e.g., "Goodnight" locks doors, dims lights,
/// and sets the thermostat).
/// </summary>
/// <param name="sceneService">The service coordinating scene CRUD and execution.</param>
[ApiController]
[Route("api/scenes")]
[Authorize]
[Produces("application/json")]
public sealed class ScenesController(ISceneService sceneService) : ControllerBase
{
    /// <summary>
    /// Retrieves all scenes.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <response code="200">The list of scenes.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SceneResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SceneResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var scenes = await sceneService.GetAllScenesAsync(cancellationToken);
        return Ok(scenes.Select(SceneMapper.ToResponse));
    }

    /// <summary>
    /// Retrieves a specific scene by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the scene.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <response code="200">The scene.</response>
    /// <response code="404">No scene exists with the specified identifier.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SceneResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        // Service throws ResourceNotFoundException if missing, which the
        // global handler maps to a 404 Problem Details response.
        var scene = await sceneService.GetSceneAsync(id, cancellationToken);
        return Ok(SceneMapper.ToResponse(scene));
    }

    /// <summary>
    /// Creates a new scene.
    /// </summary>
    /// <param name="request">The scene definition.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <response code="201">The newly created scene.</response>
    /// <response code="400">The request failed validation or a domain invariant.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SceneResponse>> Create(
        [FromBody] SceneRequest request,
        CancellationToken cancellationToken)
    {
        var actions = SceneMapper.ToDomain(request.Actions);
        var scene = await sceneService.CreateSceneAsync(request.Name, actions, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = scene.Id },
            SceneMapper.ToResponse(scene));
    }

    /// <summary>
    /// Updates a scene's name and actions. The action list is replaced wholesale.
    /// </summary>
    /// <param name="id">The unique identifier of the scene to update.</param>
    /// <param name="request">The new scene definition.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <response code="200">The updated scene.</response>
    /// <response code="400">The request failed validation or a domain invariant.</response>
    /// <response code="404">No scene exists with the specified identifier.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SceneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SceneResponse>> Update(
        Guid id,
        [FromBody] SceneRequest request,
        CancellationToken cancellationToken)
    {
        var actions = SceneMapper.ToDomain(request.Actions);
        var scene = await sceneService.UpdateSceneAsync(id, request.Name, actions, cancellationToken);

        return Ok(SceneMapper.ToResponse(scene));
    }

    /// <summary>
    /// Deletes a scene.
    /// </summary>
    /// <param name="id">The unique identifier of the scene to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <response code="204">The scene was deleted.</response>
    /// <response code="404">No scene exists with the specified identifier.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        // Service throws ResourceNotFoundException if missing, which the
        // global handler maps to a 404 Problem Details response — same
        // shape as the GetById/Update/Execute 404s.
        await sceneService.DeleteSceneAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Executes a scene — runs each of its actions in order, recording the outcome
    /// of each in the device command history. Individual action failures do not
    /// abort the batch; per-action results are reported in the response.
    /// </summary>
    /// <param name="id">The unique identifier of the scene to execute.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <response code="200">The execution result with per-action outcomes.</response>
    /// <response code="404">No scene exists with the specified identifier.</response>
    [HttpPost("{id:guid}/execute")]
    [ProducesResponseType(typeof(SceneExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SceneExecutionResponse>> Execute(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sceneService.ExecuteSceneAsync(id, cancellationToken);
        return Ok(SceneMapper.ToResponse(result));
    }
}