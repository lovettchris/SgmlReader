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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
#if WINDOWS_DESKTOP
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif
using System.Text;
using System.Xml;

namespace Sgml
{
    /// <summary>
    /// Provides DTD parsing and support for the SgmlParser framework.
    /// </summary>
    public class SgmlDtd
    {
        private readonly Dictionary<string, ElementDecl> _elements;
        private readonly Dictionary<string, Entity> _pentities;
        private readonly Dictionary<string, Entity> _entities;
        private readonly StringBuilder _sb;
        private Entity _current;
        private readonly IEntityResolver _resolver;

        /// <summary>
        /// Initialises a new instance of the <see cref="SgmlDtd"/> class.
        /// </summary>
        /// <param name="name">The name of the DTD. This value is assigned to <see cref="Name"/>.</param>
        /// <param name="nt">The <see cref="XmlNameTable"/> is NOT used.</param>
        /// <param name="resolver">The resolver to use for loading this entity</param>
        public SgmlDtd(string name, XmlNameTable nt, IEntityResolver resolver)
        {
            Name = name;
            _elements = new();
            _pentities = new();
            _entities = new();
            _sb = new StringBuilder();
            _resolver = resolver;
        }

        /// <summary>
        /// The name of the DTD.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the XmlNameTable associated with this implementation.
        /// </summary>
        /// <value>The XmlNameTable enabling you to get the atomized version of a string within the node.</value>
        public XmlNameTable NameTable => null;

        /// <summary>
        /// Parses a DTD and creates a <see cref="SgmlDtd"/> instance that encapsulates the DTD.
        /// </summary>
        /// <param name="baseUri">The base URI of the DTD.</param>
        /// <param name="name">The name of the DTD. This value is assigned to <see cref="Name"/>.</param>
        /// <param name="pubid"></param>
        /// <param name="url"></param>
        /// <param name="subset"></param>
        /// <param name="nt">The <see cref="XmlNameTable"/> is NOT used.</param>
        /// <param name="resolver">The resolver to use for loading this entity</param>
        /// <returns>A new <see cref="SgmlDtd"/> instance that encapsulates the DTD.</returns>
        public static SgmlDtd Parse(Uri baseUri, string name, string pubid, string url, string subset, XmlNameTable nt, IEntityResolver resolver)
        {
            SgmlDtd dtd = new SgmlDtd(name, nt, resolver);
            if (!string.IsNullOrEmpty(url))
            {
                dtd.PushEntity(baseUri, new Entity(dtd.Name, pubid, url, resolver));
            }

            if (!string.IsNullOrEmpty(subset))
            {
                dtd.PushEntity(baseUri, new Entity(name, subset, resolver));
            }

            try 
            {
                dtd.Parse();
            } 
            catch (Exception e)
            {
                throw new SgmlParseException(e.Message + dtd._current.Context());
            }

            return dtd;
        }

        /// <summary>
        /// Parses a DTD and creates a <see cref="SgmlDtd"/> instance that encapsulates the DTD.
        /// </summary>
        /// <param name="baseUri">The base URI of the DTD.</param>
        /// <param name="name">The name of the DTD. This value is assigned to <see cref="Name"/>.</param>
        /// <param name="input">The reader to load the DTD from.</param>
        /// <param name="subset"></param>
        /// <param name="nt">The <see cref="XmlNameTable"/> is NOT used.</param>
        /// <param name="resolver">The resolver to use for loading this entity</param>
        /// <returns>A new <see cref="SgmlDtd"/> instance that encapsulates the DTD.</returns>
        public static SgmlDtd Parse(Uri baseUri, string name, TextReader input, string subset, XmlNameTable nt, IEntityResolver resolver)
        {
            SgmlDtd dtd = new SgmlDtd(name, nt, resolver);
            dtd.PushEntity(baseUri, new Entity(dtd.Name, baseUri, input, resolver));
            if (!string.IsNullOrEmpty(subset))
            {
                dtd.PushEntity(baseUri, new Entity(name, subset, resolver));
            }

            try
            {
                dtd.Parse();
            } 
            catch (Exception e)
            {
                throw new SgmlParseException(e.Message + dtd._current.Context());
            }

            return dtd;
        }

