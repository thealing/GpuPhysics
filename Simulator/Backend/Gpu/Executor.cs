namespace Simulator.Backend.Gpu;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ILGPU;
using ILGPU.Backends;
using ILGPU.Backends.OpenCL;
using ILGPU.Runtime;
using Simulator.Engine.Core;

public class Executor : IExecutor
{
	public Executor(Accelerator accelerator)
	{
		_accelerator = accelerator;
		_kernelMap = new Dictionary<Type, object>();
	}

	public void Execute<TCommand>(TCommand command, int count)
		where TCommand : struct, ICommand
	{
		Type commandType = typeof(TCommand);
		if (_kernelMap.TryGetValue(commandType, out object? kernelObject) == false || kernelObject is not Action<Index1D, TCommand> kernel)
		{
			kernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, TCommand>(Kernel);
			_kernelMap[commandType] = kernel;
			DumpKernel(kernel, commandType.Name);
		}
		if (count > 0)
		{
			kernel(count, command);
			_accelerator.Synchronize();
		}
	}

	private static void Kernel<TCommand>(Index1D index, TCommand command)
		where TCommand : struct, ICommand
	{
		command.Execute(index);
	}

	private static void DumpKernel<TCommand>(Action<Index1D, TCommand> kernel, string name)
		where TCommand : struct, ICommand
	{
		CompiledKernel compiledKernel = kernel.GetCompiledKernel();
		if (compiledKernel is CLCompiledKernel clCompiledKernel)
		{
			string kernelDirectoryName = "Kernels";
			Directory.CreateDirectory(kernelDirectoryName);
			string kernelFileName = Path.Combine(kernelDirectoryName, $"{name}.c");
			using FileStream fileStream = new FileStream(kernelFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
			using StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8);
			writer.WriteLine($"// {name}");
			writer.WriteLine();
			writer.WriteLine(clCompiledKernel.Source);
		}
	}

	private readonly Accelerator _accelerator;
	private readonly Dictionary<Type, object> _kernelMap;
}
