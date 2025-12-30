namespace Simulator.Engine.Geometry.Validation;

public static partial class SphereValidator
{
	public static Result Validate<TSphere>(TSphere sphere, float tolerance)
		where TSphere : ISphere
	{
		if (IsFinite(sphere) == false)
		{
			return Result.NotFinite;
		}
		if (sphere.Radius <= -tolerance)
		{
			return Result.RadiusIsNotPositive;
		}
		return Result.Valid;
	}

	public static bool IsFinite<TSphere>(TSphere sphere)
		where TSphere : ISphere
	{
		return VectorValidator.IsFinite(sphere.Center) && float.IsFinite(sphere.Radius);
	}
}
