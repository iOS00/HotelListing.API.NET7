using HotelListing.API.Core.Configurations;
using HotelListing.API.Core.Interfaces;
using HotelListing.API.Core.Middleware;
using HotelListing.API.Core.Repository;
using HotelListing.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("HotelListingDbConnectionString");
builder.Services.AddDbContext<HotelListingDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Add Identity Core
builder.Services.AddIdentityCore<ApiUser>()  //base IdentityUser comes with user fields and encryption
    .AddRoles<IdentityRole>()  // to configure roles for RBAC
    .AddTokenProvider<DataProtectorTokenProvider<ApiUser>>("HotelListingApi")
    .AddEntityFrameworkStores<HotelListingDbContext>()  // select database on which to apply Identity and RBAC
    .AddDefaultTokenProviders();  // in case we need other default Token providers in future 

// Swagger
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Hotel Listing API", Version = "v1"});
    
    // collects login from user that will be compared with security requirements set
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // defines security schema to compare with
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                Scheme = "0auth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b => b.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
});

// enable API Versioning
builder.Services.AddApiVersioning(options => 
{
    options.AssumeDefaultVersionWhenUnspecified = true;  // default version if not specified
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);  // our own specification
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("api-version"),  // to specify in query string
        new HeaderApiVersionReader("X-Version"),  // in request header
        new MediaTypeApiVersionReader("ver")  // in media type search
    );
});

// configure API Version Exploler
builder.Services.AddVersionedApiExplorer(  // spicify how versioning looks in Url 
    options => 
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// ctx - contenxt, lc - logger configuration
builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console().ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddAutoMapper(typeof(MapperConfig));

// register repositories

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ICountriesRepository, CountriesRepository>();
builder.Services.AddScoped<IHotelsRepository, HotelsRepository>();
builder.Services.AddScoped<IAuthManager, AuthManager>();

// Add Authentication with JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;  //adds "Bearer" to scheme
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;  //adds "Bearer" to scheme
}).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,  // we'll configure to accept tokens issued only by our API
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],  //all below from appsettings.json
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            builder.Configuration["JwtSettings:Key"]))  //Encrypt password
    };
});

// Enable Caching of Responses
builder.Services.AddResponseCaching(options => 
{
    options.MaximumBodySize = 1024;  // allow up to 1Mb of response body cached
    options.UseCaseSensitivePaths = true;  // Case sensitive requests - separate cache!  
});

// Health Check (API and SqlServer database)
/* Required NuGet packages: 1) Asp.NetCore.HealthChecks.SqlServer
2) Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore  */
builder.Services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>(
        "Custom Health Check",  // name to be displayed for custom Health Check
        failureStatus: HealthStatus.Degraded,  // set "Degraded" considered as failure
        tags: new[] { "custom" }  // add tag to our custom Health Check
        )
        .AddSqlServer(connectionString, tags: new[] { "database" })  // Connect to SqlServer Db
        .AddDbContextCheck<HotelListingDbContext>(tags: new[] { "database" });  // Database health check

// OData for data filtring and sorting on controller level
builder.Services.AddControllers().AddOData(options => 
{
    options.Select().Filter().OrderBy();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks(
    "/healthcheck",  // endpoint for custom API Health Check
    new HealthCheckOptions
    {
        Predicate = healthcheck => healthcheck.Tags.Contains("custom"),  // run on failure
        ResultStatusCodes =  // specify Response Codes based on Health Status
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
        },
        ResponseWriter = WriteResponse  // use of Func<HttpContext, HealthReport, Task>
    }) ;

app.MapHealthChecks(
    "/databasehealthcheck",  // endpoint for database Health Check
    new HealthCheckOptions
    {
        Predicate = healthcheck => healthcheck.Tags.Contains("database"), 
        ResultStatusCodes =  // specify Response Codes based on Health Status
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
        },
        ResponseWriter = WriteResponse  // use of Func<HttpContext, HealthReport, Task>
    });

app.MapHealthChecks(
    "/healthz",  // endpoint for all health checks without filtering/tags
    new HealthCheckOptions
    {
        ResultStatusCodes =  // specify Response Codes based on Health Status
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
        },
        ResponseWriter = WriteResponse  // use of Func<HttpContext, HealthReport, Task>
    });

static Task WriteResponse(HttpContext context, HealthReport healthReport)
{
    context.Response.ContentType = "application/json; charset=utf-8";
    var options = new JsonWriterOptions { Indented = true };
    
    using var memoryStream = new MemoryStream();
    using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
    {
        jsonWriter.WriteStartObject();  // begin Json object
        jsonWriter.WriteString("status", healthReport.Status.ToString());
        jsonWriter.WriteStartObject("results");

        foreach (var healthReportEntry in  healthReport.Entries)
        {
            jsonWriter.WriteStartObject(healthReportEntry.Key);
            jsonWriter.WriteString("status", healthReportEntry.Value.ToString());
            jsonWriter.WriteString("description", healthReportEntry.Value.Description);
            jsonWriter.WriteStartObject("data");

            foreach (var item in healthReportEntry.Value.Data)
            {
                jsonWriter.WritePropertyName(item.Key);
                JsonSerializer.Serialize(jsonWriter, item.Value,
                    item.Value?.GetType() ?? typeof(object));
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndObject();  // close json object
    }
    return context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
}

app.MapHealthChecks("/health"); // endpoint for all default health check

app.UseMiddleware<ExceptionMiddleware>();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

//Enable caching middleware
app.UseResponseCaching();  // make sure to place after app.UseCors 

//can be refactored into custom middleware
app.Use(async (context, next) =>   // context(HTTP response)
{
    context.Response.GetTypedHeaders().CacheControl =
    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
    {
        Public = true,  // cache will be public
        MaxAge = TimeSpan.FromSeconds(10)  // cached every 10 seconds
    };
    context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
    new string[] { "Accept-Encoding" };  //allows encoded header names

    await next();  // get next response
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


class CustomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var isHealthy = true;

        /* 
          here place logic for changing status isHealthy = false
        */

        if(isHealthy)
        {
            return Task.FromResult(HealthCheckResult.Healthy("All systems are looking good"));
        }

        return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus, 
            "System Unhealthy"));  // or we can return Exception (see other choises)
    }
}