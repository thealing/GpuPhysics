namespace Simulator.Engine;

using System;

public static class RealMath
{
	public static float Abs(float x)
	{
		return MathF.Abs(x);
	}

	public static double Abs(double x)
	{
		return Math.Abs(x);
	}

	public static float Ceiling(float x)
	{
		return MathF.Ceiling(x);
	}

	public static double Ceiling(double x)
	{
		return Math.Ceiling(x);
	}

	public static float Cos(float x)
	{
		return MathF.Cos(x);
	}

	public static double Cos(double x)
	{
		return Math.Cos(x);
	}

	public static float Cube(float x)
	{
		return x * x * x;
	}

	public static double Cube(double x)
	{
		return x * x * x;
	}

	public static float Exp(float x)
	{
		return MathF.Exp(x);
	}

	public static double Exp(double x)
	{
		return Math.Exp(x);
	}

	public static float Floor(float x)
	{
		return MathF.Floor(x);
	}

	public static double Floor(double x)
	{
		return Math.Floor(x);
	}

	public static float Log(float x)
	{
		return MathF.Log(x);
	}

	public static double Log(double x)
	{
		return Math.Log(x);
	}

	public static float Max(float x, float y)
	{
		return MathF.Max(x, y);
	}

	public static float Max(float x, float y, float z)
	{
		return MathF.Max(MathF.Max(x, y), z);
	}

	public static double Max(double x, double y)
	{
		return Math.Max(x, y);
	}

	public static double Max(double x, double y, double z)
	{
		return Math.Max(Math.Max(x, y), z);
	}

	public static float Min(float x, float y)
	{
		return MathF.Min(x, y);
	}

	public static float Min(float x, float y, float z)
	{
		return MathF.Min(MathF.Min(x, y), z);
	}

	public static double Min(double x, double y)
	{
		return Math.Min(x, y);
	}

	public static double Min(double x, double y, double z)
	{
		return Math.Min(Math.Min(x, y), z);
	}

	public static float Pow(float x, float y)
	{
		return MathF.Pow(x, y);
	}

	public static double Pow(double x, double y)
	{
		return Math.Pow(x, y);
	}

	public static float Round(float x)
	{
		return MathF.Round(x);
	}

	public static double Round(double x)
	{
		return Math.Round(x);
	}

	public static int Sign(float x)
	{
		return MathF.Sign(x);
	}

	public static int Sign(double x)
	{
		return Math.Sign(x);
	}

	public static float Sin(float x)
	{
		return MathF.Sin(x);
	}

	public static double Sin(double x)
	{
		return Math.Sin(x);
	}

	public static float Sqrt(float x)
	{
		return MathF.Sqrt(x);
	}

	public static double Sqrt(double x)
	{
		return Math.Sqrt(x);
	}

	public static float Square(float x)
	{
		return x * x;
	}

	public static double Square(double x)
	{
		return x * x;
	}

	public static float Truncate(float x)
	{
		return MathF.Truncate(x);
	}

	public static double Truncate(double x)
	{
		return Math.Truncate(x);
	}

	public static float Clamp(float value, float min, float max)
	{
		return MathF.Min(MathF.Max(value, min), max);
	}

	public static double Clamp(double value, double min, double max)
	{
		return Math.Min(Math.Max(value, min), max);
	}

	public static float CopySign(float x, float y)
	{
		return MathF.CopySign(x, y);
	}

	public static double CopySign(double x, double y)
	{
		return Math.CopySign(x, y);
	}

	public static float ScaleB(float x, int n)
	{
		return MathF.ScaleB(x, n);
	}

	public static double ScaleB(double x, int n)
	{
		return Math.ScaleB(x, n);
	}

	public static int ILogB(float x)
	{
		int bits = BitConverter.SingleToInt32Bits(x);
		int exponent = (bits >> 23) & 0xFF;
		return exponent - 127;
	}

	public static int ILogB(double x)
	{
		long bits = BitConverter.DoubleToInt64Bits(x);
		int exponent = (int)(bits >> 52 & 0x7FF);
		return exponent - 1023;
	}
}
