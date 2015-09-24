using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrettyBots.Strategies;
using PrettyBots.Strategies.Repository;

namespace UnitTestProject1
{
    [TestClass]
    public class DataModelTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var repos = new PrimaryRepository();
            var session = repos.GetSession("primary");
            Trace.WriteLine(session.Name);
            repos.Dispose();
        }
    }
}
