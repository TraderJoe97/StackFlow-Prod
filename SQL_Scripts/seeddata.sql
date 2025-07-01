-- Consolidated SQL script to seed data into roles, users, projects, tickets, and comments tables.
-- This script assumes the tables have been created according to the schema (e.g., by running the Database Reset Script).

-- IMPORTANT: This script uses dynamic ID lookups for foreign keys.
-- Ensure the role names and user emails used in WHERE clauses match your data.

-- Password hash for "dyanamic123" is "$2a$11$DmS.q1WT4caZlAe1h09rEucEMGJBlOdBq0z4xaBn0c.DkLnPVpJse"
DECLARE @PasswordHash NVARCHAR(255) = '$2a$11$DmS.q1WT4caZlAe1h09rEucEMGJBlOdBq0z4xaBn0c.DkLnPVpJse';

-- 1. Seed Roles
PRINT 'Seeding roles...';
INSERT INTO roles (role_name, role_description) VALUES
('Admin', 'Administrator with full access'),
('Project Manager', 'Manages projects and tickets'),
('Developer', 'Works on assigned tickets'),
('Tester', 'Tests tickets marked for review');
PRINT 'Roles seeded.';
GO

-- Declare variables for role IDs
DECLARE @AdminRoleId INT;
DECLARE @ProjectManagerRoleId INT;
DECLARE @DeveloperRoleId INT;
DECLARE @TesterRoleId INT;

-- Get role IDs (assuming they were just inserted or already exist)
SELECT @AdminRoleId = id FROM roles WHERE role_name = 'Admin';
SELECT @ProjectManagerRoleId = id FROM roles WHERE role_name = 'Project Manager';
SELECT @DeveloperRoleId = id FROM roles WHERE role_name = 'Developer';
SELECT @TesterRoleId = id FROM roles WHERE role_name = 'Tester';

-- Check if role IDs were found
IF @AdminRoleId IS NULL OR @ProjectManagerRoleId IS NULL OR @DeveloperRoleId IS NULL OR @TesterRoleId IS NULL
BEGIN
    PRINT 'Error: One or more role IDs could not be retrieved. Please ensure roles are seeded correctly.';
    RETURN;
END;
GO

-- 2. Seed Users
PRINT 'Seeding users...';
DECLARE @PasswordHash NVARCHAR(255) = '$2a$11$DmS.q1WT4caZlAe1h09rEucEMGJBlOdBq0z4xaBn0c.DkLnPVpJse'; -- Redefine if GO separated

DECLARE @AdminRoleId INT;
DECLARE @ProjectManagerRoleId INT;
DECLARE @DeveloperRoleId INT;
DECLARE @TesterRoleId INT;

SELECT @AdminRoleId = id FROM roles WHERE role_name = 'Admin';
SELECT @ProjectManagerRoleId = id FROM roles WHERE role_name = 'Project Manager';
SELECT @DeveloperRoleId = id FROM roles WHERE role_name = 'Developer';
SELECT @TesterRoleId = id FROM roles WHERE role_name = 'Tester';

INSERT INTO users (name, email, password, role_id, created_at, isVerified, isDeleted) VALUES
('Joseph', 'Joseph@omnitak.com', '$2a$11$cIqKx7iOXNOuWdYvzVV1Ju11lfjwuK2c26lwuHLgx1z6I14i0I70W', @AdminRoleId, GETDATE(), 1, 0), -- Set Joseph, Phumy, Siphokazim, Karabo as verified
('Phumy', 'Phumy@omnitak.com', '$2a$11$UfIyLytVKfcgRGiEmq8F8.w2iMK27A3fHo/i5omtJVrYDhoJdX/Um', @AdminRoleId, GETDATE(), 1, 0),
('Siphokazim', 'siphokazi@omnitak.com', '$2a$11$NTk7vqw8XfWXBPsCGJCHM.q0eHCBW38QvwPFouL14a67j8ONFkxCW', @AdminRoleId, GETDATE(), 1, 0),
('Karabo', 'Karabo@omnitak.com', '$2a$11$XFJTsw/0BmU6S9yW9lxsr.qCun9fev4TID1nVM1H/b/O6ZzKpbqRi', @AdminRoleId, GETDATE(), 1, 0),
('Alice Admin', 'alice.admin@omnitak.com', @PasswordHash, @AdminRoleId, GETDATE(), 1, 0),
('Bob ProjectManager', 'bob.pm@omnitak.com', @PasswordHash, @ProjectManagerRoleId, GETDATE(), 1, 0),
('Charlie Developer', 'charlie.dev@omnitak.com', @PasswordHash, @DeveloperRoleId, GETDATE(), 1, 0),
('Diana Tester', 'diana.tester@omnitak.com', @PasswordHash, @TesterRoleId, GETDATE(), 1, 0),
('Eve Deleted', 'eve.deleted@omnitak.com', @PasswordHash, @DeveloperRoleId, GETDATE(), 0, 1), -- Set Eve as deleted and unverified
('Frank Unverified', 'frank.unverified@omnitak.com', @PasswordHash, @DeveloperRoleId, GETDATE(), 0, 0); -- Set Frank as unverified and not deleted

