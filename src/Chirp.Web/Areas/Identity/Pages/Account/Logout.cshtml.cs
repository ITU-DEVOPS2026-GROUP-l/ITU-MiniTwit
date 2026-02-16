// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Chirp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Chirp.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;
        private readonly SignInManager<Chirp.Core.Models.Author> _signInManager;

        public LogoutModel(
            ILogger<LogoutModel> logger,
            SignInManager<Chirp.Core.Models.Author> signInManager)
        {
            _logger = logger;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGet(string returnUrl = "/")
        {
            SessionAuth.SignOut(HttpContext.Session);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return LocalRedirect(returnUrl ?? "/");
        }

        public async Task<IActionResult> OnPost(string returnUrl = "/")
        {
            SessionAuth.SignOut(HttpContext.Session);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return LocalRedirect(returnUrl ?? "/");
        }
    }
}

