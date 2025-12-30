namespace Simulator.Backend.Gpu;

using ILGPU;
using ILGPU.Runtime;
using Simulator.Backend.Common;
using Simulator.Core;
using Simulator.Engine.Collisions;
using Simulator.Engine.Collisions.NarrowPhase;
using Simulator.Engine.Geometry;
using Simulator.Engine.Physics;
using Simulator.Engine.Physics.Simulation;

public struct WorldStorage : IWorldStorage
{
	public int BodyCount { get; set; }
	public int ShapeCount { get; set; }
	public int ContactCount => ContactStorage.GetContactCountOnCpu();
	public byte UseWarmStarting => Config.UseWarmStarting;
	public float CorrectionVelocityFactor => Config.CorrectionVelocityFactor;
	public float CorrectionVelocityLimit => Config.CorrectionVelocityLimit;
	public WorldConfig Config;
	public ArrayView<Shape> Shapes;
	public ArrayView<Sphere> Spheres;
	public PolyhedronStorage PolyhedronStorage;
	public BodyStorage BodyStorage;
	public ContactStorage ContactStorage;
	public DynamicGridStorage DynamicGridStorage;

	public WorldStorage(Accelerator accelerator)
	{
		Shapes = accelerator.AllocateZeroedView<Shape>(0);
		Spheres = accelerator.AllocateZeroedView<Sphere>(0);
		PolyhedronStorage = new PolyhedronStorage(accelerator);
		BodyStorage = new BodyStorage(accelerator);
		ContactStorage = new ContactStorage(accelerator);
		DynamicGridStorage = new DynamicGridStorage(accelerator);
	}

	public void CopyFromCPU(Cpu.WorldStorage worldStorage)
	{
		BodyCount = worldStorage.BodyCount;
		ShapeCount = worldStorage.ShapeCount;
		Config = worldStorage.Config;
		Shapes.SafeCopyFromCPU(worldStorage.Shapes);
		Spheres.SafeCopyFromCPU(worldStorage.Spheres);
		PolyhedronStorage.CopyFromCPU(worldStorage.PolyhedronStorage);
		BodyStorage.CopyFromCPU(worldStorage.BodyStorage);
		ContactStorage.CopyFromCPU(worldStorage.ContactStorage);
		DynamicGridStorage.CopyFromCPU(worldStorage.DynamicGridStorage);
	}

	public void CopyToCPU(Cpu.WorldStorage worldStorage)
	{
		Shapes.CopyToCPU(worldStorage.Shapes);
		Spheres.CopyToCPU(worldStorage.Spheres);
		Config = worldStorage.Config;
		PolyhedronStorage.CopyToCPU(worldStorage.PolyhedronStorage);
		BodyStorage.CopyToCPU(worldStorage.BodyStorage);
		ContactStorage.CopyToCPU(worldStorage.ContactStorage);
		DynamicGridStorage.CopyToCPU(worldStorage.DynamicGridStorage);
	}

	public void PrepareStep(IExecutor executor)
	{
		BodyStorage.Reset(executor);
		ContactStorage.Reset();
	}

	public float GetDeltaTime()
	{
		return Config.DeltaTime;
	}

	public Twist GetBodyGravity(int index)
	{
		if (BodyStorage.InverseMasses[index].Linear == 0)
		{
			return Twist.Zero;
		}
		return Config.Gravity;
	}

	public ref Transform GetBodyTransform(int index)
	{
		return ref BodyStorage.Transforms[index];
	}

	public ref Twist GetBodyVelocity(int index)
	{
		return ref BodyStorage.Velocities[index];
	}

	public ref InverseMass GetBodyInverseMass(int index)
	{
		return ref BodyStorage.InverseMasses[index];
	}

	public MassProperties GetBodyMassProperties(int index)
	{
		return BodyStorage.MassProperties[index];
	}

	public int GetShapeBodyIndex(int index)
	{
		return Shapes[index].BodyIndex;
	}

	public Material GetShapeMaterial(int index)
	{
		return Shapes[index].Material;
	}

