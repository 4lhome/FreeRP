using FreeRP.GrpcService.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Server.Mapper
{
    public class Sqlite : IQueryToSql
    {
        //FormattableString erben
        //https://www.meziantou.net/interpolated-strings-advanced-usages.htm

        //https://www.sqlite.org/lang_corefunc.html
        //https://www.sqlite.org/json1.html

        private const string ArrayName = "arr";

        private readonly List<Query> _queries = [];
        private string _tableName = string.Empty;
        private string _jsonColName = string.Empty;

        private Query _query = new();
        private int _index = 0;
        private readonly List<string> _selectFrom = [];
        private readonly StringBuilder _sb = new();

        public string GetSql(IEnumerable<Query> queries, string tableName, string jsonColName, string where = "")
        {
            _queries.Clear();
            _queries.AddRange(queries);
            _tableName = tableName;
            _jsonColName = jsonColName;

            _index = -1;
            _selectFrom.Clear();
            _sb.Clear();

            while (TryGetNext())
            {
                if (_query.CallType == QueryType.QueryNone)
                {
                    _sb.Append(MemberOrValueToSql());
                }
                else
                {
                    switch (_query.CallType)
                    {
                        case QueryType.CallContains:
                            CallContains();
                            break;
                        case QueryType.CallStartWith:
                        case QueryType.CallEndsWith:
                            StartEndsWith();
                            break;
                        case QueryType.CallEquals:
                            CallEquals();
                            break;
                        case QueryType.CallToLower:
                            _sb.Append($"(lower({MemberOrValueToSql()}))");
                            break;
                        case QueryType.CallToUpper:
                            _sb.Append($"(upper({MemberOrValueToSql()}))");
                            break;
                        case QueryType.CallIsNullOrEmpty:
                            IsNullOrEmpty();
                            break;
                        case QueryType.CallCount:
                            CallCount();
                            break;
                        case QueryType.CallArrayIndex:
                            CallArrayIndex();
                            break;
                        case QueryType.CallIndexOf:
                            CallIndexOf();
                            break;
                        default:
                            break;
                    }
                }

                if (_query.Next != QueryType.QueryNone)
                    _sb.Append($" {GetSqlName(_query.Next)} ");
            }

            StringBuilder sbQuery = new();
            sbQuery.Append($"select * from {_tableName}");
            if (_selectFrom.Count != 0)
            {
                sbQuery.Append(", ");
                sbQuery.AppendJoin(", ", _selectFrom);
            }

            sbQuery.Append(" where ");

            if (where != "")
                sbQuery.Append($"{where} ");

            sbQuery.Append(_sb);

            return sbQuery.ToString();
        }

        private void CallContains()
        {
            if (_query.IsMember && _query.MemberType == QueryType.ValueString)
                StartEndsWith();
            else if (_query.IsMember && _query.MemberType == QueryType.ValueArray)
            {
                var arr = $"{ArrayName}{_index}";
                _selectFrom.Add($"json_each({_tableName}.{_jsonColName}, '{_query.Name}') {arr}");

                if (_query.Next == QueryType.QueryEqual || _query.Next == QueryType.QueryOrElse)
                {
                    string q = $"{arr}.value = {GetValue(_query.ValueType, _query.Value)}";
                    if (TryGetNext() && _query.Value == (false).ToString())
                        q = q.Replace("=", "!=");

                    _sb.Append(q);
                }
                else
                    _sb.Append($"{arr}.value = {GetValue(_query.ValueType, _query.Value)}");
            }
        }

        private void StartEndsWith()
        {
            _sb.Append(MemberOrValueToSql());
            string val;
            if (_query.CallType == QueryType.CallStartWith)
                val = $"'{_query.Value}%'";
            else if (_query.CallType == QueryType.CallEndsWith)
                val = $"'%{_query.Value}'";
            else
                val = $"'%{_query.Value}%'";

            if ((_query.Next == QueryType.QueryEqual || _query.Next == QueryType.QueryNotEqual))
            {
                if (TryGetNext() && _query.ValueType == QueryType.ValueBoolean)
                {
                    if (_query.Value == (true).ToString())
                        _sb.Append($" like {val}");
                    else
                        _sb.Append($" not like {val}");
                }
            }
            else
            {
                _sb.Append($" like {val}");
            }
        }

        private void CallEquals()
        {
            _sb.Append(MemberOrValueToSql());

            if (_query.Next == QueryType.QueryEqual || _query.Next == QueryType.QueryNotEqual)
            {
                var vt = _query.ValueType;
                var val = _query.Value;

                _sb.Append($" {GetSqlName(_query.Next)} ");
                if (TryGetNext() && _query.ValueType == QueryType.ValueBoolean)
                {
                    _sb.Append(GetValue(vt, val));
                }
            }
            else
            {
                _sb.Append($" = {GetValue(_query.ValueType, _query.Value)}");
            }
        }

        private void IsNullOrEmpty()
        {
            var mem = MemberOrValueToSql();
            _sb.Append(mem);

            if (_query.Next == QueryType.QueryEqual || _query.Next == QueryType.QueryNotEqual)
            {
                if (TryGetNext() && _query.ValueType == QueryType.ValueBoolean)
                {
                    if (_query.Value == (true).ToString())
                        _sb.Append($" is null or {mem} = ''");
                    else
                        _sb.Append($" is not null and {mem} != ''");
                }
            }
            else
            {
                _sb.Append($" is null or {mem} = ''");
            }
        }

        private void CallCount()
        {
            if (_query.IsMember && _query.MemberType == QueryType.ValueString)
            {
                _sb.Append($"length({MemberOrValueToSql()})");
            }
            else if (_query.IsMember && _query.MemberType == QueryType.ValueArray)
            {
                _sb.Append($"json_array_length({MemberOrValueToSql()})");
            }
        }

        private void CallArrayIndex()
        {
            _sb.Append($"json_extract({_jsonColName}, '{_query.Name}[{_query.Value}]')");
        }

        private void CallIndexOf()
        {
            if (_query.MemberType == QueryType.ValueString)
            {
                _sb.Append($"instr({MemberOrValueToSql()}, {GetValue(_query.ValueType, _query.Value)}) {GetSqlName(_query.Next)}");
                if (TryGetNext() && int.TryParse(_query.Value, out int c))
                {
                    c++;
                    _sb.Append($" {c}");
                }
            }
            else if (_query.MemberType == QueryType.ValueArray)
            {
                var arr = $"{ArrayName}{_index}";
                _selectFrom.Add($"json_each({_tableName}.{_jsonColName}, '{_query.Name}') {arr}");
                _sb.Append($"{arr}.value = {GetValue(_query.ValueType, _query.Value)}");
                if (TryGetNext())
                {
                    _sb.Append($" and {arr}.key = {GetValue(_query.ValueType, _query.Value)}");
                }
            }
        }

        bool TryGetNext()
        {
            _index++;

            if (_index < _queries.Count)
            {
                _query = _queries[_index];
                return true;
            }

            return false;
        }

        private string MemberOrValueToSql()
        {
            if (_query.IsMember)
            {
                return $"json_extract({_jsonColName}, '{_query.Name}')";
            }

            return GetValue(_query.ValueType, _query.Value);
        }

        public static string GetSqlName(QueryType queryType)
        {
            return queryType switch
            {
                QueryType.QueryAdd => "+",
                QueryType.QueryDivide => "/",
                QueryType.QueryMultiply => "*",
                QueryType.QuerySubtract => "-",
                QueryType.QueryGreaterThan => ">",
                QueryType.QueryGreaterThanOrEqual => ">=",
                QueryType.QueryLessThan => "<",
                QueryType.QueryLessThanOrEqual => "<=",
                QueryType.QueryEqual => "=",
                QueryType.QueryNotEqual => "!=",
                QueryType.QueryAnd => "&",
                QueryType.QueryOr => "|",
                QueryType.QueryAndAlso => "and",
                QueryType.QueryOrElse => "or",
                _ => ""
            };
        }

        public static string GetValue(QueryType queryType, string val)
        {
            return queryType switch
            {
                QueryType.ValueString => $"'{val}'",
                QueryType.ValueArray or
                QueryType.ValueNumber or
                QueryType.ValueBoolean or
                QueryType.ValueObject => val,
                _ => "'null'"
            };
        }
    }
}
