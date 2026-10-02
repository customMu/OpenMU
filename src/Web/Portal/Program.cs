// <copyright file="Program.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MUnique.OpenMU.Web.Portal;
using MUnique.OpenMU.Web.Portal.Data;
using MUnique.OpenMU.Web.Portal.Services;

[assembly: InternalsVisibleTo("MUnique.OpenMU.Web.Portal.Tests")]

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.Configure<PortalOptions>(builder.Configuration.GetSection(PortalOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("OpenMU")
    ?? throw new InvalidOperationException("The connection string 'OpenMU' is not configured.");
services.AddDbContext<PortalDbContext>(options => options.UseNpgsql(connectionString));

services.AddMemoryCache();
services.AddScoped<AccountService>();
services.AddScoped<RankingService>();
services.AddHttpClient<ServerStatusService>(client => client.Timeout = TimeSpan.FromSeconds(5));

services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.Cookie.Name = "OpenMU.Portal";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });
services.AddAuthorization();

// Limits brute force attempts on login and registration per client address.
services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Credentials, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Account");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
});

services.Configure<ForwardedHeadersOptions>(options =>
{
    // Required when running behind a reverse proxy (nginx, caddy, ...), so that the rate limiter
    // sees the real client address. Only proxies on the loopback address are trusted by default;
    // add the address of your proxy to KnownProxies when it runs on another host.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
