namespace Simulator.Engine;

public struct Quaternion
{
	public static readonly Quaternion Identity = new Quaternion(0, 0, 0, 1);

	public float X;
	public float Y;
	public float Z;
	public float W;

	public Quaternion(float x, float y, float z, float w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public readonly Quaternion Normalize()
	{
		float length = RealMath.Sqrt(X * X + Y * Y + Z * Z + W * W);
		return new Quaternion(X / length, Y / length, Z / length, W / length);
	}

	public readonly Matrix ToMatrix()
	{
		float xX = X * X;
		float yY = Y * Y;
		float zZ = Z * Z;
		float xY = X * Y;
		float zX = Z * X;
		float yZ = Y * Z;
		float wX = W * X;
		float wY = W * Y;
		float wZ = W * Z;
		Vector x = new Vector(1 - (yY + zZ) * 2, (xY - wZ) * 2, (zX + wY) * 2);
		Vector y = new Vector((xY + wZ) * 2, 1 - (xX + zZ) * 2, (yZ - wX) * 2);
		Vector z = new Vector((zX - wY) * 2, (yZ + wX) * 2, 1 - (xX + yY) * 2);
		return new Matrix(x, y, z);
	}

	public static Quaternion FromAngle(Vector angle)
	{
		float sinX = RealMath.Sin(angle.X / 2);
		float cosX = RealMath.Cos(angle.X / 2);
		float sinY = RealMath.Sin(angle.Y / 2);
		float cosY = RealMath.Cos(angle.Y / 2);
		float sinZ = RealMath.Sin(angle.Z / 2);
		float cosZ = RealMath.Cos(angle.Z / 2);
		float x = cosZ * cosY * sinX - sinZ * sinY * cosX;
		float y = cosZ * sinY * cosX + sinZ * cosY * sinX;
		float z = sinZ * cosY * cosX - cosZ * sinY * sinX;
		float w = cosZ * cosY * cosX + sinZ * sinY * sinX;
		return new Quaternion(x, y, z, w);
	}

	public static Quaternion operator *(Quaternion q, Quaternion r)
	{
		float x = q.X * r.W + q.W * r.X + q.Z * r.Y - q.Y * r.Z;
		float y = q.Y * r.W - q.Z * r.X + q.W * r.Y + q.X * r.Z;
		float z = q.Z * r.W + q.Y * r.X - q.X * r.Y + q.W * r.Z;
		float w = q.W * r.W - q.X * r.X - q.Y * r.Y - q.Z * r.Z;
		return new Quaternion(x, y, z, w);
	}

	public static Vector operator *(Quaternion q, Vector v)
	{
		Vector r = new Vector(q.X, q.Y, q.Z);
		Vector t = Vector.Cross(r, v) * 2;
		return v + t * q.W + Vector.Cross(r, t);
	}
}
