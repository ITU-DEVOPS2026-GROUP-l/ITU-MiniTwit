using Chirp.Core.Models;

namespace Chirp.Core.Repositories
{
    public interface IAuthorRepository
    {
        /// <summary>
        /// Returns a paginated collection of all cheeps.
        /// </summary>
        /// <param name="page">The page number of results to retrieve. Must be greater than or equal to 1. Defaults to 1.</param>        
        /// <param name="pageSize">The pagination int i.e 32 as default</param>
        /// <returns>Returns an Enumerable collection of <see cref="Cheep"/> objects representing cheeps from all authors for the requested page.
        /// If no cheeps are found. the collection will be empty. </returns>
        public Author? FindAuthorById(string id);
        
        public Author? FindAuthorByUserName(string username);

        /// <summary>
        /// Retrieves the collection of authors who follow the specified author.
        /// </summary>
        /// <param name="author">The author whose followers are to be retrieved. Cannot be null.</param>
        /// <returns>An enumerable collection of <see cref="Author"/> objects representing the followers of the specified author.
        /// The collection is empty if the author has no followers.</returns>
        IEnumerable<Author> GetFollowers(Author author);

        /// <summary>
        /// Returns a collection of authors that the specified author is following.
        /// </summary>
        /// <param name="author">The author whose followed authors are to be retrieved. Cannot be null.</param>
        /// <returns>An enumerable collection of <see cref="Author"/> objects representing the authors followed by the specified
        /// author. The collection is empty if the author is not following anyone.</returns>
        IEnumerable<Author> GetFollowing(Author author);

        /// <summary>
        /// Determines whether the specified author is following another author.
        /// </summary>
        /// <param name="Follower">The author whose following status is to be checked.</param>
        /// <param name="Followee">The author to check if they are being followed by <paramref name="Follower"/>.</param>
        /// <returns>true if <paramref name="Follower"/> is following <paramref name="Followee"/>; otherwise,
        /// false.</returns>
        bool DoesAuthorFollow(Author Follower, Author Followee);

        /// <summary>
        /// Adds a follow relationship between the specified follower and followee authors.
        /// </summary>
        /// <remarks>If the follower is already following the followee, this method performs no
        /// action.</remarks>
        /// <param name="Follower">The author who will follow the specified followee. Cannot be null.</param>
        /// <param name="Followee">The author to be followed. Cannot be null.</param>
        void FollowAuthor(Author Follower, Author Followee);

        /// <summary>
        /// Removes the follow relationship between the specified follower and followee authors.
        /// </summary>
        /// <remarks>If the follower is not currently following the followee, this method performs no
        /// action.</remarks>
        /// <param name="Follower">The author who is unfollowing another author. Cannot be null.</param>
        /// <param name="Followee">The author to be unfollowed. Cannot be null.</param>
        void UnfollowAuthor(Author Follower, Author Followee);

        /// <summary>
        /// Retrieves the karma score for the specified author.
        /// </summary>
        /// <param name="authorId">The unique identifier of the author whose karma score is to be retrieved. Cannot be null.</param>
        /// <returns>The karma score of the author if found; otherwise, 0.</returns>
        int GetKarmaScore(string authorId);

        /// <summary>
        /// Changes the karma score of the specified author by the given amount.
        /// </summary>
        /// <param name="karma">The karma amount one would like to add onto the <paramref name="authorId"/> 's Karma</param>
        /// <param name="authorId"> The unique identifier of the author whose karma score is to be changed. Cannot be null.</param>
        void ChangeKarma(int karma, string authorId);
    }
}
