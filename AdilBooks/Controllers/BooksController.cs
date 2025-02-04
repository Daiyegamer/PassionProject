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
            List<Book> Books = await _context.Books
                 .ToListAsync();

            List<BookListDto> BookListDtos = new List<BookListDto>();

            foreach (Book Book in Books)
            {
                BookListDtos.Add(new BookListDto()
                {
                    BookId = Book.BookId,
                    Title = Book.Title,
                    Year = Book.Year





                });

            }
            // return 200 OK with BookDtos
            return Ok(BookListDtos);

        }
    
        // GET: api/Books/5
        [HttpGet(template: "Find/{id}")]

        public async Task<ActionResult<BookDto>> FindBook(int id)
        {
            var book = await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Publisher)
                .FirstOrDefaultAsync(b => b.BookId == id);


            if (book == null)
            {
                return NotFound();
            }
            BookDto BookDto = new BookDto()
            {
                BookId = book.BookId,
                Title = book.Title,
                Year = book.Year,
                Synopsis = book.Synopsis,
                AuthorNames = book.Authors.Select(Authors => Authors.Name).ToList(),
                PublisherName = book.Publisher.PublisherName


            };
            return Ok(BookDto);
        }

        // PUT: api/Books/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateBook(int id, UpdateBookDto updateBookDto)
        {
            if (id != updateBookDto.BookId)
            {
                return BadRequest();  // Return 400 if IDs don't match
            }

            // Fetch the book from the database
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.BookId == id);

            // If the book doesn't exist, return 404 Not Found
            if (book == null)
            {
                return NotFound();
            }

            // Update only the fields that are provided in the DTO
            book.Title = updateBookDto.Title;
            book.Year = updateBookDto.Year;
            book.Synopsis = updateBookDto.Synopsis;

            // Mark the entity as modified (EF will track changes)
            _context.Entry(book).State = EntityState.Modified;

            try
            {
                // Save changes to the database
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle concurrency issues if the book was modified by another user
                if (!BookExists(id))
                {
                    return NotFound();  // Return 404 if the book doesn't exist anymore
                }
                else
                {
                    throw;
                }
            }

            // Return NoContent (HTTP 204) to indicate success without returning data
            return NoContent();
        }

        // POST: api/Books
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        /// <summary>
        /// Adds a Book to the Books table.
        /// </summary>
        /// <remarks>
        /// We add a Book using AddBookDto as input, which contains the required fields,
        /// and return the same AddBookDto as output with additional details of the created book.
        /// </remarks>
        /// <param name="addBookDto">The input information to add the book.</param>
        /// <returns>
        /// 201 Created if the book is added successfully, or 404 Not Found if any related entities are missing.
        /// </returns>
        /// <example>
        /// api/Books/Add -> Adds a new Book
        /// </example>
        [HttpPost("Add")]
        public async Task<ActionResult<AddBookDto>> AddBook(AddBookDto addBookDto)
        {
            // Validate the PublisherId
            var publisher = await _context.Publishers.FindAsync(addBookDto.PublisherId);
            if (publisher == null)
            {
                return NotFound("Publisher not found.");
            }

            // Validate the AuthorIds
            var authors = await _context.Authors
                .Where(a => addBookDto.AuthorIds.Contains(a.AuthorId))
                .ToListAsync();

            if (authors.Count != addBookDto.AuthorIds.Count)
            {
                return NotFound("One or more authors were not found.");
            }

            // Create the Book entity
            Book book = new Book
            {
                Title = addBookDto.Title,
                Year = addBookDto.Year,
                Synopsis = addBookDto.Synopsis,
                PublisherId = addBookDto.PublisherId,
                Authors = authors
            };

            // Add the book to the database
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Prepare the output AddBookDto
            addBookDto.BookId = book.BookId;
            addBookDto.PublisherName = publisher.PublisherName;
            addBookDto.AuthorNames = authors.Select(a => a.Name).ToList();

            // Return the created book with a 201 status
            return CreatedAtAction(nameof(FindBook), new { id = book.BookId }, addBookDto);
        }
        // DELETE: api/Books/5
        [HttpDelete(template: "Delete/{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            // Find the book by ID
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            // Remove the book from the database
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            // Return 204 No Content
            return NoContent();
        }

        /// <summary>
        /// Checks if the Book exists in the database
        /// </summary>
        /// <param name="id">The ID of the Book to check</param>
        /// <returns>True if the Book exists, false otherwise</returns>
        private bool BookExists(int id)
        {
            return _context.Books.Any(b => b.BookId == id);
        }
        /// <summary>
        /// Retrieves all authors associated with a specific book.
        /// </summary>
        /// <remarks>
        /// This method returns a list of authors for the book identified by its ID.
        /// </remarks>
        /// <param name="id">The ID of the book whose authors are to be retrieved.</param>
        /// <returns>
        /// 200 OK with the list of authors, or 404 Not Found if the book or authors are not found.
        /// </returns>
        /// <example>
        /// api/Books/GetAuthors/{id} -> Retrieves all authors for the book with the given {id}.
        /// </example>
        [HttpGet("GetAuthors/{id}")]
        public async Task<ActionResult<IEnumerable<ListAuthorDto>>> GetAuthors(int id)
        {
            // Check if the book exists
            var book = await _context.Books
                .Include(b => b.Authors)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                return NotFound("Book not found.");
            }

            if (book.Authors == null || !book.Authors.Any())
            {
                return NotFound("No authors found for this book.");
            }

            // Map the authors to ListAuthorDto and return them
            var listAuthorDtos = book.Authors.Select(author => new ListAuthorDto
            {
                AuthorId = author.AuthorId,
                AuthorName = author.Name  // Ensure this matches the property in your model
            }).ToList();

            return Ok(listAuthorDtos);
        }

        [HttpPost("LinkAuthorToBook/{AuthorId}/{BookId}")]
        /// <summary>
        /// Links an author to a specific book using route parameters.
        /// </summary>
        /// <param name="authorId">The ID of the author to link.</param>
        /// <param name="bookId">The ID of the book to link to.</param>
        /// <returns>200 OK if linked successfully, or 404 Not Found if either the author or book is missing.</returns>
        /// <example>
        /// api/Authors/LinkAuthorToBook/{authorId}/{bookId} -> Links the specified author to a book.
        /// </example>
        public async Task<IActionResult> LinkAuthorToBook(int AuthorId, int BookId)
        {
            // Retrieve the book and author
            var Book = await _context.Books
                .Include(b => b.Authors)  // Include the authors to modify the relationship
                .FirstOrDefaultAsync(b => b.BookId == BookId);

            var Author = await _context.Authors.FindAsync(AuthorId);

            // Check if the book or author exists
            if (Book == null)
            {
                return NotFound("Book not found.");
            }

            if (Author == null)
            {
                return NotFound("Author not found.");
            }

            // Check if the author is already linked to the book
            if (Book.Authors.Any(a => a.AuthorId == AuthorId))
            {
                return BadRequest("Author is already linked to this book.");
            }

            // Link the author to the book
            Book.Authors.Add(Author);
            await _context.SaveChangesAsync();

            return Ok("Author linked to book successfully.");
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
            // Retrieve the book and author
            var Book = await _context.Books
                .Include(b => b.Authors)  // Include the authors to modify the relationship
                .FirstOrDefaultAsync(b => b.BookId == BookId);

            var Author = await _context.Authors.FindAsync(AuthorId);

            // Check if the book or author exists
            if (Book == null)
            {
                return NotFound("Book not found.");
            }

            if (Author == null)
            {
                return NotFound("Author not found.");
            }

            // Check if the author is linked to the book
            var existingAuthorLink = Book.Authors.FirstOrDefault(a => a.AuthorId == AuthorId);

            if (existingAuthorLink == null)
            {
                return BadRequest("Author is not linked to this book.");
            }

            // Remove the author from the book's Authors collection
            Book.Authors.Remove(Author);

            // Save the changes to the database
            await _context.SaveChangesAsync();

            return Ok("Author unlinked from book successfully.");
        }

    }




    }


         



