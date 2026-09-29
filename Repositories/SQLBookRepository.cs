using Microsoft.EntityFrameworkCore;
using WebAPI_simple.Data;
using WebAPI_simple.Models.Domain;
using WebAPI_simple.Models.DTO;

namespace WebAPI_simple.Repositories
{
    public class SQLBookRepository : IBookRepository
    {
        private readonly AppDbContext _dbContext;
        public SQLBookRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<BookWithAuthorAndPublisherDTO> GetAllBooks(
    string? filterOn = null, string? filterQuery = null,
    string? sortBy = null, bool isAscending = true,
    int pageNumber = 1, int pageSize = 100)
        {
            // bắt đầu từ IQueryable của Domain Model (chưa chạm database)
            var query = _dbContext.Books.AsQueryable();

            // 1. Filtering
            if (!string.IsNullOrWhiteSpace(filterOn) && !string.IsNullOrWhiteSpace(filterQuery))
            {
                if (filterOn.Equals("title", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.Title.Contains(filterQuery));
                }
                else if (filterOn.Equals("genre", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.Genre.Contains(filterQuery));
                }
                else if (filterOn.Equals("description", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.Description.Contains(filterQuery));
                }
            }

            // 2. Sorting
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (sortBy.Equals("title", StringComparison.OrdinalIgnoreCase))
                {
                    query = isAscending ? query.OrderBy(x => x.Title) : query.OrderByDescending(x => x.Title);
                }
                else if (sortBy.Equals("dateadded", StringComparison.OrdinalIgnoreCase))
                {
                    query = isAscending ? query.OrderBy(x => x.DateAdded) : query.OrderByDescending(x => x.DateAdded);
                }
                else
                {
                    query = query.OrderBy(x => x.Id);
                }
            }
            else
            {
                // Skip/Take cần có thứ tự cố định, nếu không kết quả các trang có thể bị lặp/sót
                query = query.OrderBy(x => x.Id);
            }

            // 3. Pagination
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 100;
            var skipResults = (pageNumber - 1) * pageSize;

            // 4. Map sang DTO rồi mới chạy SQL (ToList)
            return query
                .Skip(skipResults)
                .Take(pageSize)
                .Select(book => new BookWithAuthorAndPublisherDTO()
                {
                    Id = book.Id,
                    Title = book.Title,
                    Description = book.Description,
                    IsRead = book.IsRead,
                    DateRead = book.IsRead ? book.DateRead : null,
                    Rate = book.IsRead ? book.Rate : null,
                    Genre = book.Genre,
                    CoverUrl = book.CoverUrl,
                    DateAdded = book.DateAdded,
                    PublisherName = book.Publisher != null ? book.Publisher.Name : "Unknown",
                    AuthorNames = book.Book_Authors.Select(n => n.Author.FullName).ToList()
                }).ToList();
        }

        public BookWithAuthorAndPublisherDTO? GetBookById(int id)
        {
            var bookWithIdDTO = _dbContext.Books
                .Include(b => b.Publisher)
                .Include(b => b.Book_Authors).ThenInclude(ba => ba.Author)
                .Where(n => n.Id == id)
                .Select(book => new BookWithAuthorAndPublisherDTO()
                {
                    Id = book.Id,
                    Title = book.Title,
                    Description = book.Description,
                    IsRead = book.IsRead,
                    DateRead = book.DateRead,
                    Rate = book.Rate,
                    Genre = book.Genre,
                    CoverUrl = book.CoverUrl,
                    DateAdded = book.DateAdded,
                    PublisherName = book.Publisher != null ? book.Publisher.Name : "Unknown",
                    AuthorNames = book.Book_Authors.Select(n => n.Author.FullName).ToList()
                }).FirstOrDefault();

            return bookWithIdDTO;
        }

        public AddBookRequestDTO AddBook(AddBookRequestDTO addBookRequestDTO)
        {
            // map DTO to Domain Model
            var bookDomainModel = new Book
            {
                Title = addBookRequestDTO.Title ?? string.Empty,
                Description = addBookRequestDTO.Description ?? string.Empty,
                IsRead = addBookRequestDTO.IsRead,
                DateRead = addBookRequestDTO.DateRead,
                Rate = addBookRequestDTO.Rate,
                Genre = addBookRequestDTO.Genre ?? string.Empty,
                CoverUrl = addBookRequestDTO.CoverUrl,
                DateAdded = addBookRequestDTO.DateAdded,
                PublisherID = addBookRequestDTO.PublisherID
            };

            // Use Domain Model to add Book
            _dbContext.Books.Add(bookDomainModel);
            _dbContext.SaveChanges();

            foreach (var id in addBookRequestDTO.AuthorIds)
            {
                var _book_author = new Book_Author()
                {
                    BookId = bookDomainModel.Id,
                    AuthorId = id
                };
                _dbContext.Books_Authors.Add(_book_author);
                _dbContext.SaveChanges();
            }

            return addBookRequestDTO;
        }

        public AddBookRequestDTO? UpdateBookById(int id, AddBookRequestDTO bookDTO)
        {
            var bookDomain = _dbContext.Books.FirstOrDefault(n => n.Id == id);
            if (bookDomain == null)
            {
                return null;
            }

            bookDomain.Title = bookDTO.Title ?? string.Empty;
            bookDomain.Description = bookDTO.Description ?? string.Empty;
            bookDomain.IsRead = bookDTO.IsRead;
            bookDomain.DateRead = bookDTO.DateRead;
            bookDomain.Rate = bookDTO.Rate;
            bookDomain.Genre = bookDTO.Genre ?? string.Empty;
            bookDomain.CoverUrl = bookDTO.CoverUrl;
            bookDomain.DateAdded = bookDTO.DateAdded;
            bookDomain.PublisherID = bookDTO.PublisherID;
            _dbContext.SaveChanges();

            var existingBookAuthors = _dbContext.Books_Authors.Where(a => a.BookId == id).ToList();
            if (existingBookAuthors.Count > 0)
            {
                _dbContext.Books_Authors.RemoveRange(existingBookAuthors);
                _dbContext.SaveChanges();
            }

            foreach (var authorId in bookDTO.AuthorIds)
            {
                var _book_author = new Book_Author()
                {
                    BookId = id,
                    AuthorId = authorId
                };
                _dbContext.Books_Authors.Add(_book_author);
                _dbContext.SaveChanges();
            }

            return bookDTO;
        }

        public Book? DeleteBookById(int id)
        {
            var bookDomain = _dbContext.Books.FirstOrDefault(n => n.Id == id);
            if (bookDomain != null)
            {
                var existingBookAuthors = _dbContext.Books_Authors.Where(a => a.BookId == id).ToList();
                if (existingBookAuthors.Count > 0)
                {
                    _dbContext.Books_Authors.RemoveRange(existingBookAuthors);
                    _dbContext.SaveChanges();
                }

                _dbContext.Books.Remove(bookDomain);
                _dbContext.SaveChanges();
            }
            return bookDomain;
        }
    }
}