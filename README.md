# Multi-threaded 3D Physics Simulator for CPU and GPU

## Build Process

The solution can be built using **dotnet build** or by opening and running it in Visual Studio 2022. Some nuget dependencies (less than 10 MB) will be installed if required.

## Solution Structure

### Simulator

Contains the cross-platform physics code and the executors for running the simulation on the CPU (single- or multi-threaded) and on the GPU.

- ### Engine
  Contains the storage and executor agnostic algorithms.
  
- ### Backend
  Contains ILGPU setup (**DeviceManager**), and device specific backends. Due to lack of dependant types in C#, large interfaces and lot of boilerplate was required.
  
  - ### Cpu
    Contains squential and parallel CPU executors and storage. The objects are stored in lists to support easy addition of bodies and shapes.
  
  - ### Gpu
    Contains GPU executor and storage (based on the ILGPU **ArrayView** container). Every type supports copying to and from it's CPU side equivalent.

### WpfApp2

Contains a WPF GUI application with example demos and debug tools.

I want to emphasize, that because of the limited time frame, **I made no effort to uphold any code quality in this one** (even the project name reflects this). Also, it's written using heavy assistance from AI.
