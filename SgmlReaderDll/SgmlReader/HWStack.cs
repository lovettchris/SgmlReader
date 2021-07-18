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

namespace Sgml
{
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
}