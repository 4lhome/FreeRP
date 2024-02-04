namespace FreeRP.Net.Server.Data
{
    public class StreamingData
    {
        public Stream? Stream { get; set; }
        public string? File { get; set; }
        public DateTime Deadline { get; set; }
    }
}
