using Definitions;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEditor.VersionControl;

public class CardTests
{
    [SetUp]
    public void Setup()
    {
        var cardsLibrary =
            AssetDatabase.LoadAssetAtPath<CardLibraryAuthoring>(
                $"Packages/com.ite.lookups/Examples/Editor/Data/ExampleCardLibraryAuthoring.asset");
        cardsLibrary.BakeAndUseNonSerialized();
    }

    [TearDown]
    public void TearDown()
    {
        CardLibraryAsset.Dispose();
    }

    [Test]
    public void BakedLibraryHasTwoCards()
    {
        var maxIdInLibrary = CardLibraryAsset.MaxId;
        Assert.AreEqual(2, maxIdInLibrary);
    }
    
    [Test]
    public void FirstElementIsDefaultElement()
    {
        Assert.AreEqual(default(byte), CardLibraryAsset.Data(Card.Id.None).Cost);
        Assert.AreEqual(default(FixedString32Bytes), CardLibraryAsset.Data(Card.Id.None).Name);
        Assert.AreEqual(default(BlobArray<int>), CardLibraryAsset.Data(Card.Id.None).Values);
    }
    
    [Test]
    public void FirstActualElementIsNotDefaultElement()
    {
        var card = (Card.Id)1;
        Assert.AreEqual("MyExampleCard", card.Name().Value);
        Assert.AreEqual(5, card.Cost());
        Assert.AreEqual(3, card.ValuesLength());
    }
}