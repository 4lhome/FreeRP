extern alias fx;
using fx::FreeRP.GrpcService.Database;
using fx::FreeRP.Net.Client.Database;

namespace FreeRP.Test.Client.LinqMapper
{
    [TestClass]
    public class ListQuery
    {
        public class TestModel
        {
            public List<int> IntList { get; set; } = new() { 5 };
        }

        [TestMethod]
        public void Contains()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.IntList.Contains(5));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 1);

            var q1 = qc1.ElementAt(0);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.IntList" && q1.MemberType == QueryType.ValueArray &&
                q1.CallType == QueryType.CallContains && q1.Value == "5" && q1.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void Count()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.IntList.Count == 5);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.IntList" && q1.MemberType == QueryType.ValueArray &&
                q1.CallType == QueryType.CallCount && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "5" && q2.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void Index()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.IntList[1] == 5);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.IntList" && q1.MemberType == QueryType.ValueArray &&
                q1.Value == "1" && q1.ValueType == QueryType.ValueNumber &&
                q1.CallType == QueryType.CallArrayIndex && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "5" && q2.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void IndexOf()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.IntList.IndexOf(1) == 5);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.IntList" && q1.MemberType == QueryType.ValueArray &&
                q1.Value == "1" && q1.ValueType == QueryType.ValueNumber &&
                q1.CallType == QueryType.CallIndexOf && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "5" && q2.ValueType == QueryType.ValueNumber);
        }
    }
}
