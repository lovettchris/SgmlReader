/*
 * Copyright (c) 2020 Microsoft Corporation. All rights reserved.
 * Modified work Copyright (c) 2008 MindTouch. All rights reserved. 
 * s
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace Sgml
{
    /// <summary>
    /// SGML is case insensitive, so here you can choose between converting
    /// to lower case or upper case tags.  "None" means that the case is left
    /// alone, except that end tags will be folded to match the start tags.
    /// </summary>
    public enum CaseFolding
    {
        /// <summary>
        /// Do not convert case, except for converting end tags to match start tags.
        /// </summary>
        None,

        /// <summary>
        /// Convert tags to upper case.
        /// </summary>
        ToUpper,

        /// <summary>
        /// Convert tags to lower case.
        /// </summary>
        ToLower
    }

    /// <summary>
    /// Due to the nature of adjacent self-closing and auto-closing SGML elements the <see cref="SgmlReader"/>'s default behavior may cause unwanted whitespace to appear at either end of the element's text content. This enum instructs <see cref="SgmlReader"/> that such whitespace is not significant-whitespace.<br />
    /// Note that this is a <see cref="FlagsAttribute"/>-enum.
    /// </summary>
    /// <remarks>Internally strings are not actually trimmed using <see cref="String.Trim(char[])"/>, but instead by skipping leading and trailing whitespace during the read-phase - this avoids unnecessary string copying and heap-allocation.</remarks>
    [Flags]
    public enum TextWhitespaceHandling
    {
        /// <summary>
        /// <c>0x00</c> - All leading and trailing whitespace in <c>InnerText</c> and <c>Value</c> properties is kept verbatim. This is the default behavior.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// <c>0x01</c> - Leading whitespace will not be present in <c>TextContent</c> and <c>Value</c> properties.
        /// </summary>
        TrimLeading  = 1,

        /// <summary>
        /// <c>0x02</c> - Trailing whitespace will not be present in <c>TextContent</c> and <c>Value</c> properties.
        /// </summary>
        TrimTrailing = 2,

        /// <summary>
        /// <c>0x04</c> - This flag only takes effect when either or both <see cref="TrimLeading"/> and <see cref="TrimTrailing"/> are set. When this flag is set, only line-break characters <c>\r</c> (CR) and <c>\n</c> (LF) will be trimmed.
        /// </summary>
        OnlyLineBreaks = 4,

        /// <summary>
        /// <c>0x03</c> - (This is a combination of flags). Neither leading nor trailing whitespace will be be present in <c>TextContent</c> and <c>Value</c> properties.
        /// </summary>
        TrimBoth  = TrimLeading | TrimTrailing,

        /// <summary>
        /// <c>0x06</c> - (This is a combination of flags). Only trailing <c>\r</c> (CR) and <c>\n</c> (LF) characters will be trimmed from trailing whitespace in in <c>InnerText</c> and <c>Value</c> properties.
        /// </summary>
        OnlyTrailingLineBreaks = OnlyLineBreaks | TrimTrailing
    }

    internal static class EnumExtensions
    {
        // Bitwise operators on enums to check for flags are much faster than Enum.HasFlag(), unfortunately.

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool HasFlagBits(this TextWhitespaceHandling value, TextWhitespaceHandling bits) => (value & bits) == bits;
    }

    /// <summary>
    /// This stack maintains a high water mark for allocated objects so the client
    /// can reuse the objects in the stack to reduce memory allocations, this is
    /// used to maintain current state of the parser for element stack, and attributes
    /// in each element.
    /// </summary>
    internal sealed class HWStack<T>
        where T: class
    {
        private T[] _items;
        private int _size;
        private int _count;
        private readonly int _growth;

        /// <summary>
        /// Initialises a new instance of the HWStack class.
        /// </summary>
        /// <param name="growth">The amount to grow the stack space by, if more space is needed on the stack.</param>
        public HWStack(int growth)
        {
            _growth = growth;
        }

        /// <summary>
        /// The number of items currently in the stack.
        /// </summary>
        public int Count
        {
            get => _count;
            set => _count = value;
        }

        /// <summary>
        /// The size (capacity) of the stack.
        /// </summary>
        public int Size => _size;

        /// <summary>
        /// Returns the item at the requested index or null if index is out of bounds
        /// </summary>
        /// <param name="i">The index of the item to retrieve.</param>
        /// <returns>The item at the requested index or null if index is out of bounds.</returns>
        public T this[int i]
        {
            get => (i >= 0 && i < _size) ? _items[i] : null;
            set => _items[i] = value;
        }

        /// <summary>
        /// Removes and returns the item at the top of the stack
        /// </summary>
        /// <returns>The item at the top of the stack.</returns>
        public T Pop()
        {
            _count--;
            if (_count > 0)
            {
                return _items[_count - 1];
            }

            return default(T);
        }

        /// <summary>
        /// Pushes a new slot at the top of the stack.
        /// </summary>
        /// <returns>The object at the top of the stack.</returns>
        /// <remarks>
        /// This method tries to reuse a slot, if it returns null then
        /// the user has to call the other Push method.
        /// </remarks>
        public T Push()
        {
            if (_count == _size)
            {
                int newsize = _size + _growth;
                T[] newarray = new T[newsize];
                if (_items != null)
                    Array.Copy(_items, newarray, _size);

                _size = newsize;
                _items = newarray;
            }
            return _items[_count++];
        }

        public T Top
        {
            get
            {
                if (_count - 1 >= 0)
                {
                    return _items[_count - 1];
                }
                return default(T);
            }
            set
            {
                if (_count - 1 >= 0)
                {
                    _items[_count - 1] = value;
                }
                else
                {
                    throw new Exception("Stack is empty");
                }
            }
        }

        /// <summary>
        /// Remove a specific item from the stack.
        /// </summary>
        /// <param name="i">The index of the item to remove.</param>
        public void RemoveAt(int i)
        {
            _items[i] = default(T);
            Array.Copy(_items, i + 1, _items, i, _count - i - 1);
            _count--;
        }
    }

    /// <summary>
    /// This class represents an attribute.  The AttDef is assigned
    /// from a validation process, and is used to provide default values.
    /// </summary>
    internal class Attribute
    {
        internal string Name;    // the atomized name.
        internal AttDef DtdType; // the AttDef of the attribute from the SGML DTD.
        internal char QuoteChar; // the quote character used for the attribute value.
        private string _literalValue; // the attribute value

        /// <summary>
        /// Attribute objects are reused during parsing to reduce memory allocations, 
        /// hence the Reset method.
        /// </summary>
        public void Reset(string name, string value, char quote)
        {
            Name = name;
            _literalValue = value;
            QuoteChar = quote;
            DtdType = null;
        }

        public string Value
        {
            get
            {
                if (_literalValue != null) 
                    return _literalValue;
                if (DtdType != null) 
                    return DtdType.Default;
                return null;
            }
/*            set
            {
                this.m_literalValue = value;
            }*/
        }

        public bool IsDefault => _literalValue is null;
    }    

    /// <summary>
    /// This class models an XML node, an array of elements in scope is maintained while parsing
    /// for validation purposes, and these Node objects are reused to reduce object allocation,
    /// hence the reset method.  
    /// </summary>
    [DebuggerDisplay("Sgml.Node, Type: {NodeType}, Name: {Name}, Value: {Value}")]
    internal sealed class Node
    {
        internal XmlNodeType NodeType;
        internal string Value;
        internal XmlSpace Space;
        internal string XmlLang;
        internal bool IsEmpty;        
        internal string Name;
        internal ElementDecl DtdType; // the DTD type found via validation
        internal State CurrentState;
        internal bool Simulated; // tag was injected into result stream.
        internal HashSet<string> Included;
        internal HashSet<string> Excluded;
        private readonly HWStack<Attribute> _attributes = new (10);

        /// <summary>
        /// Attribute objects are reused during parsing to reduce memory allocations, 
        /// hence the Reset method. 
        /// </summary>
        public void Reset(string name, XmlNodeType nt, string value, ElementDecl e) 
        {           
            Value = value;
            Name = name;
            NodeType = nt;
            Space = XmlSpace.None;
            XmlLang= null;
            IsEmpty = true;
            _attributes.Count = 0;
            DtdType = e;
            if (e != null)
            {
                IsEmpty = (e.ContentModel.DeclaredContent == DeclaredContent.EMPTY);
            } 
            else if (nt == XmlNodeType.Element)
            {
                IsEmpty = false;
            }
        }

        public Attribute AddAttribute(string name, string value, char quotechar, bool caseInsensitive) 
        {
            Attribute a;
            // check for duplicates!
            for (int i = 0, n = _attributes.Count; i < n; i++)
            {
                a = _attributes[i];
                if (string.Equals(a.Name, name, caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    return null;
                }
            }
            // This code makes use of the high water mark for attribute objects,
            // and reuses exisint Attribute objects to avoid memory allocation.
            a = _attributes.Push();
            if (a is null)
            {
                a = new Attribute();
                _attributes[_attributes.Count-1] = a;
            }
            a.Reset(name, value, quotechar);
            return a;
        }

        public void RemoveAttribute(string name, bool caseInsensitive)
        {
            for (int i = 0, n = _attributes.Count; i < n; i++)
            {
                Attribute a  = _attributes[i];
                if (string.Equals(a.Name, name, caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    _attributes.RemoveAt(i);
                    return;
                }
            }
        }
        public void CopyAttributes(Node n) 
        {
            for (int i = 0, len = n._attributes.Count; i < len; i++)
            {
                Attribute a = n._attributes[i];
                Attribute na = this.AddAttribute(a.Name, a.Value, a.QuoteChar, false);
                na.DtdType = a.DtdType;
            }
        }

        public int AttributeCount => _attributes.Count;

        public int GetAttribute(string name, bool caseInsensitive) 
        {
            for (int i = 0, n = _attributes.Count; i < n; i++) 
            {
                Attribute a = _attributes[i];
                if (string.Equals(a.Name, name, caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) 
                {
                    return i;
                }
            }
            return -1;
        }

        public Attribute GetAttribute(int i) 
        {
            if (i>=0 && i<_attributes.Count)
            {
                Attribute a = _attributes[i];
                return a;
            }
            return null;
        }

        internal bool Excludes(string name)
        {
            if (Excluded == null)
            {
                return false;
            }
            return Excluded.Contains(name);
        }

        internal bool Includes(string name)
        {
            if (Included == null)
            {
                return false;
            }
            return Included.Contains(name);
        }
    }

    internal enum State
    {
        Initial,    // The initial state (Read has not been called yet)
        Markup,     // Expecting text or markup
        EndTag,     // Positioned on an end tag
        Attr,       // Positioned on an attribute
        AttrValue,  // Positioned in an attribute value
        Text,       // Positioned on a Text node.
        PartialTag, // Positioned on a text node, and we have hit a start tag
        AutoClose,  // We are auto-closing tags (this is like State.EndTag), but end tag was generated
        CData,      // We are on a CDATA type node, eg. <scipt> where we have special parsing rules.
        PartialText,
        PseudoStartTag, // we pushed a pseudo-start tag, need to continue with previous start tag.
        ContinueStartTag, // we began parsing a start tag, but had to inject default start tags.
        ContinueTextNode, // we began parsing a text node, but had to inject default start tags.
        Eof
    }

    /// <summary>
    /// SgmlReader is an XmlReader API over any SGML document (including built in 
    /// support for HTML).  
    /// </summary>
    public class SgmlReader : XmlReader
    {
        /// <summary>
        /// The value returned when a namespace is queried and none has been specified.
        /// </summary>
        public const string UNDEFINED_NAMESPACE = "#unknown";

        private XmlReaderSettings _settings;
        private SgmlDtd _dtd;
        private Entity _current;
        private State _state;
        private char _partial;
        private string _endTag;
        private HWStack<Node> _stack;
        private Node _node; // current node (except for attributes)
        // Attributes are handled separately using these members.
        private Attribute _a;
        private int _apos; // which attribute are we positioned on in the collection.
        private Uri _baseUri;
        private StringBuilder _sb;
        private StringBuilder _name;
        private TextWriter _log;
        private bool _foundRoot;
        private bool _ignoreDtd;

        // autoclose support
        private Node _newnode;
        private int _poptodepth;
        private int _rootCount;
        private bool _isHtml;
        private string _rootElementName;

        private string _href;
        private Entity _lastError;
        private TextReader _inputStream;
        private string _syslit;
        private string _pubid;
        private string _subset;
        private string _docType;
        private WhitespaceHandling _whitespaceHandling;
        private CaseFolding _folding = CaseFolding.None;
        bool _caseInsensitive = true;
        IEqualityComparer<string> _comparer = StringComparer.OrdinalIgnoreCase;
        private bool _stripDocType = true;
        //private string m_startTag;
        private readonly Dictionary<string, string> _unknownNamespaces = new ();
        private XmlNameTable _nameTable;
        private IEntityResolver _resolver;


        /// <summary>
        /// Initialises a new instance of the SgmlReader class.
        /// </summary>
        public SgmlReader()
        {
            Init();
        }

        /// <summary>
        /// Initialises a new instance of the SgmlReader class with an existing <see cref="XmlNameTable"/>, which is NOT used.
        /// </summary>
        /// <param name="nt">The nametable to use.</param>
        public SgmlReader(XmlNameTable nt) 
        {
            _nameTable = nt;
            Init();
        }

        /// <summary>
        /// Initialises a new instance of the SgmlReader class.
        /// </summary>
        public SgmlReader(XmlReaderSettings settings)
        {
            _settings = settings;
            _nameTable = settings.NameTable;
            Init();
        }

        /// <summary>
        /// Specify the SgmlDtd object directly.  This allows you to cache the Dtd and share
        /// it across multipl SgmlReaders.  To load a DTD from a URL use the SystemLiteral property.
        /// </summary>
        public SgmlDtd Dtd
        {
            get
            {
                if (_dtd is null)
                {
                    LazyLoadDtd(_baseUri);
                }

                return _dtd; 
            }
            set
            {
                _dtd = value;
            }
        }

        /// <summary>
        /// On portable platforms the mechanism for loading files is different.  For example,
        /// WinRT uses StorageFile, HttpClient and Windows.ApplicationModel.Resources.ResourceLoader.  
        /// So the caller must implement this interface and provide the platform specific mechanism
        /// for finding relative resources (DTDs, external entities, etc).
        /// </summary>
        public IEntityResolver Resolver
        {
            get => _resolver;
            set => _resolver = value;
        }

        private void LazyLoadDtd(Uri baseUri)
        {
            if (_dtd is null && !_ignoreDtd && (!string.IsNullOrEmpty(_syslit) || !string.IsNullOrEmpty(_subset) || StringUtilities.EqualsIgnoreCase(_docType, "html")))
            {
                _dtd = SgmlDtd.Parse(baseUri, _docType, _pubid, _syslit, _subset, _resolver);
            }

            if (_dtd?.Name is string dtdName)
            {
                _rootElementName = this.CaseFolding switch
                {
                    CaseFolding.ToUpper => dtdName.ToUpperInvariant(),
                    CaseFolding.ToLower => dtdName.ToLowerInvariant(),
                    _ => dtdName
                };
                _isHtml = StringUtilities.EqualsIgnoreCase(dtdName, "html");
            }
        }

        /// <summary>
        /// The name of root element specified in the DOCTYPE tag.
        /// </summary>
        public string DocType
        {
            get => _docType;
            set => _docType = value;
        }

        /// <summary>
        /// The root element of the document.
        /// </summary>
        public string RootElementName => _rootElementName;

        /// <summary>
        /// The PUBLIC identifier in the DOCTYPE tag
        /// </summary>
        public string PublicIdentifier
        {
            get => _pubid;
            set => _pubid = value;
        }

        /// <summary>
        /// The SYSTEM literal in the DOCTYPE tag identifying the location of the DTD.
        /// </summary>
        public string SystemLiteral
        {
            get => _syslit;
            set => _syslit = value;
        }

        /// <summary>
        /// The DTD internal subset in the DOCTYPE tag
        /// </summary>
        public string InternalSubset
        {
            get => _subset;
            set => _subset = value;
        }

        /// <summary>
        /// The input stream containing SGML data to parse.
        /// You must specify this property or the Href property before calling Read().
        /// </summary>
        public TextReader InputStream
        {
            get => _inputStream;
            set
            {
                _inputStream = value;
                Init();
            }
        }

#if !WINDOWS_UWP
        /// <summary>
        /// Sometimes you need to specify a proxy server in order to load data via HTTP
        /// from outside the firewall.  For example: "itgproxy:80".
        /// </summary>
        public WebProxy WebProxy
        {
            get
            {
                if (_resolver is null) return null;
                return ((DesktopEntityResolver)_resolver).Proxy;
            }
            set
            {
                if (_resolver is DesktopEntityResolver der)
                {
                    der.Proxy = value;
                }
                else
                {
                    throw new NotSupportedException("Cannot set WebProxy on unknown IEntityResolver (see Resolver property)");
                }
            }
        }
#endif

        /// <summary>
        /// The base Uri is used to resolve relative Uri's like the SystemLiteral and
        /// Href properties.  This is a method because BaseURI is a read-only
        /// property on the base XmlReader class.
        /// </summary>
        public void SetBaseUri(string uri)
        {
            _baseUri = new Uri(uri);
        }

        /// <summary>
        /// Specify the location of the input SGML document as a URL.
        /// </summary>
        public string Href
        {
            get => _href;
            set
            {
                _href = value; 
                Init();
                if (_baseUri is null && !string.IsNullOrWhiteSpace(value))
                {
                    _baseUri = new Uri(_href, UriKind.RelativeOrAbsolute);
                }
            }
        }

        /// <summary>
        /// Whether to strip out the DOCTYPE tag from the output (default true)
        /// </summary>
        public bool StripDocType
        {
            get => _stripDocType;
            set => _stripDocType = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore any DTD reference.
        /// </summary>
        /// <value><c>true</c> if DTD references should be ignored; otherwise, <c>false</c>.</value>
        public bool IgnoreDtd
        {
            get => _ignoreDtd;
            set => _ignoreDtd = value;
        }

        /// <summary>
        /// The case conversion behaviour while processing tags.
        /// </summary>
        public CaseFolding CaseFolding
        {
            get => _folding;
            set => _folding = value;
        }

        /// <summary>
        /// DTD validation errors are written to this stream.
        /// </summary>
        public TextWriter ErrorLog
        {
            get => _log;
            set => _log = value;
        }

        private void Log(string msg, params string[] args)
        {
            if (ErrorLog != null)
            {
                string err = string.Format(CultureInfo.CurrentUICulture, msg, args);
                if (_lastError != _current)
                {
                    err = err + "    " + _current.Context();
                    _lastError = _current;
                    ErrorLog.WriteLine("### Error:" + err);
                }
                else
                {
                    string path = "";
                    if (_current.ResolvedUri != null)
                    {
                        path = _current.ResolvedUri.AbsolutePath;
                    }

                    ErrorLog.WriteLine("### Error in {0}#{1}, line {2}, position {3}: {4}", path, _current.Name, _current.Line, _current.LinePosition, err);
                }
            }
        }

        private void Log(string msg, char ch)
        {
            Log(msg, ch.ToString());
        }

        private void Init()
        {
            _nameTable ??= new NameTable();
            _settings ??= new XmlReaderSettings
            {
                NameTable = _nameTable
            };
        
            _state = State.Initial;
            _stack = new HWStack<Node>(10);
            _node = Push(null, XmlNodeType.Document, null, null);
            _node.IsEmpty = false;
            _sb = new StringBuilder();
            _name = new StringBuilder();
            _poptodepth = 0;
            _current = null;
            _partial = '\0';
            _endTag = null;
            _a = null;
            _apos = 0;
            _newnode = null;
            _rootCount = 0;
            _foundRoot = false;
            _unknownNamespaces.Clear();
            _resolver = new DesktopEntityResolver();
        }

        private Node Push(string name, XmlNodeType nt, string value, ElementDecl e)
        {
            Node previous = _stack.Top;
            Node result = _stack.Push();
            if (result is null)
            {
                result = new Node();
                _stack.Top = result;
            }

            result.Reset(name, nt, value, e);

            if (previous != null)
            {
                result.Included = previous.Included;
                result.Excluded = previous.Excluded;
            }

            if (e != null)
            {
                // Now we have to maintain the inherited list of inclusions and exclusions
                // so that at any given point in the tree we can efficiently find out when
                // a given inclusion or exclusion is in effect.
                if (e.Exclusions != null && e.Exclusions.Length > 0)
                {
                    if (result.Excluded == null)
                    {
                        result.Excluded = new HashSet<string>(e.Exclusions, _comparer);                        
                    }
                    else if (e.Exclusions.Any(i => !result.Excluded.Contains(i)))
                    {
                        result.Excluded = new HashSet<string>(result.Excluded, _comparer); // clone
                        result.Excluded.AddRange(e.Exclusions); // and add.
                    }
                }

                if (e.Inclusions != null && e.Inclusions.Length > 0)
                {
                    if (result.Included == null)
                    {
                        result.Included = new HashSet<string>(e.Inclusions, _comparer);
                    }
                    else if (e.Inclusions.Any(i => !result.Included.Contains(i)))
                    {
                        result.Included = new HashSet<string>(result.Included, _comparer); // clone
                        result.Included.AddRange(e.Inclusions); // and add.
                    }
                }
            }
            
            _node = result;
            return result;
        }

        private Node Push(Node n)
        {
            // we have to do a deep clone of the Node object because
            // it is reused in the stack.
            Node n2 = Push(n.Name, n.NodeType, n.Value, n.DtdType);
            n2.Space = n.Space;
            n2.XmlLang = n.XmlLang;
            n2.CurrentState = n.CurrentState;
            n2.CopyAttributes(n);
            _node = n2;
            return n2;
        }

        private void Pop()
        {
            if (_stack.Count > 1)
            {
                _node = _stack.Pop();
            }
        }

        private Node Top()
        {
            int top = _stack.Count - 1;
            if (top > 0)
            {
                return _stack[top];
            }

            return null;
        }

        /// <summary>
        /// The node type of the node currently being parsed.
        /// </summary>
        public override XmlNodeType NodeType
        {
            get
            {
                return _state switch
                {
                    State.Attr => XmlNodeType.Attribute,
                    State.AttrValue => XmlNodeType.Text,
                    State.EndTag or State.AutoClose => XmlNodeType.EndElement,
                    _ => _node.NodeType
                };
            }
        }

        /// <summary>
        /// The name of the current node, if currently positioned on a node or attribute.
        /// </summary>
        public override string Name
        {
            get
            {
                string result = null;
                if (_state == State.Attr)
                {
                    result = XmlConvert.EncodeName(_a.Name);
                }
                else if (_state != State.AttrValue)
                {
                    result = _node.Name;
                }

                return result;
            }
        }

        /// <summary>
        /// The local name of the current node, if currently positioned on a node or attribute.
        /// </summary>
        public override string LocalName
        {
            get
            {
                string result = Name;
                if (result != null)
                {
                    int colon = result.IndexOf(':');
                    if (colon != -1)
                    {
                        result = result.Substring(colon + 1);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// The namespace of the current node, if currently positioned on a node or attribute.
        /// </summary>
        /// <remarks>
        /// If not positioned on a node or attribute, <see cref="UNDEFINED_NAMESPACE"/> is returned.
        /// </remarks>
        public override string NamespaceURI
        {
            get
            {
                // SGML has no namespaces, unless this turned out to be an xmlns attribute.
                if (_state == State.Attr && NameEquals(_a.Name, "xmlns"))
                {
                    return "http://www.w3.org/2000/xmlns/";
                }

                string prefix = Prefix;
                switch (Prefix)
                {
                case "xmlns":
                    return "http://www.w3.org/2000/xmlns/";
                case "xml":
                    return "http://www.w3.org/XML/1998/namespace";
                case null: // Should never occur since Prefix never returns null
                case "":
                    if (NodeType == XmlNodeType.Attribute)
                    {
                        // attributes without a prefix are never in any namespace
                        return string.Empty;
                    }
                    else if (NodeType == XmlNodeType.Element)
                    {
                        // check if a 'xmlns:prefix' attribute is defined
                        for (int i = _stack.Count - 1; i > 0; --i)
                        {
                            Node node = _stack[i];
                            if ((node != null) && (node.NodeType == XmlNodeType.Element))
                            {
                                int index = node.GetAttribute("xmlns", _caseInsensitive);
                                if (index >= 0)
                                {
                                    string value = node.GetAttribute(index).Value;
                                    if (value != null)
                                    {
                                        return value;
                                    }
                                }
                            }
                        }
                    }

                    return string.Empty;
                default: {
                        string value;
                        if ((NodeType is XmlNodeType.Attribute or XmlNodeType.Element)) 
                         {

                            // check if a 'xmlns:prefix' attribute is defined
                            string key = "xmlns:" + prefix;
                            for (int i = _stack.Count - 1; i > 0; --i)
                            {
                                Node node = _stack[i];
                                if ((node != null) && (node.NodeType == XmlNodeType.Element)) 
                                {
                                    int index = node.GetAttribute(key, _caseInsensitive);
                                    if (index >= 0) 
                                    {
                                        value = node.GetAttribute(index).Value;
                                        if (value != null) 
                                        {
                                            return value;
                                        }
                                    }
                                }
                            }
                        }

                        // check if we've seen this prefix before
                        if (!_unknownNamespaces.TryGetValue(prefix, out value)) 
                        {
                            if (_unknownNamespaces.Count > 0) 
                            {
                                value = UNDEFINED_NAMESPACE + _unknownNamespaces.Count.ToString();
                            } 
                            else 
                            {
                                value = UNDEFINED_NAMESPACE;
                            }
                            _unknownNamespaces[prefix] = value;
                        }
                        return value;
                    }
                }
            }
        }

        /// <summary>
        /// The prefix of the current node's name.
        /// </summary>
        public override string Prefix
        { 
            get
            {
                string result = Name;
                if (result != null)
                {
                    int colon = result.IndexOf(':');
                    if(colon != -1)
                    {
                        result = result.Substring(0, colon);
                    } 
                    else 
                    {
                        result = string.Empty;
                    }
                }
                return result ?? string.Empty;
            }
        }

        /// <summary>
        /// Whether the current node has a value or not.
        /// </summary>
        public override bool HasValue
        { 
            get
            {
                if (_state is State.Attr or State.AttrValue)
                {
                    return true;
                }

                return (_node.Value is not null);
            }
        }

        /// <summary>
        /// The value of the current node.
        /// </summary>
        public override string Value
        {
            get
            {
                if (_state is State.Attr or State.AttrValue)
                {
                    return _a.Value;
                }

                return _node.Value;
            }
        }

        /// <summary>
        /// Gets the depth of the current node in the XML document.
        /// </summary>
        /// <value>The depth of the current node in the XML document.</value>
        public override int Depth
        { 
            get
            {
                if (_state == State.Attr)
                {
                    return _stack.Count;
                }
                else if (_state == State.AttrValue)
                {
                    return _stack.Count + 1;
                }

                return _stack.Count - 1;
            }
        }

        /// <summary>
        /// Gets the base URI of the current node.
        /// </summary>
        /// <value>The base URI of the current node.</value>
        public override string BaseURI => _baseUri is null ? "" : _baseUri.AbsoluteUri;

        /// <summary>
        /// Gets a value indicating whether the current node is an empty element (for example, &lt;MyElement/&gt;).
        /// </summary>
        public override bool IsEmptyElement
        {
            get
            {
                if (_state is State.Markup or State.Attr or State.AttrValue)
                {
                    return _node.IsEmpty;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current node is an attribute that was generated from the default value defined in the DTD or schema.
        /// </summary>
        /// <value>
        /// true if the current node is an attribute whose value was generated from the default value defined in the DTD or
        /// schema; false if the attribute value was explicitly set.
        /// </value>
        public override bool IsDefault
        {
            get
            {
                return (_state is State.Attr or State.AttrValue)
                    ? _a.IsDefault
                    : false;
            }
        }

        /// <summary>
        /// Gets the quotation mark character used to enclose the value of an attribute node.
        /// </summary>
        /// <value>The quotation mark character (" or ') used to enclose the value of an attribute node.</value>
        /// <remarks>
        /// This property applies only to an attribute node.
        /// </remarks>
        public override char QuoteChar
        {
            get
            {
                if (_a != null)
                    return _a.QuoteChar;

                return '\0';
            }
        }

        /// <summary>
        /// Gets the current xml:space scope.
        /// </summary>
        /// <value>One of the <see cref="XmlSpace"/> values. If no xml:space scope exists, this property defaults to XmlSpace.None.</value>
        public override XmlSpace XmlSpace
        {
            get
            {
                for (int i = _stack.Count - 1; i > 1; i--)
                {
                    Node n = _stack[i];
                    XmlSpace xs = n.Space;
                    if (xs != XmlSpace.None)
                        return xs;
                }

                return XmlSpace.None;
            }
        }

        /// <summary>
        /// Gets the current xml:lang scope.
        /// </summary>
        /// <value>The current xml:lang scope.</value>
        public override string XmlLang
        {
            get
            {
                for (int i = _stack.Count - 1; i > 1; i--)
                {
                    Node n = _stack[i];
                    string xmllang = n.XmlLang;
                    if (xmllang != null)
                        return xmllang;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Specifies how whitespace nodes are handled.
        /// </summary>
        public WhitespaceHandling WhitespaceHandling
        {
            get => _whitespaceHandling;
            set => _whitespaceHandling = value;
        }

        private TextWhitespaceHandling _textWhitespace;

        /// <summary>
        /// Specifies how leading and trailing whitespace in <c>InnerText</c> and <c>Value</c> properties is handled.
        /// </summary>
        /// <remarks>
        /// This property is intended to make it easier to process files such as OFX feeds with self-closing elements
        /// separated by line-breaks as it allows <c>InnerText</c> to be passed directly into methods that expect string
        /// inputs to be trimmed of whitespace as a precondition.
        /// </remarks>
        public TextWhitespaceHandling TextWhitespace
        {
            get => _textWhitespace;
            set
            {
                // Prevent invalid values: only respect the lower 3 bits, and only respect the 3rd bit (OnlyLineBreaks) if either of the lower 2 bits are set - otherwise treat it as zero/empty/None.
                value &= (TextWhitespaceHandling)0x07;
                if ((value & TextWhitespaceHandling.TrimBoth) != 0)
                {
                    _textWhitespace = value;
                }
                else
                {
                    _textWhitespace = 0;
                }
            }
        }

        /// <summary>
        /// Gets the number of attributes on the current node.
        /// </summary>
        /// <value>The number of attributes on the current node.</value>
        public override int AttributeCount
        {
            get
            {
                if (_state is State.Attr or State.AttrValue)
                    //For compatibility with mono
                    return _node.AttributeCount;
                else if (_node.NodeType is XmlNodeType.Element or XmlNodeType.DocumentType)
                    return _node.AttributeCount;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Gets the value of an attribute with the specified <see cref="Name"/>.
        /// </summary>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <returns>The value of the specified attribute. If the attribute is not found, a null reference (Nothing in Visual Basic) is returned. </returns>
        public override string GetAttribute(string name)
        {
            if (_state != State.Attr && _state != State.AttrValue)
            {
                int i = _node.GetAttribute(name, _caseInsensitive);
                if (i >= 0)
                    return GetAttribute(i);
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the attribute with the specified <see cref="LocalName"/> and <see cref="NamespaceURI"/>.
        /// </summary>
        /// <param name="name">The local name of the attribute.</param>
        /// <param name="namespaceURI">The namespace URI of the attribute.</param>
        /// <returns>The value of the specified attribute. If the attribute is not found, a null reference (Nothing in Visual Basic) is returned. This method does not move the reader.</returns>
        public override string GetAttribute(string name, string namespaceURI)
        {
            return GetAttribute(name); // SGML has no namespaces.
        }

        /// <summary>
        /// Gets the value of the attribute with the specified index.
        /// </summary>
        /// <param name="i">The index of the attribute.</param>
        /// <returns>The value of the specified attribute. This method does not move the reader.</returns>
        public override string GetAttribute(int i)
        {
            if (_state != State.Attr && _state != State.AttrValue)
            {
                Attribute a = _node.GetAttribute(i);
                if (a != null)
                    return a.Value;
            }

            throw new ArgumentOutOfRangeException(nameof(i));
        }

        /// <summary>
        /// Gets the value of the attribute with the specified index.
        /// </summary>
        /// <param name="i">The index of the attribute.</param>
        /// <returns>The value of the specified attribute. This method does not move the reader.</returns>
        public override string this[int i] => GetAttribute(i);

        /// <summary>
        /// Gets the value of an attribute with the specified <see cref="Name"/>.
        /// </summary>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <returns>The value of the specified attribute. If the attribute is not found, a null reference (Nothing in Visual Basic) is returned. </returns>
        public override string this[string name] => GetAttribute(name);
       
        /// <summary>
        /// Gets the value of the attribute with the specified <see cref="LocalName"/> and <see cref="NamespaceURI"/>.
        /// </summary>
        /// <param name="name">The local name of the attribute.</param>
        /// <param name="namespaceURI">The namespace URI of the attribute.</param>
        /// <returns>The value of the specified attribute. If the attribute is not found, a null reference (Nothing in Visual Basic) is returned. This method does not move the reader.</returns>
        public override string this[string name, string namespaceURI] => GetAttribute(name, namespaceURI);

        /// <summary>
        /// Moves to the atttribute with the specified <see cref="Name"/>.
        /// </summary>
        /// <param name="name">The qualified name of the attribute.</param>
        /// <returns>true if the attribute is found; otherwise, false. If false, the reader's position does not change.</returns>
        public override bool MoveToAttribute(string name)
        {            
            int i = _node.GetAttribute(name, _caseInsensitive);
            if (i >= 0)
            {
                MoveToAttribute(i);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Moves to the attribute with the specified <see cref="LocalName"/> and <see cref="NamespaceURI"/>.
        /// </summary>
        /// <param name="name">The local name of the attribute.</param>
        /// <param name="ns">The namespace URI of the attribute.</param>
        /// <returns>true if the attribute is found; otherwise, false. If false, the reader's position does not change.</returns>
        public override bool MoveToAttribute(string name, string ns)
        {
            return MoveToAttribute(name);
        }

        /// <summary>
        /// Moves to the attribute with the specified index.
        /// </summary>
        /// <param name="i">The index of the attribute to move to.</param>
        public override void MoveToAttribute(int i)
        {
            Attribute a = _node.GetAttribute(i);
            if (a != null)
            {
                _apos = i;
                _a = a; 
                //Make sure that AttrValue does not overwrite the preserved value
                if (_state != State.Attr && _state != State.AttrValue)
                {
                    _node.CurrentState = _state; //save current state.
                }

                _state = State.Attr;
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(i));
        }

        /// <summary>
        /// Moves to the first attribute.
        /// </summary>
        /// <returns></returns>
        public override bool MoveToFirstAttribute()
        {
            if (_node.AttributeCount > 0)
            {
                MoveToAttribute(0);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Moves to the next attribute.
        /// </summary>
        /// <returns>true if there is a next attribute; false if there are no more attributes.</returns>
        /// <remarks>
        /// If the current node is an element node, this method is equivalent to <see cref="MoveToFirstAttribute"/>. If <see cref="MoveToNextAttribute"/> returns true,
        /// the reader moves to the next attribute; otherwise, the position of the reader does not change.
        /// </remarks>
        public override bool MoveToNextAttribute()
        {
            if (_state != State.Attr && _state != State.AttrValue)
            {
                return MoveToFirstAttribute();
            }
            else if (_apos < _node.AttributeCount - 1)
            {
                MoveToAttribute(_apos + 1);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Moves to the element that contains the current attribute node.
        /// </summary>
        /// <returns>
        /// true if the reader is positioned on an attribute (the reader moves to the element that owns the attribute); false if the reader is not positioned
        /// on an attribute (the position of the reader does not change).
        /// </returns>
        public override bool MoveToElement()
        {
            if (_state is State.Attr or State.AttrValue)
            {
                _state = _node.CurrentState;
                _a = null;
                return true;
            }
            else
                return _node.NodeType == XmlNodeType.Element;
        }

        /// <summary>
        /// Gets whether the content is HTML or not.
        /// </summary>
        public bool IsHtml => _isHtml;

        /// <summary>
        /// Returns the encoding of the current entity.
        /// </summary>
        /// <returns>The encoding of the current entity.</returns>
        public Encoding GetEncoding()
        {
            if (_current is null)
            {
                OpenInput();
            }

            return _current.Encoding;
        }

        private void OpenInput()
        {
            LazyLoadDtd(_baseUri);

            if (this.Href != null)
            {
                _current = new Entity("#document", null, _href, _resolver);
            }
            else if (_inputStream != null)
            {
                _current = new Entity("#document", null, _inputStream, _resolver);           
            }
            else
            {
                throw new InvalidOperationException("You must specify input either via Href or InputStream properties");
            }

            _current.IsHtml = IsHtml;
            _current.Open(null, _baseUri);
            if (_current.ResolvedUri is not null)
                _baseUri = _current.ResolvedUri;

            if (_current.IsHtml && _dtd is null)
            {
                _docType = "HTML";
                LazyLoadDtd(_baseUri);
            }
        }

        private bool NameEquals(string name1, string name2)
        {
            return string.Equals(name1, name2, (_caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        }

        private bool DtdNameEquals(string name1, string name2)
        {
            // compatisons with DTD elements is always case insensitive.
            return string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);
        }



        /// <summary>
        /// Reads the next node from the stream.
        /// </summary>
        /// <returns>true if the next node was read successfully; false if there are no more nodes to read.</returns>
        public override bool Read()
        {
            if (_current is null)
            {
                OpenInput();
            }

            bool foundnode = false;
            while (!foundnode)
            {
                switch (_state)
                {
                    case State.Initial:
                        _state = State.Markup;
                        _current.ReadChar();
                        goto case State.Markup;
                    case State.Eof:
                        if (_current.Parent != null)
                        {
                            _current.Close();
                            _current = _current.Parent;
                        }
                        else
                        {                           
                            return false;
                        }
                        break;
                    case State.EndTag:
                        if (NameEquals(_endTag, _node.Name))
                        {
                            Pop(); // we're done!
                            _state = State.Markup;
                            goto case State.Markup;
                        }                     
                        Pop(); // close one element
                        foundnode = true;// return another end element.
                        break;
                    case State.Markup:
                        if (_node.IsEmpty)
                        {
                            Pop();
                        }
                        foundnode = ParseMarkup();
                        break;
                    case State.PartialTag:
                        Pop(); // remove text node.
                        _state = State.Markup;
                        foundnode = ParseTag(_partial);
                        break;
                    case State.PseudoStartTag:
                        foundnode = ParseStartTag('<');                        
                        break;
                    case State.ContinueStartTag:
                        foundnode = ContinueStartTag(this._startTag, this._startTagChar);
                        break;
                    case State.ContinueTextNode:
                        foundnode = ContinueTextNode(this._delayedText, false);
                        break;
                    case State.AutoClose:
                        Pop(); // close next node.
                        if (_stack.Count <= _poptodepth)
                        {
                            _state = State.Markup;
                            if (_newnode != null)
                            {
                                Push(_newnode); // now we're ready to start the new node.
                                _newnode = null;
                                _state = State.Markup;
                            }
                            else if (_node.NodeType == XmlNodeType.Document)
                            {
                                _state = State.Eof;
                                goto case State.Eof;
                            }
                        } 
                        foundnode = true;
                        break;
                    case State.CData:
                        foundnode = ParseCData();
                        break;
                    case State.Attr:
                        goto case State.AttrValue;
                    case State.AttrValue:
                        _state = State.Markup;
                        goto case State.Markup;
                    case State.Text:
                        Pop();
                        goto case State.Markup;
                    case State.PartialText:
                        foundnode = ParseText(_current.Lastchar, false);
                        break;
                }

                if (foundnode && _node.NodeType == XmlNodeType.Whitespace && _whitespaceHandling == WhitespaceHandling.None)
                {
                    // strip out whitespace (caller is probably pretty printing the XML).
                    foundnode = false;
                }
                if (!foundnode && _state == State.Eof && _stack.Count > 1 && _rootCount == 0)
                {
                    _poptodepth = 1;
                    _state = State.AutoClose;
                    _node = Top();
                    return true;
                }
            }
            return true;
        }

        private bool ParseMarkup()
        {
            char ch = _current.Lastchar;
            if (ch == '<')
            {
                ch = _current.ReadChar();
                return ParseTag(ch);
            } 
            else if (ch != Entity.EOF)
            {
                if (_node.DtdType != null && _node.DtdType.ContentModel.DeclaredContent == DeclaredContent.CDATA)
                {
                    // e.g. SCRIPT or STYLE tags which contain unparsed character data.
                    _partial = '\0';
                    _state = State.CData;
                    return false;
                }
                else if (ParseText(ch, true))
                {
                    return true;
                }
            }

            _state = State.Eof;
            return false;
        }

        private const string declterm = " \t\r\n><";
        private bool ParseTag(char ch)
        {
            if (ch == '!')
            {
                ch = _current.ReadChar();
                if (ch == '-')
                {
                    return ParseComment();
                }
                else if (ch == '[')
                {
                    return ParseConditionalBlock();
                }
                else if (ch != '_' && !char.IsLetter(ch))
                {
                    // perhaps it's one of those nasty office document hacks like '<![if ! ie ]>'
                    string value = _current.ScanToEnd(_sb, "Recovering", ">"); // skip it
                    Log("Ignoring invalid markup '<!"+value+">");
                    return false;
                }
                else
                {
                    string name = _current.ScanToken(_sb, SgmlReader.declterm, false);
                    if (NameEquals(name, "DOCTYPE"))
                    {
                        ParseDocType();

                        // In SGML DOCTYPE SYSTEM attribute is optional, but in XML it is required,
                        // therefore if there is no SYSTEM literal then add an empty one.
                        if (this.GetAttribute("SYSTEM") is null && this.GetAttribute("PUBLIC") != null)
                        {
                            _node.AddAttribute("SYSTEM", "", '"', _caseInsensitive);
                        }

                        if (_stripDocType)
                        {
                            return false;
                        }
                        else
                        {
                            _node.NodeType = XmlNodeType.DocumentType;
                            return true;
                        }
                    }
                    else
                    {
                        Log("Invalid declaration '<!{0}...'.  Expecting '<!DOCTYPE' only.", name);
                        _current.ScanToEnd(null, "Recovering", ">"); // skip it
                        return false;
                    }
                }
            } 
            else if (ch == '?')
            {
                _current.ReadChar();// consume the '?' character.
                return ParsePI();
            }
            else if (ch == '/')
            {
                return ParseEndTag();
            }
            else
            {
                return ParseStartTag(ch);
            }
        }

        private string ScanName(string terminators)
        {
            string name = _current.ScanToken(_sb, terminators, false);
            return FoldName(name);
        }

        private string FoldName(string name)
        { 
            switch (_folding)
            {
                case CaseFolding.ToUpper:
                    name = name.ToUpperInvariant();
                    break;
                case CaseFolding.ToLower:
                    name = name.ToLowerInvariant();
                    break;
            }

            return _nameTable.Add(name);
        }

        private static bool VerifyName(string name)
        {
            try
            {
                XmlConvert.VerifyName(name);
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }

        private bool InjectOptionalStartTag(string name, XmlNodeType nt, ElementDecl e)
        {
            if (_dtd == null)
            {
                return false;
            }
            ElementDecl decl = null;
            // special case for missing root elements.
            if (!_foundRoot && (nt is XmlNodeType.Element or XmlNodeType.Text or XmlNodeType.CDATA))
            {
                _foundRoot = true;
                var rootDecl = _dtd.FindElement(_dtd.Name);
                if (rootDecl != null)
                {
                    if (DtdNameEquals(name, rootDecl.Name))
                    {
                        // hey it matches, so we're good!
                        return false;
                    }
                    if (rootDecl.StartTagOptional)
                    {
                        decl = rootDecl;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Missing required root element {rootDecl.Name}");
                    }
                }
            }
            else if (nt is XmlNodeType.Text or XmlNodeType.CDATA)
            {
                if (_node?.DtdType == null)
                {
                    return false;
                }
                if (_node.DtdType.CanContainText())
                {
                    return false;
                }
                // need to insert something that can contain #PCDATA.
                decl = FindOptionalTextContainer(_node.DtdType);
            }
            else
            {
                // now similar code for child content that is also missing required start tags, this one
                // is a bit more complicated because we have to search for optional start tags that "can"
                // contain the new element.
                if (e == null)
                {
                    return false;
                }
                Node top = _stack.Top;
                if (top != null && top.Includes(name))
                {
                    // we have an inherited inclusion then, so good to go!
                    return false; 
                }

                if (_node?.DtdType == null)
                {
                    return false;
                }

                if (_node.DtdType.CanContain(name, _comparer))
                {
                    return false; // all good!
                }
                decl = FindOptionalContainer(_node.DtdType, name);
            }
            if (decl != null)
            {
                // Simulate an the root element!
                _node.CurrentState = _state;
                Node node = Push(FoldName(decl.Name), XmlNodeType.Element, null, decl);
                node.Simulated = true;
                return true;
            }
            return false;
        }

        internal ElementDecl FindOptionalTextContainer(ElementDecl dtdType)
        {
            // We know _dtd is not null.
            
            // We have to work backwards from elements that can contain name to the current dtdType.
            // For example if we have text and only <HTML> then we need to insert <BODY> and 
            // BODY isMixed so text can live there.
            Stack<ElementDecl> stack = new Stack<ElementDecl>();
            foreach (ElementDecl f in _dtd.FindOptionalTextContainers())
            {
                if (f.Name == dtdType.Name)
                {
                    // bugbug this shouldn't happen since we already checked dtdType cannot contain text.
                    return f;
                }
                else
                {
                    stack.Push(f);
                }
            }

            HashSet<ElementDecl> visited = new HashSet<ElementDecl>();
            while (stack.Count > 0)
            {
                var e = stack.Pop();
                // Ok, we have an element that can contain text, but is it allowed inside dtdType?
                var f = FindOptionalContainer(dtdType, e.Name);
                if (f != null)
                {                   
                    return f;
                }
            }

            return null;
        }

        internal ElementDecl FindOptionalContainer(ElementDecl dtdType, string name)
        {
            // We have to work backwards from elements that can contain name to the current dtdType.
            // For example if we just got a <td> and dtdType is <HTML> then we need to insert
            // 2 start tags, "<BODY>" and "<TABLE>".  This method will find that chain, and return
            // the top of the chain that needs to be inserted, in this example "<BODY>".
            Stack<ElementDecl> stack = new Stack<ElementDecl>();
            var decl = _dtd.FindElement(name);
            if (decl == null)
            {
                return null;
            }
            Node top = _stack.Top;
            stack.Push(decl);
            HashSet<ElementDecl> visited = new HashSet<ElementDecl>();
            visited.Add(decl);
            while (stack.Count > 0)
            {
                var e = stack.Pop();
                foreach (ElementDecl f in _dtd.FindOptionalContainers(dtdType))
                {
                    if ((top != null && top.Includes(f.Name)) || f.CanContain(e.Name, _comparer))
                    {
                        if (f.Name == dtdType.Name)
                        {
                            return e;
                        }
                        else if (!visited.Contains(f))
                        {
                            // keep looking.
                            stack.Push(f); 
                            visited.Add(f);
                        }
                    }
                }
            }

            return null;
        }

        private const string tagterm = " \t\r\n=/><";
        private const string aterm = " \t\r\n='\"/>";
        private const string avterm = " \t\r\n>";
        private bool ParseStartTag(char ch)
        {
            string name = null;
            if (_state != State.PseudoStartTag)
            {
                if (SgmlReader.tagterm.IndexOf(ch) >= 0)
                {
                    _sb.Length = 0;
                    _sb.Append('<');
                    _state = State.PartialText;
                    return false;
                }

                name = ScanName(SgmlReader.tagterm);
            }
            else
            {
                // TODO: Changes by mindtouch mean that  this.startTag is never non-null.  The effects of this need checking.
                //name = this.startTag;
                _state = State.Markup;
            }

            return ContinueStartTag(name, ch);
        }

        private string _startTag;
        private char _startTagChar;

        private bool ContinueStartTag(string name, char ch)
        {
            ElementDecl e = _dtd?.FindElement(name);
            if (InjectOptionalStartTag(name, XmlNodeType.Element, e))
            {
                _startTag = name;
                _startTagChar = ch;
                _state = State.ContinueStartTag;
                return true;
            }

            _state = State.Markup;
            Node n = Push(name, XmlNodeType.Element, null, e);
            ch = _current.SkipWhitespace();
            while (ch != Entity.EOF && ch != '>')
            {
                if (ch == '/')
                {
                    n.IsEmpty = true;
                    ch = _current.ReadChar();
                    if (ch != '>')
                    {
                        Log("Expected empty start tag '/>' sequence instead of '{0}'", ch);
                        _current.ScanToEnd(null, "Recovering", ">");
                        return false;
                    }
                    break;
                } 
                else if (ch == '<')
                {
                    Log("Start tag '{0}' is missing '>'", name);
                    break;
                }

                string aname = ScanName(SgmlReader.aterm);
                ch = _current.SkipWhitespace();

                if (aname.Length == 1 && aname[0] is ',' or '=' or ':' or ';')
                {
                    continue;
                }

                string value = null;
                char quote = '\0';
                if (ch is '=' or '"' or '\'')
                {
                    if (ch == '=' )
                    {
                        _current.ReadChar();
                        ch = _current.SkipWhitespace();
                    }

                    if (ch is '\'' or '\"')
                    {
                        quote = ch;
                        value = ScanLiteral(_sb, ch);
                    }
                    else if (ch != '>')
                    {
                        string term = SgmlReader.avterm;
                        value = _current.ScanToken(_sb, term, false);
                    }
                }

                if (ValidAttributeName(aname))
                {
                    Attribute a = n.AddAttribute(aname, value ?? aname, quote, _caseInsensitive);
                    if (a is null)
                    {
                        Log("Duplicate attribute '{0}' ignored", aname);
                    }
                    else
                    {
                        ValidateAttribute(n, a);
                    }
                }

                ch = _current.SkipWhitespace();
            }

            if (ch == Entity.EOF)
            {
                _current.Error("Unexpected EOF parsing start tag '{0}'", name);
            } 
            else if (ch == '>')
            {
                _current.ReadChar(); // consume '>'
            }

            if (Depth == 1)
            {
                if (_rootCount == 1 && _settings.ConformanceLevel != ConformanceLevel.Fragment)
                {
                    // Hmmm, we found another root level tag, soooo, the only
                    // thing we can do to keep this a valid XML document is stop
                    _state = State.Eof;
                    return false;
                }
                _rootCount++;
            }
            ValidateContent(n);
            return true;
        }

        private bool ParseEndTag()
        {
            _state = State.EndTag;
            _current.ReadChar(); // consume '/' char.
            string name = this.ScanName(SgmlReader.tagterm);
            char ch = _current.SkipWhitespace();
            if (ch != '>')
            {
                Log("Expected empty start tag '/>' sequence instead of '{0}'", ch);
                _current.ScanToEnd(null, "Recovering", ">");
            }

            _current.ReadChar(); // consume '>'

            _endTag = name;

            // Make sure there's a matching start tag for it.     
            _node = _stack[_stack.Count - 1];
            for (int i = _stack.Count - 1; i > 0; i--)
            {
                Node n = _stack[i];
                if (NameEquals(n.Name, name))
                {
                    _endTag = n.Name;
                    return true;
                }
            }

            Log("No matching start tag for '</{0}>'", name);
            _state = State.Markup;
            return false;
        }

        private bool ParseComment()
        {
            char ch = _current.ReadChar();
            if (ch != '-')
            {
                Log("Expecting comment '<!--' but found {0}", ch);
                _current.ScanToEnd(null, "Comment", ">");
                return false;
            }

            string value = _current.ScanToEnd(_sb, "Comment", "-->");
            
            // Make sure it's a valid comment!
            int i = value.IndexOf("--");

            while (i >= 0)
            {
                int j = i + 2;
                while (j < value.Length && value[j] == '-')
                    j++;

                if (i > 0)
                {
                    value = value.Substring(0, i - 1) + "-" + value.Substring(j);
                } 
                else
                {
                    value = "-" + value.Substring(j);
                }

                i = value.IndexOf("--");
            }

            if (value.Length > 0 && value[value.Length - 1] == '-')
            {
                value += " "; // '-' cannot be last character
            }

            Push(null, XmlNodeType.Comment, value, null);
            return true;
        }

        private const string cdataterm = "\t\r\n[]<>";
        private bool ParseConditionalBlock()
        {
            char ch = _current.ReadChar(); // skip '['
            ch = _current.SkipWhitespace();
            string name = _current.ScanToken(_sb, cdataterm, false);
            if (name.StartsWith("if "))
            {
                // 'downlevel-revealed' comment (another atrocity of the IE team)
                _current.ScanToEnd(null, "CDATA", ">");
                return false;
            }
            else if (!NameEquals(name, "CDATA"))
            {
                Log("Expecting CDATA but found '{0}'", name);
                _current.ScanToEnd(null, "CDATA", ">");
                return false;
            }
            else
            {
                ch = _current.SkipWhitespace();
                if (ch != '[')
                {
                    Log("Expecting '[' but found '{0}'", ch);
                    _current.ScanToEnd(null, "CDATA", ">");
                    return false;
                }

                string value = _current.ScanToEnd(_sb, "CDATA", "]]>");

                Push(null, XmlNodeType.CDATA, value, null);
                return true;
            }
        }

        private const string dtterm = " \t\r\n>";
        private void ParseDocType()
        {
            char ch = _current.SkipWhitespace();
            string name = _current.ScanToken(_sb, SgmlReader.dtterm, false);
            Push(name, XmlNodeType.DocumentType, null, null);
            ch = _current.SkipWhitespace();
            if (ch != '>')
            {
                string subset = "";
                string pubid = "";
                string syslit = "";

                if (ch != '[')
                {
                    string token = _current.ScanToken(_sb, SgmlReader.dtterm, false);
                    if (NameEquals(token, "PUBLIC"))
                    {
                        ch = _current.SkipWhitespace();
                        if (ch is '\"' or '\'')
                        {
                            pubid = _current.ScanLiteral(_sb, ch);
                            _node.AddAttribute(token, pubid, ch, _caseInsensitive);
                        }
                    } 
                    else if (!NameEquals(token, "SYSTEM"))
                    {
                        Log("Unexpected token in DOCTYPE '{0}'", token);
                        _current.ScanToEnd(null, "DOCTYPE", ">");
                    }
                    ch = _current.SkipWhitespace();
                    if (ch is '\"' or '\'')
                    {
                        token = "SYSTEM";
                        syslit = _current.ScanLiteral(_sb, ch);
                        _node.AddAttribute(token, syslit, ch, _caseInsensitive);  
                    }
                    ch = _current.SkipWhitespace();
                }

                if (ch == '[')
                {
                    subset = _current.ScanToEnd(_sb, "Internal Subset", "]");
                    _node.Value = subset;
                }

                ch = _current.SkipWhitespace();
                if (ch != '>')
                {
                    Log("Expecting end of DOCTYPE tag, but found '{0}'", ch);
                    _current.ScanToEnd(null, "DOCTYPE", ">");
                }

                if (_dtd != null && !DtdNameEquals(_dtd.Name, name))
                {
                    throw new InvalidOperationException("DTD does not match document type");
                }

                _docType = name;
                _pubid = pubid;
                _syslit = syslit;
                _subset = subset;
                LazyLoadDtd(_current.ResolvedUri);
            }

            _current.ReadChar();
        }

        private const string piterm = " \t\r\n?";
        private bool ParsePI()
        {
            string name = _current.ScanToken(_sb, SgmlReader.piterm, false);
            string value = null;
            if (_current.Lastchar != '?')
            {
                // Notice this is not "?>".  This is because Office generates bogus PI's that end with "/>".
                value = _current.ScanToEnd(_sb, "Processing Instruction", ">");
                value = value.TrimEnd('/');
            }
            else
            {
                // error recovery.
                value = _current.ScanToEnd(_sb, "Processing Instruction", ">");
            }

            // check if the name has a prefix; if so, ignore it
            int colon = name.IndexOf(':');
            if (colon > 0) 
            {
                name = name.Substring(colon + 1);
            }

            // skip xml declarations, since these are generated in the output instead.
            if (!NameEquals(name, "xml"))
            {
                Push(name, XmlNodeType.ProcessingInstruction, value, null);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the text encountered is only comprised of whitespace characters.
        /// </summary>
        private bool ParseText(char ch, bool newtext)
        {
            bool ws = !newtext || _current.IsWhitespace; // `ws` indicates if the current run of text so-far is ONLY whitespace or not.
            if (newtext)
                _sb.Length = 0;

            _state = State.Text;
            while (ch != Entity.EOF)
            {
                if (ch == '<')
                {
                    ch = _current.ReadChar();
                    if (ch is '/' or '!' or '?' || char.IsLetter(ch))
                    {
                        // Hit a tag, so return XmlNodeType.Text token
                        // and remember we partially started a new tag.
                        _state = State.PartialTag;
                        _partial = ch;
                        break;
                    } 
                    else
                    {
                        // not a tag, so just proceed.
                        _sb.Append('<');
                        _sb.Append(ch);
                        ws = false;
                        ch = _current.ReadChar();
                    }
                } 
                else if (ch == '&')
                {
                    ExpandEntity(_sb, '<');
                    ws = false;
                    ch = _current.Lastchar;
                }
                else
                {
                    if (!_current.IsWhitespace)
                    {
                        ws = false;
                    }
                            
                    _sb.Append(ch);
                    ch = _current.ReadChar();
                }
            }

            string value;
            if (_textWhitespace == TextWhitespaceHandling.None)
            {
                value = _sb.ToString();
            }
            else
            {
                value = TrimStringBuilder(_sb, _textWhitespace);
            }

            _delayedState = _state;
            return ContinueTextNode(value, ws);
        }

        private string _delayedText;
        private State _delayedState;


        private bool ContinueTextNode(string text, bool isWhitespace)
        {
            if (!isWhitespace && InjectOptionalStartTag(null, XmlNodeType.Text, null))
            {
                _delayedText = text;
                _state = State.ContinueTextNode;
                return true;
            }
            
            _state = _delayedState;
            var node = Push(name: null, XmlNodeType.Text, text, null);
            if (isWhitespace)
            {
                node.NodeType = XmlNodeType.Whitespace;
            }
            return true;
        }

        private static string TrimStringBuilder(StringBuilder sb, TextWhitespaceHandling handling) // It's simpler to return a substring from within a StringBuilder than it is to prevent whitespace from being added in the first place, hence this approach.
        {
            int startIndex = 0;
            int endIndex   = sb.Length - 1; // `endIndex` is inclusive, not exclusive.

            if (handling.HasFlagBits(TextWhitespaceHandling.TrimLeading))
            {
                while (startIndex < sb.Length && CharTrimPredicate(sb[startIndex], onlyLineBreaks: handling.HasFlagBits(TextWhitespaceHandling.OnlyLineBreaks)))
                {
                    startIndex++;
                }

                if (startIndex >= sb.Length) return string.Empty;
            }

            if (handling.HasFlagBits(TextWhitespaceHandling.TrimTrailing))
            {
                while (endIndex >= 0 && CharTrimPredicate(sb[endIndex], onlyLineBreaks: handling.HasFlagBits(TextWhitespaceHandling.OnlyLineBreaks)))
                {
                    endIndex--;
                }

                if (endIndex < 0 ) return string.Empty;
            }

            int exclusiveEndIndex = endIndex + 1;
            int length = exclusiveEndIndex - startIndex;
            return sb.ToString(startIndex, length);
        }

        private static bool CharTrimPredicate(char c, bool onlyLineBreaks)
        {
            return onlyLineBreaks ? (c == '\r' || c == '\n') : char.IsWhiteSpace(c);
        }

        /// <summary>
        /// Consumes and returns a literal block of text, expanding entities as it does so.
        /// </summary>
        /// <param name="sb">The string builder to use.</param>
        /// <param name="quote">The delimiter for the literal.</param>
        /// <returns>The consumed literal.</returns>
        /// <remarks>
        /// This version is slightly different from <see cref="Entity.ScanLiteral"/> in that
        /// it also expands entities.
        /// </remarks>
        private string ScanLiteral(StringBuilder sb, char quote)
        {
            sb.Length = 0;
            char ch = _current.ReadChar();
            while (ch != Entity.EOF && ch != quote && ch != '>')
            {
                if (ch == '&')
                {
                    ExpandEntity(sb, quote);
                    ch = _current.Lastchar;
                }               
                else
                {
                    sb.Append(ch);
                    ch = _current.ReadChar();
                }
            }
            if (ch == quote) 
            {
                _current.ReadChar(); // consume end quote.
            }
            return sb.ToString();
        }

        private bool ParseCData()
        {
            // Like ParseText(), only it doesn't allow elements in the content.  
            // It allows comments and processing instructions and text only and
            // text is not returned as text but CDATA (since it may contain angle brackets).
            // And initial whitespace is ignored.  It terminates when we hit the
            // end tag for the current CDATA node (e.g. </style>).
            bool ws = _current.IsWhitespace;
            _sb.Length = 0;
            char ch = _current.Lastchar;
            if (_partial != '\0')
            {
                Pop(); // pop the CDATA
                switch (_partial)
                {
                    case '!':
                        _partial = ' '; // and pop the comment next time around
                        return ParseComment();
                    case '?':
                        _partial = ' '; // and pop the PI next time around
                        return ParsePI();
                    case '/':
                        _state = State.EndTag;
                        return true;    // we are done!
                    case ' ':
                        break; // means we just needed to pop the Comment, PI or CDATA.
                }
            }            
            
            // if this.partial == '!' then parse the comment and return
            // if this.partial == '?' then parse the processing instruction and return.            
            while (ch != Entity.EOF)
            {
                if (ch == '<')
                {
                    ch = _current.ReadChar();
                    if (ch == '!')
                    {
                        ch = _current.ReadChar();
                        if (ch == '-')
                        {
                            // return what CDATA we have accumulated so far
                            // then parse the comment and return to here.
                            if (ws)
                            {
                                _partial = ' '; // pop comment next time through
                                return ParseComment();
                            } 
                            else
                            {
                                // return what we've accumulated so far then come
                                // back in and parse the comment.
                                _partial = '!';
                                break; 
                            }
#if FIX
                        } 
                        else if (ch == '[')
                        {
                            // We are about to wrap this node as a CDATA block because of it's
                            // type in the DTD, but since we found a CDATA block in the input
                            // we have to parse it as a CDATA block, otherwise we will attempt
                            // to output nested CDATA blocks which of course is illegal.
                            if (this.ParseConditionalBlock())
                            {
                                this.partial = ' ';
                                return true;
                            }
#endif
                        }
                        else
                        {
                            // not a comment, so ignore it and continue on.
                            _sb.Append('<');
                            _sb.Append('!');
                            _sb.Append(ch);
                            ws = false;
                        }
                    } 
                    else if (ch == '?')
                    {
                        // processing instruction.
                        _current.ReadChar();// consume the '?' character.
                        if (ws)
                        {
                            _partial = ' '; // pop PI next time through
                            return ParsePI();
                        } 
                        else
                        {
                            _partial = '?';
                            break;
                        }
                    }
                    else if (ch == '/')
                    {
                        // see if this is the end tag for this CDATA node.
                        string temp = _sb.ToString();
                        if (ParseEndTag() && NameEquals(_endTag, _node.Name))
                        {
                            if (ws || string.IsNullOrEmpty(temp))
                            {
                                // we are done!
                                return true;
                            } 
                            else
                            {
                                // return CDATA text then the end tag
                                _partial = '/';
                                _sb.Length = 0; // restore buffer!
                                _sb.Append(temp);
                                _state = State.CData;
                                break;
                            }
                        } 
                        else
                        {
                            // wrong end tag, so continue on.
                            _sb.Length = 0; // restore buffer!
                            _sb.Append(temp);
                            _sb.Append("</" + _endTag + ">");
                            ws = false;

                            // NOTE (steveb): we have one character in the buffer that we need to process next
                            ch = _current.Lastchar;
                            continue;
                        }
                    }
                    else
                    {
                        // must be just part of the CDATA block, so proceed.
                        _sb.Append('<');
                        _sb.Append(ch);
                        ws = false;
                    }
                } 
                else
                {
                    if (!_current.IsWhitespace && ws)
                        ws = false;
                    _sb.Append(ch);
                }

                ch = _current.ReadChar();
            }

            // NOTE (steveb): check if we reached EOF, which means it's over
            if(ch == Entity.EOF) 
            {
                _state = State.Eof;
                return false;
            }

            string value = _sb.ToString();

            // NOTE (steveb): replace any nested CDATA sections endings
            value = value.Replace("<![CDATA[", string.Empty);
            value = value.Replace("]]>", string.Empty);
            value = value.Replace("/**/", string.Empty);

            Push(null, XmlNodeType.CDATA, value, null);
            if (_partial == '\0')
                _partial = ' ';// force it to pop this CDATA next time in.

            return true;
        }

        private void ExpandEntity(StringBuilder sb, char terminator)
        {
            char ch = _current.ReadChar();
            if (ch == '#')
            {
                string charent = _current.ExpandCharEntity();
                sb.Append(charent);
                ch = _current.Lastchar;
            } 
            else
            {
                _name.Length = 0;
                while (ch != Entity.EOF &&
                    (char.IsLetter(ch) || ch is '_' or '-') || ((_name.Length > 0) && char.IsDigit(ch)))
                {
                    _name.Append(ch);
                    ch = _current.ReadChar();
                }
                string name = _name.ToString();

                // TODO (steveb): don't lookup amp, gt, lt, quote
                switch(name)
                {
                case "amp":
                    sb.Append('&');
                    if(ch != terminator && ch != '&' && ch != Entity.EOF)
                        ch = _current.ReadChar();
                    return;
                case "lt":
                    sb.Append('<');
                    if(ch != terminator && ch != '&' && ch != Entity.EOF)
                        ch = _current.ReadChar();
                    return;
                case "gt":
                    sb.Append('>');
                    if(ch != terminator && ch != '&' && ch != Entity.EOF)
                        ch = _current.ReadChar();
                    return;
                case "quot":
                    sb.Append('"');
                    if(ch != terminator && ch != '&' && ch != Entity.EOF)
                        ch = _current.ReadChar();
                    return;
                case "apos":
                    sb.Append("'");
                    if(ch != terminator && ch != '&' && ch != Entity.EOF)
                        ch = _current.ReadChar();
                    return;
                }

                if (_dtd != null && !string.IsNullOrEmpty(name))
                {
                    Entity e = _dtd.FindEntity(name);
                    if (e != null)
                    {
                        if (e.IsInternal)
                        {
                            sb.Append(e.Literal);
                            if (ch != terminator && ch != '&' && ch != Entity.EOF)
                                ch = _current.ReadChar();

                            return;
                        } 
                        else
                        {
                            var ex = new Entity(name, e.PublicId, e.Uri, _resolver);
                            e.Open(_current, new Uri(e.Uri));
                            _current = ex;
                            _current.ReadChar();
                            return;
                        }
                    } 
                    else
                    {
                        Log("Undefined entity '{0}'", name);
                    }
                }
                // Entity is not defined, so just keep it in with the rest of the
                // text.
                sb.Append('&');
                sb.Append(name);
                if(ch != terminator && ch != '&' && ch != Entity.EOF)
                {
                    sb.Append(ch);
                    ch = _current.ReadChar();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the reader is positioned at the end of the stream.
        /// </summary>
        /// <value>true if the reader is positioned at the end of the stream; otherwise, false.</value>
        public override bool EOF => _state == State.Eof;

        /// <summary>
        /// Disposes this object.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_current != null)
            {
                _current.Close();
                _current = null;
            }

            if (_log != null)
            {
                _log.Dispose();
                _log = null;
            }
        }

        /// <summary>
        /// Gets the state of the reader.
        /// </summary>
        /// <value>One of the ReadState values.</value>
        public override ReadState ReadState
        {
            get
            {
                if (_state == State.Initial)
                    return ReadState.Initial;
                else if (_state == State.Eof)
                    return ReadState.EndOfFile;
                else
                    return ReadState.Interactive;
            }
        }


        /// <summary>
        /// Reads the contents of an element or text node as a string.
        /// </summary>
        /// <returns>The contents of the element or an empty string.</returns>
        public override string ReadContentAsString()
        {
            if (_node.NodeType == XmlNodeType.Element)
            {
                _sb.Length = 0;
                while (Read())
                {
                    switch (this.NodeType)
                    {
                        case XmlNodeType.CDATA:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.Text:
                            _sb.Append(_node.Value);
                            break;
                        default:
                            return _sb.ToString();
                    }
                }

                return _sb.ToString();
            }

            return _node.Value;
        }

        /// <summary>
        /// Reads all the content, including markup, as a string.
        /// </summary>
        /// <returns>
        /// All the XML content, including markup, in the current node. If the current node has no children,
        /// an empty string is returned. If the current node is neither an element nor attribute, an empty
        /// string is returned.
        /// </returns>
        public override string ReadInnerXml()
        {
            var sw = new StringWriter(CultureInfo.InvariantCulture);
            var settings = new XmlWriterSettings
            {
                Indent = true,
                ConformanceLevel = ConformanceLevel.Fragment
            };
            using (XmlWriter xw = XmlWriter.Create(sw, settings))
            {
                switch (this.NodeType)
                {
                    case XmlNodeType.Element:
                        Read();
                        while (!this.EOF && this.NodeType != XmlNodeType.EndElement)
                        {
                            xw.WriteNode(this, true);
                        }
                        Read(); // consume the end tag
                        break;
                    case XmlNodeType.Attribute:
                        sw.Write(this.Value);
                        break;
                    default:
                        // return empty string according to XmlReader spec.
                        break;
                }
            }
            return sw.ToString();
        }

        /// <summary>
        /// Reads the content, including markup, representing this node and all its children.
        /// </summary>
        /// <returns>
        /// If the reader is positioned on an element or an attribute node, this method returns all the XML content, including markup, of the current node and all its children; otherwise, it returns an empty string.
        /// </returns>
        public override string ReadOuterXml()
        {
            var sw = new StringWriter(CultureInfo.InvariantCulture);
            var settings = new XmlWriterSettings
            {
                Indent = true
            };
            using (XmlWriter xw = XmlWriter.Create(sw, settings))
            {
                xw.WriteNode(this, true);
            }
            return sw.ToString();
        }

        /// <summary>
        /// Gets the XmlNameTable associated with this implementation.
        /// </summary>
        /// <value>The XmlNameTable enabling you to get the atomized version of a string within the node.</value>
        public override XmlNameTable NameTable => _nameTable;

        /// <summary>
        /// Resolves a namespace prefix in the current element's scope.
        /// </summary>
        /// <param name="prefix">The prefix whose namespace URI you want to resolve. To match the default namespace, pass an empty string.</param>
        /// <returns>The namespace URI to which the prefix maps or a null reference (Nothing in Visual Basic) if no matching prefix is found.</returns>
        public override string LookupNamespace(string prefix)
        {
            return null; // there are no namespaces in SGML.
        }

        /// <summary>
        /// Resolves the entity reference for EntityReference nodes.
        /// </summary>
        /// <exception cref="InvalidOperationException">SgmlReader does not resolve or return entities.</exception>
        public override void ResolveEntity()
        {
            // We never return any entity reference nodes, so this should never be called.
            throw new InvalidOperationException("Not on an entity reference.");
        }

        /// <summary>
        /// Parses the attribute value into one or more Text, EntityReference, or EndEntity nodes.
        /// </summary>
        /// <returns>
        /// true if there are nodes to return. false if the reader is not positioned on an attribute node when the initial call is made or if all the
        /// attribute values have been read. An empty attribute, such as, misc="", returns true with a single node with a value of string.Empty.
        /// </returns>
        public override bool ReadAttributeValue()
        {
            if (_state == State.Attr)
            {
                _state = State.AttrValue;
                return true;
            }
            else if (_state == State.AttrValue)
            {
                return false;
            }
            else
                throw new InvalidOperationException("Not on an attribute.");
        }   

        private void Validate(Node node)
        {
            if (_dtd != null)
            {
                ElementDecl e = _dtd.FindElement(node.Name);
                if (e != null)
                {
                    node.DtdType = e;
                    node.IsEmpty = (e.ContentModel.DeclaredContent == DeclaredContent.EMPTY);
                }
            }
        }

        private static void ValidateAttribute(Node node, Attribute a)
        {
            ElementDecl e = node.DtdType;
            if (e != null)
            {
                AttDef ad = e.FindAttribute(a.Name);
                if (ad != null)
                {
                    a.DtdType = ad;
                }
            }
        }

        private static bool ValidAttributeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            try
            {
                XmlConvert.VerifyNMTOKEN(name);
                int index = name.IndexOf(':');
                if (index >= 0)
                {
                    XmlConvert.VerifyNCName(name.Substring(index + 1));
                }

                return true;
            }
            catch (XmlException)
            {
                return false;
            }
            catch (ArgumentNullException)
            {
                // (steveb) this is probably a bug in XmlConvert.VerifyNCName when passing in an empty string
                return false;
            }
        }

        private void ValidateContent(Node node)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                if (!VerifyName(node.Name))
                {
                    Pop();
                    Push(null, XmlNodeType.Text, "<" + node.Name + ">", null);
                    return;
                }
            }

            if (_dtd != null)
            {
                // See if this element is allowed inside the current element.
                // If it isn't, then auto-close elements until we find one
                // that it is allowed to be in.                                  
                string name = node.Name.ToUpperInvariant(); // DTD is in upper case
                int i = 0;
                int top = _stack.Count - 2;

                if (node.DtdType != null) 
                {
                    // it is a known element, let's see if it's allowed in the
                    // current context.
                    for (i = top; i > 0; i--)
                    {
                        Node n = _stack[i];
                        if (n.IsEmpty)
                            continue; // we'll have to pop this one
                        ElementDecl f = n.DtdType;
                        if (f != null)
                        {
                            if (n.Excludes(name))
                                continue;    // element is explicitly not allowed here which may mean we need to auto-close the parent.

                            if (n.Includes(name))
                                return;   // element is allowed

                            if (_isHtml && (i == 2) && DtdNameEquals(f.Name, "BODY"))
                            {
                                // NOTE (steveb): never close the BODY tag too early
                                break;
                            }
                            else if (f.CanContain(name, _comparer))
                            {
                                break;
                            }
                            else if (!f.EndTagOptional)
                            {
                                // If the end tag is not optional then we can't
                                // auto-close it.  We'll just have to live with the
                                // junk we've found and move on.
                                break;
                            }
                        } 
                        else
                        {
                            // Since we don't understand this tag anyway,
                            // we might as well allow this content!
                            break;
                        }
                    }
                }

                if (i < 1)
                {
                    // Tag was not found or is the root tag, or is not allowed anywhere, ignore it and 
                    // continue on.  _stack[0] is the Document node, _stack[1] is the root element.
                    return;
                }
                else if (i < top)
                {
                    Node n = _stack[top];     
#if DEBUG
                    string closing = "";
                    for (int k = top; k >= i+1; k--) 
                    {
                        if (closing != "") closing += ",";
                        Node n2 = _stack[k];
                        closing += "<" + n2.Name + ">";
                    }
                    Log("Element '{0}' not allowed inside '{1}', closing {2}.", name, n.Name, closing);
#endif

                    _state = State.AutoClose;
                    _newnode = node;
                    Pop(); // save this new node until we pop the others
                    _poptodepth = i + 1;
                }
            }
        }
    }

    static class Extensions
    {
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
        {
            foreach (var i in items)
            {
                set.Add(i);
            }
        }
    }
}