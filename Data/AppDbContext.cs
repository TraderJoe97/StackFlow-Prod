using Microsoft.EntityFrameworkCore;
using StackFlow.Models;

namespace StackFlow.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> User { get; set; } = null!;
        public DbSet<Role> Role { get; set; } = null!;
        public DbSet<Project> Project { get; set; } = null!;
        public DbSet<Ticket> Ticket { get; set; } = null!;
        public DbSet<Comment> TicketComment { get; set; } = null!; // Renamed from Comment to TicketComment to match DbSet name

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicitly map entity names to their snake_case table names in the database
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Role>().ToTable("roles");
            modelBuilder.Entity<Project>().ToTable("projects");
            modelBuilder.Entity<Ticket>().ToTable("tickets");
            modelBuilder.Entity<Comment>().ToTable("comments");


            // User and Role relationship
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.Role_Id)
                .IsRequired(); // RoleId is NOT NULL based on schema for user 'role id'
                               

            // Project and User (CreatedBy) relationship
            // KEEP THIS AS CASCADE if you want deleting a user to delete their projects
            // This is one part of the problematic cycle User -> Project (Cascade) -> Ticket (Cascade)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.CreatedBy)
                .WithMany(u => u.CreatedProjects) // Assuming you have ICollection<Project> CreatedProjects in User.cs
                .HasForeignKey(p => p.Created_By)
                .IsRequired(); // Assuming created_by is NOT NULL
                               // Default DeleteBehavior.Cascade is assumed here, contributing to the cycle.

            // Ticket and Project relationship
            // KEEP THIS AS CASCADE if you want deleting a project to delete its tickets
            // This is another part of the problematic cycle User -> Project -> Ticket (Cascade)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tickets) // Assuming Project has ICollection<Ticket> Tickets
                .HasForeignKey(t => t.Project_Id)
                .IsRequired(); // Assuming project_id is NOT NULL
                               // Default DeleteBehavior.Cascade is assumed here, contributing to the cycle.


            // Ticket and User (AssignedTo) relationship
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTickets) // Assuming User has ICollection<Ticket> AssignedTickets
                .HasForeignKey(t => t.Assigned_To)
                .IsRequired(false); // Assigned_to is NULLABLE
                                    // Default DeleteBehavior.SetNull for optional foreign keys, or NoAction if not explicitly set. Fine as is.

            // Ticket and User (CreatedBy) relationship - MODIFIED TO RESTRICT
            // This was the first conflict point for User -> Ticket
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.CreatedBy)
                .WithMany(u => u.CreatedTickets) // Assuming User has ICollection<Ticket> CreatedTickets
                .HasForeignKey(t => t.Created_By)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict); // CHANGE: Prevents multiple cascade paths from User to Ticket

            // Comment and Ticket relationship
            // KEEP THIS AS CASCADE if you want deleting a ticket to delete its comments
            // This is one part of the problematic cycle Ticket -> Comment (Cascade)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Ticket)
                .WithMany(t => t.TicketComments)
                .HasForeignKey(tc => tc.Ticket_Id)
                .IsRequired(); // Assuming ticket_id is NOT NULL
                               // Default DeleteBehavior.Cascade is assumed here, contributing to the cycle.

            // Comment and User relationship - MODIFIED TO RESTRICT
            // This was the second conflict point for User -> Comment
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.CreatedBy)
                .WithMany(u => u.TicketComments)
                .HasForeignKey(tc => tc.Created_By)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict); // CHANGE: Prevents multiple cascade paths from User to Comment

            // Seed a default project for easy testing (optional)
            // Ensure that a user with Id = 1 exists in your database or this will fail.
            // This seeding will only run if you apply migrations after adding it.
            // (Your seeding logic would go here if uncommented)
        }
    }
}