namespace Simulator.Engine.Geometry;

public partial class PolyhedronDefinition
{
	public class Side
	{
		public int[] PointIndices { get; }

		public Side(params int[] pointIndices)
		{
			PointIndices = pointIndices;
		}
	}
}
