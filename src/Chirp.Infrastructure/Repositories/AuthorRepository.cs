using Chirp.Core.Data;
using Chirp.Core.Models;
using Chirp.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Razor.Repositories
{
    public class AuthorRepository : IAuthorRepository
    {
        private readonly ChirpDBContext _context;

        public AuthorRepository(ChirpDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves the author with the specified identifier, if one exists.
        /// </summary>
        /// <param name="id">The unique identifier of the author to find. Cannot be null or empty.</param>
        /// <returns>— The <see cref="Author"/> with the specified identifier, or null if no matching author is
        /// found.</returns>
        public Author? FindAuthorById(string id)
        {
            return _context.Authors
                .Where(a => a.Id == id)
                .FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the author with the specified username, if one exists.
        /// </summary>
        /// <param name="username">The username to find. Cannot be null or empty.</param>
        /// <returns>The matching <see cref="Author"/> or null if not found.</returns>
        public Author? FindAuthorByUserName(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var trimmed = username.Trim();
            var normalized = trimmed.ToUpperInvariant();

            return _context.Authors
                .FirstOrDefault(a => a.UserName == trimmed || a.NormalizedUserName == normalized);
        }
        
        public Author? FindAuthorByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var trimmed = email.Trim();
            var normalized = trimmed.ToUpperInvariant();

            return _context.Authors
                .FirstOrDefault(a => a.Email == trimmed || a.NormalizedEmail == normalized);
        }

        /// <summary>
        /// Retrieves the collection of authors who follow the specified author.
        /// </summary>
        /// <param name="author">The author whose followers are to be retrieved. Cannot be null.</param>
        /// <returns>An enumerable collection of <see cref="Author"/> objects representing the followers of the specified author.
        /// The collection is empty if the author has no followers.</returns>
        public IEnumerable<Author> GetFollowers(Author author) 
        {
            return _context.UserFollows
                .Where(x => x.FolloweeId == author.Id)
                .Include(x => x.Follower)
                .Select(x => x.Follower)
                .ToList();
        }

        /// <summary>
        /// Returns a collection of authors that the specified author is following.
        /// </summary>
        /// <param name="author">The author whose followed authors are to be retrieved. Cannot be null.</param>
        /// <returns>An enumerable collection of <see cref="Author"/> objects representing the authors followed by the specified
        /// author. The collection is empty if the author is not following anyone.</returns>
        public IEnumerable<Author> GetFollowing(Author author) 
        {
            return _context.UserFollows
                .Where(x => x.FollowerId == author.Id)
                .Include(x => x.Followee)
                .Select(x => x.Followee)
                .ToList();
        }

        /// <summary>
        /// Determines whether the specified author is following another author.
        /// </summary>
        /// <param name="Follower">The author whose following status is to be checked.</param>
        /// <param name="Followee">The author to check if they are being followed by <paramref name="Follower"/>.</param>
        /// <returns>true if <paramref name="Follower"/> is following <paramref name="Followee"/>; otherwise,
        /// false.</returns>
        public bool DoesAuthorFollow(Author Follower, Author Followee) 
        {
            return _context.UserFollows.Any(x =>
                x.FollowerId == Follower.Id &&
                x.FolloweeId == Followee.Id);
        }
        
        /// <summary>
        /// Adds a follow relationship between the specified follower and followee authors.
        /// </summary>
        /// <remarks>If the follower is already following the followee, this method performs no
        /// action.</remarks>
        /// <param name="Follower">The author who will follow the specified followee. Cannot be null.</param>
        /// <param name="Followee">The author to be followed. Cannot be null.</param>
        public void FollowAuthor(Author Follower, Author Followee) 
        {
            if (DoesAuthorFollow(Follower, Followee))
            {
                return;
            }

            _context.UserFollows.Add(new UserFollow
            {
                FollowerId = Follower.Id,
                FolloweeId = Followee.Id,
                TimeStamp = DateTime.UtcNow
            });

            _context.SaveChanges();
        }

        /// <summary>
        /// Removes the follow relationship between the specified follower and followee authors.
        /// </summary>
        /// <remarks>If the follower is not currently following the followee, this method performs no
        /// action.</remarks>
        /// <param name="Follower">The author who is unfollowing another author. Cannot be null.</param>
        /// <param name="Followee">The author to be unfollowed. Cannot be null.</param>
        public void UnfollowAuthor(Author Follower, Author Followee) 
        {
            var follow = _context.UserFollows.FirstOrDefault(x =>
                x.FollowerId == Follower.Id &&
                x.FolloweeId == Followee.Id);

            if (follow == null)
            {
                return;
            }

            _context.UserFollows.Remove(follow);

            _context.SaveChanges();
        }

        /// <summary>
        /// Retrieves the karma score for the specified author.
        /// </summary>
        /// <param name="authorId">The unique identifier of the author whose karma score is to be retrieved. Cannot be null.</param>
        /// <returns>The karma score of the author if found; otherwise, 0.</returns>
        public int GetKarmaScore(string authorId) 
        {
            var author = _context.Authors
                .Where(a => a.Id == authorId)
                .FirstOrDefault();
            if (author == null)
            {
                return 0;
            }
            return author.karma;
        }

        /// <summary>
        /// Changes the karma score of the specified author by the given amount.
        /// </summary>
        /// <param name="karma">The karma amount one would like to add onto the <paramref name="authorId"/> 's Karma</param>
        /// <param name="authorId"> The unique identifier of the author whose karma score is to be changed. Cannot be null.</param>
        public void ChangeKarma(int karma, string authorId) 
        {
            var author = _context.Authors
                .Where(a => a.Id == authorId)
                .FirstOrDefault();
            if (author == null)
            {
                return;
            }
            author.karma += karma;
            _context.SaveChanges();
        }
    }
}
