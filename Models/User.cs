using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StackFlow.Models
{
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [StringLength(150)] // Max length from schema
        public required string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Column("email")]
        [StringLength(255)] // Max length from schema
        public required string Email { get; set; } = string.Empty;

        [Required]
        [Column("password")]
        [StringLength(255)] // Max length for password hash
        public required string PasswordHash { get; set; } = string.Empty;

        [Column("role_id")] // Explicit foreign key property, good practice
        public required int Role_Id { get; set; } = 2;
        [ForeignKey("role_Id")]
        public Role? Role { get; set; } // Navigation property for Role

        [Column("created_at", TypeName = "date")] // Using TypeName for 'date'
        public DateTime Created_At { get; set; } = DateTime.UtcNow;



        // Navigation property for Tasks created by this user
        public ICollection<Ticket> CreatedTickets { get; set; }

        // Navigation property for Tasks assigned to this user
        public ICollection<Ticket> AssignedTickets { get; set; }

        // Navigation property for TaskComments made by this user
        public ICollection<Comment> TicketComments { get; set; }

        // Navigation property for Projects created by this user
        public ICollection<Project> CreatedProjects { get; set; }
    }
}
