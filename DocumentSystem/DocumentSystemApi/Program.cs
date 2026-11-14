
using DocumentSystemApi.Data;
using DocumentSystemApi.Models;
using DocumentSystemApi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocumentSystemApi.Data;
using DocumentSystemApi.Models;
using DocumentSystemApi.Services;

var builder = WebApplication.CreateBuilder(args);

//Register API controllers and enable automatic CSRF validation
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(
        new AutoValidateAntiforgeryTokenAttribute()
    );
});


//Configure antiforgery protection
builder.Services.AddAntiforgery(options =>
{
    //Name of the header containing the CSRF request token
    options.HeaderName = "X-CSRF-TOKEN";

    //Configure the antiforgery cookie
    options.Cookie.Name = "DocumentSystem.CSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});


//Connect Entity Framework Core to SQL Server
builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DocumentDB")
    ));


//Password hashing service
builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();


//Authentication service
builder.Services.AddScoped<SystemAuthenticationService>();

//Document upload and management
builder.Services.AddScoped<DocumentService>();

//Document extraction
builder.Services.AddScoped<DocumentExtractionService>();

//Document processing and chunking
builder.Services.AddScoped<DocumentProcessingService>();

//AI document classification
builder.Services.AddHttpClient<DocumentClassificationService>();

//AI general document analysis
builder.Services.AddHttpClient<GeneralAnalysisService>();

//Specialized QA analysis service
builder.Services.AddHttpClient<QAAnalysisService>(
    client =>
    {
        //Maximum time allowed for one AI analysis request
        client.Timeout = TimeSpan.FromSeconds(120);
    });

//AI summary generation service
builder.Services.AddHttpClient<SummaryGenerationService>(
    client =>
    {
        //Set the maximum duration of an AI request
        client.Timeout = TimeSpan.FromSeconds(120);
    });

//Save generated summaries service
builder.Services.AddScoped<SummaryPersistenceService>();

//Save analyses and findings into SQL Server
builder.Services.AddScoped<AnalysisPersistenceService>();

//Configure cookie authentication
builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme
    )
    .AddCookie(options =>
    {
        //Authentication cookie settings
        options.Cookie.Name = "QADocumentSystem.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        //Authentication session duration
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;

        //Return 401 instead of redirecting to a login page
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return Task.CompletedTask;
        };

        //Return 403 when the user lacks permission
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return Task.CompletedTask;
        };
    });


//Enable authorization services
builder.Services.AddAuthorization();

//Build the application
var app = builder.Build();

app.UseHttpsRedirection(); //Redirect HTTP requests to HTTPS
app.UseAuthentication(); //Identify the currently logged-in user
app.UseAuthorization(); //Check whether the user is allowed to access an endpoint
app.MapControllers(); //Map controller endpoints

//Start the API
app.Run();
