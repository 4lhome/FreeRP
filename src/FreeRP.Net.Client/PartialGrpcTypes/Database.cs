using FreeRP.Net.Client.Database;
using FreeRP.Net.Client.Mapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.GrpcService.Database
{
    public partial class Database
    {
        private const string KeyNotFoundText = "A property named Id or <type name>Id and type String.";
        private const string KeyNotStringText = "A property {0} is not type String.";

        internal Net.Client.Services.ConnectService ConnectService { get; set; }
        public Net.Client.Translation.I18nService I18n { get; set; }

        internal async Task<QueryResponse> ExecuteAsync(QueryRequest request)
        {
            request.DatabaseId = DatabaseId;
            var res = await ConnectService.DatabaseServiceClient.DatabaseItemQueryAsync(request, ConnectService.AuthHeader);
            return res;
        }

        public async Task<T> AddAsync<T>(T item)
        {
            return (await AddRangeAsync(new T[] { item })).First();
        }

        public async Task<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> items)
        {
            Type type = typeof(T);

            if (Mapper.GetMapId(type) is PropertyInfo p)
            {
                var r = new DataRequest()
                {
                    DatabaseId = DatabaseId,
                    TableId = type.Name
                };

                foreach (var item in items)
                {
                    if (item == null)
                        throw new NoNullAllowedException(nameof(item));

                    string id = Guid.NewGuid().ToString();
                    p.SetValue(item, id);
                    //r.Data.Add(new DataAsJson() { Id = id, Json = Net.Client.Helpers.Json.GetJson(item) });
                }

                var res = await ConnectService.DatabaseServiceClient.DatabaseItemAddAsync(r, ConnectService.AuthHeader);
                if (res.ErrorType == Core.ErrorType.ErrorNone)
                {
                    return items;
                }

                //throw new Exception(res.Error.Message);
            }

            throw new KeyNotFoundException(KeyNotFoundText);
        }

        public async Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predExpr)
        {
            return (await Where(predExpr).Take(1).ToListAsync()).FirstOrDefault();
        }

        public async Task<bool> RemoveAsync<T>(T item)
        {
            return await RemoveRangeAsync(new T[] { item });
        }

        public async Task<bool> RemoveRangeAsync<T>(IEnumerable<T> items)
        {
            Type type = typeof(T);

            if (Mapper.GetMapId(type) is PropertyInfo p)
            {
                var r = new DataRequest()
                {
                    DatabaseId = DatabaseId,
                    TableId = type.Name
                };

                foreach (var item in items)
                {
                    if (item == null)
                        throw new NoNullAllowedException(nameof(item));

                    if (p.GetValue(item) is string id)
                    {
                        //r.Data.Add(new DataAsJson() { Id = id });
                    }
                    else
                    {
                        throw new TypeAccessException(KeyNotStringText.Replace("{0}", p.Name));
                    }
                }

                var res = await ConnectService.DatabaseServiceClient.DatabaseItemRemoveAsync(r, ConnectService.AuthHeader);
                if (res.ErrorType == Core.ErrorType.ErrorNone)
                {
                    return true;
                }

                //throw new Exception(res.Error.Message);
            }

            throw new KeyNotFoundException(KeyNotFoundText);
        }

        public async Task<bool> UpdateAsync<T>(T item)
        {
            return await UpdateRangeAsync(new T[] { item });
        }

        public async Task<bool> UpdateRangeAsync<T>(IEnumerable<T> items)
        {
            Type type = typeof(T);

            if (Mapper.GetMapId(type) is PropertyInfo p)
            {
                var r = new DataRequest()
                {
                    DatabaseId = DatabaseId,
                    TableId = type.Name
                };

                foreach (var item in items)
                {
                    if (item == null)
                        throw new NoNullAllowedException(nameof(item));

                    if (p.GetValue(item) is string id)
                    {
                        //r.Data.Add(new DataAsJson() { Id = id, Json = Net.Client.Helpers.Json.GetJson(item) });
                    }
                    else
                    {
                        throw new TypeAccessException(KeyNotStringText.Replace("{0}", p.Name));
                    }
                }

                var res = await ConnectService.DatabaseServiceClient.DatabaseItemAddAsync(r, ConnectService.AuthHeader);
                if (res.ErrorType == Core.ErrorType.ErrorNone)
                {
                    return true;
                }

                //throw new Exception(res.Error.Message);
            }

            throw new KeyNotFoundException(KeyNotFoundText);
        }

        public Net.Client.Database.IQueryable<T> Where<T>(Expression<Func<T, bool>> predExpr)
        {
            return new Queryable<T>(this).Where(predExpr);
        }
    }
}
