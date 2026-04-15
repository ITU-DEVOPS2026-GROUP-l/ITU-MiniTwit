using Chirp.Application.DTO;
using Chirp.Application.Services.Interface;
using Chirp.Core.Models;
using Chirp.Core.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Chirp.Application.Services.Implementation
{
    public class AuthorService : IAuthorService
    {
        private readonly IAuthorRepository _repo;
        private readonly ILogger<AuthorService> _logger;

        public AuthorService(IAuthorRepository repo,  ILogger<AuthorService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public AuthorDTO? FindAuthorByUsername(string username)
        {
            var author = _repo.FindAuthorByUserName(username);

            if (author == null)
            {
                return null;
            }

            return CreateAuthorDTO(author);
        }
        
        /// <summary>
        /// Creates an <see cref="AuthorDTO"/> from an <see cref="Author"/> entity.
        /// If the provided author is null, the method returns null.
        /// </summary>
        /// <param name="author">
        /// The <see cref="Author"/> entity to convert.
        /// </param>
        /// <returns>
        /// An <see cref="AuthorDTO"/> representing the author, or null if the
        /// input author is null.
        /// </returns>
        private AuthorDTO? CreateAuthorDTO(Author author)
        {
            if (author == null)
                return null;

            return new AuthorDTO
            {
                Id = author.Id,
                Name = author.Name,
                karma = author.karma,
                Email = author.Email == null ? "noEmaiFound@nomail.dk" : author.Email,
                CreationDate = author.CreationDate.ToString("dd/MM/yyyy HH:mm"),
                ProfilePicPath = author.ProfilePicPath == null ? "/images/default-profile.png" : author.ProfilePicPath
            };
        }

        /// <summary>
        /// Retrieves the author with the specified authorID, if one exists.
        /// </summary>
        /// <param name="authorId">The unique authorID of the author to retrieve. Cannot be null or empty.</param>
        /// <returns>An <see cref="AuthorDTO"/> representing the author with the specified authorID, or null.
        /// if no such author exists.</returns>
        public AuthorDTO? FindAuthorById(string authorId)
        {
            var author = _repo.FindAuthorById(authorId);

            if (author == null)
            {
                return null;
            }

            return CreateAuthorDTO(author);
        }

        public AuthorDTO? FindAuthorByEmail(string email)
        {
            var author = _repo.FindAuthorByEmail(email);

            if (author == null)
            {
                return null;
            }

            return CreateAuthorDTO(author);
        }

        /// <summary>
        /// Retrieves a list of followers for the specified author.
        /// </summary>
        /// <param name ="authorId"> The unique authorID of the author to retrieve the list from. Cannot be null or empty.</param>
        /// <returns> Returns a list representing the followers of specified <see cref="Author"/>, or null </returns>
        public IEnumerable<AuthorDTO?> GetFollowers(string authorId)
        {
            Author? author = _repo.FindAuthorById(authorId);

            if (author == null) return Enumerable.Empty<AuthorDTO>();

            return _repo.GetFollowers(author).Select(CreateAuthorDTO).ToList();
        }

        /// <summary>
        /// Retrieves a list of following for the specified author.
        /// </summary>
        /// <param name ="authorId"> The unique authorID of the author to retrieve the list from. Cannot be null or empty.</param>
        /// <returns> Returns a list representing the following of specified <see cref="Author"/>, or null </returns>
        public IEnumerable<AuthorDTO?> GetFollowing(string authorId)
        {
            Author? author = _repo.FindAuthorById(authorId);

            if (author == null) return Enumerable.Empty<AuthorDTO>();

            return _repo.GetFollowing(author).Select(CreateAuthorDTO).ToList();
        }

        /// <summary>
        /// Is author with authorId following author with followeeId-boolean.
        /// </summary>
        /// <param name="authorId"> The unique authorID of the author who might be following. Cannot be null or empty. </param>
        /// <param name="followeeId"> The unique authorID of the author who might be followed. Cannot be null or empty. </param>
        /// <returns> Returns a boolean, true if AuthorID is following followeeID, false if not.</returns>
        public bool IsFollowing(string authorId, string followeeId)
        {
            var follower = _repo.FindAuthorById(authorId);
            var followee = _repo.FindAuthorById(followeeId);

            if (follower == null || followee == null)
            {
                return false;
            }

            return _repo.DoesAuthorFollow(follower, followee);
        }
           
        /// <summary>
        /// Adds the specified author as a follower of another author.
        /// </summary>
        /// <remarks>If either <paramref name="authorId"/> or <paramref name="followeeId"/> does not
        /// correspond to an existing author, no action is taken.</remarks>
        /// <param name="authorId">The unique identifier of the author who will follow another author. Cannot be null or empty.</param>
        /// <param name="followeeId">The unique identifier of the author to be followed. Cannot be null or empty.</param>
        public void FollowAuthor(string authorId, string followeeId)
        {
            var follower = _repo.FindAuthorById(authorId);
            var followee = _repo.FindAuthorById(followeeId);

            if (follower == null || followee == null)
            {
                return;
            }

            _logger.LogInformation("NEW_FOLLOW follower={Follower} target={Target}", follower.Name, followee.Name);
            _repo.FollowAuthor(follower, followee);
        }

        /// <summary>
        /// Removes the specified author from the list of authors followed by another author.
        /// </summary>
        /// <remarks>If either the follower or the followee does not exist, the method performs no
        /// action.</remarks>
        /// <param name="authorId">The unique identifier of the author who will stop following another author. Cannot be null or empty.</param>
        /// <param name="followeeId">The unique identifier of the author to be unfollowed. Cannot be null or empty.</param>
        public void UnfollowAuthor(string authorId, string followeeId)
        {
            var follower = _repo.FindAuthorById(authorId);
            var followee = _repo.FindAuthorById(followeeId);

            if (follower == null || followee == null)
            {
                return;
            }

            _repo.UnfollowAuthor(follower, followee);
        }

        /// <summary>
        /// Retrieves the current karma score for the specified author.
        /// </summary>
        /// <param name="authorId">The unique identifier of the author whose karma score is to be retrieved. Cannot be null
        /// or empty.</param>
        /// <returns>The karma score associated with the specified author. Returns 0 if the author has no recorded karma.</returns>
        public int GetKarmaScore(string authorId)
        {
            return _repo.GetKarmaScore(authorId);
        }

        /// <summary>
        /// Changes the karma score of the specified author by the given amount.
        /// </summary>
        /// <param name="karma">The karma amount one would like to add onto the <paramref name="authorId"/> 's Karma</param>
        /// <param name="authorId"> The unique identifier of the author whose karma score is to be changed. Cannot be null.</param>
        public void ChangeKarma(int karma, string authorId)
        {
            _repo.ChangeKarma(karma, authorId);
        }
    }
}