PRINT 'Users seeded.';
GO

-- Declare variables for user IDs
DECLARE @AliceId INT;
DECLARE @BobId INT;
DECLARE @CharlieId INT;
DECLARE @DianaId INT;

-- Get user IDs
SELECT @AliceId = id FROM users WHERE email = 'alice.admin@omnitak.com';
SELECT @BobId = id FROM users WHERE email = 'bob.pm@omnitak.com';
SELECT @CharlieId = id FROM users WHERE email = 'charlie.dev@omnitak.com';
SELECT @DianaId = id FROM users WHERE email = 'diana.tester@omnitak.com';

-- Check if user IDs were found
IF @AliceId IS NULL OR @BobId IS NULL OR @CharlieId IS NULL OR @DianaId IS NULL
BEGIN
    PRINT 'Error: One or more user IDs could not be retrieved. Please ensure users are seeded correctly.';
    RETURN;
END;
GO

-- 3. Seed Projects
PRINT 'Seeding projects...';
DECLARE @AliceId INT;
DECLARE @BobId INT;
DECLARE @CharlieId INT;
DECLARE @DianaId INT;

SELECT @AliceId = id FROM users WHERE email = 'alice.admin@omnitak.com';
SELECT @BobId = id FROM users WHERE email = 'bob.pm@omnitak.com';
SELECT @CharlieId = id FROM users WHERE email = 'charlie.dev@omnitak.com';
SELECT @DianaId = id FROM users WHERE email = 'diana.tester@omnitak.com';

INSERT INTO projects (project_name, project_description, project_status, project_start_date, project_due_date, created_by) VALUES
('Website Redesign', 'Complete overhaul of the company website to improve UX and modern aesthetics.', 'Active', '2024-01-15', '2024-07-31', @BobId),
('Mobile App Development', 'Building a new iOS and Android application for customer engagement.', 'Active', '2024-03-01', '2024-12-31', @AliceId),
('Internal Tool Refactor', 'Refactoring an old internal legacy tool for better performance and maintainability.', 'On Hold', '2024-02-01', '2024-09-30', @CharlieId),
('E-commerce Platform Launch', 'Develop and launch a new e-commerce platform.', 'Active', '2024-06-01', '2025-01-31', @BobId),
('Legacy System Migration', 'Migrate data and functionality from an old system to a new one.', 'Completed', '2023-10-01', '2024-04-30', @AliceId);
PRINT 'Projects seeded.';
GO

-- Declare variables for project IDs
DECLARE @WebsiteRedesignId INT;
DECLARE @MobileAppId INT;
DECLARE @InternalToolRefactorId INT;
DECLARE @EcommercePlatformId INT;
DECLARE @LegacySystemMigrationId INT;

-- Get project IDs
SELECT @WebsiteRedesignId = id FROM projects WHERE project_name = 'Website Redesign';
SELECT @MobileAppId = id FROM projects WHERE project_name = 'Mobile App Development';
SELECT @InternalToolRefactorId = id FROM projects WHERE project_name = 'Internal Tool Refactor';
SELECT @EcommercePlatformId = id FROM projects WHERE project_name = 'E-commerce Platform Launch';
SELECT @LegacySystemMigrationId = id FROM projects WHERE project_name = 'Legacy System Migration';