        /// <summary>
        /// Finds an entity in the DTD with the specified name.
        /// </summary>
        /// <param name="name">The name of the <see cref="Entity"/> to find.</param>
        /// <returns>The specified Entity from the DTD.</returns>
        public Entity FindEntity(string name)
        {
            _entities.TryGetValue(name, out Entity e);
            return e;
        }

        /// <summary>
        /// Finds an element declaration in the DTD with the specified name.
        /// </summary>
        /// <param name="name">The name of the <see cref="ElementDecl"/> to find and return.</param>
        /// <returns>The <see cref="ElementDecl"/> matching the specified name.</returns>
        public ElementDecl FindElement(string name)
        {
            _elements.TryGetValue(name.ToUpperInvariant(), out ElementDecl el);
            return el;
        }

        //-------------------------------- Parser -------------------------
        private void PushEntity(Uri baseUri, Entity e)
        {
            e.Open(_current, baseUri);
            _current = e;
            _current.ReadChar();
        }

        private void PopEntity()
        {
            if (_current != null) _current.Close();
            if (_current.Parent != null) 
            {
                _current = _current.Parent;
            } 
            else 
            {
                _current = null;
            }
        }

        private void Parse()
        {
            char ch = _current.Lastchar;
            while (true) 
            {
                switch (ch) 
                {
                    case Entity.EOF:
                        PopEntity();
                        if (_current is null)
                            return;
                        ch = _current.Lastchar;
                        break;
                    case ' ':
                    case '\n':
                    case '\r':
                    case '\t':
                        ch = _current.ReadChar();
                        break;
                    case '<':
                        ParseMarkup();
                        ch = _current.ReadChar();
                        break;
                    case '%':
                        Entity e = ParseParameterEntity(SgmlDtd.WhiteSpace);
                        try 
                        {
                            PushEntity(_current.ResolvedUri, e);
                        } 
                        catch (Exception ex) 
                        {
                            // BUG: need an error log.
                            Debug.WriteLine(ex.Message + _current.Context());
                        }
                        ch = _current.Lastchar;
                        break;
                    default:
                        _current.Error("Unexpected character '{0}'", ch);
                        break;
                }               
            }
        }

        void ParseMarkup()
        {
            char ch = _current.ReadChar();
            if (ch != '!') 
            {
                _current.Error("Found '{0}', but expecing declaration starting with '<!'");
                return;
            }
            ch = _current.ReadChar();
            if (ch == '-') 
            {
                ch = _current.ReadChar();
                if (ch != '-') _current.Error("Expecting comment '<!--' but found {0}", ch);
                _current.ScanToEnd(_sb, "Comment", "-->");
            } 
            else if (ch == '[') 
            {
                ParseMarkedSection();
            }
            else 
            {
                string token = _current.ScanToken(_sb, SgmlDtd.WhiteSpace, true);
                switch (token) 
                {
                    case "ENTITY":
                        ParseEntity();
                        break;
                    case "ELEMENT":
                        ParseElementDecl();
                        break;
                    case "ATTLIST":
                        ParseAttList();
                        break;
                    default:
                        _current.Error("Invalid declaration '<!{0}'.  Expecting 'ENTITY', 'ELEMENT' or 'ATTLIST'.", token);
                        break;
                }
            }
        }

        char ParseDeclComments()
        {
            char ch = _current.Lastchar;
            while (ch == '-') 
            {
                ch = ParseDeclComment(true);
            }
            return ch;
        }

        char ParseDeclComment(bool full)
        {
            // This method scans over a comment inside a markup declaration.
            char ch = _current.ReadChar();
            if (full && ch != '-') _current.Error("Expecting comment delimiter '--' but found {0}", ch);
            _current.ScanToEnd(_sb, "Markup Comment", "--");
            return _current.SkipWhitespace();
        }

        void ParseMarkedSection()
        {
            // <![^ name [ ... ]]>
            _current.ReadChar(); // move to next char.
            string name = ScanName("[");
            if (string.Equals(name, "INCLUDE", StringComparison.OrdinalIgnoreCase)) 
            {
                ParseIncludeSection();
            } 
            else if (string.Equals(name, "IGNORE", StringComparison.OrdinalIgnoreCase)) 
            {
                ParseIgnoreSection();
            }
            else 
            {
                _current.Error("Unsupported marked section type '{0}'", name);
            }
        }

