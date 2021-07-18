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
#if WINDOWS_DESKTOP
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

namespace Sgml
{
    /// <summary>
    /// An element declaration in a DTD.
    /// </summary>
    public class ElementDecl
    {
        private readonly string _name;
        private readonly bool _startTagOptional;
        private readonly bool _endTagOptional;
        private readonly ContentModel _contentModel;
        private readonly string[] _inclusions;
        private readonly string[] _exclusions;
        private Dictionary<string, AttDef> _attList;

        /// <summary>
        /// Initialises a new element declaration instance.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="startTagOptional">Whether the start tag is optional.</param>
        /// <param name="endTagOptional">Whether the end tag is optional.</param>
        /// <param name="contentModel">The <see cref="ContentModel"/> of the element.</param>
        /// <param name="inclusions"></param>
        /// <param name="exclusions"></param>
        public ElementDecl(string name, bool startTagOptional, bool endTagOptional, ContentModel contentModel, string[] inclusions, string[] exclusions)
        {
            _name = name;
            _startTagOptional = startTagOptional;
            _endTagOptional = endTagOptional;
            _contentModel = contentModel;
            _inclusions = inclusions;
            _exclusions = exclusions;
        }

        /// <summary>
        /// The element name.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// The <see cref="Sgml.ContentModel"/> of the element declaration.
        /// </summary>
        public ContentModel ContentModel => _contentModel;

        /// <summary>
        /// Whether the end tag of the element is optional.
        /// </summary>
        /// <value>true if the end tag of the element is optional, otherwise false.</value>
        public bool EndTagOptional => _endTagOptional;

        /// <summary>
        /// Whether the start tag of the element is optional.
        /// </summary>
        /// <value>true if the start tag of the element is optional, otherwise false.</value>
        public bool StartTagOptional => _startTagOptional; 

        /// <summary>
        /// Finds the attribute definition with the specified name.
        /// </summary>
        /// <param name="name">The name of the <see cref="AttDef"/> to find.</param>
        /// <returns>The <see cref="AttDef"/> with the specified name.</returns>
        /// <exception cref="InvalidOperationException">If the attribute list has not yet been initialised.</exception>
        public AttDef FindAttribute(string name)
        {
            if (_attList is null)
                throw new InvalidOperationException("The attribute list for the element declaration has not been initialised.");

            _attList.TryGetValue(name.ToUpperInvariant(), out AttDef a);
            return a;
        }

        /// <summary>
        /// Adds attribute definitions to the element declaration.
        /// </summary>
        /// <param name="list">The list of attribute definitions to add.</param>
        public void AddAttDefs(Dictionary<string, AttDef> list)
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));

            if (_attList is null) 
            {
                _attList = list;
            } 
            else 
            {
                foreach (AttDef a in list.Values) 
                {
                    if (!_attList.ContainsKey(a.Name)) 
                    {
                        _attList.Add(a.Name, a);
                    }
                }
            }
        }

        /// <summary>
        /// Tests whether this element can contain another specified element.
        /// </summary>
        /// <param name="name">The name of the element to check for.</param>
        /// <param name="dtd">The DTD to use to do the check.</param>
        /// <returns>True if the specified element can be contained by this element.</returns>
        public bool CanContain(string name, SgmlDtd dtd)
        {            
            // return true if this element is allowed to contain the given element.
            if (_exclusions is not null) 
            {
                foreach (string s in _exclusions) 
                {
                    if (string.Equals(s, name, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            if (_inclusions is not null) 
            {
                foreach (string s in _inclusions) 
                {
                    if (string.Equals(s, name, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return _contentModel.CanContain(name, dtd);
        }
    }
}
