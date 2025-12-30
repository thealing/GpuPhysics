namespace Simulator.Engine.Collisions;

public static partial class PolyhedronCollision
{
	private readonly struct EdgeCheckOutput
	{
		public readonly float Depth;
		public readonly Vector Normal;
		public readonly int EdgeIndexA;
		public readonly int EdgeIndexB;

		public EdgeCheckOutput(float depth, Vector normal, int edgeIndexA, int edgeIndexB)
		{
			Depth = depth;
			Normal = normal;
			EdgeIndexA = edgeIndexA;
			EdgeIndexB = edgeIndexB;
		}
	}
}
