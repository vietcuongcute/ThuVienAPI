using Microsoft.AspNetCore.Mvc;
using WebAPI_simple.Data;
using WebAPI_simple.Models.DTO;
using WebAPI_simple.Repositories;
using WebAPI_simple.CustomActionFilters;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace WebAPI_simple.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IBookRepository _bookRepository;
        private readonly ILogger<BooksController> _logger;

        public BooksController(AppDbContext dbContext, IBookRepository bookRepository, ILogger<BooksController> logger)
        {
            _dbContext = dbContext;
            _bookRepository = bookRepository;
            _logger = logger;
        }

        [HttpGet("get-all-books")]
        [Authorize(Roles = "Read")]
        public IActionResult GetAll(
            [FromQuery] string? filterOn, [FromQuery] string? filterQuery,
            [FromQuery] string? sortBy, [FromQuery] bool isAscending = true,
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100)
        {
            _logger.LogInformation("GetAll Book Action method was invoked");   // 3. dùng "_logger" có gạch dưới

            var allBooks = _bookRepository.GetAllBooks(filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);

            _logger.LogInformation("Finished GetAllBook request with {Count} results", allBooks.Count);

            return Ok(allBooks);
        }


        [HttpPost("add-book")]
        [Authorize(Roles = "Write")]
        public IActionResult AddBook([FromBody] AddBookRequestDTO addBookRequestDTO)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AddBook validation failed: {@Errors}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                var bookAdd = _bookRepository.AddBook(addBookRequestDTO);
                _logger.LogInformation("Book added successfully: {Title}", addBookRequestDTO.Title);
                return Ok(bookAdd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding book");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("delete-book-by-id/{id:int}")]
        [Authorize(Roles = "Write")]
        public IActionResult DeleteBookById(int id)
        {
            var deleteBook = _bookRepository.DeleteBookById(id);
            if (deleteBook == null) return NotFound(new { message = "Không tìm thấy sách" });
            return Ok(deleteBook);
        }

        [HttpGet("get-book-by-id/{id:int}")]
        [Authorize(Roles = "Read")]
        public IActionResult GetBookById(int id)
        {
            var book = _bookRepository.GetBookById(id);
            if (book == null)
            {
                return NotFound(new { message = "Không tìm thấy sách" });
            }
            return Ok(book);
        }

        [HttpPut("update-book-by-id/{id:int}")]
        [Authorize(Roles = "Write")]
        public IActionResult UpdateBookById(int id, [FromBody] AddBookRequestDTO bookDTO)
        {
            var updatedBook = _bookRepository.UpdateBookById(id, bookDTO);
            if (updatedBook == null)
            {
                return NotFound(new { message = "Không tìm thấy sách để cập nhật" });
            }
            return Ok(updatedBook);
        }

        #region Private methods
        private bool ValidateAddBook(AddBookRequestDTO addBookRequestDTO)
        {
            if (addBookRequestDTO == null)
            {
                ModelState.AddModelError(nameof(addBookRequestDTO), "Please add book data");
                return false;
            }

            // kiểm tra Description không được rỗng
            if (string.IsNullOrEmpty(addBookRequestDTO.Description))
            {
                ModelState.AddModelError(nameof(addBookRequestDTO.Description),
                    $"{nameof(addBookRequestDTO.Description)} cannot be null");
            }

            // kiểm tra rating (0,5)
            if (addBookRequestDTO.Rate < 0 || addBookRequestDTO.Rate > 5)
            {
                ModelState.AddModelError(nameof(addBookRequestDTO.Rate),
                    $"{nameof(addBookRequestDTO.Rate)} cannot be less than 0 and more than 5");
            }

            if (ModelState.ErrorCount > 0)
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}