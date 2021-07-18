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
using System.Diagnostics;
using System.Globalization;
using System.IO;
#if WINDOWS_DESKTOP
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif
using System.Text;

namespace Sgml
{
    /// <summary>
    /// An Entity declared in a DTD.
    /// </summary>
    [DebuggerDisplay("Sgml.Entity, {LiteralType}, Name: {Name}, Is Whitespace: {IsWhitespace}")]
    public class Entity : IDisposable
    {
        /// <summary>
        /// The character indicating End Of File.
        /// </summary>
        public const char EOF = (char)65535;

        private readonly string _name;
        private readonly bool _isInternal;
        private readonly string _publicId;
        private readonly string _uri;
        private readonly string _literal;
        private LiteralType _literalType;
        private Entity _parent;
        private bool _isHtml;
        private int _line;
        private char _lastchar;
        private bool _isWhitespace;
        private readonly IEntityResolver _resolver;

        private Encoding _encoding;
        private Uri _resolvedUri;
        private TextReader _stm;
        private bool _weOwnTheStream;
        private int _lineStart;
        private int _absolutePos;

        /// <summary>
        /// Initialises a new instance of an Entity declared in a DTD.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        /// <param name="pubid">The public id of the entity.</param>
        /// <param name="uri">The uri of the entity.</param>
        /// <param name="resolver">The resolver to use for loading this entity</param>
        public Entity(string name, string pubid, string uri, IEntityResolver resolver)
        {
            _name = name;
            _publicId = pubid;
            _uri = uri;
            _isHtml = (name != null && Extensions.EqualsIgnoreCase(name, "html"));
            _resolver = resolver;
        }

        /// <summary>
        /// Initialises a new instance of an Entity declared in a DTD.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        /// <param name="literal">The literal value of the entity.</param>
        /// <param name="resolver">The resolver to use for loading this entity</param>
        public Entity(string name, string literal, IEntityResolver resolver)
        {
            _name = name;
            _literal = literal;
            _isInternal = true;
            _resolver = resolver;
        }

        /// <summary>
        /// Initialises a new instance of an Entity declared in a DTD.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        /// <param name="baseUri">The baseUri for the entity to read from the TextReader.</param>
        /// <param name="stm">The TextReader to read the entity from.</param>
        /// <param name="resolver">The resolver to use for loading this entity</param>
        public Entity(string name, Uri baseUri, TextReader stm, IEntityResolver resolver)
        {
            _name = name;
            _isInternal = true;
            _stm = stm;
            _resolvedUri = baseUri;
            _isHtml = string.Equals(name, "html", StringComparison.OrdinalIgnoreCase);
            _resolver = resolver;
        }

        /// <summary>
        /// The name of the entity.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// True if the entity is the html element entity.
        /// </summary>
        public bool IsHtml
        {
            get => _isHtml;
            set => _isHtml = value;
        }

        /// <summary>
        /// The public identifier of this entity.
        /// </summary>
        public string PublicId => _publicId;

        /// <summary>
        /// The Uri that is the source for this entity.
        /// </summary>
        public string Uri => _uri;

