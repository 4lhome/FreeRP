using FreeRP.Net.Server.Data;
using FreeRP.Net.Server.GrpcServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

string settingsJson = FrpSettings.GetSettingsSavePath(builder.Environment.ContentRootPath);
FrpSettings? frpSettings = null;
if (File.Exists(settingsJson))
{
    var json = File.ReadAllText(settingsJson);
    frpSettings = FreeRP.Net.Server.Helpers.Json.GetModel<FrpSettings>(json);
}

if (frpSettings is null)
{
    frpSettings = FrpSettings.Create(builder.Environment.ContentRootPath);
}

builder.Services.AddGrpc(o =>
{
    o.MaxSendMessageSize = frpSettings.GrpcMessageSize;
    o.MaxReceiveMessageSize = frpSettings.GrpcMessageSize;
});
builder.Services.AddSingleton(frpSettings);
builder.Services.AddSingleton<UriService>();
builder.Services.AddSingleton<IFrpDataService, FrpDataService>();
builder.Services.AddScoped<AuthService>();

#region Auth

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthorization();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(cfg =>
    {
        cfg.RequireHttpsMetadata = true;
        cfg.SaveToken = true;
        cfg.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(frpSettings.JwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

#endregion

builder.Services.AddCors(o => o.AddPolicy("AllowAll", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader()
           .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding", "X-Grpc-Web", "User-Agent");
}));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
    FileProvider = new PhysicalFileProvider(frpSettings.PublicRootPath),
    RequestPath = "/public"
});

app.UseGrpcWeb();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<FreeRP.Net.Server.Middleware.Auth>();

app.MapGrpcService<ConnectService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<ContentService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<DatabaseService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<PdfService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<AdminService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<UserService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
