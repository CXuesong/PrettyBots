using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrettyBots.Strategies.Repository;

namespace UnitTestProject1
{
    [TestClass]
    public class DataModelTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var context = new PrimaryRepository();
            foreach (var item in context.Accounts.GetAccounts())
                Trace.WriteLine(item);
            Trace.WriteLine("");
            foreach (var item in context.Loggings.GetLogs())
                Trace.WriteLine(item);
        }
    }
}
