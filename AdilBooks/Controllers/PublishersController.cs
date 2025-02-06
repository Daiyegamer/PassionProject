﻿using Microsoft.AspNetCore.Mvc;
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
        /// <returns>
        /// 200 OK with a list of PublisherDto objects.
        /// [{PublisherDto}, {PublisherDto}, ...]
        /// </returns>
        /// <example>
        /// GET: api/Publishers/List -> [{PublisherDto}, {PublisherDto}, ...]
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
                return StatusCode(500, "An error occurred while retrieving the publishers.");
            }
        }

        /// <summary>
        /// Retrieves details of a specific publisher by ID.
        /// </summary>
        /// <param name="id">The ID of the publisher to retrieve.</param>
        /// <returns>
        /// 200 OK with a PublisherDto object, or 404 Not Found if the publisher is not found.
        /// {PublisherDto}
        /// </returns>
        /// <example>
        /// GET: api/Publishers/Find/1 -> {PublisherDto}
        /// </example>
        [HttpGet("Find/{id}")]
        public async Task<ActionResult> FindPublisher(int id)
        {
            try
            {
                var publisher = await _context.Publishers
                    .Include(p => p.Books)
                    .FirstOrDefaultAsync(p => p.PublisherId == id);

                if (publisher == null) return NotFound(new { message = "Publisher not found." });

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
                return StatusCode(500, "An error occurred while retrieving the publisher.");
            }
        }

        /// <summary>
        /// Adds a new publisher to the database.
        /// </summary>
        /// <param name="publisherDto">The details of the publisher to add.</param>
        /// <returns>
        /// 201 Created with the created PublisherDto object.
        /// </returns>
        /// <example>
        /// POST: api/Publishers/Add -> {PublisherDto}
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
                return StatusCode(500, "An error occurred while adding the publisher.");
            }
        }

        /// <summary>
        /// Updates an existing publisher's details.
        /// </summary>
        /// <param name="id">The ID of the publisher to update.</param>
        /// <param name="publisherDto">The updated details of the publisher.</param>
        /// <returns>
        /// 200 OK with a success message, or 404 Not Found if the publisher does not exist.
        /// </returns>
        /// <example>
        /// PUT: api/Publishers/Update/1 -> 200 OK
        /// </example>
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdatePublisher(int id, PublisherDto publisherDto)
        {
            if (id != publisherDto.PublisherId) return BadRequest(new { message = "Publisher ID mismatch." });

            try
            {
                var publisher = await _context.Publishers.FindAsync(id);
                if (publisher == null) return NotFound(new { message = "Publisher not found." });

                publisher.PublisherName = publisherDto.PublisherName;

                _context.Entry(publisher).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Publisher updated successfully." });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PublisherExists(id)) return NotFound(new { message = "Publisher not found." });
                throw;
            }
            catch
            {
                return StatusCode(500, "An error occurred while updating the publisher.");
            }
        }

        /// <summary>
        /// Deletes a specific publisher by their ID.
        /// </summary>
        /// <param name="id">The ID of the publisher to delete.</param>
        /// <returns>
        /// 200 OK with a success message, or 404 Not Found if the publisher does not exist.
        /// </returns>
        /// <example>
        /// DELETE: api/Publishers/Delete/1 -> 200 OK
        /// </example>
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeletePublisher(int id)
        {
            try
            {
                var publisher = await _context.Publishers.FindAsync(id);
                if (publisher == null) return NotFound(new { message = "Publisher not found." });

                _context.Publishers.Remove(publisher);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Publisher deleted successfully." });
            }
            catch
            {
                return StatusCode(500, "An error occurred while deleting the publisher.");
            }
        }

        /// <summary>
        /// Retrieves all books associated with a specific publisher.
        /// </summary>
        /// <param name="id">The ID of the publisher whose books are to be retrieved.</param>
        /// <returns>
        /// 200 OK with a list of GetBooksDto objects, or 404 Not Found if the publisher or books are not found.
        /// [{GetBooksDto}, {GetBooksDto}, ...]
        /// </returns>
        /// <example>
        /// GET: api/Publishers/GetBooks/1 -> [{GetBooksDto}, {GetBooksDto}, ...]
        /// </example>
        [HttpGet("GetBooks/{id}")]
        public async Task<ActionResult> GetBooksByPublisher(int id)
        {
            try
            {
                var publisher = await _context.Publishers
                    .Include(p => p.Books)
                    .FirstOrDefaultAsync(p => p.PublisherId == id);

                if (publisher == null) return NotFound(new { message = "Publisher not found." });

                if (publisher.Books == null || !publisher.Books.Any()) return NotFound(new { message = "No books found for this publisher." });

                var booksDto = publisher.Books.Select(book => new GetBooksDto
                {
                    Title = book.Title,
                    Year = book.Year
                }).ToList();

                return Ok(new { message = "Books retrieved successfully.", data = booksDto });
            }
            catch
            {
                return StatusCode(500, "An error occurred while retrieving books for the publisher.");
            }
        }

        /// <summary>
        /// Checks if a publisher exists by their ID.
        /// </summary>
        /// <param name="id">The ID of the publisher to check.</param>
        /// <returns>True if the publisher exists, otherwise false.</returns>
        private bool PublisherExists(int id)
        {
            return _context.Publishers.Any(p => p.PublisherId == id);
        }
    }
}
