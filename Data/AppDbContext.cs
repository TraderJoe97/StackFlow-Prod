using Microsoft.EntityFrameworkCore;
using StackFlow.Models;
using System.Data;

namespace StackFlow.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> User { get; set; } = null!;
        public DbSet<Role> Role { get; set; } = null!;
        public DbSet<Project> Project { get; set; } = null!;
        public DbSet<Ticket> Ticket { get; set; } = null!;
        public DbSet<Comment> TicketComment { get; set; } = null!;

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
            modelBuilder.Entity<Project>()
                .HasOne(p => p.CreatedBy)
                .WithMany(u => u.CreatedProjects) // Assuming you have ICollection<Project> CreatedProjects in User.cs
                .HasForeignKey(p => p.Created_By)
                .IsRequired(); // Assuming created_by is NOT NULL

            // Task and Project relationship
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tickets) // Assuming Project has ICollection<Task> Tasks
                .HasForeignKey(t => t.Project_Id)
                .IsRequired(); // Assuming project_id is NOT NULL

            // Task and User (AssignedTo) relationship
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTickets) // Assuming User has ICollection<Task> AssignedTasks
                .HasForeignKey(t => t.Assigned_To)
                .IsRequired(false); // Assigned_to is NULLABLE

            // Task and User (TaskCreatedBy) relationship
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.CreatedBy)
                .WithMany(u => u.CreatedTickets) // Assuming User has ICollection<Task> CreatedTasks
                .HasForeignKey(t => t.Created_By)
                .IsRequired(); // Assuming task_created_by is NOT NULL

            // TaskComment and Task relationship
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Ticket)
                .WithMany(t => t.TicketComments)
                .HasForeignKey(tc => tc.Ticket_Id)
                .IsRequired(); // Assuming task_id is NOT NULL

            // TaskComment and User relationship
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.CreatedBy)
                .WithMany(u => u.TicketComments)
                .HasForeignKey(tc => tc.Created_By)
                .IsRequired(); // Assuming user_id is NOT NULL


            // Seed a default project for easy testing (optional)
            // Ensure that a user with Id = 1 exists in your database or this will fail.
            // This seeding will only run if you apply migrations after adding it.

        }
    }
}
