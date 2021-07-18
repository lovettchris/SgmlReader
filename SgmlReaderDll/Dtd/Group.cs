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
    /// Defines a group of elements nested within another element.
    /// </summary>
    public class Group
    {
        private readonly Group _parent;
        private readonly List<object> _members;
        private GroupType _groupType;
        private Occurrence _occurrence;
        private bool _isMixed;

        /// <summary>
        /// The <see cref="Occurrence"/> of this group.
        /// </summary>
        public Occurrence Occurrence => _occurrence;

        /// <summary>
        /// Checks whether the group contains only text.
        /// </summary>
        /// <value>true if the group is of mixed content and has no members, otherwise false.</value>
        public bool TextOnly => _isMixed && _members.Count == 0;

        /// <summary>
        /// The parent group of this group.
        /// </summary>
        public Group Parent => _parent;
        /// <summary>
        /// Initialises a new Content Model Group.
        /// </summary>
        /// <param name="parent">The parent model group.</param>
        public Group(Group parent)
        {
            _parent = parent;
            _members = new List<object>();
            _groupType = GroupType.None;
            _occurrence = Occurrence.Required;
        }

        /// <summary>
        /// Adds a new child model group to the end of the group's members.
        /// </summary>
        /// <param name="g">The model group to add.</param>
        public void AddGroup(Group g)
        {
            _members.Add(g);
        }

        /// <summary>
        /// Adds a new symbol to the group's members.
        /// </summary>
        /// <param name="sym">The symbol to add.</param>
        public void AddSymbol(string sym)
        {
            if (string.Equals(sym, "#PCDATA", StringComparison.OrdinalIgnoreCase)) 
            {               
                _isMixed = true;
            } 
            else 
            {
                _members.Add(sym);
            }
        }

        /// <summary>
        /// Adds a connector onto the member list.
        /// </summary>
        /// <param name="c">The connector character to add.</param>
        /// <exception cref="SgmlParseException">
        /// If the content is not mixed and has no members yet, or if the group type has been set and the
        /// connector does not match the group type.
        /// </exception>
        public void AddConnector(char c)
        {
            if (!_isMixed && _members.Count == 0) 
            {
                throw new SgmlParseException($"Missing token before connector '{c}'.");
            }

            GroupType gt = c switch
            {
                ',' => GroupType.Sequence,
                '|' => GroupType.Or,
                '&' => GroupType.And,
                _   => GroupType.None
            };

            if (_groupType != GroupType.None && _groupType != gt) 
            {
                throw new SgmlParseException($"Connector '{c}' is inconsistent with {_groupType} group.");
            }

            _groupType = gt;
        }

        /// <summary>
        /// Adds an occurrence character for this group, setting it's <see cref="Occurrence"/> value.
        /// </summary>
        /// <param name="c">The occurrence character.</param>
        public void AddOccurrence(char c)
        {
            _occurrence = c switch
            {
                '?' => Occurrence.Optional,
                '+' => Occurrence.OneOrMore,
                '*' => Occurrence.ZeroOrMore,
                _   => Occurrence.Required
            };
        }

        /// <summary>
        /// Checks whether an element using this group can contain a specified element.
        /// </summary>
        /// <param name="name">The name of the element to look for.</param>
        /// <param name="dtd">The DTD to use during the checking.</param>
        /// <returns>true if an element using this group can contain the element, otherwise false.</returns>
        /// <remarks>
        /// Rough approximation - this is really assuming an "Or" group
        /// </remarks>
        public bool CanContain(string name, SgmlDtd dtd)
        {
            if (dtd is null)
                throw new ArgumentNullException(nameof(dtd));

            // Do a simple search of members.
            foreach (object obj in _members) 
            {
                if (obj is string s) 
                {
                    if (string.Equals(s, name, StringComparison.OrdinalIgnoreCase))
                        return true;
                } 
            }
            // didn't find it, so do a more expensive search over child elements
            // that have optional start tags and over child groups.
            foreach (object obj in _members) 
            {
                if (obj as string is string s)
                {
                    ElementDecl e = dtd.FindElement(s);
                    if (e != null) 
                    {
                        if (e.StartTagOptional) 
                        {
                            // tricky case, the start tag is optional so element may be
                            // allowed inside this guy!
                            if (e.CanContain(name, dtd))
                                return true;
                        }
                    }
                } 
                else 
                {
                    Group m = (Group)obj;
                    if (m.CanContain(name, dtd)) 
                        return true;
                }
            }

            return false;
        }
    }
}
