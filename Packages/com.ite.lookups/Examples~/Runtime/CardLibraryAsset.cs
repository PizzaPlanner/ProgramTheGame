using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Definitions
{
    [CreateAssetMenu(menuName = "Lookups/Runtime/Cards")]
    public class CardLibraryAsset : LibraryAsset<CardDefinition>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref CardDefinition Data(Card.Id id) => ref Library.Data.Value.Data[(ushort)id];
    }

    // Everything in here is Burst compatible
    public static class Card
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Cost(this Id id) => CardLibraryAsset.Data(id).Cost;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString32Bytes Name(this Id id) => CardLibraryAsset.Data(id).Name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Value(this Id id, int index) => CardLibraryAsset.Data(id).Values[index];
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValuesLength(this Id id) => CardLibraryAsset.Data(id).Values.Length;

        public enum Id : ushort
        {
            None = 0
        }
    }

    public struct CardDefinition
    {
        public BlobArray<int> Values;
        public byte Cost;
        public FixedString32Bytes Name;
    }
}