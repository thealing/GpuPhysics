#define POLY

namespace WpfApp2;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using ILGPU;
using ILGPU.Backends.OpenCL;
using ILGPU.Backends.PTX;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using Simulator.Backend;
using Simulator.Backend.Common;
using Simulator;
using Simulator.Engine;
using Simulator.Engine.Collisions;
using Simulator.Engine.Collisions.BroadPhase;
using Simulator.Engine.Collisions.NarrowPhase;
using Simulator.Engine.Geometry;
using Simulator.Engine.Geometry.Validation;
using Simulator.Engine.Physics;
using Simulator.Engine.Physics.Simulation;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using ILGPU.Backends;
using Simulator.Engine.Core;

public static class Program
{
	public static Vector RandomPointOnUnitSphere(Random rng)
	{
		float u = (float)(rng.NextDouble() * 2.0 - 1.0); 
		float phi = (float)(rng.NextDouble() * 2.0 * Math.PI);

		float sqrtOneMinusUSquared = MathF.Sqrt(1 - u * u);
		float x = sqrtOneMinusUSquared * MathF.Cos(phi);
		float y = sqrtOneMinusUSquared * MathF.Sin(phi);
		float z = u;

		return new Vector(x, y, z);
	}

	public static Vector RandomPointOnUnitCube(Random rng)
	{
		int face = rng.Next(6); // 0..5
		float a = (float)(rng.NextDouble() * 2.0 - 1.0);
		float b = (float)(rng.NextDouble() * 2.0 - 1.0);
		switch (face)
		{
			case 0:
				return new Vector(1, a, b); // +X face
			case 1:
				return new Vector(-1, a, b); // -X face
			case 2:
				return new Vector(a, 1, b); // +Y face
			case 3:
				return new Vector(a, -1, b); // -Y face
			case 4:
				return new Vector(a, b, 1); // +Z face
			default:
				return new Vector(a, b, -1); // -Z face
		}
	}

	public static object o = new();

	[DllImport("msvcrt.dll")]
	public static extern uint _control87(uint a, uint b);

	[DllImport("msvcrt.dll")]
	public static extern uint _clearfp();

	// Not working sometimes
	public static void TrapNans()
	{
		uint empty = 0;
		uint cw = _control87(empty, empty);
		uint MCW_EM = 0x0008001f;
		uint _EM_INVALID = 0x00000010;

		cw &= ~(_EM_INVALID);
		_clearfp();
		_control87(cw, MCW_EM); 
	}

	public static void AllowNans()
	{
		const uint DEFAULT_CW = 0x027F;  // x87 defaults (mask all exceptions)

		_clearfp();
		_control87(DEFAULT_CW, 0xFFFF);
	}

	public struct DO : IDisposable
	{
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Console.WriteLine("DOO");
		}

		public static void F(DO d)
		{
			Console.WriteLine("in f");
			G(d);
		}

