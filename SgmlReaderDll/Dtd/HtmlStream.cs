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
#if WINDOWS_DESKTOP
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif
using System.Text;

namespace Sgml
{
    // This class decodes an HTML/XML stream correctly.
    internal sealed class HtmlStream : TextReader
    {
        private readonly Stream _stm;
        private readonly byte[] _rawBuffer;
        private int _rawPos;
        private int _rawUsed;
        private Encoding _encoding;
        private readonly Decoder _decoder;
        private char[] _buffer;
        private int _used;
        private int _position;
        private const int BUFSIZE = 16384;
        private const int EOF = -1;

        public HtmlStream(Stream stm, Encoding defaultEncoding)
        {            
            defaultEncoding ??= Encoding.UTF8; // default is UTF8

            if (!stm.CanSeek)
            {
                // Need to be able to seek to sniff correctly.
                stm = CopyToMemoryStream(stm);
            }
            _stm = stm;
            _rawBuffer = new byte[BUFSIZE];
            _rawUsed = stm.Read(_rawBuffer, 0, 4); // maximum byte order mark
            _buffer = new char[BUFSIZE];

            // Check byte order marks
            _decoder = AutoDetectEncoding(_rawBuffer, ref _rawPos, _rawUsed);
            int bom = _rawPos;
            if (_decoder is null)
            {
                _decoder = defaultEncoding.GetDecoder();
                _rawUsed += stm.Read(_rawBuffer, 4, BUFSIZE-4);                
                DecodeBlock();
                // Now sniff to see if there is an XML declaration or HTML <META> tag.
                Decoder sd = SniffEncoding();
                if (sd != null)
                {
                    _decoder = sd;
                }
            }            

            // Reset to get ready for Read()
            _stm.Seek(0, SeekOrigin.Begin);
            _position = _used = 0;
            // skip bom
            if (bom > 0)
            {
                stm.Read(_rawBuffer, 0, bom);
            }
            _rawPos = _rawUsed = 0;            
        }

        public Encoding Encoding => _encoding;

        private static Stream CopyToMemoryStream(Stream s)
        {
            int size = 100000; // large heap is more efficient
            var copyBuff = new byte[size];
            int len;
            MemoryStream r = new MemoryStream();
            while ((len = s.Read(copyBuff, 0, size)) > 0)
                r.Write(copyBuff, 0, len);

            r.Seek(0, SeekOrigin.Begin);                            
            s.Dispose();
            return r;
        }

        internal void DecodeBlock() 
        {
            // shift current chars to beginning.
            if (_position > 0)
            {
                if (_position < _used) 
                {
                    Array.Copy(_buffer, _position, _buffer, 0, _used - _position);
                }
                _used -= _position;
                _position = 0;
            }
            int len = _decoder.GetCharCount(_rawBuffer, _rawPos, _rawUsed - _rawPos);
            int available = _buffer.Length - _used;
            if (available < len)
            {
                char[] newbuf = new char[_buffer.Length + len];
                Array.Copy(_buffer, _position, newbuf, 0, _used - _position);
                _buffer = newbuf;
            }
            _used = _position + _decoder.GetChars(_rawBuffer, _rawPos, _rawUsed - _rawPos, _buffer, _position);
            _rawPos = _rawUsed; // consumed the whole buffer!
        }

        internal static Decoder AutoDetectEncoding(byte[] buffer, ref int index, int length) 
        {
            if (4 <= (length - index))
            {
                uint w = (uint)buffer[index + 0] << 24 | (uint)buffer[index + 1] << 16 | (uint)buffer[index + 2] << 8 | (uint)buffer[index + 3];
                // see if it's a 4-byte encoding
                switch (w)
                {
                    case 0xfefffeff: 
                        index += 4; 
                        return new Ucs4DecoderBigEngian();

                    case 0xfffefffe: 
                        index += 4; 
                        return new Ucs4DecoderLittleEndian();

                    case 0x3c000000: 
                        goto case 0xfefffeff;

                    case 0x0000003c: 
                        goto case 0xfffefffe;
                }
                w >>= 8;
                if (w == 0xefbbbf) 
                {
                    index += 3;
                    return Encoding.UTF8.GetDecoder();
                }
                w >>= 8;
                switch (w) 
                {
                    case 0xfeff: 
                        index += 2; 
                        return UnicodeEncoding.BigEndianUnicode.GetDecoder();

                    case 0xfffe: 
                        index += 2; 
                        return new UnicodeEncoding(false, false).GetDecoder();

                    case 0x3c00: 
                        goto case 0xfeff;

                    case 0x003c: 
                        goto case 0xfffe;
                }
            }
            return null;
        }

