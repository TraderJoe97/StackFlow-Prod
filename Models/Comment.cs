using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StackFlow.Models
{
    public class Comment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("ticket_id")]
        public int Ticket_Id { get; set; }
        [ForeignKey("Ticket_Id")]
        public Ticket? Ticket { get; set; }


        [Required]
        [Column("content", TypeName = "text")] // TypeName for 'text'
        public string Content { get; set; } = string.Empty;



        [Column("created_by")]
        public int Created_By { get; set; }
        [ForeignKey("Created_By")]
        public User? CreatedBy { get; set; }


        [Column("comment_created_at", TypeName = "date")]
        public DateTime Created_At { get; set; } = DateTime.UtcNow;

    }
}
