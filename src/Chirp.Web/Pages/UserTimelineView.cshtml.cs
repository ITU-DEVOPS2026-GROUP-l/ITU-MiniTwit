using Chirp.Application.DTO;
using Chirp.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Chirp.Application.Services.Interface;
using Chirp.Web.Services;

namespace Chirp.Web.Pages;
public class UserTimelineView : PageModel
{
    private readonly ICheepService _cheepService;
    private readonly IAuthorService _authorService;
    public IEnumerable<CheepDTO> Cheeps { get; set; } = new List<CheepDTO>();
    public IEnumerable<AuthorDTO?> Following { get; set; } = new List<AuthorDTO>();
    public int CurrentPage { get; set; } //Tracker til pagination
    public AuthorDTO? Author { get; set; } = null;  //Tracker til author navn
    public AuthorDTO? IdentityAuthor { get; set; } = null;
    
    /**
     * Manages a users timeline, and handles posts of followers, as well as own
     * posts being shown on the users own timeline, as well as on timelines
     * which are visited
     */
    public UserTimelineView(ICheepService cheepService, IAuthorService authorService)
    {
        _cheepService = cheepService;
        _authorService = authorService;
    }

    public async Task<ActionResult> OnGet(string authorId, [FromQuery] int page = 1) //Pagination via query string
    {
        Author = _authorService.FindAuthorById(authorId);
        
        //Switch from _identityService to SessionAuth to manage user login
        var sessionUserId = SessionAuth.GetUserId(HttpContext.Session);
        IdentityAuthor = string.IsNullOrWhiteSpace(sessionUserId) ? null : _authorService.FindAuthorById(sessionUserId);

        if (Author == null)
        {
            return NotFound();
        }

        CurrentPage = page;
        Following = _authorService.GetFollowing(authorId);

        if (IdentityAuthor != null && authorId == Author.Id)
        {
            Cheeps = _cheepService.GetCheepsFromMultipleAuthors(
                Following.Select(a =>
                {
                    if (a != null)
                    {
                        return a.Id;
                    }

                    return string.Empty;
                })
                .Append(authorId)
                .ToList(), page);
        }

        if (!Cheeps.Any())
        {
            Cheeps = _cheepService.GetCheepsFromAuthorId(authorId, page);
        }

        return Page();
    }

    public async Task<ActionResult> OnPostToggleFollow(string followeeId)
    {
        // grab my current user.

        var sessionUserId = SessionAuth.GetUserId(HttpContext.Session);
        if (string.IsNullOrWhiteSpace(sessionUserId))
        {
            return RedirectToPage();
        }

        var userAuthor = _authorService.FindAuthorById(sessionUserId);

        if (userAuthor == null)
        {
            return RedirectToPage();
        }

        if (!_authorService.IsFollowing(userAuthor.Id, followeeId))
        {
            _authorService.FollowAuthor(userAuthor.Id, followeeId);
        }
        else
        {
            _authorService.UnfollowAuthor(userAuthor.Id, followeeId);
        }

        return RedirectToPage();
    }
    public string GetUserName(string id)
    {
        AuthorDTO? author = _authorService.FindAuthorById(id);

        if (author == null)
        {
            return string.Empty;
        }

        return author.Name;
    }

    /**
     * Handles likes and dislikes. Uses userId to check if user has already liked a post
     * Session based login is used instead of identity based cookies.
     */
    public async Task<IActionResult> OnPostCheepLikeAsync(int cheepId, string userId)
    {
        var sessionUserId = SessionAuth.GetUserId(HttpContext.Session);
        if (string.IsNullOrWhiteSpace(sessionUserId))
            return RedirectToPage();

        var currentAuthor = _authorService.FindAuthorById(sessionUserId);
        if (currentAuthor == null)
            return RedirectToPage();

        Like like = await _cheepService.GetLikeAsync(cheepId, currentAuthor.Id, true);

        string authorId = _cheepService.GetById(cheepId)!.AuthorId;

        int karmaChange = 0;

        if (like.likeStatus == -1) { karmaChange = 20; }
        else if (like.likeStatus == 0) { karmaChange = 10; }
        else if (like.likeStatus == 1) { karmaChange = -10; }


        _cheepService.Like(cheepId, currentAuthor.Id, true);
        _authorService.ChangeKarma(karmaChange, authorId);

        // Redirect back to the same author’s page
        return RedirectToPage("/UserTimelineView", new { authorId = currentAuthor.Id, page = CurrentPage });
    }

    public async Task<IActionResult> OnPostCheepDislikeAsync(int cheepId, string userId)
    {
        var sessionUserId = SessionAuth.GetUserId(HttpContext.Session);
        if (string.IsNullOrWhiteSpace(sessionUserId))
            return RedirectToPage();

        var currentAuthor = _authorService.FindAuthorById(sessionUserId);
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
        return RedirectToPage("/UserTimelineView", new { authorId = currentAuthor.Id, page = CurrentPage });
    }
}
