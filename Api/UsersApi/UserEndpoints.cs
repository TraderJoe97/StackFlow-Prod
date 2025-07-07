using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using StackFlow.Api.DataTransferObjects;
using StackFlow.Data;
using StackFlow.Models;

namespace StackFlow.Api.UsersApi
{
    public static class UserEndpoints
    {
        
        public static void AddUserEndpoints(this WebApplication app)
        {

            app.MapGet("/users", async (AppDbContext context) =>
            {
                var users = await context.User.ToListAsync();
                return users is not null ? Results.Ok(users) : Results.NotFound(new { message = $"Users not found" });
            });
            app.MapGet("/users/{id}", async (int id, AppDbContext context) =>
            {
                var user = await context.User.FindAsync(id);
                return user is not null ? Results.Ok(user) : Results.NotFound(new { message = $"User with ID {id} not found" });
            });

            app.MapDelete("/users/delete/{id}", async (int id, AppDbContext context) =>
            {
                var deleteUser = await context.User.FindAsync(id);

                if (deleteUser == null)
                {
                    return Results.NotFound(new { message = $"User with ID {id} not found" });
                }
                try
                {
                    context.User.Remove(deleteUser);
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = $"User with ID {id} deleted successfully." });

                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                {
                    // Catch specific database update exceptions (e.g., foreign key constraints)
                    // and return a 400 Bad Request with a more informative error.
                    return Results.BadRequest(new { message = "Could not delete user due to a database constraint. It might be linked to other records.", error = dbEx.Message });
                }
                catch (Exception ex)
                {
                    // Catch any other unexpected exceptions and return a 500 Internal Server Error.
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                }
                ;
            });

            app.MapPost("/users/create", async (UserDTO userDTO, AppDbContext context) =>
            {

                try
                {
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(userDTO.Password);

                    var newUser = new User
                    {
                        Name = userDTO.Name,
                        Email = userDTO.Email,
                        Role_Id = 2,
                        PasswordHash = passwordHash, // Assign the ID of the 'Developer' role
                        Created_At = DateTime.UtcNow,
                        IsVerified = false, // New users are unverified by default
                        IsDeleted = false   // New users are not deleted by default
                    };

                    context.User.Add(newUser);
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = $"User created successfully.", newUser });

                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                {
                    // Catch specific database update exceptions (e.g., foreign key constraints)
                    // and return a 400 Bad Request with a more informative error.
                    return Results.BadRequest(new { message = "Could not delete user due to a database constraint. It might be linked to other records.", error = dbEx.Message });
                }
                catch (Exception ex)
                {
                    // Catch any other unexpected exceptions and return a 500 Internal Server Error.
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                }
                ;

            });
        }
    }
}
       