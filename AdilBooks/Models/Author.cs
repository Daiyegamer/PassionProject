
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AdilBooks.Models
{
    public class Author
    {
        [Key]
        public int AuthorId { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }

        public virtual ICollection<Book> Books { get; set; }

    }

   
    public class AuthorDto
        {
            public int AuthorId { get; set; }
            public string Name { get; set; }
            public string Bio { get; set; }
            public string Titles { get; set; } 
        }
    public class AuthorListDto
    {
        public int AuthorId { get; set; }
        public string Name { get; set; }
    }
    public class GetBooksDto
    {
        public string Title { get; set; }
        public int Year { get; set; }
    }


}