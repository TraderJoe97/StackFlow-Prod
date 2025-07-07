using System; // For DateTime

namespace StackFlow.ApiControllers
{
    // This namespace will contain all DTOs used by API controllers.
    // This helps in organizing and reusing DTOs across different API controllers.
    namespace Dtos
    {
        /// <summary>
        /// Represents user data returned by the API, excluding sensitive information like password hash.
        /// </summary>
        public class UserDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Role { get; set; } // This will be the Role.Title
            public bool IsVerified { get; set; }
            public bool IsDeleted { get; set; }
            public DateTime Created_At { get; set; }
        }

        /// <summary>
        /// DTO for API Login Request.
        /// </summary>
        public class LoginApiRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        /// <summary>
        /// DTO for API Login Response, containing the JWT token and user details.
        /// </summary>
        public class LoginApiResponse
        {
            public string Token { get; set; }
            public UserDto User { get; set; }
        }

        /// <summary>
        /// DTO for API User Registration Request.
        /// </summary>
        public class RegisterUserDto
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
        }

        /// <summary>
        /// DTO for updating a user's username (self-update via API).
        /// </summary>
        public class UpdateUsernameDto
        {
            public string NewUsername { get; set; }
        }

        /// <summary>
        /// DTO for updating a user's password (self-update via API).
        /// </summary>
        public class UpdatePasswordDto
        {
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
            public string ConfirmNewPassword { get; set; }
        }

        /// <summary>
        /// DTO for updating a user's role by an admin (via API).
        /// </summary>
        public class UpdateUserRoleDto
        {
            public int NewRoleId { get; set; }
        }

        /// <summary>
        /// DTO for Project data returned by the API.
        /// </summary>
        public class ProjectDto
        {
            public int Id { get; set; }
            public string ProjectName { get; set; }
            public string ProjectDescription { get; set; }
            public DateTime ProjectStartDate { get; set; }
            public DateTime? ProjectEndDate { get; set; }
            public string ProjectStatus { get; set; }
            public int CreatedByUserId { get; set; }
            public string CreatedByUsername { get; set; } // To display creator's username
            public int TotalTickets { get; set; } // Count of associated tickets
        }

        /// <summary>
        /// DTO for creating a new Project via API.
        /// </summary>
        public class CreateProjectDto
        {
            public string ProjectName { get; set; }
            public string ProjectDescription { get; set; }
            public DateTime ProjectStartDate { get; set; }
            public DateTime? ProjectEndDate { get; set; }
            public string ProjectStatus { get; set; }
        }

        /// <summary>
        /// DTO for updating an existing Project via API.
        /// </summary>
        public class UpdateProjectDto
        {
            public string ProjectName { get; set; }
            public string ProjectDescription { get; set; }
            public DateTime ProjectStartDate { get; set; }
            public DateTime? ProjectEndDate { get; set; }
            public string ProjectStatus { get; set; }
        }

        /// <summary>
        /// DTO for Role data returned by the API.
        /// </summary>
        public class RoleDto
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
        }

        /// <summary>
        /// DTO for Ticket data returned by the API.
        /// </summary>
        public class TicketDto
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public int ProjectId { get; set; }
            public string ProjectName { get; set; }
            public int? AssignedToUserId { get; set; }
            public string AssignedToUsername { get; set; }
            public string Status { get; set; }
            public string Priority { get; set; }
            public int CreatedByUserId { get; set; }
            public string CreatedByUsername { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? CompletedAt { get; set; }
        }

        /// <summary>
        /// DTO for creating a new Ticket via API.
        /// </summary>
        public class CreateTicketDto
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public int ProjectId { get; set; }
            public int? AssignedToUserId { get; set; }
            public string Status { get; set; }
            public string Priority { get; set; }
            public DateTime? DueDate { get; set; }
        }

        /// <summary>
        /// DTO for updating an existing Ticket via API.
        /// </summary>
        public class UpdateTicketDto
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public int ProjectId { get; set; }
            public int? AssignedToUserId { get; set; }
            public string Status { get; set; }
            public string Priority { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? CompletedAt { get; set; }
        }

        /// <summary>
        /// DTO for Ticket Comment data returned by the API.
        /// </summary>
        public class TicketCommentDto
        {
            public int Id { get; set; }
            public int TicketId { get; set; }
            public int UserId { get; set; } // Maps to Created_By in DB snapshot
            public string Username { get; set; } // To display commenter's username
            public string CommentText { get; set; }
            public DateTime CommentCreatedAt { get; set; }
        }

        /// <summary>
        /// DTO for creating a new Ticket Comment via API.
        /// </summary>
        public class CreateTicketCommentDto
        {
            public string CommentText { get; set; }
        }

        /// <summary>
        /// DTO for updating an existing Ticket Comment via API.
        /// </summary>
        public class UpdateTicketCommentDto
        {
            public string CommentText { get; set; }
        }
    }
}
