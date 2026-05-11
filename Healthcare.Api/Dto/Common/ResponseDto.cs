namespace Healthcare.Api.Dto.Common
{
    public class ResponseDto
    {
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public byte[]? File { get; set; }
    }
}
