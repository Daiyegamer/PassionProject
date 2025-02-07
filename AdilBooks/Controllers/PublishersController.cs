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
    public class PublishersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PublishersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of all publishers.
        /// </summary>
        /// <returns>200 OK with a list of PublisherDto objects, or an error message if something goes wrong.</returns>
        /// <example>
        /// GET: api/Publishers/List
        /// Success Example:
        /// {
        ///     "message": "Publishers retrieved successfully.",
        ///     "data": [
        ///         { "publisherId": 1, "publisherName": "Publisher A", "books": ["Book One", "Book Two"] },
        ///         { "publisherId": 2, "publisherName": "Publisher B", "books": ["Book Three"] }
        ///     ]
        /// }
        ///
        /// Error Example:
        /// {
        ///     "error": "InternalServerError",
        ///     "message": "An error occurred while retrieving the publishers."
        /// }
        /// </example>
        [HttpGet("List")]
        public async Task<ActionResult> ListPublishers()
        {
            try
            {
                var publishers = await _context.Publishers
                    .Include(p => p.Books)
                    .ToListAsync();

                var publisherDtos = publishers.Select(publisher => new PublisherDto
                {
                    PublisherId = publisher.PublisherId,
                    PublisherName = publisher.PublisherName,
                    Books = publisher.Books.Select(book => book.Title).ToList()
                }).ToList();

                return Ok(new { message = "Publishers retrieved successfully.", data = publisherDtos });
            }
            catch
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while retrieving the publishers." });
            }
        }

        /// <summary>
        /// Retrieves details of a specific publisher by ID.
        /// </summary>
        /// <param name="PublisherId">The ID of the publisher to retrieve.</param>
        /// <returns>200 OK with a PublisherDto object, or 404 Not Found if the publisher is not found.</returns>
        /// <example>
        /// GET: api/Publishers/Find/1
        /// Success Example:
        /// {
        ///     "message": "Publisher retrieved successfully.",
        ///     "data": { "publisherId": 1, "publisherName": "Publisher A", "books": ["Book One", "Book Two"] }
        /// }
        ///
        /// Error Example:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Publisher not found."
        /// }
        /// </example>
        [HttpGet("Find/{PublisherId}")]
        public async Task<ActionResult> FindPublisher(int PublisherId)
        {
            try
            {
                var publisher = await _context.Publishers
                    .Include(p => p.Books)
                    .FirstOrDefaultAsync(p => p.PublisherId == PublisherId);

                if (publisher == null) return NotFound(new { error = "NotFound", message = "Publisher not found." });

                var publisherDto = new PublisherDto
                {
                    PublisherId = publisher.PublisherId,
                    PublisherName = publisher.PublisherName,
                    Books = publisher.Books.Select(book => book.Title).ToList()
                };

                return Ok(new { message = "Publisher retrieved successfully.", data = publisherDto });
            }
            catch
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while retrieving the publisher." });
            }
        }

        /// <summary>
        /// Adds a new publisher to the database.
        /// </summary>
        /// <param name="publisherDto">The details of the publisher to add.</param>
        /// <returns>201 Created with the created PublisherDto object, or an error message if something goes wrong.</returns>
        /// <example>
        /// POST: api/Publishers/Add
        /// Input:
        /// {
        ///     "publisherName": "New Publisher"
        /// }
        ///
        /// Success Example:
        /// {
        ///     "message": "Publisher added successfully.",
        ///     "data": { "publisherId": 5, "publisherName": "New Publisher", "books": [] }
        /// }
        ///
        /// Error Example:
        /// {
        ///     "error": "InternalServerError",
        ///     "message": "An error occurred while adding the publisher."
        /// }
        /// </example>
        [HttpPost("Add")]
        public async Task<ActionResult> AddPublisher(PublisherDto publisherDto)
        {
            try
            {
                var publisher = new Publisher
                {
                    PublisherName = publisherDto.PublisherName
                };

                _context.Publishers.Add(publisher);
                await _context.SaveChangesAsync();

                publisherDto.PublisherId = publisher.PublisherId;
                return CreatedAtAction(nameof(FindPublisher), new { id = publisher.PublisherId }, new { message = "Publisher added successfully.", data = publisherDto });
            }
            catch
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while adding the publisher." });
            }
        }

        /// <summary>
        /// Updates an existing publisher's details.
        /// </summary>
        /// <param name="PublisherId">The ID of the publisher to update.</param>
        /// <param name="publisherDto">The updated details of the publisher.</param>
        /// <returns>200 OK with a success message, or an error message if something goes wrong.</returns>
        /// <example>
        /// PUT: api/Publishers/Update/1
        /// Input:
        /// {
        ///     "publisherId": 1,
        ///     "publisherName": "Updated Publisher Name"
        /// }
        ///
        /// Success Example:
        /// {
        ///     "message": "Publisher updated successfully."
        /// }
        ///
        /// Error Example:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Publisher not found."
        /// }
        /// </example>
        [HttpPut("Update/{PublisherId}")]
        public async Task<IActionResult> UpdatePublisher(int PublisherId, PublisherDto publisherDto)
        {
            if (PublisherId != publisherDto.PublisherId) return BadRequest(new { message = "Publisher ID mismatch." });

            try
            {
                var publisher = await _context.Publishers.FindAsync(PublisherId);
                if (publisher == null) return NotFound(new { error = "NotFound", message = "Publisher not found." });

                publisher.PublisherName = publisherDto.PublisherName;

                _context.Entry(publisher).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Publisher updated successfully." });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PublisherExists(PublisherId)) return NotFound(new { error = "NotFound", message = "Publisher not found." });
                throw;
            }
            catch
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while updating the publisher." });
            }
        }

        /// <summary>
        /// Deletes a specific publisher by their ID.
        /// </summary>
        /// <param name=PublisherId">The ID of the publisher to delete.</param>
        /// <returns>200 OK with a success message, or an error message if something goes wrong.</returns>
        /// <example>
        /// DELETE: api/Publishers/Delete/1
        ///
        /// Success Example:
        /// {
        ///     "message": "Publisher deleted successfully."
        /// }
        ///
        /// Error Example:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Publisher not found."
        /// }
        /// </example>
        [HttpDelete("Delete/{PublisherId}")]
        public async Task<IActionResult> DeletePublisher(int PublisherId)
        {
            try
            {
                var publisher = await _context.Publishers.FindAsync(PublisherId);
                if (publisher == null) return NotFound(new { error = "NotFound", message = "Publisher not found." });

                _context.Publishers.Remove(publisher);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Publisher deleted successfully." });
            }
            catch
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while deleting the publisher." });
            }
        }

        /// <summary>
        /// Retrieves all books associated with a specific publisher.
        /// </summary>
        /// <param name="PublisherId">The ID of the publisher whose books are to be retrieved.</param>
        /// <returns>200 OK with a list of GetBooksDto objects, or an error message if something goes wrong.</returns>
        /// <example>
        /// GET: api/Publishers/GetBooks/1
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
        ///     "message": "Publisher not found."
        /// }
        /// </example>
        [HttpGet("ListBooksByPublisher/{PublisherId}")]
        public async Task<ActionResult> GetBooksByPublisher(int PublisherId)
        {
            try
            {
                var publisher = await _context.Publishers
                    .Include(p => p.Books)
                    .FirstOrDefaultAsync(p => p.PublisherId == PublisherId);

                if (publisher == null) return NotFound(new { error = "NotFound", message = "Publisher not found." });

                if (publisher.Books == null || !publisher.Books.Any()) return NotFound(new { error = "NotFound", message = "No books found for this publisher." });

                var booksDto = publisher.Books.Select(book => new GetBooksDto
                {
                    Title = book.Title,
                    Year = book.Year
                }).ToList();

                return Ok(new { message = "Books retrieved successfully.", data = booksDto });
            }
            catch
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while retrieving books for the publisher." });
            }
        }

        /// <summary>
        /// Checks if a publisher exists by their ID.
        /// </summary>
        /// <param name="PublisherId">The ID of the publisher to check.</param>
        /// <returns>True if the publisher exists, otherwise false.</returns>
        private bool PublisherExists(int PublisherId)
        {
            return _context.Publishers.Any(p => p.PublisherId == PublisherId);
        }
    }
}
