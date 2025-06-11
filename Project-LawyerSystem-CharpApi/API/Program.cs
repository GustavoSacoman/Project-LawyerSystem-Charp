using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Project_LawyerSystem_CharpApi.Application.Services;
using Project_LawyerSystem_CharpApi.Domain.Interfaces;
using Project_LawyerSystem_CharpApi.Infrastructure.CustomMigration;
using Project_LawyerSystem_CharpApi.Infrastructure.Data;
using Project_LawyerSystem_CharpApi.Infrastructure.Repositories;
using System.Text;

//DotNetEnv.Env.Load();
// Db Set
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var frontRoute = builder.Configuration["FRONT_HOST"];

if (string.IsNullOrEmpty(frontRoute))
{
    throw new Exception("FRONT_HOST is not configured in the environment variables.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", builder =>
    {
        builder.WithOrigins(frontRoute)
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var secretKey = builder.Configuration["secretKey"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new Exception("JWT Secret Key is not configured in the environment variables.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "http://localhost:5000",
        ValidAudience = "http://localhost:5000",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
    };
});

// Sawgger configuration
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddScoped<ILawyerRepository, LawyerRepository>();
builder.Services.AddScoped<LawyerService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<CaseService>();
builder.Services.AddScoped<ICaseRepository, CaseRepository>();
builder.Services.AddScoped<ICaseEventRepository, CaseEventRepository>();
builder.Services.AddScoped<CaseEventService>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Run Swagger
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Crie o UserMigration dentro do escopo!
    var migration = new UserMigration(
        builder.Configuration,
        scope.ServiceProvider.GetRequiredService<AppDbContext>(),
        scope.ServiceProvider.GetRequiredService<AuthService>());

#pragma warning disable SA1503
    if (!await migration.TestDb()) throw new Exception("Database connection failed");
#pragma warning restore SA1503

    Console.WriteLine("teste");

    await migration.CreateMainData();
}

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.MapControllers();

// Create a link to Swagger
app.Map("/", () =>
{
    return Results.Redirect("/swagger");
});
app.Run();