        private void ParseIncludeSection()
        {
            throw new NotImplementedException("Include Section");
        }

        void ParseIgnoreSection()
        {
            char ch = _current.SkipWhitespace();
            if (ch != '[') _current.Error("Expecting '[' but found {0}", ch);
            _current.ScanToEnd(_sb, "Conditional Section", "]]>");
        }

        string ScanName(string term)
        {
            // skip whitespace, scan name (which may be parameter entity reference
            // which is then expanded to a name)
            char ch = _current.SkipWhitespace();
            if (ch == '%') 
            {
                Entity e = ParseParameterEntity(term);
                ch = _current.Lastchar;
                // bugbug - need to support external and nested parameter entities
                if (!e.IsInternal) throw new NotSupportedException("External parameter entity resolution");
                return e.Literal.Trim();
            } 
            else 
            {
                return _current.ScanToken(_sb, term, true);
            }
        }

        private Entity ParseParameterEntity(string term)
        {
            // almost the same as this.current.ScanToken, except we also terminate on ';'
            _current.ReadChar();
            string name =  _current.ScanToken(_sb, ";"+term, false);
            if (_current.Lastchar == ';') 
                _current.ReadChar();
            Entity e = GetParameterEntity(name);
            return e;
        }

        private Entity GetParameterEntity(string name)
        {
            _pentities.TryGetValue(name, out Entity e);
            if (e is null)
                _current.Error("Reference to undefined parameter entity '{0}'", name);

            return e;
        }

        /// <summary>
        /// Returns a dictionary for looking up entities by their <see cref="Entity.Literal"/> value.
        /// </summary>
        /// <returns>A dictionary for looking up entities by their <see cref="Entity.Literal"/> value.</returns>
        public Dictionary<string, Entity> GetEntitiesLiteralNameLookup()
        {
            Dictionary<string, Entity> hashtable = new Dictionary<string, Entity>();
            foreach (Entity entity in _entities.Values)
                hashtable[entity.Literal] = entity;

            return hashtable;
        }
        
        private const string WhiteSpace = " \r\n\t";

        private void ParseEntity()
        {
            char ch = _current.SkipWhitespace();
            bool pe = (ch == '%');
            if (pe)
            {
                // parameter entity.
                _current.ReadChar(); // move to next char
                ch = _current.SkipWhitespace();
            }
            string name = _current.ScanToken(_sb, SgmlDtd.WhiteSpace, true);
            ch = _current.SkipWhitespace();
            Entity e;
            if (ch is '"' or '\'') 
            {
                string literal = _current.ScanLiteral(_sb, ch);
                e = new Entity(name, literal, _resolver);                
            } 
            else 
            {
                string pubid = null;
                string extid;
                string tok = _current.ScanToken(_sb, SgmlDtd.WhiteSpace, true);
                if (Entity.IsLiteralType(tok))
                {
                    ch = _current.SkipWhitespace();
                    string literal = _current.ScanLiteral(_sb, ch);
                    e = new Entity(name, literal, _resolver);
                    e.SetLiteralType(tok);
                }
                else 
                {
                    extid = tok;
                    if (string.Equals(extid, "PUBLIC", StringComparison.OrdinalIgnoreCase)) 
                    {
                        ch = _current.SkipWhitespace();
                        if (ch is '"' or '\'') 
                        {
                            pubid = _current.ScanLiteral(_sb, ch);
                        } 
                        else 
                        {
                            _current.Error("Expecting public identifier literal but found '{0}'",ch);
                        }
                    } 
                    else if (!string.Equals(extid, "SYSTEM", StringComparison.OrdinalIgnoreCase)) 
                    {
                        _current.Error("Invalid external identifier '{0}'.  Expecing 'PUBLIC' or 'SYSTEM'.", extid);
                    }
                    string uri = null;
                    ch = _current.SkipWhitespace();
                    if (ch is '"' or '\'') 
                    {
                        uri = _current.ScanLiteral(_sb, ch);
                    } 
                    else if (ch != '>')
                    {
                        _current.Error("Expecting system identifier literal but found '{0}'",ch);
                    }
                    e = new Entity(name, pubid, uri, _resolver);
                }
            }
            ch = _current.SkipWhitespace();
            if (ch == '-') 
                ch = ParseDeclComments();
            if (ch != '>') 
            {
                _current.Error("Expecting end of entity declaration '>' but found '{0}'", ch);  
            }           
            if (pe)
                _pentities.Add(e.Name, e);
            else
                _entities.Add(e.Name, e);
        }

