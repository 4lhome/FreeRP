extern alias fx;
using fx::FreeRP.GrpcService.Database;
using fx::FreeRP.Net.Client.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Test.Client.LinqMapper
{
    [TestClass]
    public class StringQuery
    {
        public class TestModel
        {
            public string Name { get; set; } = "FooBar";
        }

        [TestMethod]
        public void AndAlso()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name == "a" && x.Name.Length == 1);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 4);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);
            var q4 = qc1.ElementAt(3);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Name" && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "a" && q2.ValueType == QueryType.ValueString && q2.Next == QueryType.QueryAndAlso);

            Assert.IsTrue(
                q3.IsMember && q3.Name == "$.Name" && q3.MemberType == QueryType.ValueString &&
                q3.CallType == QueryType.CallCount && q3.Next == QueryType.QueryEqual);
            Assert.IsTrue(q4.Value == "1" && q4.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void OrElse()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name == "a" || x.Name.Length == 1);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 4);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);
            var q4 = qc1.ElementAt(3);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Name" && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "a" && q2.ValueType == QueryType.ValueString && q2.Next == QueryType.QueryOrElse);

            Assert.IsTrue(
                q3.IsMember && q3.Name == "$.Name" && q3.MemberType == QueryType.ValueString &&
                q3.CallType == QueryType.CallCount && q3.Next == QueryType.QueryEqual);
            Assert.IsTrue(q4.Value == "1" && q4.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void Contains()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.Contains("ab"));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 1);

            var q1 = qc1.ElementAt(0);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallContains && q1.Value == "ab" && q1.ValueType == QueryType.ValueString);
        }

        [TestMethod]
        public void ContainsIsFalse()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.Contains("ab") == false);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallContains && q1.Value == "ab" && q1.ValueType == QueryType.ValueString);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == QueryType.ValueBoolean);
        }

        [TestMethod]
        public void ContainsIsTrue()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.Contains("ab") == true);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallContains && q1.Value == "ab" && q1.ValueType == QueryType.ValueString);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == QueryType.ValueBoolean);
        }

        [TestMethod]
        public void Count()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.Count() == 5);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallCount && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "5" && q2.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void EndsWith()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.EndsWith("ab"));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 1);

            var q1 = qc1.ElementAt(0);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallEndsWith && q1.Value == "ab" && q1.ValueType == QueryType.ValueString);
        }

        [TestMethod]
        public void EqualNot()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name != "a");
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.Name" && q1.Next == QueryType.QueryNotEqual);
            Assert.IsTrue(q2.Value == "a" && q2.ValueType == QueryType.ValueString);
        }

        [TestMethod]
        public void Equals()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.Equals("a") == false);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallEquals && q1.Value == "a" && q1.ValueType == QueryType.ValueString && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == QueryType.ValueBoolean);
        }

        [TestMethod]
        public void Length()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.Length == 5);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallCount && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "5" && q2.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void IndexOf()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.IndexOf("a") == 1);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString && q1.Value == "a" &&
                q1.CallType == QueryType.CallIndexOf && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "1" && q2.ValueType == QueryType.ValueNumber);
        }

        [TestMethod]
        public void IsNullOrEmpty()
        {
            var c1 = new Queryable<TestModel>().Where(x => string.IsNullOrEmpty(x.Name));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 1);

            var q1 = qc1.ElementAt(0);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallIsNullOrEmpty);
        }

        [TestMethod]
        public void IsNullOrEmptyIsTrue()
        {
            var c1 = new Queryable<TestModel>().Where(x => string.IsNullOrEmpty(x.Name) == true);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallIsNullOrEmpty && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == QueryType.ValueBoolean);
        }

        [TestMethod]
        public void IsNullOrEmptyIsFalse()
        {
            var c1 = new Queryable<TestModel>().Where(x => string.IsNullOrEmpty(x.Name) == false);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallIsNullOrEmpty && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == QueryType.ValueBoolean);
        }

        [TestMethod]
        public void StartWith()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.StartsWith("ab"));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 1);

            var q1 = qc1.ElementAt(0);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallStartWith && q1.Value == "ab" && q1.ValueType == QueryType.ValueString);
        }

        [TestMethod]
        public void ToLower()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.ToLower() == "foobar");
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallToLower && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "foobar" && q2.ValueType == QueryType.ValueString);
        }

        [TestMethod]
        public void ToUpper()
        {
            var c1 = new Queryable<TestModel>().Where(x => x.Name.ToUpper() == "FOOBAR");
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.Name" && q1.MemberType == QueryType.ValueString &&
                q1.CallType == QueryType.CallToUpper && q1.Next == QueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "FOOBAR" && q2.ValueType == QueryType.ValueString);
        }
    }
}