	public void UpdateShapeTransform(int index, Transform transform, ref Bound bound)
	{
		Shape shape = Shapes[index];
		if (shape.SphereIndex != -1)
		{
			ref Sphere sphere = ref Spheres[shape.SphereIndex];
			sphere.SetTransform(transform);
			bound = sphere.GetBound();
		}
		if (shape.PolyhedronIndex != -1)
		{
			bound = PolyhedronStorage.UpdateTransform(shape.PolyhedronIndex, transform);
		}
	}

	public bool CheckShapeCollision(int indexA, int indexB, ref Collision collision)
	{
		Shape shapeA = Shapes[indexA];
		Shape shapeB = Shapes[indexB];
		if (shapeA.SphereIndex != -1)
		{
			Sphere sphereA = Spheres[shapeA.SphereIndex];
			if (shapeB.SphereIndex != -1)
			{
				Sphere sphereB = Spheres[shapeB.SphereIndex];
				return SphereCollision.Check(sphereA, sphereB, ref collision);
			}
			if (shapeB.PolyhedronIndex != -1)
			{
				Polyhedron polyhedronB = PolyhedronStorage.GetPolyhedron(shapeB.PolyhedronIndex);
				return SpherePolyhedronCollision.Check(sphereA, polyhedronB, ref collision);
			}
		}
		if (shapeA.PolyhedronIndex != -1)
		{
			Polyhedron polyhedronA = PolyhedronStorage.GetPolyhedron(shapeA.PolyhedronIndex);
			if (shapeB.SphereIndex != -1)
			{
				Sphere sphereB = Spheres[shapeB.SphereIndex];
				return PolyhedronSphereCollision.Check(polyhedronA, sphereB, ref collision);
			}
			if (shapeB.PolyhedronIndex != -1)
			{
				Polyhedron polyhedronB = PolyhedronStorage.GetPolyhedron(shapeB.PolyhedronIndex);
				return PolyhedronCollision.Check(polyhedronA, polyhedronB, ref collision);
			}
		}
		return false;
	}

	public int GetBodyContactCount(int index)
	{
		return BodyStorage.ContactCounts[index];
	}

	public void AddContact(int shapeIndexA, int shapeIndexB, int bodyIndexA, int bodyIndexB, Contact contact)
	{
		int contactIndex = ContactStorage.Add(shapeIndexA, shapeIndexB, bodyIndexA, bodyIndexB, contact);
		if (contactIndex == -1)
		{
			// Warning: contact limit exceeded.
			return;
		}
		ILGPU.Atomic.Add(ref BodyStorage.ContactCounts[bodyIndexA], 1);
		ILGPU.Atomic.Add(ref BodyStorage.ContactCounts[bodyIndexB], 1);
		ContactSplitLink splitLink = ContactSplitLink.FromDensePairIndex(contactIndex);
		Atomic atomic = new Atomic();
		ContactStorage.SplitContacts[splitLink.SplitIndexA].Node = Node.Insert(atomic, ref BodyStorage.SplitNodes[bodyIndexA], splitLink.SplitIndexA);
		ContactStorage.SplitContacts[splitLink.SplitIndexB].Node = Node.Insert(atomic, ref BodyStorage.SplitNodes[bodyIndexB], splitLink.SplitIndexB);
	}

	public ref Contact GetContact(int index)
	{
		return ref ContactStorage.Contacts[index];
	}

	public ContactBodyLink GetContactBodyLink(int index)
	{
		return ContactStorage.ContactBodyLinks[index];
	}

	public ContactSplitLink GetContactSplitLink(int index)
	{
		return ContactSplitLink.FromDensePairIndex(index);
	}

	public int GetIterationCount()
	{
		return Config.IterationCount;
	}

	public int GetBodySplitIndex(int index)
	{
		return BodyStorage.SplitNodes[index].NextIndex;
	}

	public ref Twist GetSplitImpulse(int index)
	{
		return ref ContactStorage.SplitContacts[index].Impulse;
	}

	public int GetSplitNextIndex(int index)
	{
		return ContactStorage.SplitContacts[index].Node.NextIndex;
	}

	public void ClearContactCache(IExecutor executor)
	{
		ContactStorage.ClearContactCache(executor);
	}

	public void SaveContactCache(int index)
	{
		ContactStorage.SaveContactCache(index);
	}

	public bool LoadContactCache(int index, ref ContactCache cache)
	{
		return ContactStorage.LoadContactCache(index, ref cache);
	}
}
