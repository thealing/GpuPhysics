namespace Simulator.Engine.Geometry.Validation;

public static class VectorValidator
{
	public static bool IsFinite(Vector vector)
	{
		return float.IsFinite(vector.X) && float.IsFinite(vector.Y) && float.IsFinite(vector.Z);
	}
}
