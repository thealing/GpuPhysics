namespace Simulator.Engine.Collisions;

public static partial class PolyhedronCollision
{
	private readonly struct Edge
	{
		public readonly Vector StartPoint;
		public readonly Vector EndPoint;
		public readonly Vector LeftSideNormal;
		public readonly Vector RightSideNormal;
		public readonly Vector Direction;

		public Edge(Vector startPoint, Vector endPoint, Vector leftSideNormal, Vector rightSideNormal, Vector direction)
		{
			StartPoint = startPoint;
			EndPoint = endPoint;
			LeftSideNormal = leftSideNormal;
			RightSideNormal = rightSideNormal;
			Direction = direction;
		}
	}
}
