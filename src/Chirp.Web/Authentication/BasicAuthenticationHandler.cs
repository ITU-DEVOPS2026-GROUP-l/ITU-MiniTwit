using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Chirp.Web.Models;

public class BasicAuthenticationHandler 
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization"));

        if (authHeader != "Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh")
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization"));

        var claims = new[] { new Claim(ClaimTypes.Name, "simulator") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
    
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Status = 403,
            ErrorMsg = "You are not authorized to use this resource!"
        };

        return Response.WriteAsJsonAsync(response);
    }

}