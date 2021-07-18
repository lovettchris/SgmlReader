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
using System.Diagnostics;
using System.Xml;

namespace Sgml
{
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
        private readonly HWStack<Attribute> _attributes = new (10);

        /// <summary>
        /// Attribute objects are reused during parsing to reduce memory allocations, 
        /// hence the Reset method. 
        /// </summary>
        public void Reset(string name, XmlNodeType nt, string value) 
        {           
            Value = value;
            Name = name;
            NodeType = nt;
            Space = XmlSpace.None;
            XmlLang= null;
            IsEmpty = true;
            _attributes.Count = 0;
            DtdType = null;
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

        public void RemoveAttribute(string name)
        {
            for (int i = 0, n = _attributes.Count; i < n; i++)
            {
                Attribute a  = _attributes[i];
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))
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

        public int GetAttribute(string name) 
        {
            for (int i = 0, n = _attributes.Count; i < n; i++) 
            {
                Attribute a = _attributes[i];
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) 
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
    }
}