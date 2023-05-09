using NUnit.Framework;
using Sgml;
using System.Xml.Linq;

public class Tests
{
    [Test]
    public void LoadWikipedia()
    {
        using var reader = new SgmlReader
        {
            DocType = "html",
            PublicIdentifier = "-//W3C//DTD XHTML 1.0 Transitional//EN",
            SystemLiteral = "http://www.w3.org/TR/html4/loose.dtd",
            Href = "https://www.wikipedia.org/",
        };

        var doc = XDocument.Load(reader);
    }
}
