using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdilBooks.Data;
using AdilBooks.Models;
using System.Net;

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
        [HttpGet(template: "Find/{BookId}")]
        /// <summary>
        /// Retrieves details of a specific book by ID.
        /// </summary>
        /// <param name="BookId">The ID of the book to retrieve.</param>
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
        public async Task<ActionResult<BookDto>> FindBook(int BookId)
        {
            try
            {
                var book = await _context.Books
                    .Include(b => b.Authors)
                    .Include(b => b.Publisher)
                    .FirstOrDefaultAsync(b => b.BookId == BookId);

                if (book == null)
                {
                    return NotFound(new { error = "NotFound", message = $"Book with ID {BookId} not found." });
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
        [HttpPut("Update/{BookId}")]
        /// <summary>
        /// Updates an existing book.
        /// </summary>
        /// <param name="BookId">The ID of the book to update.</param>
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
        public async Task<IActionResult> UpdateBook(int BookId, UpdateBookDto updateBookDto)
        {
            if (updateBookDto == null)
            {
                return BadRequest(new { error = "InvalidRequest", message = "The book details cannot be null." });
            }

            if (BookId != updateBookDto.BookId)
            {
                return BadRequest(new { error = "InvalidRequest", message = "The provided ID does not match the book ID." });
            }

            try
            {
                var book = await _context.Books.FirstOrDefaultAsync(b => b.BookId == BookId);
                if (book == null)
                {
                    return NotFound(new { error = "NotFound", message = $"Book with ID {BookId} not found." });
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
        [HttpDelete(template: "Delete/{BookId}")]
        /// <summary>
        /// Deletes a book by ID.
        /// </summary>
        /// <param name="BookId">The ID of the book to delete.</param>
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
        public async Task<IActionResult> DeleteBook(int BookId)
        {
            try
            {
                var book = await _context.Books.FindAsync(BookId);
                if (book == null)
                {
                    return NotFound(new { error = "NotFound", message = $"Book with ID {BookId} not found." });
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
        [HttpGet("ListAuthorsByBook/{bookId}")]
        /// <summary>
        /// Lists all authors' names associated with a specific book.
        /// </summary>
        /// <param name="bookId">The ID of the book to list authors for.</param>
        /// <returns>200 OK with a list of author names, or an error message if something goes wrong.</returns>
        /// <example>
        /// GET: api/Books/ListAuthorsByBook/1
        ///
        /// Success Example:
        /// {
        ///     "message": "Authors retrieved successfully.",
        ///     "data": ["Author One", "Author Two"]
        /// }
        ///
        /// Error Scenarios:
        ///
        /// - Book not found:
        /// GET: api/Books/ListAuthorsByBook/999
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "Book with ID 999 not found."
        /// }
        ///
        /// - No authors associated with the book:
        /// GET: api/Books/ListAuthorsByBook/5  // Book exists but has no authors linked
        /// Output:
        /// {
        ///     "error": "NotFound",
        ///     "message": "No authors found for this book."
        /// }
        ///
        /// - Internal server error (unexpected exception):
        /// Output:
        /// {
        ///     "error": "InternalServerError",
        ///     "message": "An error occurred while retrieving authors for the book."
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
        [HttpPost("AddMultiple")]
        /// <summary>
        /// Adds multiple books to the database in a single request.
        /// </summary>
        /// <param name="books">A list of books to be added.</param>
        /// <returns>201 Created with details of successfully added books or error messages for failed additions.</returns>
        /// <example>
        /// **Success Example:**  
        /// **POST:** api/Books/AddMultiple  
        /// **Input:**  
        /// ```json
        /// [
        ///     {
        ///         "title": "Book 1",
        ///         "year": 2023,
        ///         "synopsis": "The first book in the series.",
        ///         "publisherId": 1,
        ///         "authorIds": [1, 2]
        ///     },
        ///     {
        ///         "title": "Book 2",
        ///         "year": 2022,
        ///         "synopsis": "The second book in the series.",
        ///         "publisherId": 2,
        ///         "authorIds": [3]
        ///     }
        /// ]
        /// ```
        ///  
        /// **Output:**  
        /// ```json
        /// [
        ///     {
        ///         "message": "Book added successfully.",
        ///         "data": {
        ///             "bookId": 5,
        ///             "title": "Book 1",
        ///             "year": 2023,
        ///             "synopsis": "The first book in the series.",
        ///             "publisherName": "Publisher One",
        ///             "authorNames": ["Author One", "Author Two"]
        ///         }
        ///     },
        ///     {
        ///         "message": "Book added successfully.",
        ///         "data": {
        ///             "bookId": 6,
        ///             "title": "Book 2",
        ///             "year": 2022,
        ///             "synopsis": "The second book in the series.",
        ///             "publisherName": "Publisher Two",
        ///             "authorNames": ["Author Three"]
        ///         }
        ///     }
        /// ]
        /// ```
        ///  
        /// **Error Scenarios:**  
        ///  
        /// - **Publisher Not Found:**  
        /// **Input:**  
        /// ```json
        /// [
        ///     {
        ///         "title": "Book 3",
        ///         "year": 2021,
        ///         "synopsis": "Another book.",
        ///         "publisherId": 999,  // Invalid publisher ID
        ///         "authorIds": [1]
        ///     }
        /// ]
        /// ```  
        /// **Output:**  
        /// ```json
        /// {
        ///     "error": "NotFound",
        ///     "message": "Publisher with ID 999 not found."
        /// }
        /// ```  
        ///  
        /// - **One or More Authors Not Found:**  
        /// **Input:**  
        /// ```json
        /// [
        ///     {
        ///         "title": "Book 4",
        ///         "year": 2021,
        ///         "synopsis": "A new adventure.",
        ///         "publisherId": 1,
        ///         "authorIds": [1, 999]  // One invalid author ID
        ///     }
        /// ]
        /// ```  
        /// **Output:**  
        /// ```json
        /// {
        ///     "error": "NotFound",
        ///     "message": "One or more authors were not found."
        /// }
        /// ```  
        ///  
        /// - **Internal Server Error:**  
        /// **Output:**  
        /// ```json
        /// {
        ///     "error": "InternalServerError",
        ///     "message": "An error occurred while adding the book."
        /// }
        /// ```  
        /// </example>
        public async Task<ActionResult> AddMultipleBooks(List<AddBookDto> books)
        {
            var addedBooks = new List<object>();

            foreach (var bookDto in books)
            {
                try
                {
                    // Check if the publisher exists
                    var publisher = await _context.Publishers.FindAsync(bookDto.PublisherId);
                    if (publisher == null)
                    {
                        return NotFound(new { error = "NotFound", message = $"Publisher with ID {bookDto.PublisherId} not found." });
                    }

                    // Check if authors exist
                    var authors = await _context.Authors
                        .Where(a => bookDto.AuthorIds.Contains(a.AuthorId))
                        .ToListAsync();

                    if (authors.Count != bookDto.AuthorIds.Count)
                    {
                        return NotFound(new { error = "NotFound", message = "One or more authors were not found." });
                    }

                    // Create the book entity
                    var book = new Book
                    {
                        Title = bookDto.Title,
                        Year = bookDto.Year,
                        Synopsis = bookDto.Synopsis,
                        PublisherId = bookDto.PublisherId,
                        Authors = authors
                    };

                    // Add the book to the database
                    _context.Books.Add(book);
                    await _context.SaveChangesAsync();

                    // Populate the response data
                    bookDto.BookId = book.BookId;
                    bookDto.PublisherName = publisher.PublisherName;
                    bookDto.AuthorNames = authors.Select(a => a.Name).ToList();

                    addedBooks.Add(new { message = "Book added successfully.", data = bookDto });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "InternalServerError", message = $"Error adding book '{bookDto.Title}': {ex.Message}" });
                }
            }

            return Ok(addedBooks);
        }

    }



}





