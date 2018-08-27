using System.IO;
using Microsoft.Extensions.ObjectPool;

namespace Codestellation.AspNetCore.Logging
{
    public class MemoryStreamPooledObjectPolicy : IPooledObjectPolicy<MemoryStream>
    {
        public MemoryStreamPooledObjectPolicy(int initialCapacity)
        {
            InitialCapacity = initialCapacity;

            MaximumRetainedCapacity = 8 * initialCapacity;
        }

        public int InitialCapacity { get; }

        public int MaximumRetainedCapacity { get; }

        public MemoryStream Create() => new MemoryStream(InitialCapacity);

        public bool Return(MemoryStream obj)
        {
            if (obj.Length >= MaximumRetainedCapacity)
            {
                return false;
            }

            obj.SetLength(0);
            return true;
        }
    }
}