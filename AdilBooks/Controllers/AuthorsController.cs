using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdilBooks.Data;
using AdilBooks.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
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
        public async Task<ActionResult> ListAuthors()
        {
            try
            {
                var authors = await _context.Authors.ToListAsync();
                var authorDtos = authors.Select(author => new AuthorListDto
                {
                    AuthorId = author.AuthorId,
                    Name = author.Name
                }).ToList();

                return Ok(new { message = "Authors retrieved successfully.", data = authorDtos });
            }
            catch
            {
                return StatusCode(500, "An error occurred while retrieving authors.");
            }
        }

        // GET: api/Authors/Find/{id}
        /// <summary>
        /// Retrieves details of a specific author by their ID.
        /// </summary>
        /// <param name="id">The ID of the author to retrieve.</param>
        /// <returns>Details of the specified author.</returns>
        [HttpGet("Find/{id}")]
        public async Task<ActionResult> FindAuthor(int id)
        {
            try
            {
                var author = await _context.Authors
                    .Include(a => a.Books)
                    .FirstOrDefaultAsync(a => a.AuthorId == id);

                if (author == null) return NotFound(new { message = "Author not found." });

                var authorDto = new AuthorDto
                {
                    AuthorId = author.AuthorId,
                    Name = author.Name,
                    Bio = author.Bio,
                    Titles = string.Join(", ", author.Books.Select(b => b.Title))
                };

                return Ok(new { message = "Author retrieved successfully.", data = authorDto });
            }
            catch
            {
                return StatusCode(500, "An error occurred while retrieving the author.");
            }
        }

        // POST: api/Authors/Add
        /// <summary>
        /// Adds a new author to the database.
        /// </summary>
        /// <param name="authorDto">The details of the author to add.</param>
        /// <returns>The newly added author.</returns>
        [HttpPost("Add")]
        public async Task<ActionResult> AddAuthor(AuthorDto authorDto)
        {
            try
            {
                var author = new Author
                {
                    Name = authorDto.Name,
                    Bio = authorDto.Bio,
                };

                _context.Authors.Add(author);
                await _context.SaveChangesAsync();

                authorDto.AuthorId = author.AuthorId;
                return CreatedAtAction(nameof(FindAuthor), new { id = author.AuthorId }, new { message = "Author added successfully.", data = authorDto });
            }
            catch
            {
                return StatusCode(500, "An error occurred while adding the author.");
            }
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
            if (id != authorDto.AuthorId) return BadRequest(new { message = "Author ID mismatch." });

            try
            {
                var author = await _context.Authors.FindAsync(id);
                if (author == null) return NotFound(new { message = "Author not found." });

                author.Name = authorDto.Name;
                author.Bio = authorDto.Bio;

                _context.Entry(author).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Author updated successfully." });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(id)) return NotFound(new { message = "Author not found." });
                throw;
            }
            catch
            {
                return StatusCode(500, "An error occurred while updating the author.");
            }
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
            try
            {
                var author = await _context.Authors.FindAsync(id);
                if (author == null) return NotFound(new { message = "Author not found." });

                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Author deleted successfully." });
            }
            catch
            {
                return StatusCode(500, "An error occurred while deleting the author.");
            }
        }

        // GET: api/Authors/GetBooks/{id}
        /// <summary>
        /// Retrieves all books associated with a specific author.
        /// </summary>
        /// <param name="id">The ID of the author whose books are to be retrieved.</param>
        /// <returns>200 OK with a list of books, or 404 Not Found if the author or books are not found.</returns>
        [HttpGet("GetBooks/{id}")]
        public async Task<ActionResult> GetBooksForAuthor(int id)
        {
            try
            {
                var author = await _context.Authors
                    .Include(a => a.Books)
                    .FirstOrDefaultAsync(a => a.AuthorId == id);

                if (author == null) return NotFound(new { message = "Author not found." });

                if (author.Books == null || !author.Books.Any()) return NotFound(new { message = "No books found for this author." });

                var booksDto = author.Books.Select(book => new GetBooksDto
                {
                    Title = book.Title,
                    Year = book.Year
                }).ToList();

                return Ok(new { message = "Books retrieved successfully.", data = booksDto });
            }
            catch
            {
                return StatusCode(500, "An error occurred while retrieving books for the author.");
            }
        }

        // Helper method to check if an author exists
        private bool AuthorExists(int id)
        {
            return _context.Authors.Any(a => a.AuthorId == id);
        }
    }
}
