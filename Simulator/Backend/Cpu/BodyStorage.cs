namespace Simulator.Backend.Cpu;

using System.Collections.Generic;
using Simulator.Backend.Common;
using Simulator.Core;
using Simulator.Engine.Geometry;
using Simulator.Engine.Physics;

public readonly partial struct BodyStorage
{
	public int Count => Transforms.Count;
	public List<Transform> Transforms { get; }
	public List<Twist> Velocities { get; }
	public List<MassProperties> MassProperties { get; }
	public List<InverseMass> InverseMasses { get; }
	public List<int> ContactCounts { get; }
	public List<Node> SplitNodes { get; }

	public BodyStorage()
	{
		Transforms = new List<Transform>();
		Velocities = new List<Twist>();
		MassProperties = new List<MassProperties>();
		InverseMasses = new List<InverseMass>();
		ContactCounts = new List<int>();
		SplitNodes = new List<Node>();
	}

	public int AddBody(BodyDefinition definition)
	{
		Transforms.Add(definition.Transform);
		Velocities.Add(definition.Velocity);
		MassProperties massProperties = new MassProperties();
		MassProperties.Add(massProperties);
		InverseMass inverseMass = new InverseMass();
		InverseMasses.Add(inverseMass);
		ContactCounts.Add(0);
		Node node = new Node(int.MaxValue);
		SplitNodes.Add(node);
		return Count - 1;
	}

	public void Reset(IExecutor executor)
	{
		ResetBodyCommand command = new ResetBodyCommand(this);
		executor.Execute(command, Count);
	}
}