        private void ParseElementDecl()
        {
            char ch = _current.SkipWhitespace();
            string[] names = ParseNameGroup(ch, true);
            ch = char.ToUpperInvariant(_current.SkipWhitespace());
            bool sto = false;
            bool eto = false;
            if (ch is 'O' or '-')
            {
                sto = (ch == 'O'); // start tag optional?   
                _current.ReadChar();
                ch = char.ToUpperInvariant(_current.SkipWhitespace());
                if (ch is 'O' or '-')
                {
                    eto = (ch == 'O'); // end tag optional? 
                    ch = _current.ReadChar();
                }
            }
            ch = _current.SkipWhitespace();
            ContentModel cm = ParseContentModel(ch);
            ch = _current.SkipWhitespace();

            string [] exclusions = null;
            string [] inclusions = null;

            if (ch == '-') 
            {
                ch = _current.ReadChar();
                if (ch == '(') 
                {
                    exclusions = ParseNameGroup(ch, true);
                    ch = _current.SkipWhitespace();
                }
                else if (ch == '-') 
                {
                    ch = ParseDeclComment(false);
                } 
                else 
                {
                    _current.Error("Invalid syntax at '{0}'", ch);  
                }
            }

            if (ch == '-') 
                ch = ParseDeclComments();

            if (ch == '+') 
            {
                ch = _current.ReadChar();
                if (ch != '(') 
                {
                    _current.Error("Expecting inclusions name group", ch);  
                }
                inclusions = ParseNameGroup(ch, true);
                ch = _current.SkipWhitespace();
            }

            if (ch == '-') 
                ch = ParseDeclComments();


            if (ch != '>') 
            {
                _current.Error("Expecting end of ELEMENT declaration '>' but found '{0}'", ch); 
            }

            foreach (string name in names) 
            {
                string atom = name.ToUpperInvariant();
                _elements.Add(atom, new ElementDecl(atom, sto, eto, cm, inclusions, exclusions));
            }
        }

        const string ngterm = " \r\n\t|,)";
        string[] ParseNameGroup(char ch, bool nmtokens)
        {
            List<string> names = new List<string>();
            if (ch == '(') 
            {
                ch = _current.ReadChar();
                ch = _current.SkipWhitespace();
                while (ch != ')') 
                {
                    // skip whitespace, scan name (which may be parameter entity reference
                    // which is then expanded to a name)                    
                    ch = _current.SkipWhitespace();
                    if (ch == '%') 
                    {
                        Entity e = ParseParameterEntity(SgmlDtd.ngterm);
                        PushEntity(_current.ResolvedUri, e);
                        ParseNameList(names, nmtokens);
                        PopEntity();
                        ch = _current.Lastchar;
                    }
                    else 
                    {
                        string token = _current.ScanToken(_sb, SgmlDtd.ngterm, nmtokens);
                        token = token.ToUpperInvariant();
                        names.Add(token);
                    }
                    ch = _current.SkipWhitespace();
                    if (ch is '|' or ',')
                    {
                        ch = _current.ReadChar();
                    }
                }
                _current.ReadChar(); // consume ')'
            } 
            else 
            {
                string name = _current.ScanToken(_sb, SgmlDtd.WhiteSpace, nmtokens);
                name = name.ToUpperInvariant();
                names.Add(name);
            }
            return (string[])names.ToArray();
        }

