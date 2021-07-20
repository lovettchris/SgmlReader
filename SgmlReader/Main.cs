/*
 * Copyright (c) 2020 Microsoft Corporation. All rights reserved.
 * Modified work Copyright (c) 2008 MindTouch. All rights reserved. 
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace Sgml
{
    /// <summary>
    /// This class provides a command line interface to the SgmlReader.
    /// </summary>
    public class CommandLine
    {
        private string proxy = null;
        private string output = null;
        private bool formatted = false;
        private bool noUtf8Bom = false;
        private bool noxmldecl = false;
        private Encoding encoding = null;

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                CommandLine t = new CommandLine();
                t.Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            return;
        }

        /// <summary>
        /// Run the SgmlReader command line tool with the given command line arguments.
        /// </summary>
        /// <param name="args"></param>
        public void Run(string[] args)
        {
            SgmlReader reader = new SgmlReader();
            string inputUri = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1))
                    {
                        case "e":
                            string errorlog = args[++i];
                            if ("$stderr".Equals(errorlog, StringComparison.OrdinalIgnoreCase))
                            {
                                reader.ErrorLog = Console.Error;
                            }
                            else
                            {
                                reader.ErrorLog = new StreamWriter(errorlog);
                            }
                            break;
                        case "html":
                            reader.DocType = "HTML";
                            break;
                        case "dtd":
                            reader.SystemLiteral = args[++i];
                            break;
                        case "proxy":
                            proxy = args[++i];
                            reader.WebProxy = new WebProxy(proxy);
                            break;
                        case "encoding":
                            encoding = Encoding.GetEncoding(args[++i]);
                            break;
                        case "nobom":
                            noUtf8Bom = true;
                            break;
                        case "f":
                            formatted = true;
                            reader.WhitespaceHandling = WhitespaceHandling.None;
                            break;
                        case "trimtext":
                            reader.TextWhitespace = TextWhitespaceHandling.TrimBoth;
                            break;
                        case "noxml":
                            noxmldecl = true;
                            break;
                        case "doctype":
                            reader.StripDocType = false;
                            break;
                        case "lower":
                            reader.CaseFolding = CaseFolding.ToLower;
                            break;
                        case "upper":
                            reader.CaseFolding = CaseFolding.ToUpper;
                            break;

                        default:
                            string exeName = Environment.GetCommandLineArgs()[0];
                            string exeVersion = typeof(CommandLine).Assembly.GetName().Version?.ToString();
                            Console.WriteLine("{0} - version {1}", exeName, exeVersion);
                            Console.WriteLine("  https://github.com/lovettchris/SgmlReader");
                            Console.WriteLine();
                            Console.WriteLine("Usage: {0} <options> [InputUri] [OutputFile]", exeName);
                            Console.WriteLine();
                            Console.WriteLine("<options>:");
                            Console.WriteLine("  -help          Prints this list of command-line options");
                            Console.WriteLine("  -e log         Optional log file name, name of '$STDERR' will write errors to stderr");
                            Console.WriteLine("  -f             Whether to pretty print the output.");
                            Console.WriteLine("  -html          Specify the built in HTML dtd");
                            Console.WriteLine("  -dtd url       Specify other SGML dtd to use");
                            Console.WriteLine("  -base          Add base tag to output HTML");
                            Console.WriteLine("  -noxml         Do not add XML declaration to the output");
                            Console.WriteLine("  -proxy svr:80  Proxy server to use for http requests");
                            Console.WriteLine("  -encoding name Specify an encoding for the output file (default UTF-8)");
                            Console.WriteLine("  -nobom         Prevents output of the BOM when using UTF-8");
                            Console.WriteLine("  -f             Produce indented formatted output");
                            Console.WriteLine("  -trimtext      SGML `#text` nodes will be trimmed of outer whitespace");
                            Console.WriteLine("  -lower         Convert input tags to lower case");
                            Console.WriteLine("  -upper         Convert input tags to UPPER CASE");
                            Console.WriteLine();
                            Console.WriteLine("  InputUri       The input file or http URL (defaults to stdin if not specified)");
                            Console.WriteLine("                 Supports wildcards for local file names.");
                            Console.WriteLine("  OutputFile     Output file name (defaults to stdout if not specified)");
                            Console.WriteLine("                 If input file contains wildcards then this just specifies the output file extension (default .xml)");
                            return;
                    }
                }
                else
                {
                    if (inputUri == null)
                    {
                        inputUri = arg;
                        string ext = Path.GetExtension(arg).ToLower();
                        if (ext == ".htm" || ext == ".html")
                        {
                            reader.DocType = "HTML";
                        }
                    }
                    else if (output == null)
                    {
                         output = arg;
                    }
                }
            }
            
            if (inputUri != null && !inputUri.StartsWith("http://") && inputUri.IndexOfAny(new char[] { '*', '?' }) >= 0)
            {
                // wild card processing of a directory of files.
                string path = Path.GetDirectoryName(inputUri);
                if (path == "") path = ".\\";
                string ext = ".xml";
                if (output != null) ext = Path.GetExtension(output);
                    
                foreach (string uri in Directory.GetFiles(path, Path.GetFileName(inputUri)))
                {
                    Console.WriteLine("Processing: " + uri);
                    string file = Path.GetFileName(uri);
                    output = Path.GetDirectoryName(uri) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(file) + ext;
                    Process(reader, uri);
                    reader.Close();
                }
                return;
            }

            Process(reader, inputUri);
            reader.Close();

            return;
        }

        private void Process(SgmlReader reader, string uri)
        {
            if (uri == null)
            {
                reader.InputStream = Console.In;
            }
            else
            {
                reader.Href = uri;
            }

            encoding ??= reader.GetEncoding();
            if (noUtf8Bom && encoding.Equals(Encoding.UTF8))
            {
                encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            }

            XmlTextWriter w = output != null
                ? new XmlTextWriter(output, encoding)
                : new XmlTextWriter(Console.Out);

            using (w)
            {
                if (formatted)
                {
                     w.Formatting = Formatting.Indented;
                }
                if (!noxmldecl)
                {
                    w.WriteStartDocument();
                }
                reader.Read();
                while (!reader.EOF)
                {
                    w.WriteNode(reader, true);
                }
                w.Flush();
            }
        }



    }
}
