namespace Simulator.Backend;

using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;

public class DeviceManager
{
	public Accelerator Accelerator { get; }

	public DeviceManager()
	{
		Context context = Context.CreateDefault();
		Device device = context.GetPreferredDevice(false);
		Accelerator = device.CreateAccelerator(context);
		if (true)
		{
			Context.DeviceCollection<CLDevice> clDevices = context.GetCLDevices();
			if (clDevices.Count > 1)
			{
				Accelerator = clDevices[0].CreateAccelerator(context);
			}
			Context.DeviceCollection<CudaDevice> cudaDevices = context.GetCudaDevices();
			if (cudaDevices.Count > 1)
			{
				Accelerator = cudaDevices[0].CreateAccelerator(context);
			}
		}
		Accelerator.PrintInformation();
	}
}