        void ParseNameList(List<string> names, bool nmtokens)
        {
            char ch = _current.Lastchar;
            ch = _current.SkipWhitespace();
            while (ch != Entity.EOF) 
            {
                string name;
                if (ch == '%') 
                {
                    Entity e = ParseParameterEntity(SgmlDtd.ngterm);
                    PushEntity(_current.ResolvedUri, e);
                    ParseNameList(names, nmtokens);
                    PopEntity();
                    ch = _current.Lastchar;
                } 
                else 
                {
                    name = _current.ScanToken(_sb, SgmlDtd.ngterm, true);
                    name = name.ToUpperInvariant();
                    names.Add(name);
                }
                ch = _current.SkipWhitespace();
                if (ch == '|') 
                {
                    ch = _current.ReadChar();
                    ch = _current.SkipWhitespace();
                }
            }
        }

        const string dcterm = " \r\n\t>";
        private ContentModel ParseContentModel(char ch)
        {
            ContentModel cm = new ContentModel();
            if (ch == '(') 
            {
                _current.ReadChar();
                ParseModel(')', cm);
                ch = _current.ReadChar();
                if (ch is '?' or '+' or '*') 
                {
                    cm.AddOccurrence(ch);
                    _current.ReadChar();
                }
            } 
            else if (ch == '%') 
            {
                Entity e = ParseParameterEntity(SgmlDtd.dcterm);
                PushEntity(_current.ResolvedUri, e);
                cm = ParseContentModel(_current.Lastchar);
                PopEntity(); // bugbug should be at EOF.
            }
            else
            {
                string dc = ScanName(SgmlDtd.dcterm);
                cm.SetDeclaredContent(dc);
            }
            return cm;
        }

        const string cmterm = " \r\n\t,&|()?+*";
        void ParseModel(char cmt, ContentModel cm)
        {
            // Called when part of the model is made up of the contents of a parameter entity
            int depth = cm.CurrentDepth;
            char ch = _current.Lastchar;
            ch = _current.SkipWhitespace();
            while (ch != cmt || cm.CurrentDepth > depth) // the entity must terminate while inside the content model.
            {
                if (ch == Entity.EOF) 
                {
                    _current.Error("Content Model was not closed");
                }
                if (ch == '%') 
                {
                    Entity e = ParseParameterEntity(SgmlDtd.cmterm);
                    PushEntity(_current.ResolvedUri, e);
                    ParseModel(Entity.EOF, cm);
                    PopEntity();                    
                    ch = _current.SkipWhitespace();
                } 
                else if (ch == '(') 
                {
                    cm.PushGroup();
                    _current.ReadChar();// consume '('
                    ch = _current.SkipWhitespace();
                }
                else if (ch == ')') 
                {
                    ch = _current.ReadChar();// consume ')'
                    if (ch is '*' or '+' or '?') 
                    {
                        cm.AddOccurrence(ch);
                        ch = _current.ReadChar();
                    }
                    if (cm.PopGroup() < depth)
                    {
                        _current.Error("Parameter entity cannot close a paren outside it's own scope");
                    }
                    ch = _current.SkipWhitespace();
                }
                else if (ch is ',' or '|' or '&') 
                {
                    cm.AddConnector(ch);
                    _current.ReadChar(); // skip connector
                    ch = _current.SkipWhitespace();
                }
                else
                {
                    string token;
                    if (ch == '#') 
                    {
                        ch = _current.ReadChar();
                        token = "#" + _current.ScanToken(_sb, SgmlDtd.cmterm, true); // since '#' is not a valid name character.
                    } 
                    else 
                    {
                        token = _current.ScanToken(_sb, SgmlDtd.cmterm, true);
                    }

                    token = token.ToUpperInvariant();
                    ch = _current.Lastchar;
                    if (ch is '?' or '+' or '*') 
                    {
                        cm.PushGroup();
                        cm.AddSymbol(token);
                        cm.AddOccurrence(ch);
                        cm.PopGroup();
                        _current.ReadChar(); // skip connector
                        ch = _current.SkipWhitespace();
                    } 
                    else 
                    {
                        cm.AddSymbol(token);
                        ch = _current.SkipWhitespace();
                    }                   
                }
            }
        }

        void ParseAttList()
        {
            char ch = _current.SkipWhitespace();
            string[] names = ParseNameGroup(ch, true);          
            Dictionary<string, AttDef> attlist = new Dictionary<string, AttDef>();
            ParseAttList(attlist, '>');
            foreach (string name in names)
            {
                if (!_elements.TryGetValue(name, out ElementDecl e)) 
                {
                    _current.Error("ATTLIST references undefined ELEMENT {0}", name);
                }

                e.AddAttDefs(attlist);
            }
        }

