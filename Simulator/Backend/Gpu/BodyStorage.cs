namespace Simulator.Backend.Gpu;

using ILGPU;
using ILGPU.Runtime;
using Simulator.Engine.Core;
using Simulator.Engine.Geometry;
using Simulator.Engine.Physics;

public partial struct BodyStorage
{
	public int Count;
	public ArrayView<Transform> Transforms;
	public ArrayView<Twist> Velocities;
	public ArrayView<MassProperties> MassProperties;
	public ArrayView<InverseMass> InverseMasses;
	public ArrayView<int> ContactCounts;
	public ArrayView<Node> SplitNodes;

	public BodyStorage(Accelerator accelerator)
	{
		Transforms = accelerator.AllocateZeroedView<Transform>(0);
		Velocities = accelerator.AllocateZeroedView<Twist>(0);
		MassProperties = accelerator.AllocateZeroedView<MassProperties>(0);
		InverseMasses = accelerator.AllocateZeroedView<InverseMass>(0);
		ContactCounts = accelerator.AllocateZeroedView<int>(0);
		SplitNodes = accelerator.AllocateZeroedView<Node>(0);
	}

	public void CopyFromCPU(Cpu.BodyStorage bodyStorage)
	{
		Count = bodyStorage.Count;
		Transforms.SafeCopyFromCPU(bodyStorage.Transforms);
		Velocities.SafeCopyFromCPU(bodyStorage.Velocities);
		MassProperties.SafeCopyFromCPU(bodyStorage.MassProperties);
		InverseMasses.SafeCopyFromCPU(bodyStorage.InverseMasses);
		ContactCounts.SafeCopyFromCPU(bodyStorage.ContactCounts);
		SplitNodes.SafeCopyFromCPU(bodyStorage.SplitNodes);
	}

	public void CopyToCPU(Cpu.BodyStorage bodyStorage)
	{
		Transforms.CopyToCPU(bodyStorage.Transforms);
		Velocities.CopyToCPU(bodyStorage.Velocities);
		MassProperties.CopyToCPU(bodyStorage.MassProperties);
		InverseMasses.CopyToCPU(bodyStorage.InverseMasses);
		ContactCounts.CopyToCPU(bodyStorage.ContactCounts);
		SplitNodes.CopyToCPU(bodyStorage.SplitNodes);
	}

	public void Reset(IExecutor executor)
	{
		ResetBodyCommand command = new ResetBodyCommand(this);
		executor.Execute(command, Count);
	}
}
