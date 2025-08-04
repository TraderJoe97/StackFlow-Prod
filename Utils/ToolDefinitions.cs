csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using StackFlow.ApiControllers.Dtos;

namespace StackFlow.Utils
{
    public class ToolDefinitions
    {
        private readonly IHttpClientFactory _httpClientFactory;
 private readonly IHttpContextAccessor _httpContextAccessor;
 public ToolDefinitions(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
 _httpContextAccessor = httpContextAccessor;
        }

        public List<ToolDefinition> Definitions { get; } = new List<ToolDefinition>();

        public void InitializeToolDefinitions()
        {
            // Project Tools
            Definitions.Add(new ToolDefinition
            {
                Name = "GetAllProjects",
                Description = "Retrieves a list of all projects.",
                Parameters = new Dictionary<string, string>(),
                Execute = async (parameters, jwt) => await CallApiAsync<List<ProjectDto>>("api/ProjectApi", HttpMethod.Get, null, jwt)
            });

            Definitions.Add(new ToolDefinition
            {
                Name = "GetProjectById",
                Description = "Retrieves a project by its ID.",
                Parameters = new Dictionary<string, string> { { "projectId", "The ID of the project to retrieve." } },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("projectId", out var projectId) && int.TryParse(projectId, out var id))
                    {
                        return await CallApiAsync<ProjectDto>($"api/ProjectApi/{id}", HttpMethod.Get, null, jwt);
                    }
                    return "Error: Project ID parameter missing or invalid.";
                }
            });

