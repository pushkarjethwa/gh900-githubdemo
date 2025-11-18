using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using GoofyApp.Models;

namespace GoofyApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuthenticationSchemeProvider _schemeProvider;

    public HomeController(ILogger<HomeController> logger, IAuthenticationSchemeProvider schemeProvider)
    {
        _logger = logger;
        _schemeProvider = schemeProvider;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // generate a login code which will login using google identity provider using MS Authentication Library
    public async Task<IActionResult> Login()
    {
        const string googleScheme = "Google";  
        var schemes = await _schemeProvider.GetAllSchemesAsync();

        var hasGoogleScheme = false;
        foreach (var scheme in schemes)
        {
            if (string.Equals(scheme.Name, googleScheme, StringComparison.Ordinal))
            {
                hasGoogleScheme = true;
                break;
            }
        }

        if (!hasGoogleScheme)
        {
            _logger.LogWarning("Authentication scheme '{Scheme}' is not configured.", googleScheme);
            return StatusCode(501, "Google authentication is not configured on this server.");
        }

        var redirectUrl = Url.Action("Index", "Home");
        var authProperties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(authProperties, googleScheme);
    }   

    // create a function reverse string
    public string ReverseString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        char[] charArray = input.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutAll()
    {
        _logger.LogInformation("Signing out from all available authentication schemes.");

        var schemes = await _schemeProvider.GetAllSchemesAsync();
        var signOutTasks = new List<Task>();

        foreach (var scheme in schemes)
        {
            if (string.IsNullOrEmpty(scheme.Name) || scheme.HandlerType is null)
            {
                continue;
            }

            if (typeof(IAuthenticationSignOutHandler).IsAssignableFrom(scheme.HandlerType))
            {
                signOutTasks.Add(HttpContext.SignOutAsync(scheme.Name));
            }
        }

        if (signOutTasks.Count > 0)
        {
            await Task.WhenAll(signOutTasks);
        }

        var sessionFeature = HttpContext.Features.Get<ISessionFeature>();
        if (sessionFeature?.Session is { } session)
        {
            await session.LoadAsync();
            session.Clear();
            await session.CommitAsync();
        }

        _logger.LogInformation("All authentication cookies and session data cleared successfully.");

        return RedirectToAction(nameof(Index));
    }

    public bool IsPrime(int number)
    {
        if (number <= 1)
        {
            return false;
        }

        if (number <= 3)
        {
            return true;
        }

        if (number % 2 == 0 || number % 3 == 0)
        {
            return false;
        }

        var limit = (int)Math.Sqrt(number);
        for (var i = 5; i <= limit; i += 6)
        {
            if (number % i == 0 || number % (i + 2) == 0)
            {
                return false;
            }
        }

        return true;
    }
}
