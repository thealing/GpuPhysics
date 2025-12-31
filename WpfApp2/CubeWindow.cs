namespace WpfApp2;

using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Xml.Linq;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using Simulator.Engine.Physics.Simulation;
using Simulator.Engine.Physics;
using Simulator.Engine.Geometry;
using Simulator.Engine;
using Simulator.Engine.Geometry.Validation;
using System.Diagnostics;
using System.Windows.Input;
using System.Collections.Generic;
using Simulator.Engine.Collisions.BroadPhase;
using System.Linq;
using Simulator.Backend.Common;
using ILGPU.Runtime;

using Engine = Simulator.Engine;
using Backend = Simulator.Backend;
using Simulator.Backend;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Reflection.Emit;

class CubeWindow
{
	public static readonly DeviceManager deviceManager = new();

	public static void Show()
	{
		Application application = new Application();

		Window window = new Window();
		window.Title = "GPU Physics Demo";
		window.Width = 800;
		window.Height = 600;
		window.Background = Brushes.LightBlue;

		HelixViewport3D viewport = new HelixViewport3D();

		PerspectiveCamera camera = new PerspectiveCamera();
		camera.Position = new Point3D(0, 50, 200);
		camera.LookDirection = new Vector3D(0, 0, -1);
		camera.UpDirection = new Vector3D(0, 1, 0);
		camera.NearPlaneDistance = 0.1;
		camera.FarPlaneDistance = float.PositiveInfinity;
		camera.FieldOfView = 70;

		viewport.Camera = camera;

		viewport.CameraRotationMode = CameraRotationMode.Turnball;
		viewport.CameraMode = CameraMode.WalkAround;
		viewport.ShowViewCube = false;
		viewport.ShowTriangleCountInfo = true;
		viewport.ShowFrameRate = true;

		viewport.Children.Add(new SunLight());

		// axes
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Red,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(1e6, 0, 0) }
		});
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Green,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(0, 1e6, 0) }
		});
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Blue,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(0, 0, 1e6) }
		});
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Cyan,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(-1e6, 0, 0) }
		});
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Magenta,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(0, -1e6, 0) }
		});
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Yellow,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(0, 0, -1e6) }
		});

		if (false)// hello word sphere
		{
			viewport.Children.Add(new SphereVisual3D
			{
				Center = new Point3D(0, 0, 0),
				Radius = 1.0,
				Fill = Brushes.Red,
				PhiDiv = 16,
				ThetaDiv = 16
			});
		}

		// Model

		var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };

		if (false) // rotating cubes
		{
			System.Windows.Media.Media3D.MeshGeometry3D mesh = MeshCreator.CreatePolyhedronMesh(
				new WorldPolyhedron(PolyhedronCreator.CreateDodecahedronDefinition())
			);

			var material = new DiffuseMaterial(new SolidColorBrush(Colors.Green));

			var rotation = new AxisAngleRotation3D(new Vector3D(1, 1, 0), 0);
			timer.Tick += (s, e) => { rotation.Angle += 1.0; };

			for (int i = 0; i < 0; i++)
			{
				var translation = new TranslateTransform3D(i % 1000 / 100.0, i * 31 % 1000 / 100.0, i * 57 % 1000 / 100.0);

				var group = new Transform3DGroup();
				group.Children.Add(new RotateTransform3D(rotation));
				group.Children.Add(translation);

				var cube = new GeometryModel3D(mesh, material);
				cube.Transform = group;

				var cubeVisual = new ModelVisual3D { Content = cube };
				viewport.Children.Add(cubeVisual);
			}
		}

		// physics

		//CpuExecutor executor = new();
		//CpuWorldStorage storage = new();
		//DynamicGrid<CpuDGStore> broadPhase = new(new CpuDGStore());

		Backend.Cpu.Executor parallelExecutor = new();
		Backend.Cpu.SequentialExecutor sequentialExecutor = new();
		Backend.Cpu.WorldStorage storage = new();

		List<ModelVisual3D> worldModels = new();

		bool paused = false;

		window.KeyDown += (s, e) =>
		{
			if (e.Key == System.Windows.Input.Key.Space)
				paused ^= true;
		};

		Vector3D camForward = new Vector3D(0, 0, 1);
		Vector3D camRight = new Vector3D(1, 0, 0);
		Vector3D camUp = new Vector3D(0, 1, 0);     // Always UP (no tilt!)
		Point lastMouse = default;

		Vector3D dir = camera.LookDirection;
		dir.Normalize();

		// Compute initial yaw and pitch
		double yaw = Math.Atan2(dir.X, dir.Z) * 180.0 / Math.PI;      // Y rotated toward X
		double pitch = Math.Asin(dir.Y) * 180.0 / Math.PI;

		window.MouseMove += (s, e) =>
		{
			if (Mouse.RightButton == MouseButtonState.Pressed)
			{
				var pos = e.GetPosition(window);
				var dx = pos.X - lastMouse.X;
				var dy = pos.Y - lastMouse.Y;

				double sensitivity = 0.4;

				yaw   += dx * sensitivity;
				pitch += dy * sensitivity;

				// clamp pitch to avoid flipping
				pitch = Math.Max(-89, Math.Min(89, pitch));

				// rebuild direction vectors
				var yawRad = yaw * Math.PI / 180.0;
				var pitchRad = pitch * Math.PI / 180.0;

				var forward = new Vector3D(
					Math.Cos(pitchRad) * Math.Sin(yawRad),
					Math.Sin(pitchRad),
					Math.Cos(pitchRad) * Math.Cos(yawRad));

				camForward = forward;
				camRight = Vector3D.CrossProduct(camForward, new Vector3D(0, 1, 0));
				camRight.Normalize();
				camUp = Vector3D.CrossProduct(camRight, camForward);  // rebuild orthonormal UP

				camera.LookDirection = camForward;
				camera.UpDirection = camUp;
			}

			lastMouse = e.GetPosition(window);
		};

		// ============================================ SETTINGS ===================================================

		bool disableGraphics = false;

		bool demoCamera = true;

		int executorType = 0;

		bool copyBack = true;

		List<WorldStepTimes> times = new();

		Dictionary<string, TextBlock> timeLabels = new();

		int sampleCount = 10;

		bool restart = true;

		int speed = 5;

		bool colorByShape = false;

		WorldBuilder worldBuilder = new();

		window.Loaded += (s1, e1) =>
		{
			viewport.CameraController.RotationSensitivity = 0;

			// ------------------------------------------
			// 1. Create floating tool window
			// ------------------------------------------
			var toolWindow = new Window
			{
				//Title = "Camera Tools",
				Width = 700,
				Height = 100,
				Owner = window,
				ResizeMode = ResizeMode.CanResize
			};
			toolWindow.Loaded += (s2, e2) =>
			{
				toolWindow.Top = window.Top;
				toolWindow.Left = window.Left + 800;
			};

			//var tab1 = new TabItem();
			//tab1.Header = "Options";

			//var tab2 = new TabItem();
			//tab2.Header = "Statistics";

			var tabControl = new Grid();

			tabControl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
			tabControl.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			tabControl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });

			var tab1 = new ContentControl();
			var tab2 = new ContentControl();

			Grid.SetColumn(tab1, 0);
			Grid.SetColumn(tab2, 2);

			var splitter = new GridSplitter
			{
				Width = 5,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Stretch,
				ResizeBehavior = GridResizeBehavior.PreviousAndNext,
				ResizeDirection = GridResizeDirection.Columns,
				BorderThickness = new Thickness(0, 0, 1, 0),
				BorderBrush = Brushes.Black
			};

			Grid.SetColumn(splitter, 1);

			tabControl.Children.Add(tab1);
			tabControl.Children.Add(splitter);
			tabControl.Children.Add(tab2);

			// Root panel
			var root = new StackPanel { Margin = new Thickness(10) };

			tab1.Content = root;

			var statisticsPanel = new StackPanel { Margin = new Thickness(10) };

			tab2.Content = statisticsPanel;

			// ─────────────────────────────────────────────
			// Row: "Camera:"  [ X ] [ Y ] [ Z ]
			// ─────────────────────────────────────────────
			var row = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(0, 0, 0, 10)
			};

			var camLabel = new TextBlock
			{
				Text = "Camera: ",
				VerticalAlignment = VerticalAlignment.Center,
				Width = 60
			};

			var xBox = new TextBox { Text = "0", Width = 60, Margin = new Thickness(5, 0, 0, 0) };
			var yBox = new TextBox { Text = "100", Width = 60, Margin = new Thickness(5, 0, 0, 0) };
			var zBox = new TextBox { Text = "100", Width = 60, Margin = new Thickness(5, 0, 0, 0) };

			row.Children.Add(camLabel);
			row.Children.Add(xBox);
			row.Children.Add(yBox);
			row.Children.Add(zBox);

			// ─────────────────────────────────────────────
			// Button: Teleport
			// ─────────────────────────────────────────────
			var teleportBtn = new Button
			{
				Content = "Teleport Camera",
				Margin = new Thickness(0, 0, 0, 5),
				Width = 150
			};

			// ─────────────────────────────────────────────
			// Button: Reset angle
			// ─────────────────────────────────────────────
			var resetBtn = new Button
			{
				Content = "Reset Camera Angle",
				Width = 150
			};

			// Add to window
			root.Children.Add(row);
			root.Children.Add(teleportBtn);
			root.Children.Add(resetBtn);

			//camera
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 10)
				};
				var label2 = new TextBlock
				{
					Text = "Move Speed: ",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new TextBox { Text = viewport.CameraController.MoveSensitivity.ToString(),
					Width = 60, Margin = new Thickness(5, 0, 0, 0) };
				box2.TextChanged += (s1, e1) =>
				{
					if (float.TryParse(box2.Text.Trim(), out float i2))
						viewport.CameraController.MoveSensitivity = i2;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}

			//disable rendering
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 1, 0, 1)
				};
				var label2 = new TextBlock
				{
					Text = "Demo Camera Positions: ",
					ToolTip = "Move the camera such that the world is clearly visible in the examples",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new CheckBox
				{
					Margin = new Thickness(5, 0, 0, 0),
					IsChecked = demoCamera
				};
				box2.Unchecked += (s1, e1) => {
					demoCamera = false;
				};
				box2.Checked += (s1, e1) => {
					demoCamera = true;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}

			//disable rendering
			{
				var row2 = new StackPanel {
					Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 10)
				};
				var label2 = new TextBlock {
					Text = "Disable Graphics: ", VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new CheckBox {
					Margin = new Thickness(5, 0, 0, 0)
				};
				box2.Unchecked += (s1, e1) => {
					disableGraphics = false;
				};
				box2.Checked += (s1, e1) => {
					disableGraphics = true;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}

			{//delta time
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 10)
				};
				{
					var label2 = new TextBlock
					{
						Text = "Physics 1/dt: ",
						VerticalAlignment = VerticalAlignment.Center
					};
					var box2 = new TextBox
					{
						Text = ((int)Math.Round(1 / storage.Config.DeltaTime)).ToString(),
						Width = 60,
						Margin = new Thickness(5, 0, 0, 0)
					};
					box2.TextChanged += (s1, e1) =>
					{
						if (float.TryParse(box2.Text.Trim(), out float i2))
							storage.Config.DeltaTime = 1 / i2;
					};
					row2.Children.Add(label2);
					row2.Children.Add(box2);
				}
				{
					var label2 = new TextBlock
					{
						Text = " iterations: ",
						ToolTip = "Iteration count of the velocity solver.\n" +
						"(There is no position solver)",
						VerticalAlignment = VerticalAlignment.Center
					};
					var box2 = new TextBox
					{
						Text = storage.Config.IterationCount.ToString(),
						Width = 60,
						Margin = new Thickness(5, 0, 0, 0)
					};
					box2.TextChanged += (s1, e1) =>
					{
						if (int.TryParse(box2.Text.Trim(), out int i2))
							storage.Config.IterationCount = i2;
					};
					row2.Children.Add(label2);
					row2.Children.Add(box2);
				}
				root.Children.Add(row2);
			}

			// gravity
			{
				var row2 = new StackPanel()
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 10)
				};
				{
					var label2 = new TextBlock
					{
						Text = "Gravity: ",
						VerticalAlignment = VerticalAlignment.Center
					};
					row2.Children.Add(label2);
				}
				{
					var label2 = new TextBlock
					{
						Text = "X: ",
						VerticalAlignment = VerticalAlignment.Center
					};
					var box2 = new TextBox
					{
						Text = storage.Config.Gravity.Linear.X.ToString(),
						Width = 60,
						Margin = new Thickness(5, 0, 0, 0)
					};
					box2.TextChanged += (s1, e1) =>
					{
						if (float.TryParse(box2.Text.Trim(), out float i2))
							storage.Config.Gravity.Linear.X = i2;
					};
					row2.Children.Add(label2);
					row2.Children.Add(box2);
				}
				{
					var label2 = new TextBlock
					{
						Text = " Y: ",
						VerticalAlignment = VerticalAlignment.Center
					};
					var box2 = new TextBox
					{
						Text = storage.Config.Gravity.Linear.Y.ToString(),
						Width = 60,
						Margin = new Thickness(5, 0, 0, 0)
					};
					box2.TextChanged += (s1, e1) =>
					{
						if (float.TryParse(box2.Text.Trim(), out float i2))
							storage.Config.Gravity.Linear.Y = i2;
					};
					row2.Children.Add(label2);
					row2.Children.Add(box2);
				}
				{
					var label2 = new TextBlock
					{
						Text = " Z: ",
						VerticalAlignment = VerticalAlignment.Center
					};
					var box2 = new TextBox
					{
						Text = storage.Config.Gravity.Linear.Z.ToString(),
						Width = 60,
						Margin = new Thickness(5, 0, 0, 0)
					};
					box2.TextChanged += (s1, e1) =>
					{
						if (float.TryParse(box2.Text.Trim(), out float i2))
							storage.Config.Gravity.Linear.Z = i2;
					};
					row2.Children.Add(label2);
					row2.Children.Add(box2);
				}
				root.Children.Add(row2);
			}

			//world config
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 10)
				};
				var label2 = new TextBlock
				{
					Text = "Correction Velocity: ",
					ToolTip = "min(depth / dt * factor, limit)",
					VerticalAlignment = VerticalAlignment.Center
				};
				var cb = new CheckBox
				{
					Margin = new Thickness(5, 0, 0, 0),
					IsChecked = true
				};
				var label3 = new TextBlock
				{
					Text = " factor: ",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new TextBox
				{
					Text = storage.Config.CorrectionVelocityFactor.ToString(),
					Width = 60,
					Margin = new Thickness(5, 0, 0, 0)
				};
				box2.TextChanged += (s1, e1) =>
				{
					if (float.TryParse(box2.Text.Trim(), out float i2))
						storage.Config.CorrectionVelocityFactor = i2;
				};
				var label4 = new TextBlock
				{
					Text = " limit: ",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box3 = new TextBox
				{
					Text = storage.Config.CorrectionVelocityLimit.ToString(),
					Width = 60,
					Margin = new Thickness(5, 0, 0, 0)
				};
				box3.TextChanged += (s1, e1) =>
				{
					if (float.TryParse(box3.Text.Trim(), out float i2))
						storage.Config.CorrectionVelocityLimit = i2;
				};
				cb.Unchecked += (s1, e1) => {
					storage.Config.CorrectionVelocityFactor = 0;
					storage.Config.CorrectionVelocityLimit = 0;
				};
				cb.Checked += (s1, e1) => {
					if (float.TryParse(box2.Text.Trim(), out float i3))
						storage.Config.CorrectionVelocityFactor = i3;
					if (float.TryParse(box3.Text.Trim(), out float i2))
						storage.Config.CorrectionVelocityLimit = i2;
				};
				row2.Children.Add(label2);
				row2.Children.Add(cb);
				row2.Children.Add(label3);
				row2.Children.Add(box2);
				row2.Children.Add(label4);
				row2.Children.Add(box3);
				root.Children.Add(row2);
			}
			//config
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 10)
				};
				var label2 = new TextBlock
				{
					Text = "Warm-starting: ",
					ToolTip = "Apply impulses from the previous step before the current step.",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new CheckBox
				{
					Margin = new Thickness(5, 0, 0, 0),
					IsChecked = storage.Config.UseWarmStarting != 0
				};
				box2.Unchecked += (s1, e1) => {
					storage.Config.UseWarmStarting = 0;
				};
				box2.Checked += (s1, e1) => {
					storage.Config.UseWarmStarting = 1;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}
			//config
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 10)
				};
				var label2 = new TextBlock
				{
					Text = "Executor: ",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new ComboBox
				{
					Margin = new Thickness(5, 0, 0, 0),
					Items =
					{
						"Sequential", "Parallel on CPU", "Parallel on GPU"
					},
					SelectedIndex = executorType
				};
				box2.SelectionChanged += (s1, e1) => {
					executorType = box2.SelectedIndex;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}
			//thread count
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 0, 0, 5)
				};
				var label2 = new TextBlock
				{
					Text = "CPU thread count: ",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new TextBox
				{
					Width = 80,
					Margin = new Thickness(5, 0, 0, 0),
					Text = parallelExecutor.ParallelOptions.MaxDegreeOfParallelism.ToString()
				};
				box2.TextChanged += (s1, e1) => {
					if (int.TryParse(box2.Text, out int i2))
						parallelExecutor.ParallelOptions.MaxDegreeOfParallelism = i2;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}
			//copy back
			if (false)
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 0)
				};
				var label2 = new TextBlock
				{
					Text = "Copy to CPU: ",
					ToolTip = "Copy world data from GPU to CPU for rendering and debug info.\nDoesn't affect the simulation.",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new CheckBox
				{
					Margin = new Thickness(5, 0, 0, 0),
					IsChecked = copyBack
				};
				box2.Unchecked += (s1, e1) => {
					copyBack = false;
				};
				box2.Checked += (s1, e1) => {
					copyBack = true;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}

			// example selection
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 10)
				};
				var label2 = new TextBlock
				{
					Text = "Example world: ",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new ComboBox
				{
					Margin = new Thickness(5, 0, 0, 0)
				};

				foreach (var i in Enum.GetValues<Example>().Select(e => e.ToString().Replace('_', ' ')))
					box2.Items.Add(i);

				box2.SelectedIndex = (int)worldBuilder.example;

				box2.SelectionChanged += (s1, e1) => {
					worldBuilder.example = (Example)box2.SelectedIndex;
					restart = true;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}

			// wshape
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 10)
				};
				var label2 = new TextBlock
				{
					Text = "Shape type: ",
					ToolTip = "Type of spawned shapes. (Not used in some examples.)",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new ComboBox
				{
					Margin = new Thickness(5, 0, 0, 0)
				};

				foreach (var i in Enum.GetValues<ShapeType>().Select(e => e.ToString().Replace('_', ' ')))
					box2.Items.Add(i);

				box2.SelectedIndex = (int)worldBuilder.shapeType;

				box2.SelectionChanged += (s1, e1) => {
					worldBuilder.shapeType = (ShapeType)box2.SelectedIndex;
					restart = true;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}

			//object coint
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 5)
				};
				var label2 = new TextBlock
				{
					Text = "object count: ",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new TextBox
				{
					Width = 80,
					Margin = new Thickness(5, 0, 0, 0),
					Text = worldBuilder.objectCount.ToString()
				};
				box2.TextChanged += (s1, e1) => {
					int.TryParse(box2.Text, out worldBuilder.objectCount);
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}
			//multi shape body
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 5)
				};
				var label2 = new TextBlock
				{
					Text = "multi shape count: ",
					ToolTip = "Each body will have this many shapes around a XZ plane circle around it's center.",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new TextBox
				{
					Width = 80,
					Margin = new Thickness(5, 0, 0, 0),
					Text = worldBuilder.shapeCountPerBody.ToString()
				};
				box2.TextChanged += (s1, e1) => {
					int.TryParse(box2.Text, out worldBuilder.shapeCountPerBody);
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}
			//stabilizerOffset
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 5)
				};
				var label2 = new TextBlock
				{
					Text = "color by shape: ",
					ToolTip = "If true, shapes on the same body have different colors.",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new CheckBox
				{
					Width = 80,
					Margin = new Thickness(5, 0, 0, 0),
					IsChecked = colorByShape
				};
				box2.Unchecked += (s1, e1) => {
					colorByShape = false;
				};
				box2.Checked += (s1, e1) => {
					colorByShape = true;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}
			//object size
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 5)
				};
				{
					var label2 = new TextBlock
					{
						Text = " min size: ",
						VerticalAlignment = VerticalAlignment.Center
					};
					var box2 = new TextBox
					{
						Width = 50,
						Margin = new Thickness(5, 0, 0, 0),
						Text = worldBuilder.minSize.ToString()
					};
					box2.TextChanged += (s1, e1) => {
						float.TryParse(box2.Text, out worldBuilder.minSize);
					};
					row2.Children.Add(label2);
					row2.Children.Add(box2);
				}
				{
					var label2 = new TextBlock
					{
						Text = " max size: ",
						VerticalAlignment = VerticalAlignment.Center
					};
					var box2 = new TextBox
					{
						Width = 50,
						Margin = new Thickness(5, 0, 0, 0),
						Text = worldBuilder.maxSize.ToString()
					};
					box2.TextChanged += (s1, e1) => {
						float.TryParse(box2.Text, out worldBuilder.maxSize);
					};
					row2.Children.Add(label2);
					row2.Children.Add(box2);
				}
				root.Children.Add(row2);
			}
			//reset world
			{
				var label2 = new Button
				{
					Content = "Restart Simulation",
					Width = 200,
					Margin = new Thickness(0, 5, 0, 5)
				};
				label2.Click += (s, e) =>
				{
					restart = true;
				};
				root.Children.Add(label2);
			}

			//speed
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 5)
				};
				var label2 = new TextBlock
				{
					Text = "Steps per frame: ",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new TextBox
				{
					Width = 50,
					Margin = new Thickness(5, 0, 0, 0),
					Text = speed.ToString()
				};
				box2.TextChanged += (s1, e1) => {
					int.TryParse(box2.Text, out speed);
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}

			//paused
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 5)
				};
				var label2 = new TextBlock
				{
					Text = "Pause: ",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new CheckBox
				{
					Width = 80,
					Margin = new Thickness(5, 0, 0, 0),
					IsChecked = paused
				};
				box2.Unchecked += (s1, e1) => {
					paused = false;
				};
				box2.Checked += (s1, e1) => {
					paused = true;
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				root.Children.Add(row2);
			}

			// count data
			{
				void AddLabel(Panel root, string name)
				{
					var row = new StackPanel
					{
						Orientation = Orientation.Horizontal,
						Margin = new Thickness(0, 4, 0, 4)
					};

					var nameLabel = new TextBlock
					{
						Text = name + ":",
						Width = 220,
						VerticalAlignment = VerticalAlignment.Center
					};

					var valueLabel = new TextBlock
					{
						Text = "0",
						VerticalAlignment = VerticalAlignment.Center
					};

					row.Children.Add(nameLabel);
					row.Children.Add(valueLabel);
					statisticsPanel.Children.Add(row);

					timeLabels[name] = valueLabel;
				}

				AddLabel(root, "Body count");
				AddLabel(root, "Shape count");
				AddLabel(root, "Contact count");
			}

			//sampleCount
			{
				var row2 = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 5, 0, 5)
				};
				var label2 = new TextBlock
				{
					Text = "Sample count: ",
					VerticalAlignment = VerticalAlignment.Center
				};
				var box2 = new TextBox
				{
					Width = 80,
					Margin = new Thickness(5, 0, 0, 0),
					Text = sampleCount.ToString()
				};
				box2.TextChanged += (s1, e1) => {
					int.TryParse(box2.Text, out sampleCount);
				};
				row2.Children.Add(label2);
				row2.Children.Add(box2);
				statisticsPanel.Children.Add(row2);
			}
			// create laberls
			{

				void AddTimeLabel(Panel root, string name)
				{
					var row = new StackPanel
					{
						Orientation = Orientation.Horizontal,
						Margin = new Thickness(0, 4, 0, 4)
					};

					var nameLabel = new TextBlock
					{
						Text = name + ":",
						Width = 220,
						VerticalAlignment = VerticalAlignment.Center
					};

					var valueLabel = new TextBlock
					{
						Text = "-.--- ms",
						VerticalAlignment = VerticalAlignment.Center
					};

					row.Children.Add(nameLabel);
					row.Children.Add(valueLabel);
					statisticsPanel.Children.Add(row);

					timeLabels[name] = valueLabel;
				}

				// Create labels
				AddTimeLabel(root, "Preparation");
				AddTimeLabel(root, "Shape Update");
				AddTimeLabel(root, "Body Update");
				AddTimeLabel(root, "Collision Detection");
				AddTimeLabel(root, "Contact Preparation");
				AddTimeLabel(root, "Gravity Application");
				AddTimeLabel(root, "Contact Cache Loading");
				AddTimeLabel(root, "Contact Warm Starting");
				AddTimeLabel(root, "Contact Resolution");
				AddTimeLabel(root, "Contact Cache Saving");
				AddTimeLabel(root, "Body Finalization");
			}

			ScrollViewer scroll = new();
			scroll.Content = root;

			tab1.Content = scroll;
			tab2.Content = statisticsPanel;

			toolWindow.Content = tabControl;
			toolWindow.Show();

			double maxHeight = 0;
			foreach (var v in root.Children)
			{
				if (v is Control e)
				{
					Point bottom = e.TranslatePoint(new Point(0, e.ActualHeight), toolWindow);
					double bottomY = bottom.Y;
					maxHeight = Math.Max(maxHeight, bottomY);
				}
			}
			toolWindow.Height = Math.Min(760, root.ActualHeight + 100);

			// ------------------------------------------
			// 2. Get reference to your camera
			// ------------------------------------------
			// Works for HelixViewport3D or raw WPF camera
			var cam = viewport.Camera as PerspectiveCamera;

			// ------------------------------------------
			// 3. Teleport button handler
			// ------------------------------------------
			teleportBtn.Click += (s2, e2) =>
			{
				if (double.TryParse(xBox.Text, out double xx) &&
					double.TryParse(yBox.Text, out double yy) &&
					double.TryParse(zBox.Text, out double zz))
				{
					cam.Position = new Point3D(xx, yy, zz);
				}
			};

			// ------------------------------------------
			// 4. Reset angle button handler
			// ------------------------------------------
			resetBtn.Click += (s3, e3) =>
			{
				// You can define your own default direction here
				//cam.LookDirection = new Vector3D(0, 0, 1);
				cam.UpDirection = new Vector3D(0, 1, 0);
			};
		};

		//------------------------------- GRAPHICS -------------------------------

		var sorting = new SortingVisual3D
		{
			// None of it is accurate
			Method = SortingMethod.BoundingSphereSurface,
		};
		viewport.Children.Add(sorting);

		void CreateGraphics()
		{
			foreach (var m in worldModels)
				sorting.Children.Remove(m);

			worldModels.Clear();

			// Lower-resolution sphere
			const int theta = 32;
			const int phi = 32;

			var sphereBuilder = new MeshBuilder();
			sphereBuilder.AddSphere(new Vector3(0, 0, 0), 1, theta, phi);
			var unitSphereMesh = sphereBuilder.ToMesh().ToWndMeshGeometry3D();
			unitSphereMesh.Freeze();

			// List of matrices per color

			Color[] colors = new[]
			{
				Colors.Red,
				Colors.Green,
				Colors.Blue,
				Colors.Cyan,
				Colors.Magenta,
				Colors.Yellow,
				Colors.Orange,
				Colors.Purple,
				Colors.Lime,
				Colors.Teal,
				Colors.Pink,
				Colors.Brown,
				Colors.Olive,
				Colors.Navy,
				Colors.Gold,
				Colors.Coral,
				Colors.Turquoise,
				Colors.Violet,
				Colors.SkyBlue,
				Colors.Salmon,
				Colors.Khaki,
				Colors.Plum,
				Colors.MediumSpringGreen,
				Colors.DeepSkyBlue,
				Colors.DarkOrange,
				Colors.HotPink,
				Colors.LightSeaGreen,
				Colors.SlateBlue,
				Colors.Chartreuse,
				Colors.DarkViolet,
			};

			var materials = colors.Select(c => MaterialHelper.CreateMaterial(c)).ToArray();

			var staticColor = Colors.Gray.ChangeAlpha(100);
			var staticMaterial = MaterialHelper.CreateMaterial(staticColor);

			for (int shapeIndex = 0; shapeIndex < storage.ShapeCount; shapeIndex++)
			{
				if (disableGraphics)
					break;

				int coloringIndex = shapeIndex;

				if (colorByShape == false)
				{
					coloringIndex = storage.GetShapeBodyIndex(shapeIndex);
				}

				var color = colors[coloringIndex % materials.Length];
				var material = materials[coloringIndex % materials.Length];

				if (storage.GetBodyMassProperties(storage.GetShapeBodyIndex(shapeIndex)).LinearMass == 0)
				{
					color = staticColor;
					material = staticMaterial;
				}

				var sphereId = storage.Shapes[shapeIndex].SphereIndex;
				if (sphereId != -1)
				{
					var sphere = storage.Spheres[sphereId];

					var colorIndex = shapeIndex % materials.Length;
					var center = sphere.Center.ToVector3();
					var radius = sphere.Radius;

					unitSphereMesh.Freeze();
					material.Freeze();

					// Use Helix Instancing helper
					var instancedModel = new MeshGeometryVisual3D
					{
						MeshGeometry = unitSphereMesh,
						Material = material,
						BackMaterial = MaterialHelper.CreateMaterial(null, null)
					};

					sorting.Children.Add(instancedModel);
					worldModels.Add(instancedModel);
				}

				var polyherdonId = storage.Shapes[shapeIndex].PolyhedronIndex;
				if (polyherdonId != -1)
				{
					var poly = storage.PolyhedronStorage.GetSafePolyhedron(polyherdonId);

					// Add points to a temporary array for direct indexing
					var builder = new MeshBuilder();

					// 2. Triangulate sides using indices
					for (int sideIndex = 0; sideIndex < poly.SideCount; sideIndex++)
					{
						int start = poly.GetSidePointStartIndex(sideIndex);
						int end = poly.GetSidePointEndIndex(sideIndex);

						Vector3 normal = poly.GetSideNormal(sideIndex).ToVector3();

						int baseVertex = builder.Positions.Count;

						// Add this side's vertices once
						for (int i = start; i < end; i++)
						{
							int pointIndex = poly.GetSidePointPointIndex(i);
							builder.Positions.Add(poly.GetPoint(pointIndex).ToVector3());
							builder.Normals!.Add(normal);
						}

						// Fan triangulation using local indices
						for (int i = 1; i < (end - start - 1); i++)
						{
							builder.TriangleIndices.Add(baseVertex);
							builder.TriangleIndices.Add(baseVertex + i);
							builder.TriangleIndices.Add(baseVertex + i + 1);
						}
					}

					builder.TextureCoordinates = null;

					// Use Helix Instancing helper
					var instancedModel = new MeshGeometryVisual3D
					{
						MeshGeometry = builder.ToMesh().ToWndMeshGeometry3D(),
						Material = material,
						BackMaterial = MaterialHelper.CreateMaterial(null, null),
					};

					sorting.Children.Add(instancedModel);
					worldModels.Add(instancedModel);
				}
			}
		}

		//------------------------------- PHYSICS -------------------------------

		void CreateBodies()
		{
			var config = storage.Config;
			storage = new();
			storage.Config = config;
			worldBuilder.storage = storage;
			worldBuilder.Build();
			storage = worldBuilder.storage;
		}

		storage = worldBuilder.storage;

		int stepCount = 0;

		Backend.Gpu.WorldStorage gpuStorage = new(deviceManager.Accelerator);

		Backend.Gpu.Executor gpuExecutor = new(deviceManager.Accelerator);

		bool gpuStateIsDirty = true;

		timer.Tick += (s, e) =>
		{
			if (restart)
			{
				restart = false;
				gpuStateIsDirty = true;
				stepCount = 0;

				CreateBodies();
				CreateGraphics();

				if (demoCamera)
				{
					camera.Position = worldBuilder.cameraPosition.ToPoint3D();
					camera.LookDirection = worldBuilder.cameraDirection.ToVector3D();
					camera.UpDirection = new Vector3D(0, 1, 0);

					dir = camera.LookDirection;
					dir.Normalize();
					yaw = Math.Atan2(dir.X, dir.Z) * 180.0 / Math.PI;
					pitch = Math.Asin(dir.Y) * 180.0 / Math.PI;
				}
			}

			if (Keyboard.IsKeyDown(Key.H))
			{
				for (int ib = 0; ib<storage.BodyCount; ib++)
				{
					if (storage.BodyStorage.MassProperties[ib].LinearMass == 0)
						continue;

					CollectionsMarshal.AsSpan(storage.BodyStorage.Transforms)[ib].Position.Y += 1;
				}
				gpuStateIsDirty = true;
			}

			for (int speed0 = speed; speed0 > 0; speed0--)
			{
				if (paused)
					break;

				Stopwatch stopwatch = Stopwatch.StartNew();

				if (executorType == 0)
				{
					gpuStateIsDirty = true;

					World<Backend.Cpu.WorldStorage, DynamicGrid<Backend.Cpu.DynamicGridStorage>> world = new();
					world.Storage = storage;
					world.CollisionMap = new(storage.DynamicGridStorage);

					var t = world.Step(sequentialExecutor);

					times.Add(t);
				}
				if (executorType == 1)
				{
					gpuStateIsDirty = true;

					World<Backend.Cpu.WorldStorage, DynamicGrid<Backend.Cpu.DynamicGridStorage>> world = new();
					world.Storage = storage;
					world.CollisionMap = new(storage.DynamicGridStorage);

					var t = world.Step(parallelExecutor);

					times.Add(t);
				}
				if (executorType == 2)
				{
					if (gpuStateIsDirty)
					{
						gpuStateIsDirty = false;
						gpuStorage.CopyFromCPU(storage);
					}

					World<Backend.Gpu.WorldStorage, DynamicGrid<Backend.Gpu.DynamicGridStorage>> gpuWorld = new();
					gpuWorld.Storage = gpuStorage;
					gpuWorld.CollisionMap = new(gpuStorage.DynamicGridStorage);

					var t = gpuWorld.Step(gpuExecutor);

					times.Add(t);
				}

				stepCount++;

				//if(stepCount%50==0)
				{
					float md = 0;
					for (int ic = 0; ic<storage.ContactCount; ic++)
					{
						md += storage.ContactStorage.Contacts[ic].Depth;
					}
					float j = 0;
					for(int ib=0;ib<storage.BodyCount;ib++)
					{
						j += storage.BodyStorage.Velocities[ib].Linear.GetLength();
						j += storage.BodyStorage.Velocities[ib].Angular.GetLength();
					}

					float st = (float)stopwatch.ElapsedTicks / Stopwatch.Frequency;

					Console.WriteLine("f " + stepCount + " dt " + storage.Config.DeltaTime + " st " + st + " cc " + storage.ContactCount + " d " + md + " j " + j);
				}

				while ((float)stopwatch.ElapsedTicks / Stopwatch.Frequency < storage.Config.DeltaTime / speed) 
					;
			}

			if (executorType == 2 && copyBack)
			{
				gpuStorage.CopyToCPU(storage);
			}

			// times
			if (times.Count > 0 && sampleCount > 0)
			{
				int c = Math.Min(times.Count, sampleCount);
				WorldStepTimes sum = new();

				for (int i = times.Count - c; i < times.Count; i++)
				{
					sum.PreparationTime += times[i].PreparationTime;
					sum.ShapeUpdateTime += times[i].ShapeUpdateTime;
					sum.BodyUpdateTime += times[i].BodyUpdateTime;
					sum.CollisionDetectionTime += times[i].CollisionDetectionTime;
					sum.ContactPreparationTime += times[i].ContactPreparationTime;
					sum.GravityApplicationTime += times[i].GravityApplicationTime;
					sum.ContactCacheLoadingTime += times[i].ContactCacheLoadingTime;
					sum.ContactWarmStartingTime += times[i].ContactWarmStartingTime;
					sum.ContactResolutionTime += times[i].ContactResolutionTime;
					sum.ContactCacheSavingTime += times[i].ContactCacheSavingTime;
					sum.BodyFinalizationTime += times[i].BodyFinalizationTime;
				}

				double inv = 1.0 / c;

				void Set(string key, double value)
				{
					timeLabels[key].Text = $"{1000*value * inv:0.000} ms";
				}

				void SetNumber(string key, double value)
				{
					timeLabels[key].Text = $"{value}";
				}

				Set("Preparation", sum.PreparationTime);
				Set("Shape Update", sum.ShapeUpdateTime);
				Set("Body Update", sum.BodyUpdateTime);
				Set("Collision Detection", sum.CollisionDetectionTime);
				Set("Contact Preparation", sum.ContactPreparationTime);
				Set("Gravity Application", sum.GravityApplicationTime);
				Set("Contact Cache Loading", sum.ContactCacheLoadingTime);
				Set("Contact Warm Starting", sum.ContactWarmStartingTime);
				Set("Contact Resolution", sum.ContactResolutionTime);
				Set("Contact Cache Saving", sum.ContactCacheSavingTime);
				Set("Body Finalization", sum.BodyFinalizationTime);

				SetNumber("Body count", storage.BodyCount);
				SetNumber("Shape count", storage.ShapeCount);
				SetNumber("Contact count", storage.ContactCount);
			}

			{
				// speed adjustment

				var cameraPosition = viewport.Camera.Position;

				var distanceMax = Math.Abs(cameraPosition.X) + Math.Abs(cameraPosition.Z) + Math.Abs(cameraPosition.Y);

				var senseHorizontal = Math.Sqrt(distanceMax);
				var senseVertical = Math.Sqrt(distanceMax);

				viewport.CameraController.MoveSensitivity = senseHorizontal + 10;
				viewport.CameraController.LeftRightPanSensitivity = senseHorizontal + 10;
				viewport.CameraController.UpDownPanSensitivity = senseVertical + 10;
			}

			// ======================================= GRAPHICS UPDATE =======================================

			if (disableGraphics)
				return;

			for (int shapeIndex = 0; shapeIndex < worldModels.Count; shapeIndex++)
			{
				var sphereId = storage.Shapes[shapeIndex].SphereIndex;
				if (sphereId != -1)
				{
					var sphere = storage.Spheres[sphereId];
					var center = sphere.Center.ToVector3();
					var radius = sphere.Radius;
					var world = Matrix3D.Identity;
					world.Scale(new Vector3D(radius, radius, radius));
					world.Translate(new Vector3D(center.X, center.Y, center.Z));
					var transform = new MatrixTransform3D(world);
					transform.Freeze();
					worldModels[shapeIndex].Transform = transform;
				}

				var polyherdonId = storage.Shapes[shapeIndex].PolyhedronIndex;
				if (polyherdonId != -1)
				{
					var poly = storage.PolyhedronStorage.GetPolyhedron(polyherdonId);

					var tf = storage.GetBodyTransform(storage.GetShapeBodyIndex(shapeIndex));
					var world = Matrix3D.Identity;
					world.Rotate(new System.Windows.Media.Media3D.Quaternion(tf.Rotation.X, tf.Rotation.Y, tf.Rotation.Z, tf.Rotation.W));
					world.Translate(tf.Position.ToVector3D());
					var transform = new MatrixTransform3D(world);
					transform.Freeze();
					worldModels[shapeIndex].Transform = transform;
				}
			}
		};

		timer.Start();

		window.Content = viewport;

		application.Run(window);
	}

	public static void ShowError(Action<HelixViewport3D> setup)
	{
		Application application = new Application();

		Window window = new Window();
		window.Title = "ERROR";
		window.Width = 800;
		window.Height = 600;
		window.Background = Brushes.LightBlue;

		HelixViewport3D viewport = new HelixViewport3D();

		PerspectiveCamera camera = new PerspectiveCamera();
		camera.Position = new Point3D(0, 0, 100);
		camera.LookDirection = new Vector3D(0, 0, -1);
		camera.UpDirection = new Vector3D(0, 1, 0);
		camera.NearPlaneDistance = 0.1;
		camera.FarPlaneDistance = float.PositiveInfinity;
		camera.FieldOfView = 60;

		viewport.Camera = camera;

		viewport.CameraRotationMode = CameraRotationMode.Turnball;
		viewport.CameraMode = CameraMode.WalkAround;
		viewport.ShowViewCube = false;
		viewport.ShowTriangleCountInfo = true;
		viewport.ShowFrameRate = true;

		DirectionalLight light = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));

		viewport.Children.Add(new ModelVisual3D() { Content = light });

		AmbientLight light2 = new AmbientLight(Colors.Gray);

		viewport.Children.Add(new ModelVisual3D() { Content = light2 });

		// axes
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Red,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(1e6, 0, 0) }
		});
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Green,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(0, 1e6, 0) }
		});
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Blue,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(0, 0, 1e6) }
		});
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Cyan,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(-1e6, 0, 0) }
		});
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Magenta,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(0, -1e6, 0) }
		});
		viewport.Children.Add(new LinesVisual3D
		{
			Color = Colors.Yellow,
			Thickness = 1,
			Points = new Point3DCollection { new Point3D(0, 0, 0), new Point3D(0, 0, -1e6) }
		});

		setup(viewport);

		window.Content = viewport;

		application.Run(window);
	}
}

