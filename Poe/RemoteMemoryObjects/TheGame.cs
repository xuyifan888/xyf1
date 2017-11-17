using PoeHUD.Framework;

namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class TheGame : RemoteMemoryObject
    {
        public TheGame(Memory m)
        {
            M = m;
            Address = m.ReadLong(Offsets.Base + m.AddressOfProcess, 0x8, 0xf8);//0xC40
            Game = this;
        }
        public IngameState IngameState => ReadObject<IngameState>(Address + 0x38);
        public int AreaChangeCount => M.ReadInt(M.AddressOfProcess + Offsets.AreaChangeCount);
    }
}