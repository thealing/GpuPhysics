namespace WpfApp2;

using System.Windows.Media.Media3D;
using Simulator.Engine;

public static class VectorExtension
{
	public static System.Numerics.Vector3 ToVector3(this Vector vector)
	{
		return new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
	}

	public static Vector3D ToVector3D(this Vector vector)
	{
		return new Vector3D(vector.X, vector.Y, vector.Z);
	}

	public static Point3D ToPoint3D(this Vector vector)
	{
		return new Point3D(vector.X, vector.Y, vector.Z);
	}
}
