namespace Simulator.Engine;

public struct Matrix
{
	public static readonly Matrix Zero = new Matrix(Vector.Zero, Vector.Zero, Vector.Zero);

	public Vector X;
	public Vector Y;
	public Vector Z;

	public Matrix(Vector x, Vector y, Vector z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public readonly Matrix Transpose()
	{
		Vector x = new Vector(X.X, Y.X, Z.X);
		Vector y = new Vector(X.Y, Y.Y, Z.Y);
		Vector z = new Vector(X.Z, Y.Z, Z.Z);
		return new Matrix(x, y, z);
	}

	public readonly Matrix Invert()
	{
		float xX = Y.Y * Z.Z - Y.Z * Z.Y;
		float xY = X.Z * Z.Y - X.Y * Z.Z;
		float xZ = X.Y * Y.Z - X.Z * Y.Y;
		float yX = Y.Z * Z.X - Y.X * Z.Z;
		float yY = X.X * Z.Z - X.Z * Z.X;
		float yZ = X.Z * Y.X - X.X * Y.Z;
		float zX = Y.X * Z.Y - Y.Y * Z.X;
		float zY = X.Y * Z.X - X.X * Z.Y;
		float zZ = X.X * Y.Y - X.Y * Y.X;
		Vector x = new Vector(xX, xY, xZ);
		Vector y = new Vector(yX, yY, yZ);
		Vector z = new Vector(zX, zY, zZ);
		float det = Vector.TripleProduct(X, Y, Z);
		return new Matrix(x / det, y / det, z / det);
	}

	public static Matrix CreateUnit(float value)
	{
		Vector x = new Vector(value, 0, 0);
		Vector y = new Vector(0, value, 0);
		Vector z = new Vector(0, 0, value);
		return new Matrix(x, y, z);
	}

	public static Matrix operator +(Matrix m, Matrix n)
	{
		return new Matrix(m.X + n.X, m.Y + n.Y, m.Z + n.Z);
	}

	public static Matrix operator -(Matrix m, Matrix n)
	{
		return new Matrix(m.X - n.X, m.Y - n.Y, m.Z - n.Z);
	}

	public static Matrix operator *(Matrix m, float s)
	{
		return new Matrix(m.X * s, m.Y * s, m.Z * s);
	}

	public static Matrix operator /(Matrix m, float s)
	{
		return new Matrix(m.X / s, m.Y / s, m.Z / s);
	}

	public static Matrix operator *(Matrix m, Matrix n)
	{
		float xX = m.X.X * n.X.X + m.X.Y * n.Y.X + m.X.Z * n.Z.X;
		float xY = m.X.X * n.X.Y + m.X.Y * n.Y.Y + m.X.Z * n.Z.Y;
		float xZ = m.X.X * n.X.Z + m.X.Y * n.Y.Z + m.X.Z * n.Z.Z;
		float yX = m.Y.X * n.X.X + m.Y.Y * n.Y.X + m.Y.Z * n.Z.X;
		float yY = m.Y.X * n.X.Y + m.Y.Y * n.Y.Y + m.Y.Z * n.Z.Y;
		float yZ = m.Y.X * n.X.Z + m.Y.Y * n.Y.Z + m.Y.Z * n.Z.Z;
		float zX = m.Z.X * n.X.X + m.Z.Y * n.Y.X + m.Z.Z * n.Z.X;
		float zY = m.Z.X * n.X.Y + m.Z.Y * n.Y.Y + m.Z.Z * n.Z.Y;
		float zZ = m.Z.X * n.X.Z + m.Z.Y * n.Y.Z + m.Z.Z * n.Z.Z;
		Vector x = new Vector(xX, xY, xZ);
		Vector y = new Vector(yX, yY, yZ);
		Vector z = new Vector(zX, zY, zZ);
		return new Matrix(x, y, z);
	}

	public static Vector operator *(Matrix m, Vector v)
	{
		float x = Vector.Dot(m.X, v);
		float y = Vector.Dot(m.Y, v);
		float z = Vector.Dot(m.Z, v);
		return new Vector(x, y, z);
	}
}
