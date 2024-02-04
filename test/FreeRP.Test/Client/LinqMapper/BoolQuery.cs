extern alias fx;
using fx::FreeRP.GrpcService.Database;
using fx::FreeRP.Net.Client.Database;

namespace FreeRP.Test.Client.LinqMapper
{
    [TestClass]
    public class BoolQuery
    {
        public class TestModel
        {
            public bool Active { get; set; }
        }

        [TestMethod]
        public void IsFalse()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Active == false);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Active" && q1.MemberType == QueryType.ValueBoolean && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == QueryType.ValueBoolean);
        }

        [TestMethod]
        public void Invert()
        {
            var c1 = new Queryable<TestModel>().Where(x => !x.Active);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Active" && q1.MemberType == QueryType.ValueBoolean && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == QueryType.ValueBoolean);
        }

        [TestMethod]
        public void IsTrue()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Active);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Active" && q1.MemberType == QueryType.ValueBoolean && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == QueryType.ValueBoolean);
        }
    }
}
