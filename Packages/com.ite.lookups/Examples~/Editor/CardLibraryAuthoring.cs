using Bakers;
using Definitions;
using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(menuName = "Lookups/Editor/Cards")]
public class CardLibraryAuthoring : LibraryAssetAuthoring<CardDefinition, CardAuthoring>
{
    public new Card.Id IdOf(CardAuthoring authoring) => (Card.Id)base.IdOf(authoring);

    protected override void BakeElement(CardAuthoring element, int builderIdx,
                                        BlobBuilderArray<CardDefinition> builderArray, BlobBuilder builder)
    {
        builderArray[builderIdx] = new CardDefinition
        {
            Cost = element.Cost,
            Name = element.name
        };
        var abilityBuilder = builder.Allocate(ref builderArray[builderIdx].Values, element.Values.Length);
        for (int a = 0; a < abilityBuilder.Length; a++)
        {
            abilityBuilder[a] = a;
        }
    }
}