namespace StackFlow.Models
{
    public class ProjectReport
    {
        public Project Project { get; set; } = default!;
        public int TotalTickets { get; set; }
        public int CompletedTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ToDoTickets { get; set; }
        public int InReviewTickets { get; set; }
    }
}
