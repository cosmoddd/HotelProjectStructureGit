using UnityEngine;
using System.Collections.Generic;

namespace Polybrush
{
	public struct z_Edge : System.IEquatable<z_Edge>
	{
		// tri indices
		public int x;
		public int y;

		// common indices
		public int cx;
		public int cy;

		public z_Edge(int _x, int _y)
		{
			this.x = _x;
			this.y = _y;
			this.cx = -1;
			this.cy = -1;
		}

		public z_Edge(int _x, int _y, int _cx, int _cy)
		{
			this.x = _x;
			this.y = _y;
			this.cx = _cx;
			this.cy = _cy;
		}

		public bool hasCommon { get { return cx > -1 && cy > -1; } }

		public bool Equals(z_Edge p)
		{
			if( hasCommon && p.hasCommon )
				return (p.cx == cx && p.cy == cy) || (p.cx == cy && p.cy == cx);
			else
				return (p.x == x && p.y == y) || (p.x == y && p.y == x);
		}

		public override bool Equals(System.Object b)
		{
			return b is z_Edge && this.Equals((z_Edge)b);
		}

		public static bool operator ==(z_Edge a, z_Edge b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(z_Edge a, z_Edge b)
		{
			return !a.Equals(b);
		}

		public override int GetHashCode()
		{
			// MAX(x, y) ^ MIN(x, y)
			return hasCommon ? (cx > cy ? cy ^ cx : cx ^ cy) : (x > y ? y ^ x : x ^ y);
		}

		public bool ContainsCommon(int b)
		{
			return (cx == b || cy == b);
		}

		public override string ToString()
		{
			return string.Format("{{ {{{0}:{1}}}, {{{2}:{3}}} }}", x, cx, y, cy);
		}

		public static List<int> ToList(IEnumerable<z_Edge> edges)
		{
			List<int> list = new List<int>();

			foreach(z_Edge e in edges)
			{
				list.Add(e.x);
				list.Add(e.y);
			}

			return list;
		}

		public static HashSet<int> ToHashSet(IEnumerable<z_Edge> edges)
		{
			HashSet<int> hash = new HashSet<int>();

			foreach(z_Edge e in edges)
			{
				hash.Add(e.x);
				hash.Add(e.y);
			}

			return hash;
		}
	}
}
