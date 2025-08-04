csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // Make sure System.Text.Json is correctly imported
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure;
using Azure.AI.OpenAI;
public class AiOrchestrator
{
    private readonly IEnumerable<IAiTool> _tools;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient _openAIClient;

    public AiOrchestrator(IEnumerable<IAiTool> tools)
    {

    }

 public async Task<string> OrchestrateAsync(string userInput, List<ChatRequestMessage> chatHistory)
    {
        IAiTool selectedTool = null;
        // string relevantInfo = null; // This variable is not used

        // Prepare the conversation history for the OpenAI model
        var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            Messages =
            {
                new ChatRequestSystemMessage("You are a helpful AI assistant for the StackFlow application. You can assist users with tasks related to projects and tickets. You have access to tools that allow you to retrieve information about projects and tickets. When a user asks a question, determine which tool is most relevant and extract any necessary parameters. If no tool is suitable, provide a helpful response."),
                new ChatRequestUserMessage(userInput),
            },
            Tools = _tools.Select(tool => new ChatCompletionsToolDefinition()
            {
                Type = "function",
                Function = new FunctionDefinition()
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Parameters = BinaryData.FromObjectAsJson(tool.Parameters, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true })
                }
            }).ToList(),
            ToolChoice = new ChatCompletionsToolChoice("auto")
        };

        try
        {
            // var conversationHistory = new List<ChatRequestMessage> // We now pass in the history
            
            chatHistory.Add(new ChatRequestUserMessage(userInput));
            {
            };

            while (true)
            {
                chatCompletionsOptions.Messages.Clear();
                foreach (var message in conversationHistory)
                {
                    chatCompletionsOptions.Messages.Add(message);
                }

                Response<ChatCompletions> response = await _openAIClient.GetChatCompletionsAsync(
                    _configuration["AzureOpenAI:DeploymentName"], // Replace with your deployment name
                    chatCompletionsOptions);

                ChatChoice responseChoice = response.Value.Choices[0]; // Assuming there's at least one choice
                conversationHistory.Add(responseChoice.Message);

                if (responseChoice.FinishReason == CompletionsFinishReason.ToolCalls)
                {
                    foreach (var toolCall in responseChoice.Message.ToolCalls)
                    {
                        string functionName = toolCall.Function.Name;
                        string functionArguments = toolCall.Function.Arguments;

                        selectedTool = _tools.FirstOrDefault(t => t.Name == functionName);

                        if (selectedTool != null)
                        {
                            try
                            {
                                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(functionArguments, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                                var toolResult = await selectedTool.ExecuteAsync(parameters);
                                conversationHistory.Add(new ChatRequestToolMessage(JsonSerializer.Serialize(toolResult), toolCall.Id));
                            }
                            catch (Exception ex)
                            {
                                conversationHistory.Add(new ChatRequestToolMessage($"Error executing tool {functionName}: {ex.Message}", toolCall.Id));
                            }
                        }
                        else
                        {
                            conversationHistory.Add(new ChatRequestToolMessage($"Tool not found: {functionName}", toolCall.Id));
                        }
                    }
                }
                else if (responseChoice.FinishReason == CompletionsFinishReason.Stop)
                {
                    return responseChoice.Message.Content;
                }
                else
                {
                    // Handle other finish reasons or unexpected responses
                    return "I'm sorry, I couldn't fully process your request.";
                }
            }
        }
        catch (Exception ex)
        {
            // Catch any other unexpected errors
            return $"An unexpected error occurred: {ex.Message}";
        }
    }

    public AiOrchestrator(IConfiguration configuration, IEnumerable<IAiTool> tools)
    {
        _configuration = configuration;
        _openAIClient = new OpenAIClient(
            new Uri(_configuration["AzureOpenAI:Endpoint"]),
            new AzureKeyCredential(_configuration["AzureOpenAI:ApiKey"]));
        _tools = tools;
    }
}

