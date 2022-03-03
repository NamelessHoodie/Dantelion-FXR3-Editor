using System;
using System.Collections.Generic;
using System.Text;

namespace DFXR3Editor.Dependencies
{
    public class BoolFlipAfterAccess
    {
        private uint _accessesCount = 0;
        private uint _maximumAccesses;
        private bool _boolean;
        public bool Boolean
        {
            get
            {
                if (_accessesCount >= _maximumAccesses)
                {
                    return !_boolean;
                }
                else
                {
                    _accessesCount++;
                    return _boolean;
                }
            }
        }
        /// <summary>
        /// A simple object that stores a boolean property, that boolean property is flipped after it's GET'd the provided number of times.
        /// </summary>
        /// <param name="initialState">Initial state of the boolean to flip</param>
        /// <param name="accessesBeforeFlip">Amount of times the boolean can be accessed before flipping it</param>
        public BoolFlipAfterAccess(bool initialState, uint accessesBeforeFlip)
        {
            _boolean = initialState;
            _maximumAccesses = accessesBeforeFlip;
        }
    }
}
