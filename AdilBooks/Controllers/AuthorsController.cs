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
        /// <returns>200 OK with a list of authors, or an error message if something goes wrong.</returns>
        /// <example>
        /// GET: api/Authors/List
        /// Success Example:
        /// {
        ///     "message": "Authors retrieved successfully.",
        ///     "data": [
        ///         { "authorId": 1, "name": "Author One" },
        ///         { "authorId": 2, "name": "Author Two" }
        ///     ]
        /// }
        /// 
        /// Error Example:
        /// {
        ///     "error": "InternalServerError",
        ///     "message": "An error occurred while retrieving authors."
        /// }
        /// </example>
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
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while retrieving authors." });
            }
        }

        // GET: api/Authors/Find/{id}
        /// <summary>
        /// Retrieves details of a specific author by their ID.
        /// </summary>
        /// <param name="AuthorId">The ID of the author to retrieve.</param>
        /// <returns>200 OK with the author's details, or 404 Not Found if the author is not found.</returns>
        /// <example>
        /// GET: api/Authors/Find/1
        /// Success Example:
        /// {
        ///     "message": "Author retrieved successfully.",
        ///     "data": {
        ///         "authorId": 1,
        ///         "name": "Author One",
        ///         "bio": "Author biography",
        ///         "titles": "Book One, Book Two"
        ///     }
        /// }
        /// 
        /// Error Example:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Author not found."
        /// }
        /// </example>
        [HttpGet("Find/{AuthorId}")]
        public async Task<ActionResult> FindAuthor(int AuthorId)
        {
            try
            {
                var author = await _context.Authors
                    .Include(a => a.Books)
                    .FirstOrDefaultAsync(a => a.AuthorId == AuthorId);

                if (author == null) return NotFound(new { error = "NotFound", message = "Author not found." });

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
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while retrieving the author." });
            }
        }

        // POST: api/Authors/Add
        /// <summary>
        /// Adds a new author to the database.
        /// </summary>
        /// <param name="authorDto">The details of the author to add.</param>
        /// <returns>201 Created with the added author's details, or an error message if something goes wrong.</returns>
        /// <example>
        /// POST: api/Authors/Add
        /// Input:
        /// {
        ///     "name": "New Author",
        ///     "bio": "Bio of the new author"
        /// }
        ///
        /// Success Example:
        /// {
        ///     "message": "Author added successfully.",
        ///     "data": {
        ///         "authorId": 5,
        ///         "name": "New Author",
        ///         "bio": "Bio of the new author",
        ///         "titles": ""
        ///     }
        /// }
        /// 
        /// Error Example:
        /// {
        ///     "error": "InternalServerError",
        ///     "message": "An error occurred while adding the author."
        /// }
        /// </example>
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
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while adding the author." });
            }
        }

        // PUT: api/Authors/Update/{id}
        /// <summary>
        /// Updates an existing author's details.
        /// </summary>
        /// <param name="AuthorId">The ID of the author to update.</param>
        /// <param name="authorDto">The updated details of the author.</param>
        /// <returns>200 OK if the update is successful, or an error message if something goes wrong.</returns>
        /// <example>
        /// PUT: api/Authors/Update/1
        /// Input:
        /// {
        ///     "authorId": 1,
        ///     "name": "Updated Author",
        ///     "bio": "Updated biography"
        /// }
        ///
        /// Success Example:
        /// {
        ///     "message": "Author updated successfully."
        /// }
        ///
        /// Error Example:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Author not found."
        /// }
        /// </example>
        [HttpPut("Update/{AuthorId}")]
        public async Task<IActionResult> UpdateAuthor(int AuthorId, AuthorDto authorDto)
        {
            if (AuthorId != authorDto.AuthorId) return BadRequest(new { error = "BadRequest", message = "Author ID mismatch." });

            try
            {
                var author = await _context.Authors.FindAsync(AuthorId);
                if (author == null) return NotFound(new { error = "NotFound", message = "Author not found." });

                author.Name = authorDto.Name;
                author.Bio = authorDto.Bio;

                _context.Entry(author).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Author updated successfully." });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(AuthorId)) return NotFound(new { error = "NotFound", message = "Author not found." });
                throw;
            }
            catch
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while updating the author." });
            }
        }

        // DELETE: api/Authors/Delete/{id}
        /// <summary>
        /// Deletes a specific author by their ID.
        /// </summary>
        /// <param name="AuthorId">The ID of the author to delete.</param>
        /// <returns>200 OK if the deletion is successful, or an error message if something goes wrong.</returns>
        /// <example>
        /// DELETE: api/Authors/Delete/1
        ///
        /// Success Example:
        /// {
        ///     "message": "Author deleted successfully."
        /// }
        ///
        /// Error Example:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Author not found."
        /// }
        /// </example>
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteAuthor(int id)
        {
            try
            {
                var author = await _context.Authors.FindAsync(id);
                if (author == null) return NotFound(new { error = "NotFound", message = "Author not found." });

                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Author deleted successfully." });
            }
            catch
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while deleting the author." });
            }
        }

        // GET: api/Authors/GetBooks/{id}
        /// <summary>
        /// Retrieves all books associated with a specific author.
        /// </summary>
        /// <param name="AuthorId">The ID of the author whose books are to be retrieved.</param>
        /// <returns>200 OK with a list of books, or an error message if something goes wrong.</returns>
        /// <example>
        /// GET: api/Authors/GetBooks/1
        ///
        /// Success Example:
        /// {
        ///     "message": "Books retrieved successfully.",
        ///     "data": [
        ///         { "title": "Book One", "year": 2020 },
        ///         { "title": "Book Two", "year": 2021 }
        ///     ]
        /// }
        ///
        /// Error Example:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Author not found."
        /// }
        /// </example>
        [HttpGet("ListBooksByAuthor/{AuthorId}")]
        public async Task<ActionResult> GetBooksForAuthor(int AuthorId)
        {
            try
            {
                var author = await _context.Authors
                    .Include(a => a.Books)
                    .FirstOrDefaultAsync(a => a.AuthorId == AuthorId);

                if (author == null)
                {
                    return NotFound(new { error = "NotFound", message = "Author not found." });
                }

                if (author.Books == null || !author.Books.Any())
                {
                    return NotFound(new { error = "NotFound", message = "No books found for this author." });
                }

                var booksDto = author.Books.Select(book => new GetBooksDto
                {
                    Title = book.Title,
                    Year = book.Year
                }).ToList();

                return Ok(new { message = "Books retrieved successfully.", data = booksDto });
            }
            catch
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while retrieving books for the author." });
            }
        }

        // Helper method to check if an author exists
        private bool AuthorExists(int AuthorId)
        {
            return _context.Authors.Any(a => a.AuthorId == AuthorId);
        }
    }
}
