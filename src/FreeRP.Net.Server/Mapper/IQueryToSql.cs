using FreeRP.GrpcService.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Server.Mapper
{
    public interface IQueryToSql
    {
        string GetSql(IEnumerable<Query> queries, string tableName, string jsonColName, string where = "");
    }
}
