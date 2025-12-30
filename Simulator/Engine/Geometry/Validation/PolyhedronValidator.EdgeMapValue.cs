namespace Simulator.Engine.Geometry.Validation;

public static partial class PolyhedronValidator
{
	private readonly struct EdgeMapValue
	{
		public readonly int SideIndex;

		public EdgeMapValue(int sideIndex)
		{
			SideIndex = sideIndex;
		}
	}
}
