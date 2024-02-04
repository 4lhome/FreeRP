using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Mapper
{
    internal static class LinqMapper
    {
        private static readonly List<GrpcService.Database.Query> _queries = [];

        internal static List<GrpcService.Database.Query> GetQueries(Expression ex)
        {
            if (ex is LambdaExpression lambda)
            {
                _queries.Clear();
                Visit(lambda.Body);
                return _queries;
            }
            else
            {
                throw new NotSupportedException($"Expression {ex} must be a lambda expression");
            }
        }

        private static void Visit(Expression ex)
        {
            if (ex is BinaryExpression bin)
            {
                VisitBinary(bin);
                return;
            }
            else if (ex is MethodCallExpression call)
            {
                VisitCall(call);
                return;
            }
            else if (ex is UnaryExpression unary)
            {
                VisitUnary(unary);
                return;
            }
            else if (ex is MemberExpression mem)
            {
                //x => x.bool
                var q1 = GetQuery(mem);
                q1.Next = GrpcService.Database.QueryType.QueryEqual;
                _queries.Add(q1);
                _queries.Add(new() { Value = "True", ValueType = GrpcService.Database.QueryType.ValueBoolean });
                return;
            }

            throw new NotSupportedException($"Expression not supported {ex}");
        }

        private static void VisitBinary(BinaryExpression bin)
        {
            GrpcService.Database.QueryType op = GetOperator(bin.NodeType);

            //x.Foo == "1" && x.Bar == "1" || x.Bar == "2"
            if (op == GrpcService.Database.QueryType.QueryAndAlso || op == GrpcService.Database.QueryType.QueryOrElse)
            {
                Visit(bin.Left);
                _queries.Last().Next = op;
                Visit(bin.Right);
                return;
            }

            //special ArrayIndex x => x.IntArray[1] == 5
            if (op == GrpcService.Database.QueryType.CallArrayIndex)
            {
                var q1 = GetQuery(bin.Left);
                var q2 = GetQuery(bin.Right);
                q1.CallType = op;
                q1.Value = q2.Value;
                q1.ValueType = q2.ValueType;
                _queries.Add(q1);
                return;
            }

            //x.Foo == "1"
            //x.Foo > 0
            //0 < x.Foo
            //x.String.Length == 1
            if (bin.Left.NodeType == ExpressionType.MemberAccess || bin.Left.NodeType == ExpressionType.Constant)
            {
                var left = GetQuery(bin.Left);
                left.Next = op;
                _queries.Add(left);
            }
            else
            {
                //x.Foo + 10 == 20
                //x.Foo.StartWith("a") == fase
                //x.String.Count() == 5
                Visit(bin.Left);
                _queries.Last().Next = op;
            }

            var right = GetQuery(bin.Right);
            _queries.Add(right);
        }

        private static void VisitCall(MethodCallExpression call)
        {
            var ct = GetCallType(call.Method.Name);

            if ((ct is GrpcService.Database.QueryType.CallToLower || ct is GrpcService.Database.QueryType.CallToUpper) && call.Object != null)
            {
                var q = GetQuery(call.Object);
                q.CallType = ct;
                _queries.Add(q);
                return;
            }
            else if ((ct is GrpcService.Database.QueryType.CallIsNullOrEmpty || ct is GrpcService.Database.QueryType.CallCount) && call.Arguments.Count is not 0)
            {
                var q = GetQuery(call.Arguments[0]);
                q.CallType = ct;
                _queries.Add(q);
                return;
            }
            else if (ct is not GrpcService.Database.QueryType.QueryNone && call.Arguments.Count is not 0 && call.Object is not null)
            {
                //Contains, StartWith, EndsWith, x.IntList[1], x.IntList.IndexOf(1)

                var q = GetQuery(call.Object);
                var q2 = GetQuery(call.Arguments[0]);

                q.Value = q2.Value;
                q.ValueType = q2.ValueType;
                q.CallType = ct;
                _queries.Add(q);
                return;
            }
            else if (ct is GrpcService.Database.QueryType.CallContains && call.Arguments.Count == 2)
            {
                //x => x.IntArray.Contains(5)

                var q = GetQuery(call.Arguments[0]);
                var q2 = GetQuery(call.Arguments[1]);

                q.Value = q2.Value;
                q.ValueType = q2.ValueType;
                q.CallType = ct;
                _queries.Add(q);
                return;
            }

            throw new NotSupportedException($"Call not supported {call.Method.Name}");
        }

        private static void VisitUnary(UnaryExpression unary)
        {
            if (unary.NodeType == ExpressionType.ArrayLength)
            {
                var q = GetQuery(unary.Operand);
                q.CallType = GrpcService.Database.QueryType.CallCount;
                _queries.Add(q);
                return;
            }
            else if (unary.NodeType == ExpressionType.Not)
            {
                var q = GetQuery(unary.Operand);
                q.Next = GrpcService.Database.QueryType.QueryEqual;
                _queries.Add(q);
                _queries.Add(new() { Value = "False", ValueType = GrpcService.Database.QueryType.ValueBoolean });
                return;
            }

            throw new NotSupportedException($"Unary not supported {unary.NodeType}");
        }

        private static GrpcService.Database.Query GetQuery(Expression ex)
        {
            if (ex.NodeType == ExpressionType.Constant && ex is ConstantExpression con)
            {
                if (con.Value != null)
                {
                    return new()
                    {
                        ValueType = Mapper.GetQueryType(con.Value.GetType()),
                        Value = con.Value.ToString()
                    };
                }
                else
                {
                    return new() { ValueType = GrpcService.Database.QueryType.ValueNull };
                }
            }
            else if (ex.NodeType == ExpressionType.MemberAccess && ex is MemberExpression mem)
            {
                var q = GetMemberPath(mem, new());
                if (q.IsMember)
                {
                    if (_findLength == false)
                        q.MemberType = Mapper.GetQueryType(mem.Type);
                    else if (q.Name.EndsWith("Length"))
                    {
                        q.Name = q.Name.Replace(".Length", "");
                        q.MemberType = GrpcService.Database.QueryType.ValueString;
                        q.CallType = GrpcService.Database.QueryType.CallCount;
                        _findLength = false;
                    }
                    else if (q.Name.EndsWith("Count"))
                    {
                        //x => x.List<T>.Count
                        q.Name = q.Name.Replace(".Count", "");
                        q.MemberType = GrpcService.Database.QueryType.ValueArray;
                        q.CallType = GrpcService.Database.QueryType.CallCount;
                        _findLength = false;
                    }
                    return q;
                }
                else if (_findConstant)
                {
                    if (_constantValue != null)
                    {
                        q.Value = _constantValue.ToString();
                        q.ValueType = Mapper.GetQueryType(_constantValue.GetType());
                    }

                    q.Value = "";
                    q.ValueType = GrpcService.Database.QueryType.ValueNull;

                    _findConstant = false;
                    _constantValue = null;

                    return q;
                }
            }

            throw new NotSupportedException("Cannot convert: " + ex.NodeType.ToString());
        }

        private static object? _constantValue;
        private static bool _findConstant = false;
        private static bool _findLength = false;
        private static GrpcService.Database.Query GetMemberPath(Expression ex, GrpcService.Database.Query query)
        {
            if (ex is ParameterExpression para && para.Name != null)
            {
                query.IsMember = true;
                query.Name = "$";
                return query;
            }
            else if (ex is MemberExpression mem && mem.Expression != null)
            {
                GetMemberPath(mem.Expression, query);
                if (query.IsMember)
                {
                    query.Name += $".{mem.Member.Name}";
                    if (mem.Member.Name == "Length" || mem.Member.Name == "Count")
                        _findLength = true;
                }
                else if (_constantValue != null)
                {
                    if (mem.Member is FieldInfo fi)
                    {
                        _constantValue = fi.GetValue(_constantValue);
                    }
                    else if (mem.Member is PropertyInfo pi)
                    {
                        _constantValue = pi.GetValue(_constantValue);
                    }
                }

                return query;
            }
            else if (ex is ConstantExpression con)
            {
                _findConstant = true;
                _constantValue = con.Value;
                return query;
            }

            throw new NotSupportedException($"Not supported member expression {ex.NodeType}");
        }

        private static GrpcService.Database.QueryType GetCallType(string met)
        {
            return met switch
            {
                "Contains" => GrpcService.Database.QueryType.CallContains,
                "StartsWith" => GrpcService.Database.QueryType.CallStartWith,
                "EndsWith" => GrpcService.Database.QueryType.CallEndsWith,
                "Equals" => GrpcService.Database.QueryType.CallEquals,
                "ToLower" => GrpcService.Database.QueryType.CallToLower,
                "ToUpper" => GrpcService.Database.QueryType.CallToUpper,
                "IsNullOrEmpty" => GrpcService.Database.QueryType.CallIsNullOrEmpty,
                "Count" => GrpcService.Database.QueryType.CallCount,
                "get_Item" => GrpcService.Database.QueryType.CallArrayIndex,
                "IndexOf" => GrpcService.Database.QueryType.CallIndexOf,
                _ => GrpcService.Database.QueryType.QueryNone,
            };
        }

        /// <summary>
        /// Get string operator from an Binary expression
        /// </summary>
        private static GrpcService.Database.QueryType GetOperator(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add: return GrpcService.Database.QueryType.QueryAdd;
                case ExpressionType.Multiply: return GrpcService.Database.QueryType.QueryMultiply;
                case ExpressionType.Subtract: return GrpcService.Database.QueryType.QuerySubtract;
                case ExpressionType.Divide: return GrpcService.Database.QueryType.QueryDivide;
                case ExpressionType.Equal: return GrpcService.Database.QueryType.QueryEqual;
                case ExpressionType.NotEqual: return GrpcService.Database.QueryType.QueryNotEqual;
                case ExpressionType.GreaterThan: return GrpcService.Database.QueryType.QueryGreaterThan;
                case ExpressionType.GreaterThanOrEqual: return GrpcService.Database.QueryType.QueryGreaterThanOrEqual;
                case ExpressionType.LessThan: return GrpcService.Database.QueryType.QueryLessThan;
                case ExpressionType.LessThanOrEqual: return GrpcService.Database.QueryType.QueryLessThanOrEqual;
                case ExpressionType.And: return GrpcService.Database.QueryType.QueryAnd;
                case ExpressionType.AndAlso: return GrpcService.Database.QueryType.QueryAndAlso;
                case ExpressionType.Or: return GrpcService.Database.QueryType.QueryOr;
                case ExpressionType.OrElse: return GrpcService.Database.QueryType.QueryOrElse;
                case ExpressionType.ArrayIndex: return GrpcService.Database.QueryType.CallArrayIndex;
                default:
                    break;
            }

            throw new NotSupportedException($"Operator not supported {nodeType}");
        }
    }
}
