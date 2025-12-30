namespace Simulator.Backend.Common;

public struct PolyhedronOffset
{
	public int PointOffset;
	public int SideNormalOffset;
	public int SidePointIndiceOffset;
	public int SidePointIndexOffsetOffset;
	public int EdgeOffset;

	public PolyhedronOffset(int pointOffset, int sideNormalOffset, int sidePointIndiceOffset, int sidePointIndexOffsetOffset, int edgeOffset)
	{
		PointOffset = pointOffset;
		SideNormalOffset = sideNormalOffset;
		SidePointIndiceOffset = sidePointIndiceOffset;
		SidePointIndexOffsetOffset = sidePointIndexOffsetOffset;
		EdgeOffset = edgeOffset;
	}
}
