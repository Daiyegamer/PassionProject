using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdilBooks.Data;
using AdilBooks.Models;
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
        /// <returns>A list of publishers.</returns>
        [HttpGet("List")]
        public async Task<ActionResult<IEnumerable<PublisherDto>>> ListPublishers()
        {
            var publishers = await _context.Publishers
                .Include(p => p.Books)  // Ensure books are loaded
                .ToListAsync();

            var publisherDtos = publishers.Select(publisher => new PublisherDto
            {
                PublisherId = publisher.PublisherId,
                PublisherName = publisher.PublisherName,
                // Project books into a list of book titles
                Books = publisher.Books.Select(book => book.Title).ToList()
            }).ToList();

            return Ok(publisherDtos);
        }
        // GET: api/Publishers/Find/{id}
        /// <summary>
        /// Retrieves the details of a specific publisher, including their associated books, by the publisher's ID.
        /// </summary>
        /// <param name="id">The ID of the publisher to retrieve.</param>
        /// <returns>Details of the specified publisher, including their name and a list of book titles, or a 404 Not Found if the publisher is not found.</returns>
        /// <example>
        /// GET api/Publishers/Find/1 -> Retrieves the publisher with ID 1 and their associated books.
        /// </example>

        [HttpGet("Find/{id}")]
        public async Task<ActionResult<PublisherDto>> FindPublisher(int id)
        {
            // Retrieve the publisher with the specified id, including the related books.
            var publisher = await _context.Publishers
                .Include(p => p.Books)  // Eagerly load the related Books to access them later
                .FirstOrDefaultAsync(p => p.PublisherId == id);  // Find the publisher by its PublisherId

            // If the publisher is not found, return a 404 Not Found response with a message.
            if (publisher == null)
            {
                return NotFound("Publisher not found.");
            }

            // Create a PublisherDto object to shape the data to return in the response.
            var publisherDto = new PublisherDto
            {
                PublisherId = publisher.PublisherId,  // Assign the PublisherId from the database
                PublisherName = publisher.PublisherName,  // Assign the PublisherName from the database
                                                          // Project the related books into a list of book titles.
                                                          // Here we only select the Title of each book, but you can modify this to include more book properties if needed.
                Books = publisher.Books.Select(book => book.Title).ToList()  // Converts books to a list of titles
            };

            // Return a 200 OK response with the PublisherDto, which includes the publisher's information and its book titles.
            return Ok(publisherDto);
        }


        // POST: api/Publishers/Add
        /// <summary>
        /// Adds a new publisher to the database.
        /// </summary>
        /// <param name="publisherDto">The details of the publisher to add.</param>
        /// <returns>The newly added publisher.</returns>
        [HttpPost("Add")]
        public async Task<ActionResult<PublisherDto>> AddPublisher(PublisherDto publisherDto)
        {
            var publisher = new Publisher
            {
                PublisherName = publisherDto.PublisherName
            };

            _context.Publishers.Add(publisher);
            await _context.SaveChangesAsync();

            publisherDto.PublisherId = publisher.PublisherId;
            return CreatedAtAction(nameof(FindPublisher), new { id = publisher.PublisherId }, publisherDto);
        }

        // PUT: api/Publishers/Update/{id}
        /// <summary>
        /// Updates an existing publisher's details.
        /// </summary>
        /// <param name="id">The ID of the publisher to update.</param>
        /// <param name="publisherDto">The updated details of the publisher.</param>
        /// <returns>No content on successful update.</returns>
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdatePublisher(int id, PublisherDto publisherDto)
        {
            if (id != publisherDto.PublisherId)
            {
                return BadRequest("Publisher ID mismatch.");
            }

            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
            {
                return NotFound("Publisher not found.");
            }

            publisher.PublisherName = publisherDto.PublisherName;

            _context.Entry(publisher).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PublisherExists(id))
                {
                    return NotFound("Publisher not found.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Publishers/Delete/{id}
        /// <summary>
        /// Deletes a specific publisher by their ID.
        /// </summary>
        /// <param name="id">The ID of the publisher to delete.</param>
        /// <returns>No content on successful deletion.</returns>
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeletePublisher(int id)
        {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
            {
                return NotFound("Publisher not found.");
            }

            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Publishers/GetBooks/{id}
        /// <summary>
        /// Retrieves all books associated with a specific publisher.
        /// </summary>
        /// <param name="id">The ID of the publisher whose books are to be retrieved.</param>
        /// <returns>200 OK with a list of books, or 404 Not Found if the publisher or books are not found.</returns>
        [HttpGet("GetBooks/{id}")]
        public async Task<ActionResult<IEnumerable<GetBooksDto>>> GetBooksByPublisher(int id)
        {
            var publisher = await _context.Publishers
                .Include(p => p.Books)
                .FirstOrDefaultAsync(p => p.PublisherId == id);

            if (publisher == null)
            {
                return NotFound("Publisher not found.");
            }

            if (publisher.Books == null || !publisher.Books.Any())
            {
                return NotFound("No books found for this publisher.");
            }

            var booksDto = publisher.Books.Select(book => new GetBooksDto
            {
                Title = book.Title,
                Year = book.Year
            }).ToList();

            return Ok(booksDto);
        }

        // Helper method to check if a publisher exists
        private bool PublisherExists(int id)
        {
            return _context.Publishers.Any(p => p.PublisherId == id);
        }
    }
}
