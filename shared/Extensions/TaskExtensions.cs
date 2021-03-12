using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Moonlight.Shared.Internal.Extensions
{
    [PublicAPI]
    public static class TaskExtensions
    {
        public static async void InvokeAndForget(this Task task)
        {
            await task;
        }
    }
}