        const string peterm = " \t\r\n>";
        void ParseAttList(Dictionary<string, AttDef> list, char term)
        {
            char ch = _current.SkipWhitespace();
            while (ch != term) 
            {
                if (ch == '%') 
                {
                    Entity e = ParseParameterEntity(SgmlDtd.peterm);
                    PushEntity(_current.ResolvedUri, e);
                    ParseAttList(list, Entity.EOF);
                    PopEntity();                    
                    ch = _current.SkipWhitespace();
                } 
                else if (ch == '-') 
                {
                    ch = ParseDeclComments();
                }
                else
                {
                    AttDef a = ParseAttDef(ch);
                    list.Add(a.Name, a);
                }
                ch = _current.SkipWhitespace();
            }
        }

        AttDef ParseAttDef(char ch)
        {
            ch = _current.SkipWhitespace();
            string name = ScanName(SgmlDtd.WhiteSpace);
            name = name.ToUpperInvariant();
            AttDef attdef = new AttDef(name);

            ch = _current.SkipWhitespace();
            if (ch == '-') 
                ch = ParseDeclComments();               

            ParseAttType(ch, attdef);

            ch = _current.SkipWhitespace();
            if (ch == '-') 
                ch = ParseDeclComments();               

            ParseAttDefault(ch, attdef);

            ch = _current.SkipWhitespace();
            if (ch == '-') 
                ch = ParseDeclComments();               

            return attdef;

        }

        void ParseAttType(char ch, AttDef attdef)
        {
            if (ch == '%')
            {
                Entity e = ParseParameterEntity(SgmlDtd.WhiteSpace);
                PushEntity(_current.ResolvedUri, e);
                ParseAttType(_current.Lastchar, attdef);
                PopEntity(); // bugbug - are we at the end of the entity?
                ch = _current.Lastchar;
                return;
            }

            if (ch == '(') 
            {
                //attdef.EnumValues = ParseNameGroup(ch, false);  
                //attdef.Type = AttributeType.ENUMERATION;
                attdef.SetEnumeratedType(ParseNameGroup(ch, false), AttributeType.ENUMERATION);
            } 
            else 
            {
                string token = ScanName(SgmlDtd.WhiteSpace);
                if (string.Equals(token, "NOTATION", StringComparison.OrdinalIgnoreCase)) 
                {
                    ch = _current.SkipWhitespace();
                    if (ch != '(') 
                    {
                        _current.Error("Expecting name group '(', but found '{0}'", ch);
                    }
                    //attdef.Type = AttributeType.NOTATION;
                    //attdef.EnumValues = ParseNameGroup(ch, true);
                    attdef.SetEnumeratedType(ParseNameGroup(ch, true), AttributeType.NOTATION);
                } 
                else 
                {
                    attdef.SetType(token);
                }
            }
        }

        void ParseAttDefault(char ch, AttDef attdef)
        {
            if (ch == '%')
            {
                Entity e = ParseParameterEntity(SgmlDtd.WhiteSpace);
                PushEntity(_current.ResolvedUri, e);
                ParseAttDefault(_current.Lastchar, attdef);
                PopEntity(); // bugbug - are we at the end of the entity?
                ch = _current.Lastchar;
                return;
            }

            bool hasdef = true;
            if (ch == '#') 
            {
                _current.ReadChar();
                string token = _current.ScanToken(_sb, SgmlDtd.WhiteSpace, true);
                hasdef = attdef.SetPresence(token);
                ch = _current.SkipWhitespace();
            } 
            if (hasdef) 
            {
                if (ch is '\'' or '"') 
                {
                    string lit = _current.ScanLiteral(_sb, ch);
                    attdef.Default = lit;
                    ch = _current.SkipWhitespace();
                }
                else
                {
                    string name = _current.ScanToken(_sb, SgmlDtd.WhiteSpace, false);
                    name = name.ToUpperInvariant();
                    attdef.Default = name; // bugbug - must be one of the enumerated names.
                    ch = _current.SkipWhitespace();
                }
            }
        }
    }
}
