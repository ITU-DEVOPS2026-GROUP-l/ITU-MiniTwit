using Chirp.Application.DTO;

namespace Chirp.Application.Services.Interface
{
    public interface IAuthorService
    {
        /// <summary>
        /// Retrieves the author with the specified authorID, if one exists.
        /// </summary>
        /// <param name="authorId">The unique authorID of the author to retrieve. Cannot be null or empty.</param>
        /// <returns>An <see cref="AuthorDTO"/> representing the author with the specified authorID, or null.
        /// if no such author exists.</returns>
        AuthorDTO? FindAuthorById(string name);
        
        AuthorDTO? FindAuthorByUsername(string username);

        AuthorDTO? FindAuthorByEmail(string email);


        /// <summary>
        /// Retrieves a list of followers for the specified author.
        /// </summary>
        /// <param name ="authorId"> The unique authorID of the author to retrieve the list from. Cannot be null or empty.</param>
        /// <returns> Returns a list representing the followers of specified <see cref="Author"/>, or null </returns>
        IEnumerable<AuthorDTO?> GetFollowers(string authorId);

        /// <summary>
        /// Retrieves a list of following for the specified author.
        /// </summary>
        /// <param name ="authorId"> The unique authorID of the author to retrieve the list from. Cannot be null or empty.</param>
        /// <returns> Returns a list representing the following of specified <see cref="Author"/>, or null </returns>
        IEnumerable<AuthorDTO?> GetFollowing(string authorId);

        /// <summary>
        /// Is author with authorId following author with followeeId-boolean.
        /// </summary>
        /// <param name="authorId"> The unique authorID of the author who might be following. Cannot be null or empty. </param>
        /// <param name="followeeId"> The unique authorID of the author who might be followed. Cannot be null or empty. </param>
        /// <returns> Returns a boolean, true if AuthorID is following followeeID, false if not.</returns>
        bool IsFollowing(string authorId, string followeeId);

        /// <summary>
        /// Adds the specified author as a follower of another author.
        /// </summary>
        /// <remarks>If either <paramref name="authorId"/> or <paramref name="followeeId"/> does not
        /// correspond to an existing author, no action is taken.</remarks>
        /// <param name="authorId">The unique identifier of the author who will follow another author. Cannot be null or empty.</param>
        /// <param name="followeeId">The unique identifier of the author to be followed. Cannot be null or empty.</param>
        void FollowAuthor(string authorId, string followeeId);

        /// <summary>
        /// Removes the specified author from the list of authors followed by another author.
        /// </summary>
        /// <remarks>If either the follower or the followee does not exist, the method performs no
        /// action.</remarks>
        /// <param name="authorId">The unique identifier of the author who will stop following another author. Cannot be null or empty.</param>
        /// <param name="followeeId">The unique identifier of the author to be unfollowed. Cannot be null or empty.</param>
        void UnfollowAuthor(string authorId, string followeeId);

        /// <summary>
        /// Retrieves the current karma score for the specified author.
        /// </summary>
        /// <param name="authorId">The unique identifier of the author whose karma score is to be retrieved. Cannot be null
        /// or empty.</param>
        /// <returns>The karma score associated with the specified author. Returns 0 if the author has no recorded karma.</returns>
        int GetKarmaScore(string authorId);

        /// <summary>
        /// Changes the karma score of the specified author by the given amount.
        /// </summary>
        /// <param name="karma">The karma amount one would like to add onto the <paramref name="authorId"/> 's Karma</param>
        /// <param name="authorId"> The unique identifier of the author whose karma score is to be changed. Cannot be null.</param>
        void ChangeKarma(int karma, string authorId);

    }
}
