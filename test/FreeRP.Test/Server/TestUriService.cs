using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Test.Server
{
    [TestClass]
    public class TestUriService
    {
        private readonly Net.Server.Data.FrpSettings _frpSettings;
        private readonly Net.Server.Data.UriService _uriService;

        public TestUriService()
        {
            _frpSettings = Net.Server.Data.FrpSettings.Create(AppDomain.CurrentDomain.BaseDirectory);
            _uriService = new(_frpSettings);
        }

        [TestMethod]
        public void Test()
        {
            string uri = "";
            Assert.IsNull(_uriService.GetUri(uri));

            uri = "f://";
            Assert.IsNull(_uriService.GetUri(uri));

            uri = "file://";
            Assert.IsTrue(_uriService.GetUri(uri)?.OriginalString == "file:///");

            uri = "file:///";
            Assert.IsTrue(_uriService.GetUri(uri)?.OriginalString == "file:///");

            Assert.IsTrue(_uriService.Combine("foo", "bar") == "foo/bar");
            Assert.IsTrue(_uriService.Combine("foo/", "bar") == "foo/bar");
            Assert.IsTrue(_uriService.Combine("foo/", "/bar") == "foo/bar");
        }
    }
}
