csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackFlow.ApiControllers.Dtos; // Assuming your DTOs are in this namespace
using StackFlow.Utils; // Assuming your AI orchestration logic is here

namespace StackFlow.Controllers
{
    [Authorize] // Ensures only authenticated users can access this controller
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly ILogger<AiController> _logger;
        private readonly IAIOrchestrator _aiOrchestrator; // Assuming you have an interface for your orchestrator

        public AiController(ILogger<AiController> logger, IAIOrchestrator aiOrchestrator)
        {
            _logger = logger;
            _aiOrchestrator = aiOrchestrator;
        }

        [HttpPost("process")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AiResponseDto), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ProcessInput([FromBody] AiInputDto input)
        {
            if (input == null || string.IsNullOrEmpty(input.Prompt))
            {
                return BadRequest(new { error = "Input prompt cannot be empty." });
            }

            try
            {
                // Assuming your orchestrator has a method to process the input
                // and returns a structured response or a string
                var aiResponse = await _aiOrchestrator.ProcessUserPromptAsync(input.Prompt, User);

                // Assuming AiResponseDto is a suitable structure for the response
                // You might need to map the orchestrator's output to AiResponseDto
                var responseDto = new AiResponseDto { ResponseText = aiResponse };

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI input.");
                return StatusCode(500, new { error = "An error occurred while processing your request." });
            }
        }
    }

    // Example DTOs - adjust namespaces and properties as per your project
    namespace StackFlow.ApiControllers.Dtos
    {
        public class AiInputDto
        {
            public string Prompt { get; set; }
        }

        public class AiResponseDto
        {
            public string ResponseText { get; set; }
            // Add other properties as needed for tool calls, etc.
        }
    }

    // Example Interface for AI Orchestrator - adjust namespace
    namespace StackFlow.Utils
    {
        public interface IAIOrchestrator
        {
            Task<string> ProcessUserPromptAsync(string prompt, System.Security.Claims.ClaimsPrincipal user);
        }
    }
}