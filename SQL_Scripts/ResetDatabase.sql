-- SQL script to drop all tables if they exist and then recreate them.
-- This script mirrors the schema defined in your InitialMigration.

-- IMPORTANT: This script will irreversibly delete all data in these tables.
-- Use with caution, especially in production environments.

-- Drop tables in reverse order of dependency to avoid foreign key issues
IF OBJECT_ID('comments', 'U') IS NOT NULL
BEGIN
    DROP TABLE comments;
END;
GO

IF OBJECT_ID('tickets', 'U') IS NOT NULL
BEGIN
    DROP TABLE tickets;
END;
GO

IF OBJECT_ID('projects', 'U') IS NOT NULL
BEGIN
    DROP TABLE projects;
END;
GO

IF OBJECT_ID('users', 'U') IS NOT NULL
BEGIN
    DROP TABLE users;
END;
GO

IF OBJECT_ID('roles', 'U') IS NOT NULL
BEGIN
    DROP TABLE roles;
END;
GO

-- Create tables

-- Create 'roles' table
CREATE TABLE roles (
    id INT IDENTITY(1,1) NOT NULL,
    role_name NVARCHAR(255) NOT NULL,
    role_description TEXT NOT NULL,
    CONSTRAINT PK_roles PRIMARY KEY (id)
);
GO

-- Create 'users' table
CREATE TABLE users (
    id INT IDENTITY(1,1) NOT NULL,
    name NVARCHAR(150) NOT NULL,
    email NVARCHAR(255) NOT NULL,
    password NVARCHAR(255) NOT NULL,
    role_id INT NOT NULL,
    created_at DATE NOT NULL,
    isActive BIT NOT NULL DEFAULT 1, -- New column: isActive (boolean, default to true)
    CONSTRAINT PK_users PRIMARY KEY (id),
    CONSTRAINT FK_users_roles_role_id FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE
);
GO

-- Create 'projects' table
CREATE TABLE projects (
    id INT IDENTITY(1,1) NOT NULL,
    project_name NVARCHAR(255) NOT NULL,
    project_description TEXT NOT NULL,
    project_status NVARCHAR(50) NOT NULL,
    project_start_date DATE NULL,
    project_due_date DATE NULL,
    created_by INT NOT NULL,
    CONSTRAINT PK_projects PRIMARY KEY (id),
    CONSTRAINT FK_projects_users_created_by FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE CASCADE
);
GO

-- Create 'tickets' table
CREATE TABLE tickets (
    id INT IDENTITY(1,1) NOT NULL,
    ticket_title NVARCHAR(255) NOT NULL,
    ticket_description TEXT NOT NULL,
    assigned_to INT NULL,
    ticket_status NVARCHAR(20) NOT NULL,
    ticket_priority NVARCHAR(10) NOT NULL,
    ticket_created_at DATE NOT NULL,
    ticket_due_date DATE NOT NULL,
    ticket_completed_at DATE NOT NULL,
    project_id INT NOT NULL,
    ticket_created_by INT NOT NULL,
    CONSTRAINT PK_tickets PRIMARY KEY (id),
    CONSTRAINT FK_tickets_projects_project_id FOREIGN KEY (project_id) REFERENCES projects (id) ON DELETE CASCADE,
    CONSTRAINT FK_tickets_users_assigned_to FOREIGN KEY (assigned_to) REFERENCES users (id) ON DELETE NO ACTION,
    CONSTRAINT FK_tickets_users_ticket_created_by FOREIGN KEY (ticket_created_by) REFERENCES users (id) ON DELETE NO ACTION -- Changed to ON DELETE NO ACTION
);
GO

-- Create 'comments' table
CREATE TABLE comments (
    id INT IDENTITY(1,1) NOT NULL,
    ticket_id INT NOT NULL,
    content TEXT NOT NULL,
    created_by INT NOT NULL,
    comment_created_at DATE NOT NULL,
    CONSTRAINT PK_comments PRIMARY KEY (id),
    CONSTRAINT FK_comments_tickets_ticket_id FOREIGN KEY (ticket_id) REFERENCES tickets (id) ON DELETE CASCADE,
    CONSTRAINT FK_comments_users_created_by FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE CASCADE
);
GO

-- Create indexes
CREATE INDEX IX_comments_created_by ON comments (created_by);
GO

CREATE INDEX IX_comments_ticket_id ON comments (ticket_id);
GO

CREATE INDEX IX_projects_created_by ON projects (created_by);
GO

CREATE INDEX IX_tickets_assigned_to ON tickets (assigned_to);
GO

CREATE INDEX IX_tickets_project_id ON tickets (project_id);
GO

CREATE INDEX IX_tickets_ticket_created_by ON tickets (ticket_created_by);
GO

CREATE INDEX IX_users_role_id ON users (role_id);
GO
