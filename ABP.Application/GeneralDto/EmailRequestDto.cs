namespace ABP.Application.GeneralDto
{
    public class EmailRequestDto
    {
        public string ToEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty; 
    }
}