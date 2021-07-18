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
#if WINDOWS_DESKTOP
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

namespace Sgml
{
    /// <summary>
    /// An attribute definition in a DTD.
    /// </summary>
    public class AttDef
    {
        private readonly string _name;
        private AttributeType _type;
        private string[] _enumValues;
        private string _default;
        private AttributePresence _presence;

        /// <summary>
        /// Initialises a new instance of the <see cref="AttDef"/> class.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        public AttDef(string name)
        {
            _name = name;
        }

        /// <summary>
        /// The name of the attribute declared by this attribute definition.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets of sets the default value of the attribute.
        /// </summary>
        public string Default
        {
            get => _default;
            set => _default = value;
        }

        /// <summary>
        /// The constraints on the attribute's presence on an element.
        /// </summary>
        public AttributePresence AttributePresence => _presence;

        /// <summary>
        /// Gets or sets the possible enumerated values for the attribute.
        /// </summary>
        public string[] EnumValues => _enumValues;

        /// <summary>
        /// Sets the attribute definition to have an enumerated value.
        /// </summary>
        /// <param name="enumValues">The possible values in the enumeration.</param>
        /// <param name="type">The type to set the attribute to.</param>
        /// <exception cref="ArgumentException">If the type parameter is not either <see cref="AttributeType.ENUMERATION"/> or <see cref="AttributeType.NOTATION"/>.</exception>
        public void SetEnumeratedType(string[] enumValues, AttributeType type)
        {
            if (type != AttributeType.ENUMERATION && type != AttributeType.NOTATION)
                throw new ArgumentException($"AttributeType {type} is not valid for an attribute definition with an enumerated value.");

            _enumValues = enumValues;
            _type = type;
        }

        /// <summary>
        /// The <see cref="AttributeType"/> of the attribute declaration.
        /// </summary>
        public AttributeType Type => _type;

        /// <summary>
        /// Sets the type of the attribute definition.
        /// </summary>
        /// <param name="type">The string representation of the attribute type, corresponding to the values in the <see cref="AttributeType"/> enumeration.</param>
        public void SetType(string type)
        {
            _type = type switch
            {
                "CDATA" => AttributeType.CDATA,
                "ENTITY" => AttributeType.ENTITY,
                "ENTITIES" => AttributeType.ENTITIES,
                "ID" => AttributeType.ID,
                "IDREF" => AttributeType.IDREF,
                "IDREFS" => AttributeType.IDREFS,
                "NAME" => AttributeType.NAME,
                "NAMES" => AttributeType.NAMES,
                "NMTOKEN" => AttributeType.NMTOKEN,
                "NMTOKENS" => AttributeType.NMTOKENS,
                "NUMBER" => AttributeType.NUMBER,
                "NUMBERS" => AttributeType.NUMBERS,
                "NUTOKEN" => AttributeType.NUTOKEN,
                "NUTOKENS" => AttributeType.NUTOKENS,
                _ => throw new SgmlParseException($"Attribute type '{type}' is not supported")
            };
        }

        /// <summary>
        /// Sets the attribute presence declaration.
        /// </summary>
        /// <param name="token">The string representation of the attribute presence, corresponding to one of the values in the <see cref="AttributePresence"/> enumeration.</param>
        /// <returns>true if the attribute presence implies the element has a default value.</returns>
        public bool SetPresence(string token)
        {
            bool hasDefault = true;
            if (string.Equals(token, "FIXED", StringComparison.OrdinalIgnoreCase)) 
            {
                _presence = AttributePresence.Fixed;             
            } 
            else if (string.Equals(token, "REQUIRED", StringComparison.OrdinalIgnoreCase)) 
            {
                _presence = AttributePresence.Required;
                hasDefault = false;
            }
            else if (string.Equals(token, "IMPLIED", StringComparison.OrdinalIgnoreCase)) 
            {
                _presence = AttributePresence.Implied;
                hasDefault = false;
            }
            else 
            {
                throw new SgmlParseException($"Attribute value '{token}' not supported");
            }

            return hasDefault;
        }
    }
}
