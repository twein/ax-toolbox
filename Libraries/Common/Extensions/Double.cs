
namespace AXToolbox.Common
{
    public static class DoubleExtensions
    {
        public static bool IsBetween(this double value, double low, double high)
        {
            return (value >= low && value <= high);
        }
    }
}
