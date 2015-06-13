using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrettyBots.Monitor.DataModel;

namespace UnitTestProject1
{
    [TestClass]
    public class DataModelTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var container = new PbPrimaryContainer();
            foreach (var item in container.AccountSet)
                Trace.WriteLine(item);
            Trace.WriteLine("");
            foreach (var item in container.LoggingSet)
                Trace.WriteLine(item);
        }
    }
}
