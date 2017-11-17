namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class IngameData : RemoteMemoryObject
    {
        public AreaTemplate CurrentArea => ReadObject<AreaTemplate>(Address + 0x28);
        public int CurrentAreaLevel => (int)M.ReadByte(Address + 0x40);
        public int CurrentAreaHash => M.ReadInt(Address + 0x60);
        public Entity LocalPlayer => ReadObject<Entity>(Address + 0x1A8);
        public EntityList EntityList => GetObject<EntityList>(Address + 0x270);
    }
}