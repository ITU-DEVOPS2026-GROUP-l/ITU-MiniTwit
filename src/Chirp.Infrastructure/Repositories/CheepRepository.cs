using Chirp.Core.Data;
using Chirp.Core.Models;
using Chirp.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Razor.Repositories
{
    public class CheepRepository : ICheepRepository
    {
        private readonly ChirpDBContext _context;

        public CheepRepository(ChirpDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns a paginated collection of all cheeps.
        /// </summary>
        /// <param name="page">The page number of results to retrieve. Must be greater than or equal to 1. Defaults to 1.</param>        
        /// <param name="pageSize">The pagination int i.e 32 as default</param>
        /// <returns>Returns an Enumerable collection of <see cref="Cheep"/> objects representing cheeps from all authors for the requested page.
        /// If no cheeps are found. the collection will be empty. </returns>
        public IEnumerable<Cheep> GetAll(int page = 1, int pageSize = 32)
        {
            int offset = (page - 1) * pageSize;
            return _context.Cheeps
                .AsNoTracking()
                .Include(c => c.Author)
                .Include(c => c.Likes)
                .OrderByDescending(c => c.TimeStamp)
                .Skip(offset)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Returns a paginated collection of cheeps specifically identified to be made by the same Author.
        /// </summary>
        /// <param name="authorId">The unique identification of the Author</param>
        /// <param name="page">The page number of results to retrieve. Must be grater than or equal to 1. Defaults to 1.</param>
        /// <param name="pageSize">The pagination int i.e 32 as default</param>
        /// <returns>Returns an Enumarable collection of <see cref="Cheep"/> objects representing cheeps from the specified <see cref="Author"/>,
        /// for the specified requested page. 
        /// If no cheeps are found. The collection will be empty.</returns>
        public IEnumerable<Cheep> GetByAuthorId(string authorId, int page = 1, int pageSize = 32)
        {
            int offset = (page - 1) * pageSize;
            return _context.Cheeps
                .AsNoTracking()
                .Include(c => c.Author)
                .Include(c => c.Likes)
                .Where(c => c.AuthorId == authorId)
                .OrderByDescending(c => c.TimeStamp)
                .Skip(offset)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Returns a specified Cheep from the CheepID
        /// </summary>
        /// <param name="id">The unique identifiaction of the Cheep.</param>
        /// <returns>Returns the requested <see cref="Cheep"/> from the CheepID</returns>
        public Cheep GetById(int id)
        {
            return _context.Cheeps
                .AsNoTracking()
                .Include(c => c.Author)
                .Include(c => c.Likes)
                .First(c => c.CheepId == id);
        }
        /// <summary>
        /// Returns a paginated collection of cheeps specifically identified to be made by multiple Authors.
        /// </summary>
        /// <param name="authorIds">The unique identification of the Author</param>
        /// <param name="page">The page number of results to retrieve. Must be grater than or equal to 1. Defaults to 1.</param>
        /// <param name="pageSize">The pagination int i.e 32 as default</param>
        /// <returns>Returns an Enumarable collection of <see cref="Cheep"/> objects representing cheeps from the specified <see cref="Author"/> 's,
        /// for the specified requested page. 
        /// If no cheeps are found. The collection will be empty.</returns>
        public IEnumerable<Cheep> GetByMultipleAuthors(List<string> authorIds, int page = 1, int pageSize = 32)
        {
            int offset = (page - 1) * pageSize;
            return _context.Cheeps
                .AsNoTracking()
                .Include(c => c.Author)
                .Include(c => c.Likes)
                .Where(c => authorIds.Contains(c.AuthorId))
                .OrderByDescending(c => c.TimeStamp)
                .Skip(offset)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Creates a new Cheep and saves it to the Database.
        /// </summary>
        /// <param name="text">The text you would like to save as the cheep-text</param>
        /// <param name="authorId">The unique identification of the Author</param>
        public void AddCheep(string text, string authorId)
        {
            Cheep cheep = new Cheep
            {
                AuthorId = authorId,
                Text = text,
                TimeStamp = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
            };

            _context.Cheeps.Add(cheep);
            _context.SaveChanges();
        }

        /// <summary>
        /// Registers or removes a "like" for the specified cheep by the given author.
        /// </summary>
        /// <remarks>This method updates the like status for the specified cheep and author. If the cheep
        /// or author does not exist, the operation may have no effect.</remarks>
        /// <param name="cheepId">The unique identifier of the cheep to be liked or unliked.</param>
        /// <param name="authorId">The identifier of the user performing the like or unlike action. Cannot be null or empty.</param>
        /// <param name="like">True to add a like to the cheep; False to remove an existing like.</param>
        public void Like(int cheepId, string authorID, bool like)
        {
            var cheepExists = _context.Cheeps.Any(c => c.CheepId == cheepId);
            if (!cheepExists) return;

            var authorExists = _context.Authors.Any(a => a.Id == authorID);
            if (!authorExists) return;

            var existing = _context.Likes.FirstOrDefault(l => l.CheepId == cheepId && l.authorId == authorID);

            if (existing == null)
            {
                _context.Likes.Add(new Like
                {
                    CheepId = cheepId,
                    authorId = authorID,
                    likeStatus = like ? 1 : -1
                });
            }
            else
            {
                if (like && existing.likeStatus == 1)
                {
                    _context.Likes.Remove(existing);
                }
                else if (!like && existing.likeStatus == -1)
                {
                    _context.Likes.Remove(existing);
                }
                else
                {
                    existing.likeStatus = like ? 1 : -1;
                }
            }
            _context.SaveChanges();
        }

        /// <summary>
        /// Asynchronously retrieves a <see cref="Like"/> for the specified cheep and author, filtered by like status.
        /// </summary>
        /// <param name="cheepId">The unique identifier of the cheep to search for.</param>
        /// <param name="authorId">The unique identifier of the author whose like is being queried. Cannot be null.</param>
        /// <param name="like">True to retrieve a like; false to retrieve a dislike or unlike,
        /// depending on the application's model. </param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Like"/> if found;
        /// otherwise, null. </returns>
        public async Task<Like> GetLikeAsync(int cheepId, string authorId, bool state)
        {
            var like = await _context.Likes.FirstOrDefaultAsync(l => l.CheepId == cheepId && l.authorId == authorId);
            return like ?? new Like { CheepId = cheepId, authorId = authorId, likeStatus = 0 };
        }
    }
}
