namespace PoeHUD.Poe.Components
{
    public class Player : Component
    {
        public string PlayerName
        {
            get
            {
                if (Address == 0)
                {
                    return "";
                }
                int NameLength = M.ReadInt(Address + 0x30);
                if (NameLength > 512)
                {
                    return "";
                }
                return NameLength < 8 ? M.ReadStringU(Address + 0x20, NameLength * 2) : M.ReadStringU(M.ReadLong(Address + 0x20), NameLength * 2);
            }
        }

        public uint XP => Address != 0 ? M.ReadUInt(Address + 0x48) : 0;
		public int Strength => Address != 0 ? M.ReadInt(Address + 0x4c) : 0;
		public int Dexterity => Address != 0 ? M.ReadInt(Address + 0x50) : 0;
		public int Intelligence => Address != 0 ? M.ReadInt(Address + 0x54) : 0;
        public int Level => Address != 0 ? M.ReadByte(Address + 0x58) : 1;
    }
}
