using SharpDX;
using System.Collections.Generic;
using System.Linq;

namespace PoeHUD.Poe
{
    public class Element : RemoteMemoryObject
    {
        public const int OffsetBuffers = 0x6EC;
        // dd id
        // dd (something zero)
        // 16 dup <128-bytes structure>
        // then the rest is

        public int vTable => M.ReadInt(Address + 0);
        public long ChildCount => (M.ReadLong(Address + 0x44 + OffsetBuffers) - M.ReadLong(Address + 0x3c + OffsetBuffers)) / 8;
        public bool IsVisibleLocal => (M.ReadInt(Address + 0x94 + OffsetBuffers) & 1) == 1;
        public Element Root => ReadObject<Element>(Address + 0xC4 + OffsetBuffers);
        public Element Parent => ReadObject<Element>(Address + 0xCC + OffsetBuffers);
        public float X => M.ReadFloat(Address + 0xD4 + OffsetBuffers);
        public float Y => M.ReadFloat(Address + 0xD8 + OffsetBuffers);
        public float Scale => M.ReadFloat(Address + 0x1D0 + OffsetBuffers);
        public float Width => M.ReadFloat(Address + 0x204 + OffsetBuffers);
        public float Height => M.ReadFloat(Address + 0x208 + OffsetBuffers);

        public bool IsVisible
        {
            get { return IsVisibleLocal && GetParentChain().All(current => current.IsVisibleLocal); }
        }

        public List<Element> Children => GetChildren<Element>();

        protected List<T> GetChildren<T>() where T : Element, new()
        {
            const int listOffset = 0x3C + OffsetBuffers;
            var list = new List<T>();
            if (M.ReadLong(Address + listOffset + 8) == 0 || M.ReadLong(Address + listOffset) == 0 ||
                ChildCount > 1000)
            {
                return list;
            }
            for (int i = 0; i < ChildCount; i++)
            {
                list.Add(GetObject<T>(M.ReadLong(Address + listOffset, i * 8)));
            }
            return list;
        }

        private IEnumerable<Element> GetParentChain()
        {
            var list = new List<Element>();
            var hashSet = new HashSet<Element>();
            Element root = Root;
            Element parent = Parent;
            while (!hashSet.Contains(parent) && root.Address != parent.Address && parent.Address != 0)
            {
                list.Add(parent);
                hashSet.Add(parent);
                parent = parent.Parent;
            }
            return list;
        }

        public Vector2 GetParentPos()
        {
            float num = 0;
            float num2 = 0;
            foreach (Element current in GetParentChain())
            {
                num += current.X;
                num2 += current.Y;
            }
            return new Vector2(num, num2);
        }

        public virtual RectangleF GetClientRect()
        {
            var vPos = GetParentPos();
            float width = Game.IngameState.Camera.Width;
            float height = Game.IngameState.Camera.Height;
            float ratioFixMult = width / height / 1.6f;
            float xScale = width / 2560f / ratioFixMult;
            float yScale = height / 1600f;

            float num = (vPos.X + X) * xScale;
            float num2 = (vPos.Y + Y) * yScale;
            return new RectangleF(num, num2, xScale * Width, yScale * Height);
        }

        public Element GetChildFromIndices(params int[] indices)
        {
            Element poe_UIElement = this;
            foreach (int index in indices)
            {
                poe_UIElement = poe_UIElement.GetChildAtIndex(index);
                if (poe_UIElement == null)
                {
                    return null;
                }
            }
            return poe_UIElement;
        }

        public Element GetChildAtIndex(int index)
        {
            return index >= ChildCount ? null : GetObject<Element>(M.ReadLong(Address + 0x24 + OffsetBuffers, index * 8));
        }
    }
}