		public static void G(DO d)
		{
			Console.WriteLine("in g");
		}
	}

	[STAThread]
	static void Main()
	{
		TestGPU();

		if (false)
			TestPolyhedrons();

		CubeWindow.Show();
	}

	struct CpuSpeedTest : ICommand
	{
		long[] a;
		long[] b;
		long[] c;

		public CpuSpeedTest(long[] aa, long[] bb, long[] cc)
		{
			a = aa;
			b = bb;
			c = cc;
			for (int i = 0; i<a.Length; i++)
			{
				a[i] = i;
				b[i] = a.Length - i;
			}
		}

		public double Run(IExecutor exe)
		{
			Stopwatch st = Stopwatch.StartNew();
			exe.Execute(this, c.Length);
			//exe.Execute(this, c.Length / 100);
			return st.Elapsed.TotalMilliseconds;
		}

		public void Execute(int i)
		{
			c[i] += a[i] = b[i];
		}

		public void Execute2(int j)
		{
			for (int i = j*100; i<j*100+100; i++)
			{
				c[i] += a[i] = b[i];
			}
		}

		public double RunSequential()
		{
			Stopwatch st = Stopwatch.StartNew();
			for (int i=0;i<c.Length;i++)
			{
				c[i] += a[i] = b[i];
			}
			return st.Elapsed.TotalMilliseconds;
		}
	}

	public readonly struct GpuSpeedTest : ICommand
	{
		public readonly ArrayView<long> A;
		public readonly ArrayView<long> B;
		public readonly ArrayView<long> C;

		public GpuSpeedTest(ArrayView<long> a, ArrayView<long> b, ArrayView<long> c)
		{
			A = a;
			B = b;
			C = c;
		}

		public void Execute(int i)
		{
			C[i] += A[i] = B[i];
		}
	}

	public sealed class GpuSpeedTestStorage : IDisposable
	{
		public long[] CpuA { get; }
		public long[] CpuB { get; }
		public long[] CpuC { get; }

		public GpuSpeedTest GpuCommand { get; }

		private readonly MemoryBuffer1D<long, Stride1D.Dense> _a;
		private readonly MemoryBuffer1D<long, Stride1D.Dense> _b;
		private readonly MemoryBuffer1D<long, Stride1D.Dense> _c;

		private Action<Index1D, GpuSpeedTest> kernel;

		public GpuSpeedTestStorage(Accelerator accelerator, int length)
		{
			CpuA = new long[length];
			CpuB = new long[length];
			CpuC = new long[length];

			for (int i = 0; i < length; i++)
			{
				CpuA[i] = i;
				CpuB[i] = length - i;
			}

			_a = accelerator.Allocate1D<long>(length);
			_b = accelerator.Allocate1D<long>(length);
			_c = accelerator.Allocate1D<long>(length);

			GpuCommand = new GpuSpeedTest(_a.View, _b.View, _c.View);

			kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, GpuSpeedTest>(Kernel);
		}

		public void CopyFromCPU()
		{
			_a.View.CopyFromCPU(CpuA);
			_b.View.CopyFromCPU(CpuB);
			_c.View.CopyFromCPU(CpuC);
		}

		public void CopyToCPU()
		{
			_a.View.CopyToCPU(CpuA);
			_b.View.CopyToCPU(CpuB);
			_c.View.CopyToCPU(CpuC);
		}

		public static void Kernel(Index1D i, GpuSpeedTest t)
		{
			t.C[i] += t.A[i] + t.B[i];
		}

		public double Run(IExecutor executor, Accelerator accelerator)
		{
			Stopwatch st = Stopwatch.StartNew();
			executor.Execute(GpuCommand, CpuC.Length);
			return st.Elapsed.TotalMilliseconds;
		}

		public double RunNative(Accelerator accelerator)
		{
			Stopwatch st = Stopwatch.StartNew();
			kernel(CpuC.Length, GpuCommand);
			accelerator.Synchronize();
			return st.Elapsed.TotalMilliseconds;
		}

		public void Dispose()
		{
			_a.Dispose();
			_b.Dispose();
			_c.Dispose();
		}
	}

	public static void TestGPU()
	{
		//accelerator.PrintInformation();

		//ArrayView1D<int, Stride1D.Dense> view = accelerator.Allocate1D<int>(100);

		//PropertyInfo property = view.GetType().GetProperty("ILGPU.IArrayView.Buffer", BindingFlags.Instance | BindingFlags.NonPublic);
		//MemoryBuffer buffer = property.GetValue(view) as MemoryBuffer;
		//buffer.Dispose();

		//view.Buffer.Dispose();

		if (false)
		{
			DeviceManager dm = new();
			var accelerator = dm.Accelerator;

			TestSpeed(accelerator);
		}

		if (false)
		{
			TestNewStorages();
		}
	}

	static void TestSpeed(Accelerator accelerator)
	{

		Simulator.Backend.Cpu.Executor cpuExe = new();
		Simulator.Backend.Gpu.Executor gpuExe = new(accelerator);

		using var gpuTest = new GpuSpeedTestStorage(accelerator, 10_000_000);

		var cpuTest = new CpuSpeedTest(gpuTest.CpuA, gpuTest.CpuB, gpuTest.CpuC);

		gpuTest.CopyFromCPU();

		for (int i = 0; i < 10; i++)
		{
			cpuTest.RunSequential();
			cpuTest.Run(cpuExe);
			gpuTest.Run(gpuExe, accelerator);
		}

		double seqTime = 0;
		double cpuTime = 0;
		double gpuTime = 0;

		for (int i = 0; i < 10; i++)
		{
			seqTime += cpuTest.RunSequential();
			cpuTime += cpuTest.Run(cpuExe);
			gpuTime += gpuTest.Run(gpuExe, accelerator);
		}

		Console.WriteLine(gpuTest.CpuC.Sum());
		gpuTest.CopyToCPU();
		Console.WriteLine(gpuTest.CpuC.Sum());

		Console.WriteLine(seqTime + " " + cpuTime + " " + gpuTime);
		//Console.ReadKey();
	}

	static void TestNewStorages()
	{
		DeviceManager dm = new DeviceManager();

		Simulator.Backend.Cpu.WorldStorage cws = new();
		Simulator.Backend.Gpu.WorldStorage gws = new(dm.Accelerator);

		var exe = new Simulator.Backend.Gpu.Executor(dm.Accelerator);
		var exe2 = new Simulator.Backend.Cpu.Executor();

		for (int t = 0; ;t++)
		{

			//using WorldStorageManager wsm = new WorldStorageManager(dm.Accelerator, 100000, 100000, 100000, 100000, 100000, 100000, 100000, 100000, 100000);
			//using DynamicGridStorageManager dgsm = new DynamicGridStorageManager(dm.Accelerator, 100000);

			if (t == 0 || t==10 && false)
			{
				cws.AddBody(new());

				cws.AddSphereShape(new(), new Vector(1e6f + 1000, 0, 0), 1e6f);
				cws.AddSphereShape(new(), new Vector(1e6f - 1000, 0, 0), 1e6f);
				cws.AddSphereShape(new(), new Vector(0, 1e6f + 1000, 0), 1e6f);
				cws.AddSphereShape(new(), new Vector(0, 1e6f - 1000, 0), 1e6f);
				cws.AddSphereShape(new(), new Vector(0, 0, 1e6f + 1000), 1e6f);
				cws.AddSphereShape(new(), new Vector(0, 0, 1e6f - 1000), 1e6f);

				for (int ib=0;ib<10000;ib++)
				{
					int bId = cws.AddBody(new());

					var p = new Vector(ib / 3f, ib / 1000f, 0);

					ShapeDefinition def = new() { BodyIndex = bId, Density = 1 };

					if (false)
					{
						var d = PolyhedronCreator.CreateCubeDefinition();
						d.Transform(new Transform(p, Quaternion.Identity));

						cws.AddPolyhedronShape(def, d);
					}
					else
					{
						cws.AddSphereShape(def, p, 1);
					}
				}

				cws.Config.DeltaTime = 1f / 60f;
				cws.Config.IterationCount = 10;
				//cws.WorldConfig.Gravity.Linear.Y = -10;

				//gbs.CopyFromCPU(cbs, dm.Accelerator);

				//World<Backend.Gpu.WorldStorage, DynamicGrid<Backend.Gpu.DynamicGridStorage>> world = new(wsm.GpuStorage, new(dgsm.GpuStorage));
				//world.Step(new Backend.Gpu.Executor(dm.Accelerator));
			}

			var st = Stopwatch.StartNew();

			for (int i = 0; i < 10; i++)
			{
				if (t % 10 == 0 && i == 0)
				{
					gws.CopyFromCPU(cws);

					Console.WriteLine("to gpu: " + st.Elapsed.TotalMilliseconds);
					st.Restart();

					gws.CopyToCPU(cws);

					Console.WriteLine("to cpu: " + st.Elapsed.TotalMilliseconds);
					st.Restart();
				}

				if (true) // gpu
				{
					World<Simulator.Backend.Gpu.WorldStorage, DynamicGrid<Simulator.Backend.Gpu.DynamicGridStorage>> world;
					world.Storage = gws;
					world.CollisionMap.Storage = gws.DynamicGridStorage;

					world.Step(exe);

					Console.WriteLine("c: " + world.Storage.ContactCount);
				}
				else
				{
					World<Simulator.Backend.Cpu.WorldStorage, DynamicGrid<Simulator.Backend.Cpu.DynamicGridStorage>> world;
					world.Storage = cws;
					world.CollisionMap.Storage = cws.DynamicGridStorage;

					world.Step(exe2);

					Console.WriteLine("c: " + world.Storage.ContactCount);
				}

				Console.WriteLine("run: " + st.Elapsed.TotalMilliseconds);
				st.Restart();
			}
		}

		Environment.Exit(3);
	}

	static void TestPolyhedrons()
	{
		{
			var ht = PolyhedronCreator.CreateCubeDefinition();
			ht.Scale(new Vector(0.5f*RealMath.Sqrt(3)));
			ht.Scale(new Vector(7, 8, 9));
			var wpt = new WorldPolyhedron(ht);
			//wpt.ResetTransform(new Transform(new Vector(0, 0, 0), Quaternion.FromAngle(new Vector(2, 3, 4))));
			var m = wpt.GetProperties();

			Console.WriteLine(m.InertiaTensor);
			Console.WriteLine(m.InertiaTensor.Invert());

			//Console.WriteLine(PolyhedronExtension.GetTetrahedronInertiaTensor(
			//	new Vector(1, 2, 3),
			//	new Vector(8, 9, 4),
			//	new Vector(7, 6, 5)
			//));
			//Environment.Exit(1);
		}

		//return;

		int testC = 0;
		while (true)
		{
			if (testC++ % 1000==0)
				Console.WriteLine("Test " + testC);

			var def1 = CreateRandomPlatonicSolidDefinition(Random.Shared);
			var def2 = CreateRandomPlatonicSolidDefinition(Random.Shared);

			bool scale = true;
			if (scale)
			{
				def1.Scale(new Vector(
					Random.Shared.NextSingle() * 10 + 1,
					Random.Shared.NextSingle() * 10 + 1,
					Random.Shared.NextSingle() * 10 + 1
				)
			);
				def2.Scale(new Vector(
						Random.Shared.NextSingle() * 10 + 1,
						Random.Shared.NextSingle() * 10 + 1,
						Random.Shared.NextSingle() * 10 + 1
					)
				);
			}

			var t1 = RandomTransform(10);
			var t2 = RandomTransform(10);

			WorldPolyhedron wp1 = new WorldPolyhedron(def1);
#if POLY
			WorldPolyhedron wp2 = new WorldPolyhedron(def2);
#endif

			wp1.SetTransform(t1);

			WorldSphere sp1 = new WorldSphere(Vector.Zero, 1);
			WorldSphere sp2 = new WorldSphere(Vector.Zero, 1);
			sp1.SetTransform(t1);
			sp2.SetTransform(t2);

#if POLY
			wp2.SetTransform(t2);
#else
			var wp2 = sp2;
#endif

			Collision polColl = new();
#if POLY
			var polColld = PolyhedronCollision.Check(wp1, wp2, ref polColl);
#else
			var polColld = PolyhedronSphereCollision.Check(wp1, sp2, ref polColl);
#endif

			Collision cirColl = new();
			var cirColld = SphereCollision.Check(sp1, sp2, ref cirColl);

			if (!scale && polColld && !cirColld)
			{
				Environment.Exit(6);
			}
			if (!scale && polColld && cirColld)
			{
				if (polColl.Depth > cirColl.Depth)
				{
					Environment.Exit(7);
				}
			}

			//Console.WriteLine(polColld);

			if (polColld)
			{
				for (int nt=0;nt<1000;nt++)
				{
					var normal = RandomPointOnUnitSphere(Random.Shared);

					float max1 = float.NegativeInfinity;
					for (int i = 0; i<wp1.PointCount; i++)
						max1=RealMath.Max(max1, Vector.Dot(normal, wp1.GetPoint(i)));

#if POLY
					float min1 = float.PositiveInfinity;
					for (int i = 0; i<wp2.PointCount; i++)
						min1=RealMath.Min(min1, Vector.Dot(normal, wp2.GetPoint(i)));
#else
					float min1 = Vector.Dot(normal, sp2.Center)-sp2.Radius;
#endif

					float d = max1 - min1;
					if (d < polColl.Depth - 0.001)
					{
						Environment.Exit(88);
					}
				}

#if !POLY
				for (int i = 0; i<1000; i++)
				{
					Vector p = new Vector(
						Random.Shared.NextSingle() * 20 - 10,
						Random.Shared.NextSingle() * 20 - 10,
						Random.Shared.NextSingle() * 20 - 10);

					if (wp1.ContainsPoint(p) &&
						Geometry.GetDistanceSquared(p, wp2.Center) <
						Geometry.GetDistanceSquared(polColl.Point, wp2.Center))
					{
						Environment.Exit(20);
					}
				}
#endif

				Transform st = new Transform(polColl.Normal * polColl.Depth * 0.5f, Quaternion.Identity);

				wp2.ApplyTransform(st);

				Debug.Assert(PolyhedronValidator.Validate(wp1, 0.001f)==PolyhedronValidator.Result.Valid);
				//Debug.Assert(PolyhedronValidator.Validate(wp2, 0.001f)==PolyhedronValidator.Result.Valid);

				Collision polColl1 = new();
#if POLY
				var c1 = PolyhedronCollision.Check(wp1, wp2, ref polColl1);
#else
				var c1 = PolyhedronSphereCollision.Check(wp1, sp2, ref polColl1);
#endif

				if (RealMath.Abs(polColl1.Depth-polColl.Depth*0.5f)>0.0001)
				{
					Environment.Exit(5);
				}
			}
			else
			{
				for(int i=0;i<1000;i++)
				{
					Vector p = new Vector(
						Random.Shared.NextSingle() * 20 - 10,
						Random.Shared.NextSingle() * 20 - 10,
						Random.Shared.NextSingle() * 20 - 10);

					if(wp1.ContainsPoint(p) && wp2.ContainsPoint(p))
					{
						Environment.Exit(89);
					}
				}
			}

			//break;
		}

		Environment.Exit(9);

		/*
		 
		
		if (true)
		{
			float scale = RealMath.Pow(2 / (polyhedron.GetPoint(0) - polyhedron.GetPoint(1)).GetLength(), 3);
			scale = 1;
			System.Console.WriteLine(properties.Volume * scale);
			System.Console.WriteLine(properties.Centroid * scale);
			System.Console.WriteLine(properties.InertiaTensor * scale);
			System.Console.WriteLine((polyhedron.GetPoint(0) - polyhedron.GetPoint(1)).GetLength());
		}

		 */
	}

	public static PolyhedronDefinition CreateRandomPlatonicSolidDefinition(Random r)
	{
		int index = r.Next(5);

		switch (index)
		{
			case 0:
				return PolyhedronCreator.CreateTetrahedronDefinition();
			case 1:
				return PolyhedronCreator.CreateCubeDefinition();
			case 2:
				return PolyhedronCreator.CreateOctahedronDefinition();
			case 3:
				return PolyhedronCreator.CreateDodecahedronDefinition();
			default:
				return PolyhedronCreator.CreateIcosahedronDefinition();
		}
	}

	static Transform RandomTransform(float translation)
	{
		return new Transform(
			new Vector(Random.Shared.NextSingle() * translation, Random.Shared.NextSingle() * translation, Random.Shared.NextSingle() * translation),
			//new Vector(0, 0, 0),
			Quaternion.FromAngle(
				new Vector(Random.Shared.NextSingle() * 5, Random.Shared.NextSingle() * 5, Random.Shared.NextSingle() * 5)
			)
		);
	}

	public interface IAtomic
	{
		public int CompareExchange(ref int location, int comparand, int value);
	}

	public static int InsertNodeLockFree<TAtomic>(TAtomic atomic, ref int firstIndex, int newFirstIndex)
		where TAtomic : IAtomic
	{
		while (true)
		{
			int oldFirstIndex = firstIndex;
			if (atomic.CompareExchange(ref firstIndex, newFirstIndex, oldFirstIndex) == oldFirstIndex)
			{
				return oldFirstIndex;
			}
		}
	}
}
