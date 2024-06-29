using Definitions;
using Unity.Entities;
using UnityEngine;

public partial class LibraryLoadSystem : SystemBase
{
    const string LIBRARY_LOOKUP = "ExampleLibraryLookup";

    protected override void OnCreate()
    {
        LibraryLookupAsset.LoadFromAddressableLookup("ExampleLibraryLookup");
        for (int i = 0; i < CardLibraryAsset.MaxId; i++)
        {
            var card = (Card.Id)i;
            Debug.Log($"Loaded Card {i} ({card.Name().Value}) with cost {card.Cost()}");
        }
    }

    protected override void OnUpdate() { }
}