using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdilBooks.Data;
using AdilBooks.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdilBooks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Authors/List
        /// <summary>
        /// Retrieves a list of all authors.
        /// </summary>
        /// <returns>A list of authors.</returns>
        [HttpGet("List")]
        public async Task<ActionResult<IEnumerable<AuthorListDto>>> ListAuthors()
        {
            var authors = await _context.Authors.ToListAsync();

            var authorDtos = authors.Select(author => new AuthorListDto
            {
                AuthorId = author.AuthorId,
                Name = author.Name
            }).ToList();

            return Ok(authorDtos);
        }

        // GET: api/Authors/Find/{id}
        /// <summary>
        /// Retrieves details of a specific author by their ID.
        /// </summary>
        /// <param name="id">The ID of the author to retrieve.</param>
        /// <returns>Details of the specified author.</returns>
        [HttpGet("Find/{id}")]
        public async Task<ActionResult<AuthorDto>> FindAuthor(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.AuthorId == id);

            if (author == null)
            {
                return NotFound("Author not found.");
            }

            var authorDto = new AuthorDto
            {
                AuthorId = author.AuthorId,
                Name = author.Name,
                Bio = author.Bio,
                Titles = string.Join(", ", author.Books.Select(b => b.Title))
            };

            return Ok(authorDto);
        }

        // POST: api/Authors/Add
        /// <summary>
        /// Adds a new author to the database.
        /// </summary>
        /// <param name="authorDto">The details of the author to add.</param>
        /// <returns>The newly added author.</returns>
        [HttpPost("Add")]
        public async Task<ActionResult<AuthorDto>> AddAuthor(AuthorDto authorDto)
        {
            var author = new Author
            {
                Name = authorDto.Name,
                Bio = authorDto.Bio,
            };

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            // Return the created author with their ID
            authorDto.AuthorId = author.AuthorId;
            return CreatedAtAction(nameof(FindAuthor), new { id = author.AuthorId }, authorDto);
        }

        // PUT: api/Authors/Update/{id}
        /// <summary>
        /// Updates an existing author's details.
        /// </summary>
        /// <param name="id">The ID of the author to update.</param>
        /// <param name="authorDto">The updated details of the author.</param>
        /// <returns>No content on successful update.</returns>
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateAuthor(int id, AuthorDto authorDto)
        {
            if (id != authorDto.AuthorId)
            {
                return BadRequest("Author ID mismatch.");
            }

            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound("Author not found.");
            }

            author.Name = authorDto.Name;
            author.Bio = authorDto.Bio;

            _context.Entry(author).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(id))
                {
                    return NotFound("Author not found.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Authors/Delete/{id}
        /// <summary>
        /// Deletes a specific author by their ID.
        /// </summary>
        /// <param name="id">The ID of the author to delete.</param>
        /// <returns>No content on successful deletion.</returns>
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteAuthor(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound("Author not found.");
            }

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helper method to check if an author exists
        private bool AuthorExists(int id)
        {
            return _context.Authors.Any(a => a.AuthorId == id);
        }
        [HttpGet("GetBooks/{id}")]
        /// <summary>
        /// Retrieves all books associated with a specific author.
        /// </summary>
        /// <remarks>
        /// This method returns a list of books with only their title and year for the given author.
        /// </remarks>
        /// <param name="id">The ID of the author whose books are to be retrieved.</param>
        /// <returns>200 OK with a list of books, or 404 Not Found if the author or books are not found.</returns>
        /// <example>
        /// api/Authors/GetBooksForAuthor/{id} -> Retrieves books with title and year for the given author.
        /// </example>
        public async Task<ActionResult<IEnumerable<GetBooksDto>>> GetBooksForAuthor(int id)
        {
            // Check if the author exists
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.AuthorId == id);

            if (author == null)
            {
                return NotFound("Author not found.");
            }

            if (author.Books == null || !author.Books.Any())
            {
                return NotFound("No books found for this author.");
            }

            // Map the books to GetBooksDto and return them
            var booksDto = author.Books.Select(book => new GetBooksDto
            {
                Title = book.Title,
                Year = book.Year
            }).ToList();

            return Ok(booksDto);
        }

    }
}
