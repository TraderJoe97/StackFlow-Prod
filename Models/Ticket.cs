using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StackFlow.Models
{

        enum TicketStatus
        {
            To_Do = 1,
            In_Progress,
            In_Review,
            Done
        }

        enum TicketPriority
        {
            Low = 1,
            Medium,
            High
        }

        public class Ticket
        {

        [Key]
        [Column("id")]
        public static int Id { get; set; }

        [Required]
        [Column("ticket_title")]
        [StringLength(255)] // Max length from schema (varchar 255)
        public string Title { get; set; } = string.Empty;


        [Column("ticket_description", TypeName = "text")] // TypeName for 'text'
        public string Description { get; set; } = string.Empty;


        [Column("assigned_to")]
        public int? Assigned_To { get; set; }
        [ForeignKey("Assigned_To")]
        public User? AssignedTo { get; set; }


        [Required]
        [Column("ticket_status")]
        [StringLength(20)] // Max length from schema (varchar 20)
        public string Status { get; set; } = TicketStatus.To_Do.ToString(); // e.g., "To Do", "In Progress", "Completed"


        [Column("ticket_priority")]
        [StringLength(10)] // Max length from schema (varchar 10)
        public string Priority { get; set; } = TicketPriority.Low.ToString();// e.g., "High", "Medium", "Low"



        [Column("ticket_created_at", TypeName = "date")]
        public DateTime Created_At { get; set; } = new DateTime();



        [Column("ticket_due_date", TypeName = "date")]
        public DateTime Due_Date { get; set; } = new DateTime();


        [Column("task_completed_at", TypeName = "date")]
        public DateTime Completed_At { get; set; } = new DateTime();


        [Column("project_id")]
        public int Project_Id { get; set; }
        [ForeignKey("Project_Id")]
        public Project? Project { get; set; }



        [Column("ticket_created_by")]
        public int Created_By { get; set; }
        [ForeignKey("Created_By")]
        public User? CreatedBy { get; set; }


        // Navigation property for TaskComments related to this task
        public ICollection<Comment>? TicketComments { get; set; }

        public void getTaskStatus(int statusId)
            {
                Status = ((TicketStatus)statusId).ToString();
            }

            public void getPriority(int priorityId)
            {
                Priority = ((TicketPriority)priorityId).ToString();
            }

        }
    }
