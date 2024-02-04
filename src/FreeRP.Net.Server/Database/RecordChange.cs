namespace FreeRP.Net.Server.Database
{
    public class RecordChange
    {
        private readonly RecordContext _recordContext;
        private readonly GrpcService.Database.RecordChanged _recordChanged;
        private readonly List<GrpcService.Database.Record> _records = [];

        public RecordChange(RecordContext recordContext, string userId, bool delelte)
        {
            _recordContext = recordContext;
            _recordChanged = new() { 
                RecordChangedId = Guid.NewGuid().ToString(),
                ChangeBy = userId,
                Ticks = DateTime.UtcNow.Ticks,
                Delete = delelte
            };
        }

        public void Add(GrpcService.Database.Record record) => _records.Add(record);

        public async ValueTask SaveChanges()
        {
            _recordChanged.RecordsAsJsonArray = Helpers.Json.GetJson(_records);
            _recordContext.RecordChangeds.Add(_recordChanged);
            await _recordContext.SaveChangesAsync();
        }
    }
}
