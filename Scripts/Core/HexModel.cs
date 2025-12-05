using UnityEngine;

namespace TacticalHex
{
    public class HexModel
    {
        public int Q { get; private set; }
        public int R { get; private set; }
        public Vector3 WorldPosition { get; set; }

        public HexModel(int q, int r)
        {
            Q = q;
            R = r;
        }

        public static int Distance(HexModel a, HexModel b)
        {
            if (a == null || b == null)
                return 0;

            OffsetToCube(a, out int ax, out int ay, out int az);
            OffsetToCube(b, out int bx, out int by, out int bz);

            return (Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) + Mathf.Abs(az - bz)) / 2;
        }

        private static void OffsetToCube(HexModel h, out int x, out int y, out int z)
        {
            x = h.Q;
            int q = h.Q;
            int r = h.R;

            z = r - (q - (q & 1)) / 2;
            y = -x - z;
        }

        public bool IsNeighborOffset(HexModel other)
        {
            if (other == null)
                return false;

            return Distance(this, other) == 1;
        }

        public bool IsNeighborOf(HexModel other) => IsNeighborOffset(other);
    }
}
