using System.Net;
using Chirp.Application.DTO;
using Chirp.Core.Models;
using Chirp.Core.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Chirp.Application.Services.Interface;
using Chirp.Web.Services;

namespace Chirp.Web.Pages;

public class PublicView : PageModel
{
    [BindProperty]
    public string? Text { get; set; }
    public IEnumerable<CheepDTO> Cheeps { get; set; } = new List<CheepDTO>();
    public IEnumerable<AuthorDTO?> Following { get; set; } = new List<AuthorDTO>();
    public int CurrentPage { get; set; }
    public AuthorDTO? IdentityAuthor { get; set; }
    private readonly IAuthorService _authorService;
    private readonly ICheepService _cheepService;

    public PublicView(ICheepService cheepService, IAuthorService authorService)
    {
        _cheepService = cheepService;
        _authorService = authorService;
    }

    public async Task<ActionResult> OnGet([FromQuery] int page = 1) //Pagination via query string
    {
        if (page < 1) page = 1; //Sikrer at page ikke er mindre end 1

        CurrentPage = page;
        Cheeps = _cheepService.GetCheeps(page);

        var userId = SessionAuth.GetUserId(HttpContext.Session);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            IdentityAuthor = _authorService.FindAuthorById(userId);

            if(IdentityAuthor == null)
            {
                return Page();
            }

            Following = _authorService.GetFollowing(IdentityAuthor.Id);
        }

        return Page();
    }
    
    public async Task<IActionResult> OnPost([FromQuery] int page = 1)
    {
        if (page < 1) page = 1;
        var userId = SessionAuth.GetUserId(HttpContext.Session);
        var author = string.IsNullOrWhiteSpace(userId) ? null : _authorService.FindAuthorById(userId);

        if(author == null)
        {
            return Page();
        }

        IdentityAuthor = author;
        Following = _authorService.GetFollowing(author.Id);

        if (!string.IsNullOrWhiteSpace(Text))
        {
            _cheepService.AddCheep(Text, author.Id);
        }

        CurrentPage = page;
        Cheeps = _cheepService.GetCheeps(page);
        Text = string.Empty;
        ModelState.Clear();
        return Page();
    }

    public async Task<ActionResult> OnPostToggleFollow(string followeeId)
    {
        var userId = SessionAuth.GetUserId(HttpContext.Session);
        
        // grab my current user.
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage();
        }

        var author = _authorService.FindAuthorById(userId);

        if(author == null)
        {
            return RedirectToPage();
        }

        if (!_authorService.IsFollowing(author.Id, followeeId))
        {
            _authorService.FollowAuthor(author.Id, followeeId);
        } 
        else
        {
            _authorService.UnfollowAuthor(author.Id, followeeId);
        }

        return RedirectToPage();
    }

    public async Task<string> GetUserName()
    {
        var userId = SessionAuth.GetUserId(HttpContext.Session);
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            return string.Empty;
        }
        return userId ?? "Anon";
    }

    //handle likes and dislikes
    public async Task<IActionResult> OnPostCheepLikeAsync(int cheepId, string userId)
    {
        var sessionUserId = SessionAuth.GetUserId(HttpContext.Session);
        var currentAuthor = string.IsNullOrWhiteSpace(sessionUserId) ? null : _authorService.FindAuthorById(sessionUserId);
               
        if (currentAuthor == null)
        {
            return RedirectToPage();
        }
        
        var like = await _cheepService.GetLikeAsync(cheepId, currentAuthor.Id, true);

        var authorId = _cheepService.GetById(cheepId)!.AuthorId;

        var karmaChange = 0;

        if (like.likeStatus == -1) { karmaChange = 20; }
        else if (like.likeStatus == 0) { karmaChange = 10; }
        else if (like.likeStatus == 1) { karmaChange = -10; }


        _cheepService.Like(cheepId, currentAuthor.Id, true);
        _authorService.ChangeKarma(karmaChange, authorId);

        // Redirect back to the same author’s page
        return RedirectToPage("/PublicView", new { authorId = userId, page = CurrentPage });
    }

    public async Task<IActionResult> OnPostCheepDislikeAsync(int cheepId, string userId)
    {
        var  sessionUserId = SessionAuth.GetUserId(HttpContext.Session);
        var currentAuthor = string.IsNullOrWhiteSpace(sessionUserId) ? null : _authorService.FindAuthorById(sessionUserId);
        if (currentAuthor == null)
            return RedirectToPage();

        Like like = await _cheepService.GetLikeAsync(cheepId, currentAuthor.Id, false);

        string authorId = _cheepService.GetById(cheepId)!.AuthorId;

        int karmaChange = 0;

        if (like.likeStatus == -1) { karmaChange = 10; }
        else if (like.likeStatus == 0) { karmaChange = -10; }
        else if (like.likeStatus == 1) { karmaChange = -20; }


        _cheepService.Like(cheepId, currentAuthor.Id, false);
        _authorService.ChangeKarma(karmaChange, authorId);

        // Redirect back to the same author’s page
        return RedirectToPage("/PublicView", new { authorId = userId, page = CurrentPage });
    }
}