        private int ReadChar() 
        {
            // Read only up to end of current buffer then stop.
            return (_position < _used) 
                ? _buffer[_position++]
                : EOF;
        }

        private int PeekChar()
        {
            int ch = ReadChar();
            if (ch != EOF) 
            {
                _position--;
            }
            return ch;
        }
        private bool SniffPattern(string pattern)
        {
            int ch = PeekChar();
            if (ch != pattern[0]) return false;
            for (int i = 0, n = pattern.Length; ch != EOF && i < n; i++) 
            {
                ch = ReadChar();
                char m = pattern[i];
                if (ch != m) 
                {
                    return false;
                }
            }
            return true;
        }
        private void SniffWhitespace() 
        {
            char ch = (char)PeekChar();
            while (ch is ' ' or '\t' or '\r' or '\n')
            {
                int i = _position;
                ch = (char)ReadChar();
                if (ch != ' ' && ch != '\t' && ch != '\r' && ch != '\n')
                    _position = i;
            }
        }

        private string SniffLiteral() 
        {
            int quoteChar = PeekChar();
            if (quoteChar is '\'' or '"') 
            {
                ReadChar();// consume quote char
                int i = _position;
                int ch = ReadChar();
                while (ch != EOF && ch != quoteChar)
                {
                    ch = ReadChar();
                }
                return (_position>i) ? new string(_buffer, i, _position - i - 1) : "";
            }
            return null;
        }

        private string SniffAttribute(string name) 
        {
            SniffWhitespace();
            string id = SniffName();
            if (string.Equals(name, id, StringComparison.OrdinalIgnoreCase))
            {
                SniffWhitespace();
                if (SniffPattern("=")) 
                {
                    SniffWhitespace();
                    return SniffLiteral();
                }
            }
            return null;
        }
        private string SniffAttribute(out string name) 
        {
            SniffWhitespace();
            name = SniffName();
            if (name != null)
            {
                SniffWhitespace();
                if (SniffPattern("=")) 
                {
                    SniffWhitespace();
                    return SniffLiteral();
                }
            }
            return null;
        }
        private void SniffTerminator(string term) 
        {
            int ch = ReadChar();
            int i = 0;
            int n = term.Length;
            while (i < n && ch != EOF) 
            {
                if (term[i] == ch)
                {
                    i++;
                    if (i == n) break;
                } 
                else 
                {
                    i = 0; // reset.
                }
                ch = ReadChar();
            }
        }

        internal Decoder SniffEncoding()
        {
            Decoder decoder = null;
            if (SniffPattern("<?xml"))
            {
                string version = SniffAttribute("version");
                if (version != null)
                {
                    string encoding = SniffAttribute("encoding");
                    if (encoding != null)
                    {
                        try
                        {
                            Encoding enc = Encoding.GetEncoding(encoding);
                            if (enc != null)
                            {
                                _encoding = enc;
                                return enc.GetDecoder();
                            }
                        }
                        catch (ArgumentException)
                        {
                            // oh well then.
                        }
                    }
                    SniffTerminator(">");
                }
            } 
            if (decoder is null) 
            {
                return SniffMeta();
            }
            return null;
        }

