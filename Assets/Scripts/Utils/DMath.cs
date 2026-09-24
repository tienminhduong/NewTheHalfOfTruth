public static class DMath
{
    /// <summary>
    /// Returns the next integer in the range [min, max] based on the current value and step.
    /// </summary>
    /// <param name="current">Current value</param>
    /// <param name="min">Min value</param>
    /// <param name="max">Max value</param>
    /// <param name="step">Step value</param>
    /// <param name="loop">Whether to loop around</param>
    /// <param name="clampAtMin">Whether to clamp at min, reset to exactly min if true</param>
    /// <param name="clampAtMax">If true, and the value is about to exceed max, it will be set to max, else, it remains unchanged</param>
    /// <returns></returns>
    public static int GetNextInRange(
        int current,
        int min,
        int max,
        int step = 1,
        bool loop = false,
        bool clampAtMin = false,
        bool clampAtMax = false)
    {
        current += step;
        if (current > max)
        {
            var diff = current - max;
            if (loop)
            {
                if (clampAtMin)
                {
                    return min;
                }
                else
                {
                    return min + diff - 1;
                }
            }
            else
            {
                if (clampAtMax)
                {
                    return max;
                }
                else
                {
                    return current - step; // return the previous value
                }
            }
        }
        return current;
    }
}