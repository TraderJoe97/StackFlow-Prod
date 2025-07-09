using StackFlow.Api.DataTransferObjects;
using StackFlow.Data;

namespace StackFlow.Api.AccountApi
{
    public static class UserAccountsEndpoint
    {
        public static void AddUserAccountsEndpoints(this WebApplication app)
        {
            var usersApi = app.MapGroup("/api/users");

            usersApi.MapPost("/register", async (int id, AppDbContext context) =>
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
        }
    }
}
