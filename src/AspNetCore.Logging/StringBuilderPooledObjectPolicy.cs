using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Codestellation.AspNetCore.Logging
{
    public class StringBuilderPooledObjectPolicy : IPooledObjectPolicy<StringBuilder>
    {
        public StringBuilderPooledObjectPolicy(int initialCapacity)
        {
            InitialCapacity = initialCapacity;

            MaximumRetainedCapacity = 8 * initialCapacity;
        }

        public int InitialCapacity { get; }

        public int MaximumRetainedCapacity { get; }

        public StringBuilder Create() => new StringBuilder(InitialCapacity);

        public bool Return(StringBuilder obj)
        {
            if (obj.Length >= MaximumRetainedCapacity)
            {
                return false;
            }

            obj.Clear();
            return true;
        }
    }
}