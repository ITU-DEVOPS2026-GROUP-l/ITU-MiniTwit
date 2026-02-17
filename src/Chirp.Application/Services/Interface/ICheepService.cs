using Chirp.Application.DTO;
using Chirp.Core.Models;

namespace Chirp.Application.Services.Interface
{
    public interface ICheepService
    {
        /// <summary>
        /// Retrieves a paginated collection of cheeps for the specified page.
        /// </summary>
        /// <remarks>Use this method to access cheeps in a paginated manner. The number of cheeps per page
        /// is determined by the PageSize setting.</remarks>
        /// <param name="page">The page number to retrieve. Must be greater than or equal to 1. The first page is 1.</param>
        /// <returns>An <see cref="IEnumerable{CheepDTO}"/> containing cheeps for the specified page. Returns an empty collection
        /// if no cheeps are available for the page.</returns>
        public IEnumerable<CheepDTO> GetCheeps(int page = 1, int PageSize = 32);
        
        /// <summary>
        /// Retrieves a paginated collection of cheeps authored by the specified user.
        /// </summary>
        /// <param name="authorId">The unique identifier of the author whose cheeps are to be retrieved. Cannot be null or empty.</param>
        /// <param name="page">The page number of results to retrieve. Must be greater than or equal to 1. Defaults to 1.</param>
        /// <returns>An enumerable collection of <see cref="CheepDTO"/> objects representing the cheeps authored by the specified user.
        /// Returns an empty collection if no cheeps are found for the given author or page.</returns>
        public IEnumerable<CheepDTO> GetCheepsFromAuthorId(string author, int page = 1);

        /// <summary>
        /// Adds a new cheep with the specified text and author identifier to the repository.
        /// </summary>
        /// <param name="text">The content of the cheep to add. Cannot be null or empty.</param>
        /// <param name="authorId">The unique identifier of the author creating the cheep. Cannot be null or empty.</param>
        void AddCheep(string text, string authorId);

        /// <summary>
        /// Retrieves a paginated collection of cheeps posted by the specified authors.
        /// </summary>
        /// <param name="authorIds">A list of author identifiers. Only cheeps created by these authors will be included in the result.</param>
        /// <param name="page">The page number of results to retrieve. Must be greater than or equal to 1. The default value is 1.</param>
        /// <returns>An enumerable collection of <see cref="CheepDTO"/> objects representing cheeps from the specified authors
        /// for the requested page. If no cheeps are found, the collection will be empty.</returns>
        public IEnumerable<CheepDTO> GetCheepsFromMultipleAuthors(List<string> authorIds, int page = 1);

        /// <summary>
        /// Retrieves a <see cref="CheepDTO"/> that represents the cheep with the specified identifier.
        /// </summary>
        /// <remarks>Returns null if no cheep exists with the specified parameter <paramref name="cheepId"/>.</remarks>
        /// <param name="cheepId">The unique identifier of the cheep to retrieve. Must be a positive integer.</param>
        /// <returns>A <see cref="CheepDTO"/> containing the details of the cheep if found; otherwise, null/>.</returns>
        public CheepDTO? GetById(int cheepId);

        /// <summary>
        /// Registers or removes a "like" for the specified cheep by the given author.
        /// </summary>
        /// <remarks>This method updates the like status for the specified cheep and author. If the cheep
        /// or author does not exist, the operation may have no effect.</remarks>
        /// <param name="cheepId">The unique identifier of the cheep to be liked or unliked.</param>
        /// <param name="authorId">The identifier of the user performing the like or unlike action. Cannot be null or empty.</param>
        /// <param name="like">True to add a like to the cheep; False to remove an existing like.</param>
        public void Like(int cheepId, string authorId, bool like);

        /// <summary>
        /// Asynchronously retrieves a <see cref="Like"/> for the specified cheep and author, filtered by like status.
        /// </summary>
        /// <param name="cheepId">The unique identifier of the cheep to search for.</param>
        /// <param name="authorId">The unique identifier of the author whose like is being queried. Cannot be null.</param>
        /// <param name="like">True to retrieve a like; false to retrieve a dislike or unlike,
        /// depending on the application's model. </param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Like"/> if found;
        /// otherwise, null. </returns>
        Task<Like> GetLikeAsync(int cheepId, string authorId, bool like);
    }
}