-- Check if project IDs were found
IF @WebsiteRedesignId IS NULL OR @MobileAppId IS NULL OR @InternalToolRefactorId IS NULL OR @EcommercePlatformId IS NULL OR @LegacySystemMigrationId IS NULL
BEGIN
    PRINT 'Error: One or more project IDs could not be retrieved. Please ensure projects are seeded correctly.';
    RETURN;
END;
GO

-- 4. Seed Tickets
PRINT 'Seeding tickets...';
DECLARE @AliceId INT;
DECLARE @BobId INT;
DECLARE @CharlieId INT;
DECLARE @DianaId INT;
DECLARE @WebsiteRedesignId INT;
DECLARE @MobileAppId INT;
DECLARE @InternalToolRefactorId INT;
DECLARE @EcommercePlatformId INT;
DECLARE @LegacySystemMigrationId INT;

SELECT @AliceId = id FROM users WHERE email = 'alice.admin@omnitak.com';
SELECT @BobId = id FROM users WHERE email = 'bob.pm@omnitak.com';
SELECT @CharlieId = id FROM users WHERE email = 'charlie.dev@omnitak.com';
SELECT @DianaId = id FROM users WHERE email = 'diana.tester@omnitak.com';

SELECT @WebsiteRedesignId = id FROM projects WHERE project_name = 'Website Redesign';
SELECT @MobileAppId = id FROM projects WHERE project_name = 'Mobile App Development';
SELECT @InternalToolRefactorId = id FROM projects WHERE project_name = 'Internal Tool Refactor';
SELECT @EcommercePlatformId = id FROM projects WHERE project_name = 'E-commerce Platform Launch';
SELECT @LegacySystemMigrationId = id FROM projects WHERE project_name = 'Legacy System Migration';


INSERT INTO tickets (ticket_title, ticket_description, assigned_to, ticket_status, ticket_priority, ticket_created_at, ticket_due_date, ticket_completed_at, project_id, ticket_created_by) VALUES
('Implement Header Navigation', 'Develop and style the main navigation bar for the website.', @CharlieId, 'In_Progress', 'high', GETDATE(), '2024-07-01', '1900-01-01', @WebsiteRedesignId, @BobId),
('Database Schema Design', 'Design and implement the new database schema for the mobile app backend.', @AliceId, 'In_Progress', 'high', GETDATE(), '2024-08-15', '1900-01-01', @MobileAppId, @BobId),
('Bug: Login Failure', 'Users are unable to log in to the internal tool after recent update.', @CharlieId, 'In_Review', 'high', GETDATE(), '2024-06-30', '1900-01-01', @InternalToolRefactorId, @DianaId),
('API Integration for User Profiles', 'Integrate third-party API for richer user profile data in the mobile app.', @CharlieId, 'To_Do', 'medium', GETDATE(), '2024-09-30', '1900-01-01', @MobileAppId, @BobId),
('Update Project Status Logic', 'Modify the project status update mechanism in the website backend.', @CharlieId, 'Done', 'low', '2024-05-01', '2024-05-15', '2024-05-10', @WebsiteRedesignId, @AliceId),
('Test Mobile App Login Flow', 'Perform end-to-end testing of the mobile app login and registration process.', @DianaId, 'To_Do', 'medium', GETDATE(), '2024-08-01', '1900-01-01', @MobileAppId, @BobId),
('Backend Performance Optimization', 'Optimize database queries and API response times for the e-commerce platform.', @CharlieId, 'In_Progress', 'high', GETDATE(), '2024-07-15', '1900-01-01', @EcommercePlatformId, @AliceId),
('UI/UX Review for Project Module', 'Review and provide feedback on the user interface and experience of the project management module.', @DianaId, 'In_Review', 'medium', GETDATE(), '2024-07-05', '1900-01-01', @InternalToolRefactorId, @BobId),
('Data Migration Script Development', 'Develop scripts for migrating historical user data to the new system.', @CharlieId, 'Done', 'high', '2024-03-01', '2024-03-15', '2024-03-12', @LegacySystemMigrationId, @AliceId);
PRINT 'Tickets seeded.';
GO

