using System;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace UnitTestProject1
{
    [TestClass]
    public class LibUnitTest2
    {
        //[TestMethod]
        public void TestMethod1()
        {
            var doc = new HtmlDocument();
            doc.LoadHtml("<html><body></body></html>");
            var body = doc.DocumentNode.SelectSingleNode("/html/body");
            Trace.WriteLine(body);
            body.Attributes.Append("attr", "asas\"dfsd'jhj");
            Trace.WriteLine(body.OuterHtml);
        }
    }
}
