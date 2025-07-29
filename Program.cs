using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackFlow.Data;
using System;
using StackFlow.Hubs;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Http; // Ensure this is present for context.Response.WriteAsync
using StackFlow.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.OutputFormatters.Add(new SystemTextJsonOutputFormatter(new Microsoft.AspNetCore.Mvc.JsonOptions().JsonSerializerOptions));
});

// Configure DbContext with SQL Server, retry logic, and command timeout
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(60);
        });
});

// Configure Swagger generation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "StackFlow API", Version = "v1" });

    // Include XML comments for API documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    // Check if XML file exists before including (optional, but prevents errors if it's missing)
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // --- Filter to include only API Controllers ---
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (!apiDesc.TryGetMethodInfo(out MethodInfo methodInfo))
        {
            return false;
        }
        var controllerType = methodInfo.DeclaringType;
        if (controllerType == null)
        {
            return false;
        }
        // Only include controllers decorated with [ApiController]
        return controllerType.IsDefined(typeof(ApiControllerAttribute), true);
    });

    // Configure JWT Bearer authorization in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" // The Id should be the name defined in AddSecurityDefinition
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Authentication services
builder.Services.AddAuthentication(options =>
{
    // Set default authentication scheme for MVC (cookie based)
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    // Set default challenge scheme for API (JWT Bearer based)
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
    // This tells the JWT Bearer middleware NOT to redirect to login for API calls
    // Instead, it will return a 401 Unauthorized response.
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse(); // Explicitly handle the response
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { message = "Authentication failed. Please provide a valid JWT." }));
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { message = "Authorization failed. You do not have permission to access this resource." }));
        }
    };
});

// Add Authorization services
builder.Services.AddAuthorization();

// Add SignalR services
builder.Services.AddSignalR();

// Add Email Service (Mailgun)
builder.Services.AddTransient<IEmailService>(s => new MailgunEmailService(
    builder.Configuration["MailgunSettings:ApiKey"],
    builder.Configuration["MailgunSettings:Domain"],
    builder.Configuration["MailgunSettings:FromEmail"]
));

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "StackFlow API V1");
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<DashboardHub>("/dashboardHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();