-- Declare variables for ticket IDs
DECLARE @Ticket1Id INT; -- Implement Header Navigation
DECLARE @Ticket2Id INT; -- Database Schema Design
DECLARE @Ticket3Id INT; -- Bug: Login Failure
DECLARE @Ticket5Id INT; -- Update Project Status Logic
DECLARE @Ticket7Id INT; -- Backend Performance Optimization

SELECT @Ticket1Id = id FROM tickets WHERE ticket_title = 'Implement Header Navigation';
SELECT @Ticket2Id = id FROM tickets WHERE ticket_title = 'Database Schema Design';
SELECT @Ticket3Id = id FROM tickets WHERE ticket_title = 'Bug: Login Failure';
SELECT @Ticket5Id = id FROM tickets WHERE ticket_title = 'Update Project Status Logic';
SELECT @Ticket7Id = id FROM tickets WHERE ticket_title = 'Backend Performance Optimization';

-- Check if ticket IDs were found
IF @Ticket1Id IS NULL OR @Ticket2Id IS NULL OR @Ticket3Id IS NULL OR @Ticket5Id IS NULL OR @Ticket7Id IS NULL
BEGIN
    PRINT 'Error: One or more ticket IDs could not be retrieved for comments. Please ensure tickets are seeded correctly.';
    RETURN;
END;
GO

-- 5. Seed Comments
PRINT 'Seeding comments...';
DECLARE @AliceId INT;
DECLARE @BobId INT;
DECLARE @CharlieId INT;
DECLARE @DianaId INT;
DECLARE @Ticket1Id INT;
DECLARE @Ticket2Id INT;
DECLARE @Ticket3Id INT;
DECLARE @Ticket5Id INT;
DECLARE @Ticket7Id INT;

SELECT @AliceId = id FROM users WHERE email = 'alice.admin@omnitak.com';
SELECT @BobId = id FROM users WHERE email = 'bob.pm@omnitak.com';
SELECT @CharlieId = id FROM users WHERE email = 'charlie.dev@omnitak.com';
SELECT @DianaId = id FROM users WHERE email = 'diana.tester@omnitak.com';

SELECT @Ticket1Id = id FROM tickets WHERE ticket_title = 'Implement Header Navigation';
SELECT @Ticket2Id = id FROM tickets WHERE ticket_title = 'Database Schema Design';
SELECT @Ticket3Id = id FROM tickets WHERE ticket_title = 'Bug: Login Failure';
SELECT @Ticket5Id = id FROM tickets WHERE ticket_title = 'Update Project Status Logic';
SELECT @Ticket7Id = id FROM tickets WHERE ticket_title = 'Backend Performance Optimization';


INSERT INTO comments (ticket_id, content, created_by, comment_created_at) VALUES
(@Ticket1Id, 'Starting work on this today. Will target initial draft by end of week.', @CharlieId, GETDATE()),
(@Ticket1Id, 'Looks good, Charlie. Let me know if you need any design assets.', @BobId, DATEADD(day, 1, GETDATE())),
(@Ticket2Id, 'Initial ERD has been drafted and sent for review.', @AliceId, GETDATE()),
(@Ticket3Id, 'Investigating the root cause. Seems to be related to recent dependency updates.', @CharlieId, GETDATE()),
(@Ticket3Id, 'Any updates on this bug? It is impacting several users.', @BobId, DATEADD(day, 2, GETDATE())),
(@Ticket5Id, 'Confirmed fix is deployed to staging. Awaiting QA sign-off.', @CharlieId, DATEADD(day, -5, GETDATE())),
(@Ticket7Id, 'Just completed a preliminary analysis. Identified a few key areas for improvement.', @CharlieId, GETDATE()),
(@Ticket7Id, 'Great work! Let''s schedule a meeting to discuss the findings.', @BobId, DATEADD(hour, 2, GETDATE()));
PRINT 'Comments seeded.';
GO