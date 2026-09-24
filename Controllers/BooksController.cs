using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI_simple.Data;
using WebAPI_simple.Models.DTO;
using WebAPI_simple.Models.Domain;

namespace WebAPI_simple.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public BooksController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("get-all-books")]
        public IActionResult GetAll()
        {
            var allBooksDomain = _dbContext.Books
                .Include(b => b.Publisher)
                .Include(b => b.Book_Authors).ThenInclude(ba => ba.Author);

            var allBooksDTO = allBooksDomain.Select(b => new BookWithAuthorAndPublisherDTO()
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                IsRead = b.IsRead,
                DateRead = b.IsRead ? b.DateRead : null,
                Rate = b.IsRead ? b.Rate : null,
                Genre = b.Genre,
                CoverUrl = b.CoverUrl,
                DateAdded = b.DateAdded,
                PublisherName = b.Publisher != null ? b.Publisher.Name : "Unknown",
                AuthorNames = b.Book_Authors != null
                    ? b.Book_Authors.Select(n => n.Author.FullName).ToList()
                    : new List<string>()
            }).ToList();

            return Ok(allBooksDTO);
        }

        [HttpGet]
        [Route("get-book-by-id/{id:int}")]
        public IActionResult GetBookById([FromRoute] int id)
        {
            // get bookDomain object from DB
            var bookDomain = _dbContext.Books
                .Include(b => b.Publisher)
                .Include(b => b.Book_Authors).ThenInclude(ba => ba.Author)
                .FirstOrDefault(b => b.Id == id);

            if (bookDomain == null)
            {
                return NotFound(new { message = "Không tìm thấy sách" });
            }

            var bookDTO = new BookWithAuthorAndPublisherDTO()
            {
                Id = bookDomain.Id,
                Title = bookDomain.Title,
                Description = bookDomain.Description,
                IsRead = bookDomain.IsRead,
                DateRead = bookDomain.DateRead,
                Rate = bookDomain.Rate,
                Genre = bookDomain.Genre,
                CoverUrl = bookDomain.CoverUrl,
                DateAdded = bookDomain.DateAdded,
                PublisherName = bookDomain.Publisher != null ? bookDomain.Publisher.Name : "Unknown",
                AuthorNames = bookDomain.Book_Authors?.Where(y => y.Author != null)
                    .Select(y => y.Author.FullName).ToList() ?? new List<string>()
            };
            return Ok(bookDTO);
        }

        [HttpPost("add-book")]
        public IActionResult AddBook([FromBody] AddBookRequestDTO addBookRequestDTO)
        {
            // check if publisher exists or not
            var publisherDomain = _dbContext.Publishers
                .FirstOrDefault(x => x.Id == addBookRequestDTO.PublisherID);
            if (publisherDomain == null)
            {
                return NotFound(new { message = "Không tìm thấy NXB" });
            }

            var bookDomain = new Book()
            {
                Title = addBookRequestDTO.Title ?? string.Empty,
                Description = addBookRequestDTO.Description ?? string.Empty,
                IsRead = addBookRequestDTO.IsRead,
                DateRead = addBookRequestDTO.DateRead,
                Rate = addBookRequestDTO.Rate,
                Genre = addBookRequestDTO.Genre ?? string.Empty,
                CoverUrl = addBookRequestDTO.CoverUrl,
                DateAdded = addBookRequestDTO.DateAdded,
                PublisherID = publisherDomain.Id
            };
            _dbContext.Books.Add(bookDomain);
            _dbContext.SaveChanges();

            foreach (var authorId in addBookRequestDTO.AuthorIds)
            {
                var authorDomain = _dbContext.Authors.FirstOrDefault(x => x.Id == authorId);
                if (authorDomain == null)
                {
                    return NotFound(new { message = "Không tìm thấy tác giả" });
                }

                var bookAuthorDomain = new Book_Author()
                {
                    BookId = bookDomain.Id,
                    AuthorId = authorDomain.Id
                };
                _dbContext.Books_Authors.Add(bookAuthorDomain);
                _dbContext.SaveChanges();
            }
            return Ok(bookDomain);
        }

        [HttpPut("update-book-by-id/{id:int}")]
        public IActionResult UpdateBookById(int id, [FromBody] AddBookRequestDTO addBookRequestDTO)
        {
            var bookDomain = _dbContext.Books.FirstOrDefault(x => x.Id == id);
            if (bookDomain == null)
            {
                return NotFound(new { message = "Không tìm thấy sách" });
            }

            bookDomain.Title = addBookRequestDTO.Title ?? string.Empty;
            bookDomain.Description = addBookRequestDTO.Description ?? string.Empty;
            bookDomain.IsRead = addBookRequestDTO.IsRead;
            bookDomain.DateRead = addBookRequestDTO.DateRead;
            bookDomain.Rate = addBookRequestDTO.Rate;
            bookDomain.Genre = addBookRequestDTO.Genre ?? string.Empty;
            bookDomain.CoverUrl = addBookRequestDTO.CoverUrl;
            bookDomain.DateAdded = addBookRequestDTO.DateAdded;
            bookDomain.PublisherID = addBookRequestDTO.PublisherID;
            _dbContext.SaveChanges();

            var existingBookAuthors = _dbContext.Books_Authors.Where(x => x.BookId == id).ToList();
            if (existingBookAuthors.Count > 0)
            {
                _dbContext.Books_Authors.RemoveRange(existingBookAuthors);
                _dbContext.SaveChanges();
            }

            foreach (var authorId in addBookRequestDTO.AuthorIds)
            {
                var authorDomain = _dbContext.Authors.FirstOrDefault(x => x.Id == authorId);
                if (authorDomain == null)
                {
                    return NotFound(new { message = "Không tìm thấy tác giả" });
                }

                var bookAuthorDomain = new Book_Author()
                {
                    BookId = bookDomain.Id,
                    AuthorId = authorDomain.Id
                };
                _dbContext.Books_Authors.Add(bookAuthorDomain);
                _dbContext.SaveChanges();
            }
            return Ok(addBookRequestDTO);
        }

        [HttpDelete("delete-book-by-id/{id:int}")]
        public IActionResult DeleteBookById(int id)
        {
            var bookDomain = _dbContext.Books.FirstOrDefault(x => x.Id == id);
            if (bookDomain == null)
            {
                return NotFound(new { message = "Không tìm thấy sách" });
            }

            var existingBookAuthors = _dbContext.Books_Authors.Where(x => x.BookId == id).ToList();
            if (existingBookAuthors.Count > 0)
            {
                _dbContext.Books_Authors.RemoveRange(existingBookAuthors);
                _dbContext.SaveChanges();
            }

            _dbContext.Books.Remove(bookDomain);
            _dbContext.SaveChanges();
            return Ok(bookDomain);
        }
    }
}