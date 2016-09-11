using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Sgml;
using System.Xml;
using System.IO;
using System.Xml.Linq;

namespace UnitTestUniversal
{
    [TestClass]
    public class UniversalTests
    {
        [TestMethod]
        public void SimpleUniversalTest()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            StringReader stream = new StringReader("<html><head></head><script></script><body><p><b>Test</b><br>more stuff</html> ");

            SgmlReader reader = new SgmlReader(settings);
            reader.DocType = "Html";
            reader.InputStream = stream;

            XDocument doc = XDocument.Load(reader);

            string expected = @"<html><head></head><script></script><body><p><b>Test</b><br />more stuff</p></body></html>";
            string actual = doc.ToString(SaveOptions.DisableFormatting).Trim();
            Assert.AreEqual(expected, actual, "Expecing same XML document");
        }
    }
}
