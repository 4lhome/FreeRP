using FreeRP.GrpcService.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Database
{
    public class Queryable<T> : IQueryable<T>
    {
        private readonly GrpcService.Database.Database? _db;
        private readonly List<Query> _queries = [];
        public IEnumerable<Query> GetQueries => _queries.ToArray();

        private int _skipe = 0;
        private int _take = 0;

        public Queryable(GrpcService.Database.Database db) 
        {
            _db = db;
        }

        public Queryable()
        {
            
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predExpr)
        {
            Where(predExpr);
            _take = 1;
            return (await ToListAsync()).FirstOrDefault();
        }

        public IQueryable<T> Skip(int offset)
        {
            _skipe = offset;
            return this;
        }

        public IQueryable<T> Take(int count)
        {
            _take = count;
            return this;
        }

        public async Task<IEnumerable<T>> ToArrayAsync()
        {
            return (await ToListAsync()).ToArray();
        }

        public async Task<IEnumerable<T>> ToListAsync()
        {
            if (_db != null)
            {
                var qr = new QueryRequest()
                {
                    TableId = typeof(T).Name,
                    Skipe = _skipe,
                    Take = _take
                };
                qr.Queries.AddRange(_queries);

                var res = await _db.ExecuteAsync(qr);
                if (res.Error.ErrorType == GrpcService.Core.ErrorType.ErrorNone)
                {
                    List<T> list = [];

                    if (res.Data.Count != 0)
                    {
                        foreach (var item in res.Data)
                        {
                            var m = Helpers.Json.GetModel<T>(item.Json);
                            if (m != null)
                            {
                                list.Add(m);
                            }
                        }
                    }

                    return list;
                }

                throw new Exception(res.Error.Message);
            }

            throw new ArgumentNullException(nameof(GrpcService.Database.Database));
        }

        public IQueryable<T> Where(Expression<Func<T, bool>> predExpr)
        {
            var q = Mapper.LinqMapper.GetQueries(predExpr);
            if (_queries.Count != 0 && q.Count != 0)
            {
                _queries.Last().Next = QueryType.QueryAndAlso;
                _queries.AddRange(q);
            }
            else
            {
                _queries.AddRange(q);
            }

            return this;
        }
    }
}
