namespace Space.Authoring
{
    public class MapGenNodeBlueprintAuthoring : MapGenerationNodeAuthoring<SpawnBlueprint>
    {
        public BlueprintAuthoring Blueprint;

        protected override SpawnBlueprint Data => new SpawnBlueprint { Prefab = Blueprint.GetComponent<IdentifieableAuthoring>().Id };
        class Baker : MapGenBaker<MapGenNodeBlueprintAuthoring, SpawnBlueprint> { }        
    }
}