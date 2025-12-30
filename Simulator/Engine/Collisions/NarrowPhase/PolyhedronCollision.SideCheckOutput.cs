namespace Simulator.Engine.Collisions;

public static partial class PolyhedronCollision
{
	private readonly struct SideCheckOutput
	{
		public readonly float Depth;
		public readonly int SideIndex;
		public readonly int PointIndex;

		public SideCheckOutput(float depth, int sideIndex, int pointIndex)
		{
			Depth = depth;
			SideIndex = sideIndex;
			PointIndex = pointIndex;
		}
	}
}
