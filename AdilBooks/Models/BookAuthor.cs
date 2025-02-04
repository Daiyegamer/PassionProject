using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdilBooks.Models
{
    public class BookAuthor
    {

        [Key]
        public int BookAuthorId { get; set; }
        [ForeignKey("Books")]
        public int BookId { get; set; }
        [ForeignKey("Authors")]
        public int AuthorId { get; set; }

    }
}