        internal Decoder SniffMeta()
        {
            int i = ReadChar();            
            while (i != EOF)
            {
                char ch = (char)i;
                if (ch == '<')
                {
                    string name = SniffName();
                    if (name != null && Extensions.EqualsIgnoreCase(name, "meta"))
                    {
                        string httpequiv = null;
                        string content = null;
                        while (true)
                        {
                            string value = SniffAttribute(out name);
                            if (name is null)
                                break;

                            if (Extensions.EqualsIgnoreCase(name, "http-equiv"))
                            {
                                httpequiv = value;
                            }
                            else if (Extensions.EqualsIgnoreCase(name, "content"))
                            {
                                content = value;
                            }
                        }

                        if (httpequiv != null && Extensions.EqualsIgnoreCase(httpequiv, "content-type") && content != null)
                        {
                            int j = content.IndexOf("charset");
                            if (j >= 0)
                            {
                                //charset=utf-8
                                j = content.IndexOf("=", j);
                                if (j >= 0)
                                {
                                    j++;
                                    int k = content.IndexOf(";", j);
                                    if (k<0) k = content.Length;
                                    string charset = content.Substring(j, k-j).Trim();
                                    try
                                    {
                                        Encoding e = Encoding.GetEncoding(charset);
                                        _encoding = e;
                                        return e.GetDecoder();
                                    } catch (ArgumentException) { }
                                }                                
                            }
                        }
                    }
                }
                i = ReadChar();

            }
            return null;
        }

        internal string SniffName()
        {
            int c = PeekChar();
            if (c == EOF)
                return null;
            char ch = (char)c;
            int start = _position;
            while (_position < _used - 1 && (char.IsLetterOrDigit(ch) || ch is '-' or '_' or ':'))
                ch = _buffer[++_position];

            if (start == _position)
                return null;

            return new string(_buffer, start, _position - start);
        }

        internal void SkipWhitespace()
        {
            char ch = (char)PeekChar();
            while (_position < _used - 1 && (ch is ' ' or '\r' or '\n'))
                ch = _buffer[++_position];
        }

        internal void SkipTo(char what)
        {
            char ch = (char)PeekChar();
            while (_position < _used - 1 && (ch != what))
                ch = _buffer[++_position];
        }

        internal string ParseAttribute()
        {
            SkipTo('=');
            if (_position < _used)
            {
                _position++;
                SkipWhitespace();
                if (_position < _used) 
                {
                    char quote = _buffer[_position];
                    _position++;
                    int start = _position;
                    SkipTo(quote);
                    if (_position < _used) 
                    {
                        string result = new string(_buffer, start, _position - start);
                        _position++;
                        return result;
                    }
                }
            }
            return null;
        }

        public override int Peek() 
        {
            int result = Read();
            if (result != EOF)
            {
                _position--;
            }
            return result;
        }
        public override int Read()
        {
            if (_position == _used)
            {
                _rawUsed = _stm.Read(_rawBuffer, 0, _rawBuffer.Length);
                _rawPos = 0;
                if (_rawUsed == 0) return EOF;
                DecodeBlock();
            }
            if (_position < _used) return _buffer[_position++];
            return -1;
        }

        public override int Read(char[] buffer, int start, int length) 
        {
            if (_position == _used) 
            {
                _rawUsed = _stm.Read(_rawBuffer, 0, _rawBuffer.Length);
                _rawPos = 0;
                if (_rawUsed == 0) return -1;
                DecodeBlock();
            }
            if (_position < _used) 
            {
                length = Math.Min(_used - _position, length);
                Array.Copy(_buffer, _position, buffer, start, length);
                _position += length;
                return length;
            }
            return 0;
        }

        public override int ReadBlock(char[] data, int index, int count)
        {
            return Read(data, index, count);
        }

        // Read up to end of line, or full buffer, whichever comes first.
        public int ReadLine(char[] buffer, int start, int length)
        {
            int i = 0;
            int ch = ReadChar();
            while (ch != EOF)
            {
                buffer[i+start] = (char)ch;
                i++;
                if (i+start == length) 
                    break; // buffer is full

                if (ch == '\r') 
                {
                    if (PeekChar() == '\n') 
                    {
                        ch = ReadChar();
                        buffer[i + start] = (char)ch;
                        i++;
                    }
                    break;
                } 
                else if (ch == '\n') 
                {
                    break;
                }
                ch = ReadChar();
            }
            return i;
        }

        public override string ReadToEnd()
        {
            char[] buffer = new char[100_000]; // large block heap is more efficient
            int len;
            var sb = new StringBuilder();
            while ((len = Read(buffer, 0, buffer.Length)) > 0) 
            {
                sb.Append(buffer, 0, len);
            }
            return sb.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _stm.Dispose();
        }
    }
}
