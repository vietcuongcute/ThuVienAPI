namespace WebAPI_simple.Models.DTO
{
    public class AuthorWithBooksDTO
    {
        public string FullName { get; set; } = string.Empty;
        public List<string> BookTitles { get; set; } = new();
    }
}