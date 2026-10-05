using WebAPI_simple.Data;
using WebAPI_simple.Models.Domain;

namespace WebAPI_simple.Repositories
{
    public class LocalImageRepository : IImageRepository
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _dbContext;

        public LocalImageRepository(IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor, AppDbContext dbContext)
        {
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
        }

        public Image Upload(Image image)
        {
            var localFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Images",
                $"{image.FileName}{image.FileExtension}");

            // ghi file vào thư mục Images trên ổ đĩa
            using var stream = new FileStream(localFilePath, FileMode.Create);
            image.File.CopyTo(stream);

            // ví dụ kết quả: https://localhost:7123/images/image.jpg
            var httpRequest = _httpContextAccessor.HttpContext!.Request;
            var urlFilePath = $"{httpRequest.Scheme}://{httpRequest.Host}{httpRequest.PathBase}/Images/{image.FileName}{image.FileExtension}";
            image.FilePath = urlFilePath;

            // lưu thông tin vào bảng Images
            _dbContext.Images.Add(image);
            _dbContext.SaveChanges();

            return image;
        }

        public List<Image> GetAllInfoImages()
        {
            return _dbContext.Images.ToList();
        }

        public (byte[] Content, string ContentType, string FileName) DownloadFile(int id)
        {
            var fileById = _dbContext.Images.FirstOrDefault(x => x.Id == id);
            if (fileById == null)
            {
                throw new FileNotFoundException("Không tìm thấy file");
            }

            var path = Path.Combine(_webHostEnvironment.ContentRootPath, "Images",
                $"{fileById.FileName}{fileById.FileExtension}");

            var content = File.ReadAllBytes(path);
            var fileName = fileById.FileName + fileById.FileExtension;

            return (content, "application/octet-stream", fileName);
        }
    }
}