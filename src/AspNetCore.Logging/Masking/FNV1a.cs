namespace Codestellation.AspNetCore.Logging.Masking
{
    public static class FNV1a
    {
        public static uint Hash(string value)
        {
            var hash = 0x811c9dc5;
            for (var i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                hash ^= ch;
                hash *= 16777619;
            }

            return hash;
        }
    }
}