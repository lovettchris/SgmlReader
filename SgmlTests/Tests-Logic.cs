/*
 * Original work Copyright (c) 2008 MindTouch. All rights reserved. 
 * Modified Work Copyright (c) 2016 Microsoft Corporation. All rights reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

using log4net;
using NUnit.Framework;
using Sgml;
using SgmlTests;
using System;
using System.IO;
using System.Xml;

namespace SgmlTests
{
    public partial class Tests
    {
        private delegate void XmlReaderTestCallback(XmlReader reader, XmlWriter xmlWriter);

        private enum XmlRender
        {
            Doc,
            DocClone,
            Passthrough
        }

        private static readonly ILog _log = LogManager.GetLogger(typeof(Tests));

        private static void Test(string name, XmlRender xmlRender, CaseFolding caseFolding, string doctype, bool format)
        {
            ReadTest(name, out string source, out string expected);
            expected = expected.Trim().Replace("\r", "");
            string actual;

            // determine how the document should be written back
            XmlReaderTestCallback callback;
            switch (xmlRender)
            {
                case XmlRender.Doc:
                    // test writing sgml reader using xml document load
                    callback = (reader, writer) =>
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(reader);
                        doc.WriteTo(writer);
                    };
                    break;

                case XmlRender.DocClone:
                    // test writing sgml reader using xml document load and clone
                    callback = (reader, writer) =>
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(reader);
                        XmlNode clone = doc.Clone();
                        clone.WriteTo(writer);
                    };
                    break;

                case XmlRender.Passthrough:
                    // test writing sgml reader directly to xml writer
                    callback = (reader, writer) =>
                    {
                        reader.Read();
                        while (!reader.EOF)
                        {
                            writer.WriteNode(reader, true);
                        }
                    };
                    break;

                default:
                    throw new ArgumentException("unknown value", "xmlRender");
            }
            actual = RunTest(caseFolding, doctype, format, source, callback);
            Assert.AreEqual(expected, actual);
        }

        private static void ReadTest(string name, out string before, out string after)
        {
            System.Reflection.Assembly assembly = typeof(Tests).Assembly;

            string[] test = ReadTestResource(name).Split('`');
            before = test[0];
            after = test.Length > 1 ? test[1] : "";
        }

        /// <summary></summary>
        /// <param name="name">The value provided Will be appended to &quot;<c>SgmlTests[Core].Resources.</c>&quot; to form the full manifest resource stream name.</param>
        /// <returns></returns>
        internal static string ReadTestResource(string name)
        {
            System.Reflection.Assembly assembly = typeof(Tests).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(assembly.FullName.Split(',')[0] + ".Resources." + name))
            {
                if (stream is null) throw new ArgumentException("unable to load requested resource: " + name);
                using (StreamReader sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private static string RunTest(CaseFolding caseFolding, string doctype, bool format, string source, XmlReaderTestCallback callback)
        {

            // initialize sgml reader
            SgmlReader sgmlReader = new SgmlReader
            {
                CaseFolding = caseFolding,
                DocType = doctype,
                InputStream = new StringReader(source),
                WhitespaceHandling = format ? WhitespaceHandling.None : WhitespaceHandling.All,
                Resolver = new TestEntityResolver()
            };

            if (doctype == "OFX")
            {
                sgmlReader.SystemLiteral = "ofx160.dtd";
            }

            // check if we need to use the LoggingXmlReader
            XmlReader reader = sgmlReader;
#if DEBUG
            {
                reader = new LoggingXmlReader(sgmlReader, Console.Out);
            }
#endif

            // initialize xml writer
            StringWriter stringWriter = new StringWriter();
            XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
            if (format)
            {
                xmlTextWriter.Formatting = Formatting.Indented;
            }
            callback(reader, xmlTextWriter);
            xmlTextWriter.Close();

            // reproduce the parsed document
            var actual = stringWriter.ToString();

            // ensure that output can be parsed again
            try
            {
                using (StringReader stringReader = new StringReader(actual))
                {
                    var doc = new XmlDocument();
                    doc.Load(stringReader);
                }
            }
            catch (Exception)
            {
                Assert.Fail("unable to parse " + nameof(SgmlReader) + " output:\n{0}", actual);
            }

            return actual.Trim().Replace("\r", "");
        }
    }
}
