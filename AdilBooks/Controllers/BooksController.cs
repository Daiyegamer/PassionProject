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
        /// <summary>
        /// Retrieves a list of books.
        /// </summary>
        /// <returns>200 OK with a list of books or an error message if something goes wrong.</returns>
        /// <example>
        /// GET: api/Books/List
        /// Output:
        /// {
        ///     "message": "Books retrieved successfully.",
        ///     "data": [
        ///         { "bookId": 1, "title": "Book One", "year": 2020 },
        ///         { "bookId": 2, "title": "Book Two", "year": 2021 }
        ///     ]
        /// }
        /// 
        /// Error Scenario:
        /// Output:
        /// {
        ///     "error": "InternalServerError",
        ///     "message": "An error occurred while retrieving the book list."
        /// }
        /// </example>
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
        /// <summary>
        /// Retrieves details of a specific book by ID.
        /// </summary>
        /// <param name="id">The ID of the book to retrieve.</param>
        /// <returns>200 OK with the book details or an error message if the book is not found.</returns>
        /// <example>
        /// GET: api/Books/Find/1
        /// Output:
        /// {
        ///     "message": "Book retrieved successfully.",
        ///     "data": {
        ///         "bookId": 1,
        ///         "title": "Book One",
        ///         "year": 2020,
        ///         "synopsis": "A thrilling adventure.",
        ///         "publisherName": "Publisher A",
        ///         "authorNames": ["Author One", "Author Two"]
        ///     }
        /// }
        /// 
        /// Error Scenario:
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Book with ID 1 not found."
        /// }
        /// </example>
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
        /// <summary>
        /// Updates an existing book.
        /// </summary>
        /// <param name="id">The ID of the book to update.</param>
        /// <param name="updateBookDto">The updated book details.</param>
        /// <returns>200 OK if the book is updated successfully, or an error message if something goes wrong.</returns>
        /// <example>
        /// PUT: api/Books/Update/1
        /// Input:
        /// {
        ///     "bookId": 1,
        ///     "title": "Updated Book",
        ///     "year": 2023,
        ///     "synopsis": "An updated story."
        /// }
        /// 
        /// Output:
        /// {
        ///     "message": "Book updated successfully."
        /// }
        /// 
        /// Error Scenarios:
        /// - Book not found:
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Book with ID 1 not found."
        /// }
        /// 
        /// - Concurrency error:
        /// Output:
        /// {
        ///     "error": "ConcurrencyError",
        ///     "message": "The book was updated by another user. Please refresh and try again."
        /// }
        /// </example>
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
        /// <summary>
        /// Adds a new book.
        /// </summary>
        /// <param name="addBookDto">The book details to add.</param>
        /// <returns>201 Created if the book is added successfully, or an error message if something goes wrong.</returns>
        /// <example>
        /// POST: api/Books/Add
        /// Input:
        /// {
        ///     "title": "New Book",
        ///     "year": 2023,
        ///     "synopsis": "A new adventure begins.",
        ///     "publisherId": 1,
        ///     "authorIds": [1, 2]
        /// }
        /// 
        /// Output:
        /// {
        ///     "message": "Book added successfully.",
        ///     "data": {
        ///         "bookId": 5,
        ///         "title": "New Book",
        ///         "year": 2023,
        ///         "synopsis": "A new adventure begins.",
        ///         "publisherName": "Test Publisher",
        ///         "authorNames": ["Author One", "Author Two"]
        ///     }
        /// }
        /// 
        /// Error Scenarios:
        /// - One or more authors not found:
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "One or more authors were not found."
        /// }
        /// 
        /// - Internal server error:
        /// Output:
        /// {
        ///     "error": "InternalServerError",
        ///     "message": "An error occurred while adding the book."
        /// }
        /// </example>
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
        /// <summary>
        /// Deletes a book by ID.
        /// </summary>
        /// <param name="id">The ID of the book to delete.</param>
        /// <returns>200 OK if the book is deleted successfully, or an error message if the book is not found.</returns>
        /// <example>
        /// DELETE: api/Books/Delete/1
        /// Output:
        /// {
        ///     "message": "Book deleted successfully."
        /// }
        /// 
        /// Error Scenario:
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Book with ID 1 not found."
        /// }
        /// </example>
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
        /// Links an author to a specific book.
        /// </summary>
        /// <param name="AuthorId">The ID of the author to link.</param>
        /// <param name="BookId">The ID of the book to link to.</param>
        /// <returns>200 OK if linked successfully, or an error message if something goes wrong.</returns>
        /// <example>
        /// POST: api/Books/LinkAuthorToBook/1/5
        /// Output:
        /// {
        ///     "message": "Author linked to book successfully."
        /// }
        /// 
        /// Error Scenarios:
        /// - Book not found:
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Book not found."
        /// }
        /// 
        /// - Author not found:
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Author not found."
        /// }
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
        /// <returns>200 OK if unlinked successfully, or an error message if something goes wrong.</returns>
        /// <example>
        /// DELETE: api/Books/UnlinkAuthor/1/5
        /// Output:
        /// {
        ///     "message": "Author unlinked from book successfully."
        /// }
        /// 
        /// Error Scenarios:
        /// - Book not found:
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Book not found."
        /// }
        /// 
        /// - Author not found:
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Author not found."
        /// }
        /// 
        /// - Author not linked to the book:
        /// Output:
        /// {
        ///     "error": "InvalidOperation",
        ///     "message": "Author is not linked to this book."
        /// }
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
        [HttpGet("GetAuthors/{bookId}")]
        /// POST: api/Books/Add
        /// <summary>
        /// Adds a new book.
        /// </summary>
        /// <param name="addBookDto">The book details to add.</param>
        /// <returns>201 Created if the book is added successfully, or an error message if something goes wrong.</returns>
        /// <example>
        /// Success Example:
        /// POST: api/Books/Add
        /// Input:
        /// {
        ///     "title": "New Book",
        ///     "year": 2023,
        ///     "synopsis": "A new adventure begins.",
        ///     "publisherId": 1,
        ///     "authorIds": [1, 2]
        /// }
        ///
        /// Output:
        /// {
        ///     "message": "Book added successfully.",
        ///     "data": {
        ///         "bookId": 5,
        ///         "title": "New Book",
        ///         "year": 2023,
        ///         "synopsis": "A new adventure begins.",
        ///         "publisherName": "Test Publisher",
        ///         "authorNames": ["Author One", "Author Two"]
        ///     }
        /// }
        ///
        /// Error Scenarios:
        /// 
        /// - One or more authors not found:
        /// Input:
        /// {
        ///     "title": "New Book",
        ///     "year": 2023,
        ///     "synopsis": "A new adventure begins.",
        ///     "publisherId": 1,
        ///     "authorIds": [1, 999]  // One author does not exist
        /// }
        ///
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "One or more authors were not found."
        /// }
        ///
        /// - Internal server error (unexpected exception):
        /// Output:
        /// {
        ///     "error": "InternalServerError",
        ///     "message": "An error occurred while adding the book."
        /// }
        /// </example>
        public async Task<ActionResult> ListAuthorsByBook(int bookId)
        {
            try
            {
                // Retrieve the book with its associated authors
                var book = await _context.Books
                    .Include(b => b.Authors)
                    .FirstOrDefaultAsync(b => b.BookId == bookId);

                // Check if the book exists
                if (book == null)
                {
                    return NotFound(new { error = "NotFound", message = $"Book with ID {bookId} not found." });
                }

                // Check if any authors are associated with the book
                if (book.Authors == null || !book.Authors.Any())
                {
                    return NotFound(new { error = "NotFound", message = "No authors found for this book." });
                }

                // Return only the names of the authors
                var authorNames = book.Authors.Select(author => author.Name).ToList();

                return Ok(new { message = "Authors retrieved successfully.", data = authorNames });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "InternalServerError", message = "An error occurred while retrieving authors for the book." });
            }
        }


    }




}
