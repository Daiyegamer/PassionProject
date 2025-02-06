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
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Books
        [HttpGet(template: "List")]
        public async Task<ActionResult<IEnumerable<BookListDto>>> ListBooks()
        {
            try
            {
                var books = await _context.Books.ToListAsync();
                if (!books.Any())
                {
                    return NotFound(new { message = "No books found in the database." });
                }

                var bookListDtos = books.Select(book => new BookListDto
                {
                    BookId = book.BookId,
                    Title = book.Title,
                    Year = book.Year
                }).ToList();

                return Ok(new { message = "Books retrieved successfully.", data = bookListDtos });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while retrieving the book list." });
            }
        }

        // GET: api/Books/5
        [HttpGet(template: "Find/{id}")]
        public async Task<ActionResult<BookDto>> FindBook(int id)
        {
            try
            {
                var book = await _context.Books
                    .Include(b => b.Authors)
                    .Include(b => b.Publisher)
                    .FirstOrDefaultAsync(b => b.BookId == id);

                if (book == null)
                {
                    return NotFound(new { error = "NotFound", message = $"Book with ID {id} not found." });
                }

                var bookDto = new BookDto
                {
                    BookId = book.BookId,
                    Title = book.Title,
                    Year = book.Year,
                    Synopsis = book.Synopsis,
                    AuthorNames = book.Authors.Select(a => a.Name).ToList(),
                    PublisherName = book.Publisher.PublisherName
                };

                return Ok(new { message = "Book retrieved successfully.", data = bookDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while retrieving the book details." });
            }
        }

        // PUT: api/Books/5
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateBook(int id, UpdateBookDto updateBookDto)
        {
            if (id != updateBookDto.BookId)
            {
                return BadRequest(new { error = "InvalidRequest", message = "The provided ID does not match the book ID." });
            }

            try
            {
                var book = await _context.Books.FirstOrDefaultAsync(b => b.BookId == id);
                if (book == null)
                {
                    return NotFound(new { error = "NotFound", message = $"Book with ID {id} not found." });
                }

                book.Title = updateBookDto.Title;
                book.Year = updateBookDto.Year;
                book.Synopsis = updateBookDto.Synopsis;

                _context.Entry(book).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Book updated successfully." });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { error = "ConcurrencyError", message = "The book was updated by another user. Please refresh and try again." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while updating the book." });
            }
        }

        // POST: api/Books
        [HttpPost("Add")]
        public async Task<ActionResult<AddBookDto>> AddBook(AddBookDto addBookDto)
        {
            try
            {
                var publisher = await _context.Publishers.FindAsync(addBookDto.PublisherId);
                if (publisher == null)
                {
                    return NotFound(new { error = "NotFound", message = "Publisher not found." });
                }

                var authors = await _context.Authors
                    .Where(a => addBookDto.AuthorIds.Contains(a.AuthorId))
                    .ToListAsync();

                if (authors.Count != addBookDto.AuthorIds.Count)
                {
                    return NotFound(new { error = "NotFound", message = "One or more authors were not found." });
                }

                var book = new Book
                {
                    Title = addBookDto.Title,
                    Year = addBookDto.Year,
                    Synopsis = addBookDto.Synopsis,
                    PublisherId = addBookDto.PublisherId,
                    Authors = authors
                };

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                addBookDto.BookId = book.BookId;
                addBookDto.PublisherName = publisher.PublisherName;
                addBookDto.AuthorNames = authors.Select(a => a.Name).ToList();

                return CreatedAtAction(nameof(FindBook), new { id = book.BookId }, new { message = "Book added successfully.", data = addBookDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while adding the book." });
            }
        }

        // DELETE: api/Books/5
        [HttpDelete(template: "Delete/{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                var book = await _context.Books.FindAsync(id);
                if (book == null)
                {
                    return NotFound(new { error = "NotFound", message = $"Book with ID {id} not found." });
                }

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Book deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while deleting the book." });
            }
        }
        [HttpPost("LinkAuthorToBook/{AuthorId}/{BookId}")]
        /// <summary>
        /// Links an author to a specific book using route parameters.
        /// </summary>
        /// <param name="AuthorId">The ID of the author to link.</param>
        /// <param name="BookId">The ID of the book to link to.</param>
        /// <returns>200 OK if linked successfully, or 404 Not Found if either the author or book is missing.</returns>
        /// <example>
        /// api/Authors/LinkAuthorToBook/{authorId}/{bookId} -> Links the specified author to a book.
        /// </example>
        public async Task<IActionResult> LinkAuthorToBook(int AuthorId, int BookId)
        {
            try
            {
                // Retrieve the book and author
                var Book = await _context.Books
                    .Include(b => b.Authors)  // Include the authors to modify the relationship
                    .FirstOrDefaultAsync(b => b.BookId == BookId);

                var Author = await _context.Authors.FindAsync(AuthorId);

                // Check if the book or author exists
                if (Book == null)
                {
                    return NotFound(new { error = "NotFound", message = "Book not found." });
                }

                if (Author == null)
                {
                    return NotFound(new { error = "NotFound", message = "Author not found." });
                }

                // Check if the author is already linked to the book
                if (Book.Authors.Any(a => a.AuthorId == AuthorId))
                {
                    return BadRequest(new { error = "InvalidOperation", message = "Author is already linked to this book." });
                }

                // Link the author to the book
                Book.Authors.Add(Author);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Author linked to book successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "InternalServerError", message = ex.Message });
            }
        }

        [HttpDelete("UnlinkAuthor/{AuthorId}/{BookId}")]
        /// <summary>
        /// Unlinks an author from a specific book using route parameters.
        /// </summary>
        /// <param name="AuthorId">The ID of the author to unlink.</param>
        /// <param name="BookId">The ID of the book to unlink from.</param>
        /// <returns>200 OK if unlinked successfully, or 404 Not Found if either the author or book is missing.</returns>
        /// <example>
        /// api/Authors/UnlinkAuthor/{AuthorId}/{BookId} -> Unlinks the specified author from a book.
        /// </example>
        public async Task<IActionResult> UnlinkAuthor(int AuthorId, int BookId)
        {
            try
            {
                // Retrieve the book and author
                var Book = await _context.Books
                    .Include(b => b.Authors)  // Include the authors to modify the relationship
                    .FirstOrDefaultAsync(b => b.BookId == BookId);

                var Author = await _context.Authors.FindAsync(AuthorId);

                // Check if the book or author exists
                if (Book == null)
                {
                    return NotFound(new { error = "NotFound", message = "Book not found." });
                }

                if (Author == null)
                {
                    return NotFound(new { error = "NotFound", message = "Author not found." });
                }

                // Check if the author is linked to the book
                var existingAuthorLink = Book.Authors.FirstOrDefault(a => a.AuthorId == AuthorId);

                if (existingAuthorLink == null)
                {
                    return BadRequest(new { error = "InvalidOperation", message = "Author is not linked to this book." });
                }

                // Remove the author from the book's Authors collection
                Book.Authors.Remove(Author);

                // Save the changes to the database
                await _context.SaveChangesAsync();

                return Ok(new { message = "Author unlinked from book successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "InternalServerError", message = ex.Message });
            }
        }

    }
}
