namespace Simulator.Engine.Geometry;

public readonly struct PolyhedronEdge
{
	public readonly int StartPointIndex;
	public readonly int EndPointIndex;
	public readonly int LeftSideIndex;
	public readonly int RightSideIndex;

	public PolyhedronEdge(int startPointIndex, int endPointIndex, int leftSideIndex, int rightSideIndex)
	{
		StartPointIndex = startPointIndex;
		EndPointIndex = endPointIndex;
		LeftSideIndex = leftSideIndex;
		RightSideIndex = rightSideIndex;
	}
}