        /// <summary>
        /// The resolved location of the DTD this entity is from.
        /// </summary>
        public Uri ResolvedUri
        {
            get
            {
                if (_resolvedUri != null)
                    return _resolvedUri;
                else if (_parent != null)
                    return _parent.ResolvedUri;
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets the parent Entity of this Entity.
        /// </summary>
        public Entity Parent => _parent;

        /// <summary>
        /// The last character read from the input stream for this entity.
        /// </summary>
        public char Lastchar => _lastchar;

        /// <summary>
        /// The line on which this entity was defined.
        /// </summary>
        public int Line => _line;

        /// <summary>
        /// The index into the line where this entity is defined.
        /// </summary>
        public int LinePosition => _absolutePos - _lineStart + 1;

        /// <summary>
        /// Whether this entity is an internal entity or not.
        /// </summary>
        /// <value>true if this entity is internal, otherwise false.</value>
        public bool IsInternal => _isInternal;
       
        /// <summary>
        /// The literal value of this entity.
        /// </summary>
        public string Literal => _literal;

        /// <summary>
        /// The <see cref="LiteralType"/> of this entity.
        /// </summary>
        public LiteralType LiteralType => _literalType;

        /// <summary>
        /// Whether the last char read for this entity is a whitespace character.
        /// </summary>
        public bool IsWhitespace => _isWhitespace;
        
        /// <summary>
        /// Reads the next character from the DTD stream.
        /// </summary>
        /// <returns>The next character from the DTD stream.</returns>
        public char ReadChar()
        {
            char ch = (char)_stm.Read();
            if (ch == 0)
            {
                // convert nulls to whitespace, since they are not valid in XML anyway.
                ch = ' ';
            }
            _absolutePos++;
            if (ch == 0xa)
            {
                _isWhitespace = true;
                _lineStart = _absolutePos + 1;
                _line++;
            } 
            else if (ch is ' ' or '\t')
            {
                _isWhitespace = true;
                if (_lastchar == 0xd)
                {
                    _lineStart = _absolutePos;
                    _line++;
                }
            }
            else if (ch == 0xd)
            {
                _isWhitespace = true;
            }
            else
            {
                _isWhitespace = false;
                if (_lastchar == 0xd)
                {
                    _line++;
                    _lineStart = _absolutePos;
                }
            } 

            _lastchar = ch;
            return ch;
        }

        /// <summary>
        /// Begins processing an entity.
        /// </summary>
        /// <param name="parent">The parent of this entity.</param>
        /// <param name="baseUri">The base Uri for processing this entity within.</param>
        public void Open(Entity parent, Uri baseUri)
        {
            _parent = parent;
            if (parent != null)
                _isHtml = parent.IsHtml;
            _line = 1;
            if (_isInternal)
            {
                if (_literal != null)
                    _stm = new StringReader(_literal);
            } 
            else if (_uri is null)
            {
                this.Error("Unresolvable entity '{0}'", _name);
            }
            else
            {
                if (baseUri != null)
                {
                    _resolvedUri = new Uri(baseUri, _uri);
                }
                else
                {
                    _resolvedUri = new Uri(_uri, UriKind.RelativeOrAbsolute);
                }

                IEntityContent content = _resolver.GetContent(this.ResolvedUri);
                Stream stream = content.Open();

                if (Extensions.EqualsIgnoreCase(content.MimeType, "text/html"))
                {
                    _isHtml = true;
                }
                _resolvedUri = content.Redirect;

                _weOwnTheStream = true;
                var html = new HtmlStream(stream, content.Encoding);
                _encoding = html.Encoding;
                _stm = html;
            }
        }

        /// <summary>
        /// Gets the character encoding for this entity.
        /// </summary>
        public Encoding Encoding => _encoding;

        /// <summary>
        /// Closes the reader from which the entity is being read.
        /// </summary>
        public void Close()
        {
            if (_weOwnTheStream)
            {
                _stm.Dispose();
            }
        }

        /// <summary>
        /// Returns the next character after any whitespace.
        /// </summary>
        /// <returns>The next character that is not whitespace.</returns>
        public char SkipWhitespace()
        {
            char ch = _lastchar;
            while (ch != Entity.EOF && (ch is ' ' or '\r' or '\n' or '\t'))
            {
                ch = ReadChar();
            }
            return ch;
        }

        /// <summary>
        /// Scans a token from the input stream and returns the result.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to use to process the token.</param>
        /// <param name="term">A set of characters to look for as terminators for the token.</param>
        /// <param name="nmtoken">true if the token should be a NMToken, otherwise false.</param>
        /// <returns>The scanned token.</returns>
        public string ScanToken(StringBuilder sb, string term, bool nmtoken)
        {
            if (sb is null)
                throw new ArgumentNullException(nameof(sb));

            if (term is null)
                throw new ArgumentNullException(nameof(term));

            sb.Length = 0;
            char ch = _lastchar;
            if (nmtoken && ch != '_' && !char.IsLetter(ch))
            {
                throw new SgmlParseException($"Invalid name start character '{ch}'");
            }

            while (ch != Entity.EOF && term.IndexOf(ch) < 0)
            {
                if (!nmtoken || ch is '_' or '.' or '-' or ':' || char.IsLetterOrDigit(ch)) 
                {
                    sb.Append(ch);
                } 
                else
                {
                    throw new SgmlParseException($"Invalid name character '{ch}'");
                }
                ch = ReadChar();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Read a literal from the input stream.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to use to build the literal.</param>
        /// <param name="quote">The delimiter for the literal.</param>
        /// <returns>The literal scanned from the input stream.</returns>
        public string ScanLiteral(StringBuilder sb, char quote)
        {
            if (sb is null)
                throw new ArgumentNullException(nameof(sb));

            sb.Length = 0;
            char ch = ReadChar();
            while (ch != Entity.EOF && ch != quote)
            {
                if (ch == '&')
                {
                    ch = ReadChar();
                    if (ch == '#')
                    {
                        string charent = ExpandCharEntity();
                        sb.Append(charent);
                        ch = _lastchar;
                    } 
                    else
                    {
                        sb.Append('&');
                        sb.Append(ch);
                        ch = ReadChar();
                    }
                }               
                else
                {
                    sb.Append(ch);
                    ch = ReadChar();
                }
            }

            ReadChar(); // consume end quote.
            return sb.ToString();
        }

        /// <summary>
        /// Reads input until the end of the input stream or until a string of terminator characters is found.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to use to build the string.</param>
        /// <param name="type">The type of the element being read (only used in reporting errors).</param>
        /// <param name="terminators">The string of terminator characters to look for.</param>
        /// <returns>The string read from the input stream.</returns>
        public string ScanToEnd(StringBuilder sb, string type, string terminators)
        {
            if (terminators is null)
                throw new ArgumentNullException(nameof(terminators));

            if (sb != null)
                sb.Length = 0;

            int start = _line;
            // This method scans over a chunk of text looking for the
            // termination sequence specified by the 'terminators' parameter.
            char ch = ReadChar();            
            int state = 0;
            char next = terminators[state];
            while (ch != Entity.EOF)
            {
                if (ch == next)
                {
                    state++;
                    if (state >= terminators.Length)
                    {
                        // found it!
                        break;
                    }
                    next = terminators[state];
                }
                else if (state > 0)
                {
                    // char didn't match, so go back and see how much does still match.
                    int i = state - 1;
                    int newstate = 0;
                    while (i >= 0 && newstate == 0)
                    {
                        if (terminators[i] == ch)
                        {
                            // character is part of the terminators pattern, ok, so see if we can
                            // match all the way back to the beginning of the pattern.
                            int j = 1;
                            while (i - j >= 0)
                            {
                                if (terminators[i - j] != terminators[state - j])
                                    break;

                                j++;
                            }

                            if (j > i)
                            {
                                newstate = i + 1;
                            }
                        }
                        else
                        {
                            i--;
                        }
                    }

                    if (sb != null)
                    {
                        i = (i < 0) ? 1 : 0;
                        for (int k = 0; k <= state - newstate - i; k++)
                        {
                            sb.Append(terminators[k]); 
                        }

                        if (i > 0) // see if we've matched this char or not
                            sb.Append(ch); // if not then append it to buffer.
                    }

                    state = newstate;
                    next = terminators[newstate];
                }
                else
                {
                    if (sb != null)
                        sb.Append(ch);
                }

                ch = ReadChar();
            }

            if (ch == 0)
                Error(type + " starting on line {0} was never closed", start);

            ReadChar(); // consume last char in termination sequence.
            if (sb != null)
                return sb.ToString();
            else
                return string.Empty;
        }

        /// <summary>
        /// Expands a character entity to be read from the input stream.
        /// </summary>
        /// <returns>The string for the character entity.</returns>
        public string ExpandCharEntity()
        {
            int v = ReadNumericEntityCode(out string value);
            if(v == -1)
            {
                return value;
            }

            // HACK ALERT: IE and Netscape map the unicode characters 
            if (_isHtml && v >= 0x80 & v <= 0x9F)
            {
                // This range of control characters is mapped to Windows-1252!
                int i = v - 0x80;
                int unicode = CtrlMap[i];
                return Convert.ToChar(unicode).ToString();
            }

            if (0xD800 <= v && v <= 0xDBFF)
            {
                // high surrogate
                if (_lastchar == '&')
                {
                    char ch = ReadChar();
                    if (ch == '#')
                    {
                        int v2 = ReadNumericEntityCode(out string value2);
                        if(v2 == -1)
                        {
                            return value + ";" + value2;
                        }
                        if (0xDC00 <= v2 && v2 <= 0xDFFF)
                        {
                            // low surrogate
                            v = char.ConvertToUtf32((char)v, (char)v2);
                        }
                    }
                    else
                    {
                        Error("Premature {0} parsing surrogate pair", ch);
                    }
                }
                else
                {
                    Error("Premature {0} parsing surrogate pair", _lastchar);
                }
            }

            // NOTE (steveb): we need to use ConvertFromUtf32 to allow for extended numeric encodings
            return char.ConvertFromUtf32(v);
        }

        private int ReadNumericEntityCode(out string value)
        {
            int v = 0;
            char ch = ReadChar();
            value = "&#";
            if (ch == 'x')
            {
                bool sawHexDigit = false;
                value += "x";
                ch = ReadChar();
                for (; ch != Entity.EOF && ch != ';'; ch = ReadChar())
                {
                    int p;
                    if (ch >= '0' && ch <= '9')
                    {
                        p = (int)(ch - '0');
                        sawHexDigit = true;
                    } 
                    else if (ch >= 'a' && ch <= 'f')
                    {
                        p = (int)(ch - 'a') + 10;
                        sawHexDigit = true;
                    } 
                    else if (ch >= 'A' && ch <= 'F')
                    {
                        p = (int)(ch - 'A') + 10;
                        sawHexDigit = true;
                    }
                    else
                    {
                        break; //we must be done!
                        //Error("Hex digit out of range '{0}'", (int)ch);
                    }
                    value += ch;
                    v = (v*16) + p;
                }
                if (!sawHexDigit)
                {
                    return -1;
                }
            } 
            else
            {
                bool sawDigit = false;
                for (; ch != Entity.EOF && ch != ';'; ch = ReadChar())
                {
                    if (ch >= '0' && ch <= '9')
                    {
                        v = (v*10) + (int)(ch - '0');
                        sawDigit = true;
                    } 
                    else
                    {
                        break; // we must be done!
                        //Error("Decimal digit out of range '{0}'", (int)ch);
                    }
                    value += ch;
                }
                if (!sawDigit)
                {
                    return -1;
                }
            }
            if (ch == 0)
            {
                Error("Premature {0} parsing entity reference", ch);
            }
            else if (ch == ';')
            {
                ReadChar();
            }
            return v;
        }

        static readonly int[] CtrlMap = new int[] {
                                             // This is the windows-1252 mapping of the code points 0x80 through 0x9f.
                                             8364, 129, 8218, 402, 8222, 8230, 8224, 8225, 710, 8240, 352, 8249, 338, 141,
                                             381, 143, 144, 8216, 8217, 8220, 8221, 8226, 8211, 8212, 732, 8482, 353, 8250, 
                                             339, 157, 382, 376
                                         };

        /// <summary>
        /// Raise a processing error.
        /// </summary>
        /// <param name="msg">The error message to use in the exception.</param>
        /// <exception cref="SgmlParseException">Always thrown.</exception>
        public void Error(string msg)
        {
            throw new SgmlParseException(msg, this);
        }

        /// <summary>
        /// Raise a processing error.
        /// </summary>
        /// <param name="msg">The error message to use in the exception.</param>
        /// <param name="ch">The unexpected character causing the error.</param>
        /// <exception cref="SgmlParseException">Always thrown.</exception>
        public void Error(string msg, char ch)
        {
            string str = (ch == Entity.EOF) ? "EOF" : char.ToString(ch);
            throw new SgmlParseException(string.Format(CultureInfo.CurrentUICulture, msg, str), this);
        }

        /// <summary>
        /// Raise a processing error.
        /// </summary>
        /// <param name="msg">The error message to use in the exception.</param>
        /// <param name="x">The value causing the error.</param>
        /// <exception cref="SgmlParseException">Always thrown.</exception>
        public void Error(string msg, int x)
        {
            throw new SgmlParseException(string.Format(CultureInfo.CurrentUICulture, msg, x), this);
        }

        /// <summary>
        /// Raise a processing error.
        /// </summary>
        /// <param name="msg">The error message to use in the exception.</param>
        /// <param name="arg">The argument for the error.</param>
        /// <exception cref="SgmlParseException">Always thrown.</exception>
        public void Error(string msg, string arg)
        {
            throw new SgmlParseException(string.Format(CultureInfo.CurrentUICulture, msg, arg), this);
        }

        /// <summary>
        /// Returns a string giving information on how the entity is referenced and declared, walking up the parents until the top level parent entity is found.
        /// </summary>
        /// <returns>Contextual information for the entity.</returns>
        public string Context()
        {
            Entity p = this;
            StringBuilder sb = new StringBuilder();
            while (p != null)
            {
                string msg = p._isInternal
                    ? string.Format(CultureInfo.InvariantCulture, "\nReferenced on line {0}, position {1} of internal entity '{2}'", p._line, p.LinePosition, p._name)
                    : string.Format(CultureInfo.InvariantCulture, "\nReferenced on line {0}, position {1} of '{2}' entity at [{3}]", p._line, p.LinePosition, p._name, p.ResolvedUri.AbsolutePath);
                
                sb.Append(msg);
                p = p.Parent;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Checks whether a token denotes a literal entity or not.
        /// </summary>
        /// <param name="token">The token to check.</param>
        /// <returns>true if the token is "CDATA", "SDATA" or "PI", otherwise false.</returns>
        public static bool IsLiteralType(string token)
        {
            return string.Equals(token, "CDATA", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "SDATA", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "PI", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sets the entity to be a literal of the type specified.
        /// </summary>
        /// <param name="token">One of "CDATA", "SDATA" or "PI".</param>
        public void SetLiteralType(string token)
        {
            switch (token)
            {
                case "CDATA":
                    _literalType = LiteralType.CDATA;
                    break;
                case "SDATA":
                    _literalType = LiteralType.SDATA;
                    break;
                case "PI":
                    _literalType = LiteralType.PI;
                    break;
            }
        }

#region IDisposable Members

        /// <summary>
        /// The finalizer for the Entity class.
        /// </summary>
        ~Entity()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. 
        /// </summary>
        /// <param name="isDisposing">true if this method has been called by user code, false if it has been called through a finalizer.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (_stm != null)
                {
                    _stm.Dispose();
                    _stm = null;
                }
            }
        }

#endregion
    }
}
