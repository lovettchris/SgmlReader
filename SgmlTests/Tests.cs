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

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using NUnit.Framework;
using Sgml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Linq;
namespace SgmlTests
{

    [TestFixture]
    public partial class Tests {

        //--- Methods ---

        [Test]
        public void T01_Convert_attribute_without_value() {
            Test("01.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T02_Recover_from_attribute_with_missing_closing_quote_before_closing_tag_char() {
            Test("02.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T03_Recover_from_attribute_with_missing_closing_quote_before_opening_tag_char() {
            Test("03.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T04_Recover_from_text_with_wrong_entities_or_entities_with_missing_trailing_semicolon() {
            Test("04.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T05_Read_text_with_32bit_numeric_entity() {
            Test("05.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T06_Read_text_from_ms_office() {
            Test("06.test", XmlRender.Passthrough, CaseFolding.None, "html", true);
        }

        [Test]
        public void T06_Case_folding_none()
        {
            Test("10.test", XmlRender.Passthrough, CaseFolding.None, "html", false);
        }

        [Test]
        public void T07_Recover_from_attribute_with_nested_quotes() {
            Test("07.test", XmlRender.Passthrough, CaseFolding.ToUpper, "html", true);
        }

        [Test]
        public void T08_Allow_CData_section_with_xml_chars() {
            Test("08.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T09_Convert_tags_to_lower() {
            Test("09.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T10_Test_whitespace_aware_processing() {
            Test("10.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", false);
        }

        [Test]
        public void T11_Recover_from_attribute_value_with_extra_quote() {
            Test("11.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T12_Recover_from_unclosed_xml_comment() {
            Test("12.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T13_Allow_xml_only_apos_entity_in_html() {
            Test("13.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T14_Recover_from_script_tag_as_root_element() {
            Test("14.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T15_Read_namespaced_attributes_with_missing_namespace_declaration() {
            Test("15.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T16_Decode_entity() {
            Test("16.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T17_Convert_element_with_illegal_tag_name() {
            Test("17.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T18_Strip_comments_in_CData_section() {
            Test("18.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T19_Nest_contents_of_style_element_into_a_CData_section() {
            Test("19.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T20_Dont_push_elements_out_of_the_body_element_even_when_illegal_inside_body() {
            Test("20.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T21_Clone_document_with_invalid_attribute_declarations() {
            Test("21.test", XmlRender.DocClone, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T22_Ignore_conditional_comments() {
            Test("22.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T23_Preserve_explicit_and_implicit_attribute_and_element_namespaces() {
            Test("23.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T24_Preserve_explicit_attribute_and_element_namespaces() {
            Test("24.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T25_Clone_document_with_explicit_attribute_and_element_namespaces() {
            Test("25.test", XmlRender.DocClone, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T26_Preserve_explicit_attribute_namespaces() {
            Test("26.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T27_Clone_document_with_explicit_attribute_namespaces_with_clone() {
            Test("27.test", XmlRender.DocClone, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T28_Clone_document_with_explicit_element_namespaces_with_clone() {
            Test("28.test", XmlRender.DocClone, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T29_Read_namespaced_elements_with_missing_namespace_declaration() {
            Test("29.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T30_Clone_document_with_namespaced_elements_with_missing_namespace_declaration_with_clone() {
            Test("30.test", XmlRender.DocClone, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T31_Parse_html_document_without_closing_body_tag() {
            Test("31.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T32_Parse_html_document_with_leading_whitespace_and_missing_closing_tag() {
            Test("32.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T33_Parse_doctype() {
            Test("33.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        //[Test, Ignore("Test conflicts with other behavior"]
        //public void Push_invalid_element_out_of_body_tag_34() {

        //    // NOTE (bjorg): marked as ignore, because it conflicts with another behavior of never pushing elements from the body tag.
        //    Test("34.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        //}

        [Test]
        public void T35_Add_missing_closing_element_tags() {
            Test("35.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T36_Preserve_xml_comments_inside_script_element() {
            Test("36.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T37_Allow_CDData_section_with_markup() {
            Test("37.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T38_Recover_from_rogue_open_tag_char() {
            Test("38.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T39_Ignore_invalid_char_after_tag_name() {
            Test("39.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T40_Convert_entity_to_char_code() {
            Test("40.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T41_Attribute_with_missing_equal_sign_between_key_and_value() {
            Test("41.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T42_Script_element_with_explicit_CDData_section() {
            Test("42.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T43_Convert_tags_to_lower() {
            Test("43.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T44_Load_document() {
            Test("44.test", XmlRender.Doc, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T45_Load_document_with_text_before_root_node() {
            Test("45.test", XmlRender.Doc, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T46_Load_document_with_text_before_root_node() {

            // NOTE (steveb): this is a dup of the previous test
            Test("46.test", XmlRender.Doc, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T47_Load_document_with_xml_comment_before_root_node() {
            Test("47.test", XmlRender.Doc, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T48_Decode_numeric_entities_for_non_html_content() {
            Test("48.test", XmlRender.Passthrough, CaseFolding.ToLower, null, true);
        }

        [Test]
        public void T49_Load_document_with_nested_xml_declaration() {
            Test("49.test", XmlRender.Doc, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T50_Handle_xml_processing_instruction_with_illegal_xml_namespace() {
            Test("50.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T51_Close_elements_with_missing_closing_tags() {
            Test("51.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T52_Clone_document_with_elements_with_missing_closing_tags() {
            Test("52.test", XmlRender.DocClone, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T53_Read_ofx_content() {
            Test("53.test", XmlRender.Passthrough, CaseFolding.None, "OFX", true);
        }

        [Test]
        public void T54_Read_simple_html() {
            Test("54.test", XmlRender.Passthrough, CaseFolding.ToLower, null, true);
        }

        [Test]
        public void T55_Decode_xml_entity() {
            Test("55.test", XmlRender.Passthrough, CaseFolding.ToLower, null, true);
        }

        [Test]
        public void T56_Decode_Surrogate_Pairs()
        {
            Test("56.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T57_Read_html_with_invalid_entity_reference()
        {
            Test("57.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T58_Read_html_with_invalid_entity_reference()
        {
            Test("58.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T59_Read_html_with_invalid_entity_reference()
        {
            Test("59.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T60_Read_html_with_invalid_entity_reference()
        {
            Test("60.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T61_Read_html_with_invalid_surrogate_pairs()
        {
            Test("61.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T62_Handling_newLines_in_text()
        {
            Test("62.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void Test_MoveToNextAttribute()
        {

            // Make sure we can do MoveToElement after reading multiple attributes.
            var r = new SgmlReader {
                InputStream = new StringReader("<test id='10' x='20'><a/><!--comment-->test</test>")
            };
            Assert.IsTrue(r.Read());
            while(r.MoveToNextAttribute()) {
                _log.Debug(r.Name);
            }
            if(r.MoveToElement()) {
                _log.Debug(r.ReadInnerXml());
            }
        }

        [Test]
        public void Test_for_illegal_char_value() 
        {
            const string source = "&test";
            var reader = new SgmlReader {
                DocType = "HTML",
                WhitespaceHandling = WhitespaceHandling.All,
                StripDocType = true,
                InputStream = new StringReader(source),
                CaseFolding = CaseFolding.ToLower
            };

            // test
            var element = System.Xml.Linq.XElement.Load(reader);
            string value = element.Value;
            Assert.IsFalse(string.IsNullOrEmpty(value), "element has no value");
            Assert.AreNotEqual((char)65535, value[value.Length - 1], "unexpected -1 as last char");
        }

        [Test]
        public void Test_fragment_parsing()
        {
            var settings = new XmlReaderSettings {
                ConformanceLevel = ConformanceLevel.Fragment
            };
            var stream = new StringReader("<html><head></head><body></body></html> <script></script>");

            int count = 0;
            var reader = new SgmlReader(settings);
            reader.DocType = "html";
            reader.InputStream = stream;
            reader.CaseFolding = CaseFolding.ToLower;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    XDocument doc = XDocument.Load(reader.ReadSubtree());
                    Debug.WriteLine(doc.ToString());
                    count++;
                }
            }
            Assert.AreEqual(2, count, "Expecing 2 XmlDocuments in the input stream");
        }

        [Test]
        public void Test_XPathDocument()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            StringReader stream = new StringReader("<html><script></script><body><p>Test</html> ");

            SgmlReader reader = new SgmlReader(settings);
            reader.DocType = "html";
            reader.InputStream = stream;
            reader.CaseFolding = CaseFolding.ToLower;

            XPathDocument doc = new XPathDocument(reader);

            string expected = @"<html><head><script></script></head><body><p>Test</p></body></html>";
            string actual = doc.CreateNavigator().OuterXml.Replace("\n", "").Replace("\r", "").Replace(" ", "");
            Assert.AreEqual(expected, actual, "Expecting same XML document");
        }

        [Test]
        public void Test_XPathDocument_NameTable()
        {
            NameTable nameTable = new NameTable();
            SgmlReader sgmlReader = new SgmlReader(nameTable)
            {
                InputStream = new StringReader("<html><body>abcd</body></html>"),
                DocType = "HTML",
                CaseFolding = CaseFolding.ToLower
            };
            XPathDocument xpathDocument = new XPathDocument(sgmlReader);
            XPathNavigator xpathNavigator = xpathDocument.CreateNavigator();

            // test that the navigator works properly.
            Assert.AreEqual("html", nameTable.Get("html"), "Expecting 'html' name in nametable");
            Assert.AreEqual("abcd", xpathNavigator.Evaluate("string(/html/body)"), "Expecting '/html/body' to be evaluated correctly.");

        }

        /// <summary>This is a unit-test for the <see cref="SgmlReader.TextWhitespace"/> property's logic.</summary>
        [Test]
        public void Invalid_TextWhitespace_enum_values_should_be_dropped()
        {
            SgmlReader sgmlReader = new SgmlReader()
            {
                TextWhitespace = (TextWhitespaceHandling)0xFF
            };

            Assert.AreEqual(TextWhitespaceHandling.TrimBoth | TextWhitespaceHandling.OnlyLineBreaks, sgmlReader.TextWhitespace, "The " + nameof(sgmlReader.TextWhitespace) + " property should only respect defined flags bits.");
        }

        /// <summary>https://github.com/lovettchris/SgmlReader/issues/15</summary>
        [Test]
        public void T63_DoBradescoTrailingWhitespaceTest()
        {
            // 63.test is originally "bradesco.ofx" from github.com/kevencarneiro/OFXSharp, used under the MIT license.
            string bradescoOfxText = Tests.ReadTestResource(name: "63.test");
            int indexOfOfxStart = bradescoOfxText.IndexOf("<OFX>");
            bradescoOfxText = bradescoOfxText.Substring(startIndex: indexOfOfxStart); // skip past non-SGML OFX header.

            XmlDocument xmlDocument;
            using (StringReader stringReader = new StringReader(bradescoOfxText))
            {
                SgmlReader sgmlReader = new SgmlReader()
                {
                    InputStream        = stringReader,
                    WhitespaceHandling = WhitespaceHandling.None,
                    TextWhitespace     = TextWhitespaceHandling.OnlyTrailingLineBreaks,
                    DocType            = "OFX",
                    SystemLiteral      = "ofx160.dtd",
                    Resolver           = new TestEntityResolver()
                };

                Assert.AreEqual(TextWhitespaceHandling.OnlyTrailingLineBreaks, sgmlReader.TextWhitespace, "The " + nameof(sgmlReader.TextWhitespace) + " property should persist this valid value.");

                xmlDocument = new XmlDocument();
                using (XmlWriter xmlWriter = xmlDocument.CreateNavigator().AppendChild())
                {
                    while (!sgmlReader.EOF)
                    {
                        xmlWriter.WriteNode(sgmlReader, defattr: true);
                    }
                }
            }

            // Assert:
            XmlNode codeNode = xmlDocument.SelectSingleNode("//*[local-name()='CODE']");
            XmlNodeList memoNodes = xmlDocument.SelectNodes("//*[local-name()='MEMO']");

            List<XmlNode> selectedNodes = new List<XmlNode>();
            selectedNodes.Add(codeNode);
            selectedNodes.AddRange(memoNodes.Cast<XmlNode>());

            foreach (XmlNode node in selectedNodes)
            {
                string innerText = codeNode.InnerText;
                bool hasTrailingLineBreak = innerText.EndsWith("\n") || innerText.EndsWith("\r") || innerText.EndsWith(Environment.NewLine);

                Assert.IsFalse(hasTrailingLineBreak, message: "There should be no trailing line-breaks in <" + node.Name + "> elements' innerText.");
            }
        }

        [Test]
        public void T64_Handling_DOCTYPE()
        {
            Test("64.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T65_TagMinimizationTest2()
        {
            // http://sgmljs.net/docs/tag-minimization-examples.html
            Test("65.test", XmlRender.Passthrough, CaseFolding.ToLower, null, true);
        }

        [Test]
        public void T66_HTML_Optional_Start_Tags()
        {
            Test("66.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T67_HTML_Optional_Start_Tags_Wrapping_Text()
        {
            Test("67.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T68_TagMinimizationTest6a()
        {
            // http://sgmljs.net/docs/tag-minimization-examples.html
            Test("68.test", XmlRender.Passthrough, CaseFolding.ToLower, null, true);
        }

        [Test]
        public void T69_TagMinimizationTest7a()
        {
            // http://sgmljs.net/docs/tag-minimization-examples.html
            // BUGBUG: this test should inject an <f/> tag to make the content model valid.
            Test("69.test", XmlRender.Passthrough, CaseFolding.ToLower, null, true);
        }

        [Test]
        public void T70_TagMinimizationOmitTagPCData1()
        {
            // http://sgmljs.net/docs/tag-minimization-examples.html
            Test("70.test", XmlRender.Passthrough, CaseFolding.ToLower, null, true);
        }

        [Test]
        public void T71_TagMinimizationOmitTagPCData2()
        {
            // http://sgmljs.net/docs/tag-minimization-examples.html
            Test("71.test", XmlRender.Passthrough, CaseFolding.ToLower, null, true);
        }

        [Test]
        public void T72_TagMinimizationOmitTagPCData3()
        {
            // http://sgmljs.net/docs/tag-minimization-examples.html
            //  tests that content model ending with PCDATA(always optional) gets
            // closed when infering omitted tags
            Test("72.test", XmlRender.Passthrough, CaseFolding.ToLower, null, true);
        }

        [Test]
        public void T73_InclusionTest1()
        {
            Test("73.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        [Test]
        public void T74_ExclusionTest1()
        {
            // FORM is not allowed inside another FORM by exclusion, so the previous
            // FORM element should be autoclosed.
            Test("74.test", XmlRender.Passthrough, CaseFolding.ToLower, "html", true);
        }

        /// <summary>https://github.com/lovettchris/SgmlReader/issues/47</summary>
        [Test]
        public void T75_TestDtdParser()
        {
            XmlDocument xmlDocument;
            using (StringReader stringReader = new StringReader("<test>&foo;</test>"))
            {
                SgmlReader sgmlReader = new SgmlReader()
                {
                    InputStream = stringReader,
                    TextWhitespace = TextWhitespaceHandling.OnlyTrailingLineBreaks,
                    Dtd = SgmlDtd.Parse(null, "foo", null, "<!ENTITY foo 'bar'>", null),
                    DocType = "foo",
                    WhitespaceHandling = WhitespaceHandling.Significant,
                };                

                xmlDocument = new XmlDocument();
                using (XmlWriter xmlWriter = xmlDocument.CreateNavigator().AppendChild())
                {
                    while (!sgmlReader.EOF)
                    {
                        xmlWriter.WriteNode(sgmlReader, defattr: true);
                    }
                }

                var expected = "<test>bar</test>";
                var result = xmlDocument.OuterXml;
                Assert.AreEqual(expected, result);
            }
        }

        [Test]
        public void Test_LoadWikipedia()
        {
            using (var reader = new SgmlReader()
            {
                DocType = "html",
                PublicIdentifier = "-//W3C//DTD XHTML 1.0 Transitional//EN",
                SystemLiteral = "http://www.w3.org/TR/html4/loose.dtd",
                Href = "wikipedia.html",
                Resolver = new TestEntityResolver()
            })
            {
                var doc = XDocument.Load(reader);
            }
        }
    }

}