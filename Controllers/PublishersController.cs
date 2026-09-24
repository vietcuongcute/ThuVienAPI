using Microsoft.AspNetCore.Mvc;
using WebAPI_simple.Data;
using WebAPI_simple.Models.DTO;
using WebAPI_simple.Repositories;

namespace WebAPI_simple.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublishersController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IPublisherRepository _publisherRepository;

        public PublishersController(AppDbContext dbContext, IPublisherRepository publisherRepository)
        {
            _dbContext = dbContext;
            _publisherRepository = publisherRepository;
        }

        [HttpGet("get-all-publisher")]
        public IActionResult GetAllPublisher()
        {
            var allPublishers = _publisherRepository.GetAllPublishers();
            return Ok(allPublishers);
        }

        [HttpGet("get-publisher-by-id/{id:int}")]
        public IActionResult GetPublisherById(int id)
        {
            var publisherWithId = _publisherRepository.GetPublisherById(id);
            if (publisherWithId == null)
            {
                return NotFound(new { message = "Không tìm thấy NXB" });
            }
            return Ok(publisherWithId);
        }

        [HttpPost("add-publisher")]
        public IActionResult AddPublisher([FromBody] AddPublisherRequestDTO addPublisherRequestDTO)
        {
            var publisherAdd = _publisherRepository.AddPublisher(addPublisherRequestDTO);
            return Ok(publisherAdd);
        }

        [HttpPut("update-publisher-by-id/{id:int}")]
        public IActionResult UpdatePublisherById(int id, [FromBody] PublisherNoIdDTO publisherDTO)
        {
            var publisherUpdate = _publisherRepository.UpdatePublisherById(id, publisherDTO);
            if (publisherUpdate == null)
            {
                return NotFound(new { message = "Không tìm thấy NXB" });
            }
            return Ok(publisherUpdate);
        }

        [HttpDelete("delete-publisher-by-id/{id:int}")]
        public IActionResult DeletePublisherById(int id)
        {
            var publisherDelete = _publisherRepository.DeletePublisherById(id);
            if (publisherDelete == null)
            {
                return NotFound(new { message = "Không tìm thấy NXB" });
            }
            return Ok(publisherDelete);
        }

        [HttpGet("{id:int}/books")]
        public IActionResult GetBooksByPublisherId(int id)
        {
            var result = _publisherRepository.GetBooksByPublisherId(id);
            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy NXB" });
            }
            return Ok(result);
        }
    }
}