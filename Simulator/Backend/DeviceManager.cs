namespace Simulator.Backend;

using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.OpenCL;

public class DeviceManager
{
	public Accelerator Accelerator { get; }

	public DeviceManager()
	{
		Context context = Context.CreateDefault();
		Device device = context.GetPreferredDevice(false);
		Accelerator = device.CreateAccelerator(context);
		//Accelerator = context.CreateCLAccelerator(0);
		//Accelerator = context.CreateCPUAccelerator(0, CPUAcceleratorMode.Parallel);
		Accelerator.PrintInformation();
	}
}
