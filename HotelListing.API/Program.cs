using HotelListing.API.Configurations;
using HotelListing.API.Data;
using HotelListing.API.Interfaces;
using HotelListing.API.Middleware;
using HotelListing.API.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
