namespace Simulator.Engine.Physics.Simulation;

using Simulator.Core;
using Simulator.Engine.Collisions;
using Simulator.Engine.Geometry;

public interface IWorldStorage
{
	public int BodyCount { get; }
	public int ShapeCount { get; }
	public int ContactCount { get; }
	public byte UseWarmStarting { get; }
	public float CorrectionVelocityFactor { get; }
	public float CorrectionVelocityLimit { get; }

	public void PrepareStep(IExecutor executor);

	public float GetDeltaTime();

	public ref Transform GetBodyTransform(int index);

	public ref Twist GetBodyVelocity(int index);

	public ref InverseMass GetBodyInverseMass(int index);

	public MassProperties GetBodyMassProperties(int index);

	public Twist GetBodyGravity(int index);

	public int GetShapeBodyIndex(int index);

	public Material GetShapeMaterial(int index);

	public void UpdateShapeTransform(int index, Transform transform, ref Bound bound);

	public bool CheckShapeCollision(int indexA, int indexB, ref Collision collision);

	public int GetIterationCount();

	public int GetBodyContactCount(int index);

	public int GetBodySplitIndex(int index);

	public void AddContact(int shapeIndexA, int shapeIndexB, int bodyIndexA, int bodyIndexB, Contact contact);

	public ref Contact GetContact(int index);

	public ContactBodyLink GetContactBodyLink(int index);

	public ContactSplitLink GetContactSplitLink(int index);

	public ref Twist GetSplitImpulse(int index);

	public int GetSplitNextIndex(int index);

	public void ClearContactCache(IExecutor executor);

	public void SaveContactCache(int index);

	public bool LoadContactCache(int index, ref ContactCache cache);
}
