using Unity.Collections;

namespace Identifiables
{
    // Copy of Entities.Types.TypeHash implementation of FNV1A64, but for 32-bit
    public static class Hash
    {
        const uint kFNV1A32OffsetBasis = 2166136261;
        const uint kFNV1A32Prime = 16777619;

        /// <summary>
        /// Generates a FNV1A32 hash.
        /// </summary>
        /// <param name="text">Text to hash.</param>
        /// <returns>Hash of input string.</returns>
        public static uint FNV1A32(string text)
        {
            var result = kFNV1A32OffsetBasis;
            foreach (var c in text)
            {
                result = kFNV1A32Prime * (result ^ (byte)(c & 255));
                result = kFNV1A32Prime * (result ^ (byte)(c >> 8));
            }

            return result;
        }

        /// <summary>
        /// Generates a FNV1A32 hash.
        /// </summary>
        /// <param name="text">Text to hash.</param>
        /// <typeparam name="T">Unmanaged IUTF8 type.</typeparam>
        /// <returns>Hash of input string.</returns>
        public static uint FNV1A32<T>(T text)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var result = kFNV1A32OffsetBasis;
            for (int i = 0; i < text.Length; ++i)
            {
                var c = text[i];
                result = kFNV1A32Prime * (result ^ (byte)(c & 255));
                result = kFNV1A32Prime * (result ^ (byte)(c >> 8));
            }

            return result;
        }
    }
}