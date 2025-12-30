namespace Simulator.Engine.Geometry.Validation;

public static partial class PolyhedronValidator
{
	private readonly struct EdgeMapKey
	{
		public readonly Vector StartPoint;
		public readonly Vector EndPoint;

		public EdgeMapKey(Vector startPoint, Vector endPoint)
		{
			StartPoint = startPoint;
			EndPoint = endPoint;
		}
	}
}
