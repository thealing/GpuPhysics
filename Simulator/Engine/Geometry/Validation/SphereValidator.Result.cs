namespace Simulator.Engine.Geometry.Validation;

public static partial class SphereValidator
{
	public enum Result
	{
		Valid,
		NotFinite,
		RadiusIsNotPositive,
		Count
	}
}
