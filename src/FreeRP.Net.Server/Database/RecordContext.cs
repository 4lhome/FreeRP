using FreeRP.GrpcService.Core;
using FreeRP.GrpcService.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Server.Database
{
    public class RecordContext(Data.FrpSettings frpSettings, GrpcService.Database.Database config) : DbContext
    {
        private readonly Data.FrpSettings _frpSettings = frpSettings;
        private readonly GrpcService.Database.Database _config = config;

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_config.DatabaseProvider == DatabaseProvider.Sqlite)
            {
                string path = Path.Combine(_frpSettings.DatabasesRootPath, $"{_config.DatabaseId}.db");
                options.UseSqlite($"Data Source={path}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Record>().HasKey(x => x.RecordId);
            modelBuilder.Entity<Record>().HasIndex(x => new { x.RecordId, x.RecordType, x.ChangeBy, x.Owner, x.Ticks });

            modelBuilder.Entity<RecordChanged>().HasKey(x => x.RecordChangedId);
            modelBuilder.Entity<RecordChanged>().HasIndex(x => new { x.ChangeBy, x.Ticks });

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Record> Records { get; set; }
        public DbSet<RecordChanged> RecordChangeds { get; set; }

        #region Helper

        public static Record GetRecord(string changeBy, string recordId, string recordType, string owner, string json)
        {
            return new()
            {
                DataAsJson = json,
                RecordId = recordId,
                RecordType = recordType,
                Ticks = DateTime.UtcNow.Ticks,
                ChangeBy = changeBy,
                Owner = owner
            };
        }

        public static RecordChanged GetRecordChanged(string userId, bool delete, string json)
        {
            return new RecordChanged()
            {
                RecordChangedId = Guid.NewGuid().ToString(),
                Ticks = DateTime.UtcNow.Ticks,
                ChangeBy = userId,
                Delete = delete,
                RecordsAsJsonArray = json
            };
        }

        #endregion
    }
}
