namespace Mirage
{
    public static class StringHash
    {
        /// <summary>
        /// Gets a hash for a string. This hash will be the same on all platforms 
        /// </summary>
        /// <remarks>
        /// string.GetHashCode is not guaranteed to be the same on all platforms
        /// </remarks>
        public static int GetStableHashCode(this string text)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in text)
                    hash = hash * 31 + c;
                return hash;
            }
        }
    }
}