            Definitions.Add(new ToolDefinition
            {
                Name = "CreateProject",
                Description = "Creates a new project.",
                Parameters = new Dictionary<string, string> {
                    { "name", "The name of the new project." },
                    { "description", "The description of the new project." }
                },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("name", out var name) && parameters.TryGetValue("description", out var description))
                    {
                        var projectDto = new CreateProjectRequest { Name = name, Description = description };
                        return await CallApiAsync<ProjectDto>("api/ProjectApi", HttpMethod.Post, projectDto, jwt);
                    }
                    return "Error: Name or description parameter missing for project creation.";
                }
            });

            Definitions.Add(new ToolDefinition
            {
                Name = "UpdateProject",
                Description = "Updates an existing project.",
                Parameters = new Dictionary<string, string> {
                    { "projectId", "The ID of the project to update." },
                    { "name", "The new name of the project (optional)." },
                    { "description", "The new description of the project (optional)." }
                },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("projectId", out var projectId) && int.TryParse(projectId, out var id))
                    {
                        var projectDto = new UpdateProjectRequest { ProjectId = id };
                        if (parameters.TryGetValue("name", out var name)) projectDto.Name = name;
                        if (parameters.TryGetValue("description", out var description)) projectDto.Description = description;

                        return await CallApiAsync<ProjectDto>($"api/ProjectApi/{id}", HttpMethod.Put, projectDto, jwt);
                    }
                    return "Error: Project ID parameter missing or invalid for project update.";
                }
            });

            Definitions.Add(new ToolDefinition
            {
                Name = "DeleteProject",
                Description = "Deletes a project by its ID.",
                Parameters = new Dictionary<string, string> { { "projectId", "The ID of the project to delete." } },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("projectId", out var projectId) && int.TryParse(projectId, out var id))
                    {
                        return await CallApiAsync<object>($"api/ProjectApi/{id}", HttpMethod.Delete, null, jwt);
                    }
                    return "Error: Project ID parameter missing or invalid for project deletion.";
                }
            });

            // Ticket Tools
             Definitions.Add(new ToolDefinition
            {
                Name = "GetAllTickets",
                Description = "Retrieves a list of all tickets.",
                Parameters = new Dictionary<string, string>(),
                Execute = async (parameters, jwt) => await CallApiAsync<List<TicketDto>>("api/TicketsAPI", HttpMethod.Get, null, jwt)
            });

             Definitions.Add(new ToolDefinition
            {
                Name = "GetTicketById",
                Description = "Retrieves a ticket by its ID.",
                Parameters = new Dictionary<string, string> { { "ticketId", "The ID of the ticket to retrieve." } },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("ticketId", out var ticketId) && int.TryParse(ticketId, out var id))
                    {
                        return await CallApiAsync<TicketDto>($"api/TicketsAPI/{id}", HttpMethod.Get, null, jwt);
                    }
                    return "Error: Ticket ID parameter missing or invalid.";
                }
            });

             Definitions.Add(new ToolDefinition
            {
                Name = "CreateTicket",
                Description = "Creates a new ticket.",
                Parameters = new Dictionary<string, string> {
                    { "title", "The title of the new ticket." },
                    { "description", "The description of the new ticket." },
                    { "projectId", "The ID of the project the ticket belongs to." },
                    { "assignedUserId", "The ID of the user assigned to the ticket (optional)." },
                    { "status", "The status of the ticket (optional, e.g., Open, InProgress, Closed)." }
                },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("title", out var title) && parameters.TryGetValue("description", out var description) && parameters.TryGetValue("projectId", out var projectId) && int.TryParse(projectId, out var projId))
                    {
                        var ticketDto = new CreateTicketRequest { Title = title, Description = description, ProjectId = projId };
                        if (parameters.TryGetValue("assignedUserId", out var assignedUserId) && int.TryParse(assignedUserId, out var assignedId)) ticketDto.AssignedUserId = assignedId;
                        if (parameters.TryGetValue("status", out var status)) ticketDto.Status = status;
                        return await CallApiAsync<TicketDto>("api/TicketsAPI", HttpMethod.Post, ticketDto, jwt);
                    }
                    return "Error: Missing required parameters for ticket creation (title, description, projectId).";
                }
            });

             Definitions.Add(new ToolDefinition
            {
                Name = "UpdateTicket",
                Description = "Updates an existing ticket.",
                Parameters = new Dictionary<string, string> {
                    { "ticketId", "The ID of the ticket to update." },
                    { "title", "The new title of the ticket (optional)." },
                    { "description", "The new description of the ticket (optional)." },
                    { "projectId", "The new project ID the ticket belongs to (optional)." },
                    { "assignedUserId", "The new assigned user ID for the ticket (optional)." },
                    { "status", "The new status of the ticket (optional)." }
                },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("ticketId", out var ticketId) && int.TryParse(ticketId, out var id))
                    {
                        var ticketDto = new UpdateTicketRequest { TicketId = id };
                        if (parameters.TryGetValue("title", out var title)) ticketDto.Title = title;
                        if (parameters.TryGetValue("description", out var description)) ticketDto.Description = description;
                        if (parameters.TryGetValue("projectId", out var projectId) && int.TryParse(projectId, out var projId)) ticketDto.ProjectId = projId;
                        if (parameters.TryGetValue("assignedUserId", out var assignedUserId) && int.TryParse(assignedUserId, out var assignedId)) ticketDto.AssignedUserId = assignedId;
                        if (parameters.TryGetValue("status", out var status)) ticketDto.Status = status;

                        return await CallApiAsync<TicketDto>($"api/TicketsAPI/{id}", HttpMethod.Put, ticketDto, jwt);
                    }
                    return "Error: Ticket ID parameter missing or invalid for ticket update.";
                }
            });

             Definitions.Add(new ToolDefinition
            {
                Name = "DeleteTicket",
                Description = "Deletes a ticket by its ID.",
                Parameters = new Dictionary<string, string> { { "ticketId", "The ID of the ticket to delete." } },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("ticketId", out var ticketId) && int.TryParse(ticketId, out var id))
                    {
                        return await CallApiAsync<object>($"api/TicketsAPI/{id}", HttpMethod.Delete, null, jwt);
                    }
                    return "Error: Ticket ID parameter missing or invalid for ticket deletion.";
                }
            });

            // Comment Tools
            Definitions.Add(new ToolDefinition
            {
                Name = "GetCommentsForTicket",
                Description = "Retrieves comments for a specific ticket.",
                Parameters = new Dictionary<string, string> { { "ticketId", "The ID of the ticket to get comments for." } },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("ticketId", out var ticketId) && int.TryParse(ticketId, out var id))
                    {
                        return await CallApiAsync<List<CommentDto>>($"api/TicketCommentsApi/ticket/{id}", HttpMethod.Get, null, jwt);
                    }
                    return "Error: Ticket ID parameter missing or invalid for getting comments.";
                }
            });

             Definitions.Add(new ToolDefinition
            {
                Name = "AddCommentToTicket",
                Description = "Adds a comment to a ticket.",
                Parameters = new Dictionary<string, string> {
                    { "ticketId", "The ID of the ticket to add the comment to." },
                    { "content", "The content of the comment." }
                },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("ticketId", out var ticketId) && int.TryParse(ticketId, out var id) && parameters.TryGetValue("content", out var content))
                    {
                        var commentDto = new CreateCommentRequest { TicketId = id, Content = content };
                        return await CallApiAsync<CommentDto>("api/TicketCommentsApi", HttpMethod.Post, commentDto, jwt);
                    }
                    return "Error: Missing required parameters for adding comment (ticketId, content).";
                }
            });

            // User Tools
             Definitions.Add(new ToolDefinition
            {
                Name = "GetAllUsers",
                Description = "Retrieves a list of all users.",
                Parameters = new Dictionary<string, string>(),
                Execute = async (parameters, jwt) => await CallApiAsync<List<UserDto>>("api/UsersApi", HttpMethod.Get, null, jwt)
            });

             Definitions.Add(new ToolDefinition
            {
                Name = "GetUserById",
                Description = "Retrieves a user by their ID.",
                Parameters = new Dictionary<string, string> { { "userId", "The ID of the user to retrieve." } },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("userId", out var userId) && int.TryParse(userId, out var id))
                    {
                        return await CallApiAsync<UserDto>($"api/UsersApi/{id}", HttpMethod.Get, null, jwt);
                    }
                    return "Error: User ID parameter missing or invalid.";
                }
            });

             Definitions.Add(new ToolDefinition
            {
                Name = "UpdateUser",
                Description = "Updates an existing user.",
                Parameters = new Dictionary<string, string> {
                    { "userId", "The ID of the user to update." },
                    { "email", "The new email of the user (optional)." },
                    { "username", "The new username of the user (optional)." },
                    { "roleId", "The new role ID of the user (optional)." }
                },
                Execute = async (parameters, jwt) =>
                {
                    if (parameters.TryGetValue("userId", out var userId) && int.TryParse(userId, out var id))
                    {
                        var userDto = new UpdateUserRequest { UserId = id };
                        if (parameters.TryGetValue("email", out var email)) userDto.Email = email;
                        if (parameters.TryGetValue("username", out var username)) userDto.Username = username;
                         if (parameters.TryGetValue("roleId", out var roleId) && int.TryParse(roleId, out var rId)) userDto.RoleId = rId;

                        return await CallApiAsync<UserDto>($"api/UsersApi/{id}", HttpMethod.Put, userDto, jwt);
                    }
                    return "Error: User ID parameter missing or invalid for user update.";
                }
            });


            // Role Tools
            Definitions.Add(new ToolDefinition
            {
                Name = "GetAllRoles",
                Description = "Retrieves a list of all roles.",
                Parameters = new Dictionary<string, string>(),
                Execute = async (parameters, jwt) => await CallApiAsync<List<RoleDto>>("api/RolesApi", HttpMethod.Get, null, jwt)
            });
        }

        private async Task<object> CallApiAsync<T>(string endpoint, HttpMethod method, object requestBody, string jwt)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var request = _httpContextAccessor.HttpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";

                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                HttpRequestMessage request = new HttpRequestMessage(method, endpoint);
                if (requestBody != null)
                {
                    request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                }

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error calling API: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Exception while calling API: {ex.Message}";
            }
        }
    }

    public class ToolDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public Func<Dictionary<string, string>, string, Task<object>> Execute { get; set; }
    }
}