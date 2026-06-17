using System.Collections.Generic;

namespace SmartEmergencyRoutePlanner.Utilities
{
    public static class PathFormatter
    {
        /// <summary>
        /// Formats a list of vertex IDs into a readable path representation.
        /// </summary>
        public static string Format(List<int> path)
        {
            if (path == null || path.Count == 0)
            {
                return "Unreachable / No Path";
            }
            return string.Join(" -> ", path);
        }
    }
}
