namespace FreeRP.Test.Client
{
    [TestClass]
    public class TestDatabaseService
    {
        public GrpcService.Database.Database Database { get; set; }
        private readonly Net.Client.Services.DatabaseService _databaseService = new(GlobalSetting.ConnectService, GlobalSetting.I18n);

        public TestDatabaseService()
        {
            Database = new()
            {
                DatabaseId = "db.test.com",
                DatabaseProvider = GrpcService.Database.DatabaseProvider.Sqlite,
            };

            GrpcService.Database.DatabaseTable t1 = new()
            {
                TableId = "t1",
            };
            t1.Fields.Add(new GrpcService.Database.DatabaseTableField()
            {
                DataType = GrpcService.Database.DatabaseTableDataType.FieldString,
                FieldId = "f1",
                IsId = true,
            });
        }

        [TestMethod]
        public async Task CreateDatabase()
        {
            var res = await _databaseService.DatabaseAddAsync(Database);
            Assert.IsNull(res, nameof(CreateDatabase));

            var db = await _databaseService.GetDatabaseAsync(Database.DatabaseId);
            Assert.IsNotNull(db, nameof(CreateDatabase));

            Assert.IsTrue(Database.DatabaseProvider == db.DatabaseProvider, nameof(CreateDatabase));
            Assert.IsTrue(Database.Tables[0].TableId == db.Tables[0].TableId, nameof(CreateDatabase));
            Assert.IsTrue(Database.Tables[0].Fields[0].FieldId == db.Tables[0].Fields[0].FieldId, nameof(CreateDatabase));
            Assert.IsTrue(Database.Tables[0].Fields[0].IsId == db.Tables[0].Fields[0].IsId, nameof(CreateDatabase));
        }

        [TestMethod]
        public async Task ChangeDatabase()
        {
            
        }
    }
}
