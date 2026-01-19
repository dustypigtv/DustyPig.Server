using DustyPig.Server.Data;
using DustyPig.Server.Extensions;
using DustyPig.Server.HostedServices;
using DustyPig.Server.Services;
using DustyPig.Server.Services.TMDB_Service;
using DustyPig.Server.SwaggerHelpers;
using DustyPig.Server.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);


// Add service defaults & Aspire client integrations.
// Disable seq when adding migrations
builder.AddServiceDefaults();
builder.AddSeqEndpoint("seq");
builder.AddNpgsqlDbContext_MyVersion<AppDbContext>("dustypig-v3");


//Firebase services
builder
    .AddFirebaseCloudMessaging()
    .AddFirestoreDb();


// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddRouting();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();



// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();



//Authentication & Authoriziztion
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = JWTService.ISSUER,
            ValidateIssuer = true,
            ValidAudience = JWTService.AUDIENCE,
            ValidateAudience = true,
            IssuerSigningKey = JWTService.GetSecurityKey(builder.Configuration),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSWagger();




//*** Dependency Injection ***
//Anything that requires AppDbContext should be scoped
builder.Services.AddScoped<JWTService>();
builder.Services.AddScoped<TMDBService>();




//Otherwise it can be singleton or transient
builder.Services.AddTransient<S3Service>();
builder.Services.AddTransient<FirebaseAuthService>();




//Add hosted services
builder.Services.AddHostedService<TMDB_Updater>();
builder.Services.AddHostedService<FirebaseNotificationsManager>();
builder.Services.AddHostedService<FirestoreMediaChangedTriggerManager>();
builder.Services.AddHostedService<DBCleaner>();
builder.Services.AddHostedService<ArtworkUpdater>();














var app = builder.Build();


app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapDefaultEndpoints();
app.UseSwagger();


app.MapControllers();
app.MapRazorPages();


//Apply any migrations
using(var db = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
{
    await db.Database.MigrateAsync();
}

app.Run();

