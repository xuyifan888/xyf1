namespace PoeHUD.Poe.Elements
{
    public class EntityLabel : Element
    {
        public string Text
        {
            get
            {
                int LabelLen = M.ReadInt(Address + 0xC28);
                if (LabelLen <= 0 || LabelLen > 256)
                {
                    return "";
                }
                return LabelLen >= 8 ? M.ReadStringU(M.ReadInt(Address + 0xC18), LabelLen * 2) : M.ReadStringU(Address + 0xC18, LabelLen * 2);
            }
        }
    }
}