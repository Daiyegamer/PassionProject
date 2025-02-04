using AdilBooks.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AdilBooks.Models
{
    public class Publisher
    {
        [Key]
        public int PublisherId { get; set; }
        public string PublisherName { get; set; }
        //A publisher publishes many books
        public ICollection<Book> Books { get; set; }
    }

    public class PublisherDto
    {
        public int PublisherId { get; set; }
        public string PublisherName { get; set; }
        //A publisher publishes many books
        public List<string> Books { get; set; }

    }
}
