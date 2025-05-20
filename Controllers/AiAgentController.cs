using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AiAgentController : ControllerBase
{
    private readonly ClaudeService _claudeService;

    public AiAgentController(ClaudeService claudeService)
    {
        _claudeService = claudeService;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeUserMessage([FromBody] UserMessageDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Message))
        {
            return BadRequest("Message is required.");
        }

        try
        {
            var result = await _claudeService.GetIntentFromMessage(input.Message);
            return Ok(result); 
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
