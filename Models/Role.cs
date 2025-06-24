using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StackFlow.Models
{
    public class Role
    {
        public enum UserRole
        {
            Admin = 1,
            Project_Lead,
            Developer,
            Tester
        }

        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("role_name")]
        [StringLength(255)] // Max length from schema
        public string Name { get; set; } = string.Empty;

        [Column("role_description", TypeName = "text")] // TypeName for 'text'
        public string Description { get; set; } = string.Empty;

        public ICollection<User>? Users { get; set; }



        public void getRole(int roleId)
        {
            Name = ((UserRole)roleId).ToString();
        }
    }
}
