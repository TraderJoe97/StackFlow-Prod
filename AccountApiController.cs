csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

[Route("api/[controller]")]
[ApiController]
public class AccountApiController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AccountApiController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [Authorize]
    [HttpGet("jwt")]
    public IActionResult GetJwt()
    {
        var claims = User.Claims;

        // You might want to filter or transform claims here based on what should be in the JWT
        // For example, exclude claims related to cookie authentication if necessary

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.Now.AddHours(3), // Set token expiration
            claims: claims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiration = token.ValidTo
        });
    }
}