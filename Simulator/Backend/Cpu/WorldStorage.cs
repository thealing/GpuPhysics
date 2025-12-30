namespace Simulator.Backend.Cpu;

using System.Collections.Generic;
using Simulator.Backend.Common;
using Simulator.Core;
using Simulator.Engine;
using Simulator.Engine.Collisions;
using Simulator.Engine.Collisions.NarrowPhase;
using Simulator.Engine.Geometry;
using Simulator.Engine.Physics;
using Simulator.Engine.Physics.Simulation;

public struct WorldStorage : IWorldStorage
{
	public const int MaxContactCountPerShape = 20;

	public int BodyCount => BodyStorage.Count;
	public int ShapeCount => Shapes.Count;
	public int ContactCount => ContactStorage.ContactCount.Value;
	public byte UseWarmStarting => Config.UseWarmStarting;
	public float CorrectionVelocityFactor => Config.CorrectionVelocityFactor;
	public float CorrectionVelocityLimit => Config.CorrectionVelocityLimit;
	public WorldConfig Config;
	public List<Shape> Shapes;
	public List<Sphere> Spheres;
	public PolyhedronStorage PolyhedronStorage;
	public BodyStorage BodyStorage;
	public ContactStorage ContactStorage;
	public DynamicGridStorage DynamicGridStorage;

	public WorldStorage()
	{
		Shapes = new List<Shape>();
		Spheres = new List<Sphere>();
		PolyhedronStorage = new PolyhedronStorage();
		BodyStorage = new BodyStorage();
		ContactStorage = new ContactStorage();
		DynamicGridStorage = new DynamicGridStorage();
	}

	public int AddBody(BodyDefinition definition)
	{
		return BodyStorage.AddBody(definition);
	}

	public int AddSphereShape(ShapeDefinition definition, Vector center, float radius)
	{
		Shape shape = new Shape(definition);
		shape.SphereIndex = Spheres.Count;
		Sphere sphere = new Sphere(center, radius);
		Spheres.Add(sphere);
		ShapeProperties shapeProperties = sphere.GetProperties();
		return AddShape(shape, shapeProperties, definition.Density);
	}

	public int AddPolyhedronShape(ShapeDefinition definition, PolyhedronDefinition polyhedronDefinition)
	{
		Shape shape = new Shape(definition);
		shape.PolyhedronIndex = PolyhedronStorage.Count;
		PolyhedronStorage.AddPolyhedron(polyhedronDefinition);
		ShapeProperties shapeProperties = PolyhedronStorage.GetPolyhedron(shape.PolyhedronIndex).GetProperties();
		return AddShape(shape, shapeProperties, definition.Density);
	}

	private int AddShape(Shape shape, ShapeProperties shapeProperties, float density)
	{
		if (density != 0)
		{
			MassProperties massProperties = new MassProperties(shapeProperties, density);
			ref MassProperties bodyMassProperties = ref BodyStorage.MassProperties.GetMutableItem(shape.BodyIndex);
			bodyMassProperties = MassProperties.Combine(bodyMassProperties, massProperties);
		}
		Shapes.Add(shape);
		DynamicGridStorage.ObjectCount = Shapes.Count;
		DynamicGridStorage.SetCapacity(Shapes.Capacity);
		ContactStorage.SetCapacity(Shapes.Capacity * MaxContactCountPerShape);
		return Shapes.Count - 1;
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
		return ref BodyStorage.Transforms.GetMutableItem(index);
	}

	public ref Twist GetBodyVelocity(int index)
	{
		return ref BodyStorage.Velocities.GetMutableItem(index);
	}

	public ref InverseMass GetBodyInverseMass(int index)
	{
		return ref BodyStorage.InverseMasses.GetMutableItem(index);
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
			ref Sphere sphere = ref Spheres.GetMutableItem(shape.SphereIndex);
			sphere.SetTransform(transform);
			bound = sphere.GetBound();
		}
		if (shape.PolyhedronIndex != -1)
		{
			bound = PolyhedronStorage.UpdateTransform(shape.PolyhedronIndex, transform);
			bound = PolyhedronStorage.GetPolyhedron(shape.PolyhedronIndex).GetBound();
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
				var polyhedronB = PolyhedronStorage.GetPolyhedron(shapeB.PolyhedronIndex);
				return SpherePolyhedronCollision.Check(sphereA, polyhedronB, ref collision);
			}
		}
		if (shapeA.PolyhedronIndex != -1)
		{
			var polyhedronA = PolyhedronStorage.GetPolyhedron(shapeA.PolyhedronIndex);
			if (shapeB.SphereIndex != -1)
			{
				Sphere sphereB = Spheres[shapeB.SphereIndex];
				return PolyhedronSphereCollision.Check(polyhedronA, sphereB, ref collision);
			}
			if (shapeB.PolyhedronIndex != -1)
			{
				var polyhedronB = PolyhedronStorage.GetPolyhedron(shapeB.PolyhedronIndex);
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
		ILGPU.Atomic.Add(ref BodyStorage.ContactCounts.GetMutableItem(bodyIndexA), 1);
		ILGPU.Atomic.Add(ref BodyStorage.ContactCounts.GetMutableItem(bodyIndexB), 1);
		ContactSplitLink splitLink = ContactSplitLink.FromDensePairIndex(contactIndex);
		Atomic atomic = new Atomic();
		ContactStorage.SplitContacts[splitLink.SplitIndexA].Node = Node.Insert(atomic, ref BodyStorage.SplitNodes.GetMutableItem(bodyIndexA), splitLink.SplitIndexA);
		ContactStorage.SplitContacts[splitLink.SplitIndexB].Node = Node.Insert(atomic, ref BodyStorage.SplitNodes.GetMutableItem(bodyIndexB), splitLink.SplitIndexB);
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
