using StackFlow.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace StackFlow.Api.DataTransferObjects
{
    public class UserDTO
    {
            public string? Name { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }

            public int Role_Id = 2;// Assign the ID of the 'Developer' role
            public DateTime Created_At = DateTime.UtcNow;
            public bool IsVerified = false;// New users are unverified by default
            public bool IsDeleted = false; // New users are not deleted by default
    }
}
