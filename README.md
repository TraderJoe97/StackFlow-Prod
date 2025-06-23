StackFlow - Task Management System
📊 Project Overview
StackFlow is a simple yet effective task management system built with ASP.NET Core MVC and Entity Framework Core. It allows users to manage projects, tasks, and track their status and progress.

✨ Features
User Authentication & Authorization: Secure login and registration.

Dashboard: Personalized dashboard showing tasks assigned to the current user and an overview of projects.

Project Management: Create and manage projects with names, descriptions, start/end dates, and statuses.

Task Management: Create, view, and edit tasks with titles, descriptions, project associations, assigned users, statuses, priorities, and due dates.

Task Comments: Add comments to tasks for collaborative communication.

Reporting:

Project Reports: View a summary of tasks per project (total, completed, in progress, to do).

User Reports: See task statistics for all users in the system (total assigned, completed, in progress, to do).

Database Integration: Uses SQL Server via Entity Framework Core for data persistence.

Responsive Design: Built with Bootstrap for a responsive and modern user interface.

🚀 Getting Started
Follow these steps to set up and run StackFlow locally.

Prerequisites
.NET SDK (8.0 or newer recommended)

SQL Server (or SQL Server Express/LocalDB)

Visual Studio (Recommended IDE) or a .NET compatible editor like VS Code.

Installation
Clone the Repository:

git clone https://github.com/TraderJoe97/StackFlow-Prod
cd StackFlow

Configure Database Connection:

Open appsettings.json (or appsettings.Development.json for development).

Update the DefaultConnection string to point to your SQL Server instance:

"ConnectionStrings": {
  "DefaultConnection": "Server=your_server_name;Database=StackFlowDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}

Replace your_server_name with your actual SQL Server instance name (e.g., (localdb)\\MSSQLLocalDB or . or your server IP/name).

Run Database Migrations:

Open a terminal in the project root directory (where StackFlow.csproj is located).

Apply existing migrations to create the database schema:

dotnet ef database update

(If this is the very first setup and no migrations exist, or if you need to create initial roles, you might first need to ensure your AppDbContext has seeding logic and then run dotnet ef migrations add InitialCreate followed by dotnet ef database update).

Install Frontend Libraries (NuGet):

Ensure you have jQuery and Bootstrap installed locally via NuGet. If not, run these in the Package Manager Console in Visual Studio:

Install-Package jQuery
Install-Package bootstrap
Install-Package jQuery.Validation
Install-Package jQuery.Validation.Unobtrusive

Build and Run the Application:

From the project root directory:

dotnet run

Alternatively, open the solution in Visual Studio and press F5.

The application should now launch in your browser, typically at https://localhost:70XX (the exact port might vary and can be found in Properties/launchSettings.json).

Initial User Setup
If you don't have any users or roles in your database after running migrations, you'll need to manually insert at least one role ('Admin' or 'Developer') and one user into your database directly, or configure seeding in AppDbContext.cs and run migrations.

For example, to insert a Developer role and a user (e.g., user@omnitak.com which is required by the AccountController), you can use SQL:

INSERT INTO roles (role_name, description) VALUES ('Developer', 'Standard developer role');
INSERT INTO users (username, email, password, role_id, created_at)
VALUES ('initialuser', 'user@omnitak.com', '<hashed_password_here>', 1, GETDATE());
-- Replace <hashed_password_here> with a BCrypt hash of your desired password.
-- The role_id (1 in this example) should match the ID of the 'Developer' role you inserted.
