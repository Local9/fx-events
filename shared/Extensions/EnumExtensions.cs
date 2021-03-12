using JetBrains.Annotations;
using Moonlight.Shared.Inventory;

namespace Moonlight.Shared.Internal.Extensions
{
    [PublicAPI]
    public static class EnumExtensions
    {
        public static bool Includes(this InventoryFlag source, InventoryFlag flag)
        {
            return (source & flag) != InventoryFlag.None;
        }
    }
}