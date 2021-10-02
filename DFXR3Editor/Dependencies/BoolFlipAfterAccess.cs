namespace DFXR3Editor.Dependencies
{
    public class BoolFlipAfterAccess
    {
        private uint accessesCount = 0;
        private uint maximumAccesses;
        private bool _Boolean;
        public bool Boolean
        {
            get
            {
                if (accessesCount >= maximumAccesses)
                {
                    return !_Boolean;
                }
                else
                {
                    accessesCount++;
                    return _Boolean;
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
            _Boolean = initialState;
            maximumAccesses = accessesBeforeFlip;
        }
    }
}
