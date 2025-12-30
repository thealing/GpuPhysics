namespace Simulator.Engine.Collisions;

using Simulator.Engine;

public struct Bound
{
	public Vector Lower;
	public Vector Upper;

	public Bound(Vector lower, Vector upper)
	{
		Lower = lower;
		Upper = upper;
	}

	public static bool Intersect(Bound a, Bound b)
	{
		if (a.Lower.X > b.Upper.X || b.Lower.X > a.Upper.X)
		{
			return false;
		}
		if (a.Lower.Y > b.Upper.Y || b.Lower.Y > a.Upper.Y)
		{
			return false;
		}
		if (a.Lower.Z > b.Upper.Z || b.Lower.Z > a.Upper.Z)
		{
			return false;
		}
		return true;
	}
}
