using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StackFlow.Models
{
    enum ProjectStatus
    {
        Active = 1,
        Complete,
        On_Hold
    }
    public class Project
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("project_name")]
        [StringLength(255)] // Max length based on common naming conventions, schema says varchar 255 for username/email. Adjust if you have a specific length in mind for project name.
        public required string Name { get; set; } = string.Empty;


        [Column("project_description", TypeName = "text")] // TypeName for 'text'
        public string Description { get; set; } = string.Empty;


        [Column("project_status")]
        [StringLength(50)] // Max length based on schema (varchar 50 for task status)
        public string Status { get; set; } = ProjectStatus.Active.ToString();


        [Column("project_start_date", TypeName = "date")]
        public DateTime? Start_Date { get; set; } 


        [Column("project_due_date", TypeName = "date")]
        public DateTime? Due_Date { get; set; } 


        [Column("created_by")]
        public int Created_By { get; set; }

        [ForeignKey("Created_By")]
        public User? CreatedBy { get; set; }

        // Navigation property for Tasks belonging to this project
        public ICollection<Ticket>? Tickets { get; set; }

        public void getProjectStatus(int statusId)
        {
            Status = ((ProjectStatus)statusId).ToString();
        }

    }
}

