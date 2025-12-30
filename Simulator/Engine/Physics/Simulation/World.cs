namespace Simulator.Engine.Physics.Simulation;

using Simulator.Core;
using Simulator.Engine;
using Simulator.Engine.Collisions;
using Simulator.Engine.Collisions.BroadPhase;
using Simulator.Engine.Geometry;
using Simulator.Engine.Physics;

public partial struct World<TStorage, TCollisionMap>
	where TStorage : struct, IWorldStorage
	where TCollisionMap : struct, ICollisionMap
{
	public TStorage Storage;
	public TCollisionMap CollisionMap;

	public WorldStepTimes Step(IExecutor executor)
	{
		UpdateShapeTransformCommand updateShapeTransformCommand = new UpdateShapeTransformCommand(this);
		UpdateBodyTransformCommand updateBodyTransformCommand = new UpdateBodyTransformCommand(this);
		CollisionCallback collisionCallback = new CollisionCallback(this);
		PrepareContactCommand prepareContactCommand = new PrepareContactCommand(this);
		ApplyBodyGravityCommand applyBodyGravityCommand = new ApplyBodyGravityCommand(this);
		SolveContactCommand solveContactCommand = new SolveContactCommand(this);
		LoadContactCommand loadContactCommand = new LoadContactCommand(this);
		SaveContactCommand saveContactCommand = new SaveContactCommand(this);
		ApplyBodySplitImpulsesCommand applyBodySplitImpulsesCommand = new ApplyBodySplitImpulsesCommand(this);
		FinalizeBodyTransformCommand finalizeBodyTransformCommand = new FinalizeBodyTransformCommand(this);
		WorldStepTimes times = new WorldStepTimes();
		Timer timer = new Timer();
		Storage.PrepareStep(executor);
		CollisionMap.PrepareUpdate(executor);
		times.PreparationTime = timer.Lap();
		executor.Execute(updateShapeTransformCommand, Storage.ShapeCount);
		CollisionMap.UpdateNodes(executor);
		times.ShapeUpdateTime = timer.Lap();
		executor.Execute(updateBodyTransformCommand, Storage.BodyCount);
		times.BodyUpdateTime = timer.Lap();
		CollisionMap.DetectCollisions(executor, collisionCallback);
		times.CollisionDetectionTime = timer.Lap();
		executor.Execute(prepareContactCommand, Storage.ContactCount);
		times.ContactPreparationTime = timer.Lap();
		executor.Execute(applyBodyGravityCommand, Storage.BodyCount);
		times.GravityApplicationTime = timer.Lap();
		if (Storage.UseWarmStarting != 0)
		{
			executor.Execute(loadContactCommand, Storage.ContactCount);
			times.ContactCacheLoadingTime = timer.Lap();
			executor.Execute(applyBodySplitImpulsesCommand, Storage.BodyCount);
			times.ContactWarmStartingTime = timer.Lap();
		}
		int iterationCount = Storage.GetIterationCount();
		for (int iterationIndex = 0; iterationIndex < iterationCount; iterationIndex++)
		{
			executor.Execute(solveContactCommand, Storage.ContactCount);
			executor.Execute(applyBodySplitImpulsesCommand, Storage.BodyCount);
		}
		times.ContactResolutionTime = timer.Lap();
		if (Storage.UseWarmStarting != 0)
		{
			Storage.ClearContactCache(executor);
			executor.Execute(saveContactCommand, Storage.ContactCount);
			times.ContactCacheSavingTime = timer.Lap();
		}
		executor.Execute(finalizeBodyTransformCommand, Storage.BodyCount);
		times.BodyFinalizationTime = timer.Lap();
		return times;
	}

	private void UpdateShapeTransform(int shapeIndex)
	{
		int bodyIndex = Storage.GetShapeBodyIndex(shapeIndex);
		Transform bodyTransform = Storage.GetBodyTransform(bodyIndex);
		Bound bound = new Bound();
		Storage.UpdateShapeTransform(shapeIndex, bodyTransform, ref bound);
		CollisionMap.UpdateBound(shapeIndex, bound);
	}

	private void UpdateBodyTransform(int bodyIndex)
	{
		ref Transform transform = ref Storage.GetBodyTransform(bodyIndex);
		ref InverseMass inverseMass = ref Storage.GetBodyInverseMass(bodyIndex);
		MassProperties massProperties = Storage.GetBodyMassProperties(bodyIndex);
		Matrix rotationMatrix = transform.Rotation.ToMatrix();
		if (massProperties.LinearMass != 0)
		{
			inverseMass.Linear = 1 / massProperties.LinearMass;
		}
		if (Vector.TripleProduct(massProperties.AngularMass.X, massProperties.AngularMass.Y, massProperties.AngularMass.Z) != 0)
		{
			inverseMass.Angular = rotationMatrix * massProperties.AngularMass.Invert() * rotationMatrix.Transpose();
		}
		transform.Position += transform.Rotation * massProperties.Centroid;
	}

	private void CheckShapeCollision(int shapeIndexA, int shapeIndexB)
	{
		int bodyIndexA = Storage.GetShapeBodyIndex(shapeIndexA);
		int bodyIndexB = Storage.GetShapeBodyIndex(shapeIndexB);
		if (bodyIndexA == bodyIndexB)
		{
			return;
		}
		Collision collision = new Collision();
		if (Storage.CheckShapeCollision(shapeIndexA, shapeIndexB, ref collision) == false)
		{
			return;
		}
		Material materialA = Storage.GetShapeMaterial(shapeIndexA);
		Material materialB = Storage.GetShapeMaterial(shapeIndexB);
		Material combinedMaterial = Material.Combine(materialA, materialB);
		Transform transformA = Storage.GetBodyTransform(bodyIndexA);
		Transform transformB = Storage.GetBodyTransform(bodyIndexB);
		Vector leverA = collision.Point - transformA.Position;
		Vector leverB = collision.Point - transformB.Position;
		Contact contact = new Contact(collision, combinedMaterial, leverA, leverB);
		Storage.AddContact(shapeIndexA, shapeIndexB, bodyIndexA, bodyIndexB, contact);
	}

	private void PrepareContact(int contactIndex)
	{
		ref Contact contact = ref Storage.GetContact(contactIndex);
		ContactBodyLink bodyLink = Storage.GetContactBodyLink(contactIndex);
		int bodyIndexA = bodyLink.BodyIndexA;
		int bodyIndexB = bodyLink.BodyIndexB;
		InverseMass inverseMassA = Storage.GetBodyInverseMass(bodyIndexA);
		InverseMass inverseMassB = Storage.GetBodyInverseMass(bodyIndexB);
		Twist velocityA = Storage.GetBodyVelocity(bodyIndexA);
		Twist velocityB = Storage.GetBodyVelocity(bodyIndexB);
		int contactCountA = Storage.GetBodyContactCount(bodyIndexA);
		int contactCountB = Storage.GetBodyContactCount(bodyIndexB);
		Vector effectiveVelocityA = ContactHelper.GetEffectiveVelocity(velocityA, contact.LeverA);
		Vector effectiveVelocityB = ContactHelper.GetEffectiveVelocity(velocityB, contact.LeverB);
		Vector relativeVelocity = effectiveVelocityB - effectiveVelocityA;
		float normalVelocity = Vector.Dot(contact.Normal, relativeVelocity);
		contact.TargetNormalVelocity = -RealMath.Min(normalVelocity, 0) * RealMath.Sqrt(contact.Material.Restitution);
		if (Storage.CorrectionVelocityFactor != 0 && Storage.CorrectionVelocityLimit != 0)
		{
			float correctionVelocity = RealMath.Min(contact.Depth / Storage.GetDeltaTime() * Storage.CorrectionVelocityFactor, Storage.CorrectionVelocityLimit);
			contact.TargetNormalVelocity = RealMath.Max(contact.TargetNormalVelocity, correctionVelocity);
		}
		float normalInverseMassA = ContactHelper.GetEffectiveInverseMass(inverseMassA, contact.LeverA, contact.Normal);
		float normalInverseMassB = ContactHelper.GetEffectiveInverseMass(inverseMassB, contact.LeverB, contact.Normal);
		float normalInverseMass = normalInverseMassA * contactCountA + normalInverseMassB * contactCountB;
		if (normalInverseMass != 0)
		{
			contact.NormalMass = 1 / normalInverseMass;
		}
		Vector tangentVelocity = relativeVelocity - contact.Normal * normalVelocity;
		float tangentVelocityLength = tangentVelocity.GetLength();
		if (tangentVelocityLength != 0)
		{
			contact.Tangent = tangentVelocity / tangentVelocityLength;
			float tangentInverseMassA = ContactHelper.GetEffectiveInverseMass(inverseMassA, contact.LeverA, contact.Tangent);
			float tangentInverseMassB = ContactHelper.GetEffectiveInverseMass(inverseMassB, contact.LeverB, contact.Tangent);
			float tangentInverseMass = tangentInverseMassA * contactCountA + tangentInverseMassB * contactCountB;
			contact.TangentMass = 1 / tangentInverseMass;
		}
	}

	private void SolveContact(int contactIndex)
	{
		ref Contact contact = ref Storage.GetContact(contactIndex);
		ContactBodyLink bodyLink = Storage.GetContactBodyLink(contactIndex);
		int bodyIndexA = bodyLink.BodyIndexA;
		int bodyIndexB = bodyLink.BodyIndexB;
		Twist velocityA = Storage.GetBodyVelocity(bodyIndexA);
		Twist velocityB = Storage.GetBodyVelocity(bodyIndexB);
		Vector effectiveVelocityA = ContactHelper.GetEffectiveVelocity(velocityA, contact.LeverA);
		Vector effectiveVelocityB = ContactHelper.GetEffectiveVelocity(velocityB, contact.LeverB);
		Vector relativeVelocity = effectiveVelocityB - effectiveVelocityA;
		float normalVelocity = Vector.Dot(contact.Normal, relativeVelocity);
		float normalImpulse = (contact.TargetNormalVelocity - normalVelocity) * contact.NormalMass;
		contact.AddNormalImpulse(ref normalImpulse);
		float tangentVelocity = Vector.Dot(contact.Tangent, relativeVelocity);
		float tangentImpulse = -tangentVelocity * contact.TangentMass;
		contact.AddTangentImpulse(ref tangentImpulse);
		Vector contactImpulse = contact.Normal * normalImpulse + contact.Tangent * tangentImpulse;
		ContactSplitLink splitLink = Storage.GetContactSplitLink(contactIndex);
		int splitIndexA = splitLink.SplitIndexA;
		int splitIndexB = splitLink.SplitIndexB;
		ref Twist splitImpulseA = ref Storage.GetSplitImpulse(splitIndexA);
		ref Twist splitImpulseB = ref Storage.GetSplitImpulse(splitIndexB);
		splitImpulseA = ContactHelper.GetEffectiveImpulse(contact.LeverA, -contactImpulse);
		splitImpulseB = ContactHelper.GetEffectiveImpulse(contact.LeverB, contactImpulse);
	}

	private void LoadContactCache(int contactIndex)
	{
		ContactSplitLink splitLink = Storage.GetContactSplitLink(contactIndex);
		int splitIndexA = splitLink.SplitIndexA;
		int splitIndexB = splitLink.SplitIndexB;
		ref Twist splitImpulseA = ref Storage.GetSplitImpulse(splitIndexA);
		ref Twist splitImpulseB = ref Storage.GetSplitImpulse(splitIndexB);
		ContactCache cache = new ContactCache();
		if (Storage.LoadContactCache(contactIndex, ref cache))
		{
			ref Contact contact = ref Storage.GetContact(contactIndex);
			contact.Persist(cache);
			Vector contactImpulse = contact.Normal * contact.TotalNormalImpulse + contact.Tangent * contact.TotalTangentImpulse;
			splitImpulseA = ContactHelper.GetEffectiveImpulse(contact.LeverA, -contactImpulse);
			splitImpulseB = ContactHelper.GetEffectiveImpulse(contact.LeverB, contactImpulse);
		}
		else
		{
			splitImpulseA = Twist.Zero;
			splitImpulseB = Twist.Zero;
		}
	}

	private void SaveContactCache(int contactIndex)
	{
		Storage.SaveContactCache(contactIndex);
	}

	private void ApplyBodySplitImpulses(int bodyIndex)
	{
		ref Twist velocity = ref Storage.GetBodyVelocity(bodyIndex);
		InverseMass inverseMass = Storage.GetBodyInverseMass(bodyIndex);
		int splitIndex = Storage.GetBodySplitIndex(bodyIndex);
		while (splitIndex != -1)
		{
			Twist splitImpulse = Storage.GetSplitImpulse(splitIndex);
			velocity += inverseMass * splitImpulse;
			splitIndex = Storage.GetSplitNextIndex(splitIndex);
		}
	}

	private void ApplyBodyGravity(int bodyIndex)
	{
		ref Twist velocity = ref Storage.GetBodyVelocity(bodyIndex);
		Twist gravity = Storage.GetBodyGravity(bodyIndex);
		float deltaTime = Storage.GetDeltaTime();
		velocity += gravity * deltaTime;
	}

	private void FinalizeBodyTransform(int bodyIndex)
	{
		ref Transform transform = ref Storage.GetBodyTransform(bodyIndex);
		Twist velocity = Storage.GetBodyVelocity(bodyIndex);
		float deltaTime = Storage.GetDeltaTime();
		Twist positionChange = velocity * deltaTime;
		transform.Position += positionChange.Linear;
		transform.Rotation *= Quaternion.FromAngle(positionChange.Angular);
		MassProperties massProperties = Storage.GetBodyMassProperties(bodyIndex);
		transform.Position -= transform.Rotation * massProperties.Centroid;
	}
}
