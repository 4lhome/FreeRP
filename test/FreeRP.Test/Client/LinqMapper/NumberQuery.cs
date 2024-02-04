extern alias fx;
using fx::FreeRP.GrpcService.Database;
using fx::FreeRP.Net.Client.Database;

namespace FreeRP.Test.Client.LinqMapper
{
    [TestClass]
    public class NumberQuery
    {
        public class TestModel
        {
            public int Id { get; set; }
        }

        [TestMethod]
        public void AddEqual()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Id + 10 == 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 3);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Id" && q1.Next == QueryType.QueryAdd);
            Assert.IsTrue(q2.Value == "10" && q2.ValueType == QueryType.ValueNumber && q2.Next == QueryType.QueryEqual);
            Assert.IsTrue(q3.Value == "0" && q3.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void SubtractEqual()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Id - 10 == 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 3);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Id" && q1.Next == QueryType.QuerySubtract);
            Assert.IsTrue(q2.Value == "10" && q2.ValueType == QueryType.ValueNumber && q2.Next == QueryType.QueryEqual);
            Assert.IsTrue(q3.Value == "0" && q3.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void MultiplyEqual()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Id * 10 == 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 3);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Id" && q1.Next == QueryType.QueryMultiply);
            Assert.IsTrue(q2.Value == "10" && q2.ValueType == QueryType.ValueNumber && q2.Next == QueryType.QueryEqual);
            Assert.IsTrue(q3.Value == "0" && q3.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void DivideEqual()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Id / 10 == 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 3);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Id" && q1.Next == QueryType.QueryDivide);
            Assert.IsTrue(q2.Value == "10" && q2.ValueType == QueryType.ValueNumber && q2.Next == QueryType.QueryEqual);
            Assert.IsTrue(q3.Value == "0" && q3.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void GreaterThan()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Id > 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Id" && q1.Next == QueryType.QueryGreaterThan);
            Assert.IsTrue(q2.Value == "0" && q2.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void GreaterThanOrEqual()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Id >= 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Id" && q1.Next == QueryType.QueryGreaterThanOrEqual);
            Assert.IsTrue(q2.Value == "0" && q2.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void LessThan()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Id < 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Id" && q1.Next == QueryType.QueryLessThan);
            Assert.IsTrue(q2.Value == "0" && q2.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void LessThanOrEqual()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Id <= 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Id" && q1.Next == QueryType.QueryLessThanOrEqual);
            Assert.IsTrue(q2.Value == "0" && q2.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void Equals()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Id.Equals(1) == false);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Id" && q1.MemberType == QueryType.ValueNumber &&
                q1.CallType == QueryType.CallEquals && q1.Value == "1" && q1.ValueType == QueryType.ValueNumber && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == QueryType.ValueBoolean);
        }
    }
}
