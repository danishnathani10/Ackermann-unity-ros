By following this guide, you will be able to:
* Set up ROS 2 Humble.
* Create a dedicated ROS 2 workspace.
* Build the custom `ugv_assignment_msgs` package.
* Write the `Input.msg` and `Output.msg` interfaces.
* Compile and source your custom workspace files.
* Set up the network bridge (ROS-TCP Endpoint).
* Bind Unity together with your ROS 2 nodes.
* Publish manual throttle, brake, and steering inputs to the UGV.
* Intercept live vehicle feedback coming back from Unity.

---

## 1. System Setup

Our framework splits the computational load across two operating system layers:
* **Operating Systems:** Windows hosting an Ubuntu environment through WSL.
* **Core Software:** ROS 2 Humble, Unity 3D, and the Unity Robotics ROS-TCP Connector.

The main ROS 2 stacks execute directly inside Ubuntu via WSL, while the visual Unity simulator runs natively on Windows. The two layers pass data back and forth smoothly by linking through a network socket known as the ROS-TCP Endpoint.

---

## 2. Install ROS 2 Humble

First, deploy a standard installation of ROS 2 Humble inside your Ubuntu container. Once finished, ensure that your terminal environment can find the core ROS libraries by running:

```bash
source /opt/ros/humble/setup.bash
```

To verify the installation is functional, pull up the main help menu:

```bash
ros2 --help
```

### Automating the Environment Setup
To avoid running the source command manually every time you open a clean window, append the setup path straight into your shell profile:

```bash
# Add ROS 2 environment variable automation to your profile
echo "source /opt/ros/humble/setup.bash" >> ~/.bashrc

# Reload your terminal configuration
source ~/.bashrc
```

---

## 3. Create a ROS 2 Workspace

We need a dedicated development workspace directory to house and build our custom UGV messaging architectures. Set up the target folders by running:

```bash
mkdir -p ~/ugv_ws/src
cd ~/ugv_ws/src
```

Your resulting workspace tree looks like this:
```text
ugv_ws/
└── src/
```

This source file group will store all your personalized message code structures.

---

## 4. Create the Custom Message Package

Navigate into your newly created source folder and generate an empty package template configured for the CMake build management tool:

```bash
cd ~/ugv_ws/src
ros2 pkg create --build-type ament_cmake ugv_assignment_msgs
```

Your underlying project directory will expand to reveal these boilerplate files:
```text
ugv_ws/
└── src/
    └── ugv_assignment_msgs/
        ├── CMakeLists.txt
        ├── package.xml
        └── src/
```

Now, create a dedicated `msg` folder to keep your tracking definitions isolated and clean:

```bash
cd ~/ugv_ws/src/ugv_assignment_msgs
mkdir msg
```

Your development package layout is now ready for message scripting:
```text
ugv_assignment_msgs/
├── CMakeLists.txt
├── package.xml
└── msg/
```

---

## 5. Create `Input.msg`

The `Input.msg` configuration defines the steering, torque, and braking instructions routed from your external ROS nodes over to the Unity engine canvas. 

### Writing the Interface
Create and open the file inside your terminal using the Nano text editor:

```bash
cd ~/ugv_ws/src/ugv_assignment_msgs/msg
nano Input.msg
```

Paste these three precise control properties into the file:

```text
float64 throttle
float64 brake
float64 steering_angle
```

### Parameter Explanations
* `throttle` — Maps directly to vehicle engine acceleration variables.
* `brake` — Triggers mechanical friction and decelerates the wheels.
* `steering_angle` — Defines the target heading direction.

```text
ROS 2 Command Line → /input Topic → ugv_assignment_msgs/Input → Unity Subscriber → Vehicle Controller
```

---

## 6. Create `Output.msg`

The `Output.msg` file tracks operational feedback and kinematics data harvested from the Unity physics loop and returns it directly to your ROS environment.

### Writing the Interface
Generate the companion output telemetry model using your editor:

```bash
nano Output.msg
```

Add these status properties to your configuration file:

```text
string state
float64 linear_velocity
float64 angular_velocity
float64 front_left_steering_angle
float64 front_right_steering_angle
```

### Parameter Explanations
* `state` — Tracks the vehicle's current high-level state machine or action loop.
* `linear_velocity` — Tracks true forward speed through space.
* `angular_velocity` — Tracks live turning speeds and yaw rotation dynamics.
* `front_left_steering_angle` — Tracks the exact physical orientation of the front-left wheel tire.
* `front_right_steering_angle` — Tracks the exact physical orientation of the front-right wheel tire.

```text
Unity Vehicle Physics → State Telemetry Capture → ugv_assignment_msgs/Output → /feedback Channel → ROS 2 Environment
```

---

## 7. Configure `package.xml`

We must tell the package compiler that this project requires message building extensions. Open your package dependencies configuration file:

```bash
cd ~/ugv_ws/src/ugv_assignment_msgs
nano package.xml
```

Ensure these lines are present inside the dependencies section to allow proper code interface generation during the workspace build loop:

```xml
<buildtool_depend>ament_cmake</buildtool_depend>
<build_depend>rosidl_default_generators</build_depend>
<exec_depend>rosidl_default_runtime</exec_depend>
<member_of_group>rosidl_interface_packages</member_of_group>
```

---

## 8. Configure `CMakeLists.txt`

Next, modify the main compiler instructions to explicitly register our messaging layout files during compilation. Open the build script:

```bash
nano CMakeLists.txt
```

Verify that your script calls out the message generators and links the input/output dependencies by formatting the file to include these core blocks:

```cmake
find_package(ament_cmake REQUIRED)
find_package(rosidl_default_generators REQUIRED)

rosidl_generate_interfaces(${PROJECT_NAME}
  "msg/Input.msg"
  "msg/Output.msg"
)

ament_export_dependencies(rosidl_default_runtime)
ament_package()
```

---

## 9. Build the Workspace

With your settings configured, head back to your root directory to build the custom code:

```bash
cd ~/ugv_ws
colcon build
```

### Activating the Workspace
Once the compilation finishes cleanly, refresh your active terminal window so it logs the new message models:

```bash
source install/setup.bash
```

To make sure your custom messaging workspace mounts automatically whenever a new terminal tab opens, write the tracking link into your shell script profile:

```bash
# Add your custom message package paths to automation
echo "source ~/ugv_ws/install/setup.bash" >> ~/.bashrc

# Refresh variables
source ~/.bashrc
```

---

## 10. Verify the Custom Messages

Let's double-check that your ROS 2 registry can find and read our definitions. Run a query list scan:

```bash
ros2 pkg list | grep ugv_assignment_msgs
```

Test your input format blueprint:
```bash
ros2 interface show ugv_assignment_msgs/msg/Input
```
* **Expected Terminal Reply:**
  ```text
  float64 throttle
  float64 brake
  float64 steering_angle
  ```

Test your output telemetry blueprint:
```bash
ros2 interface show ugv_assignment_msgs/msg/Output
```
* **Expected Terminal Reply:**
  ```text
  string state
  float64 linear_velocity
  float64 angular_velocity
  float64 front_left_steering_angle
  float64 front_right_steering_angle
  ```

[PLACE CUSTOM MESSAGE TERMINAL SCREENSHOT HERE]

---

## 11. Install the Unity ROS-TCP Connector

We manage network data passage inside the editor by importing Unity's official standalone communications toolkit.

### Installation Steps
1. Launch Unity and open your target project canvas.
2. Click **Window** along the top control tray, then select **Package Manager**.
3. Locate and click the **`+`** (plus sign icon) positioned in the upper-left area.
4. Select **Add package from git URL...** from the contextual drop-down choices.
5. Paste this exact destination link into the input line:
   `https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector`

The engine will download and import the tracking dependencies automatically into your project hierarchy.

[PLACE UNITY PACKAGE MANAGER SCREENSHOT HERE]

---

## 12. Install the Custom ROS Message Package in Unity

The Unity scripts require direct access to matching C# wrappers of your compiled message blueprints to communicate effectively. Use the imported tool utilities within the editor to target your local `ugv_assignment_msgs` path and generate your corresponding C# class packages.

## 13. Start the ROS-TCP Endpoint

Before hitting Play in Unity, you need to spin up the ROS-TCP Endpoint in your Linux terminal. This serves as the communication bridge that passes data between Unity and ROS 2.

Open an Ubuntu/WSL terminal and run:

```bash
# Set up your ROS 2 and workspace environments
source /opt/ros/humble/setup.bash
source ~/ugv_ws/install/setup.bash

# Launch the connection bridge server
ros2 run ros_tcp_endpoint default_server_endpoint
```

Leave this terminal window open and running in the background while you test.

[PLACE ROS-TCP ENDPOINT TERMINAL SCREENSHOT HERE]

---

## 14. Start the Unity Simulation

Open Unity and choose the specific testing environment you want to evaluate from your project files:

```text
Scenes/
├── ObstacleScene
├── DesertScene
└── SandParticleScene
```

To run your baseline communication checks, load up the scene that contains your UGV prefab and network connection configurations. Before clicking anything, double-check these four elements:
* Your Linux ROS-TCP Endpoint terminal is active and waiting.
* The network configuration IP addresses inside the Unity inspector match your WSL network setup.
* Your UGV model has its input subscriber node attached.
* Your UGV model has its output publisher node attached.

If everything looks correct, click **Play**. Unity will automatically connect to your terminal and begin exchanging live network messages.

---

## 15. Check Active ROS Topics

Open up a fresh, clean Ubuntu/WSL terminal tab to verify that your pathways are actively transmitting data. Mount your environmental paths and list the active nodes:

```bash
source /opt/ros/humble/setup.bash
source ~/ugv_ws/install/setup.bash
ros2 topic list
```

Your system graph will reveal all of the simulation's live active communication pathways:
* `/input`
* `/feedback`
* `/gps`
* `/imu`
* `/camera`
* `/lidar`
* `/tf`
* `/joint_states`

If you want to view a topic's underlying message configuration, run an info command query:

```bash
ros2 topic info /input
ros2 topic info /feedback
```

[PLACE ROS TOPIC LIST SCREENSHOT HERE]

---

## 16. Sending Input Commands to the Vehicle

The vehicle listens for driving instructions directly on the `/input` topic channel.

### Message Blueprint
* **Message Type:** `ugv_assignment_msgs/msg/Input`
* **Available Parameters:** `throttle`, `brake`, `steering_angle`

### Command Reference Guide

#### A. Basic Drive Command
To make the vehicle accelerate straight forward, run:

```bash
ros2 topic pub --once /input ugv_assignment_msgs/msg/Input "{throttle: 50.0, brake: 0.0, steering_angle: 0.0}"
```
This triggers **50% throttle**, zero brakes, and perfectly straight steering.

#### B. Steering Left/Right
To test how the vehicle handles a basic turn, add a steering offset value:

```bash
ros2 topic pub --once /input ugv_assignment_msgs/msg/Input "{throttle: 30.0, brake: 0.0, steering_angle: 0.3}"
```
This applies **30% throttle** and bends the front tires to a **0.3 steering angle**.

To steer in the opposite direction, swap to a negative steering orientation value:

```bash
ros2 topic pub --once /input ugv_assignment_msgs/msg/Input "{throttle: 30.0, brake: 0.0, steering_angle: -0.3}"
```

#### C. Testing Brakes
To test how the vehicle handles a sudden deceleration stop command, drop your acceleration value and spike the braking load parameter:

```bash
ros2 topic pub --once /input ugv_assignment_msgs/msg/Input "{throttle: 0.0, brake: 100.0, steering_angle: 0.0}"
```

### Understanding the Data Flow

```text
Terminal Command Line → /input Topic → ugv_assignment_msgs/Input → Unity ROS Subscriber → Vehicle Controller
                                                                                              │
                                                                          ┌───────────────────┼───────────────────┐
                                                                          ▼                   ▼                   ▼
                                                                      Throttle              Brake             Steering
                                                                                                                  │
                                                                                                                  ▼
                                                                                                          Ackermann Steering
                                                                                                                  │
                                                                                                                  ▼
                                                                                                           Vehicle Physics
```

[PLACE ROS INPUT COMMAND TERMINAL SCREENSHOT HERE]

[PLACE UNITY VEHICLE MOVING SCREENSHOT OR GIF HERE]

---

## 17. Checking Vehicle Feedback

Unity continuously broadcasts the vehicle's real physical behavior back across the network wire using the `/feedback` topic. To see this output data running live, execute an echo query:

```bash
ros2 topic echo /feedback
```

### Telemetry Output Parameters
* `state` — Current high-level operational loop tracker.
* `linear_velocity` — True forward speed moving across the coordinate plane.
* `angular_velocity` — Rotational yaw values tracking turn speeds.
* `front_left_steering_angle` — Exact dynamic wheel calculation for the front-left tire.
* `front_right_steering_angle` — Exact dynamic wheel calculation for the front-right tire.

This step is critical because it tells ROS 2 what the vehicle is **actually doing** in the environment rather than what it was **told to do**. For instance, sending a single generic steering command results in two entirely separate wheel angles in your terminal feedback logs, proving that the underlying Ackermann linkage physics are processing properly.

```text
ROS 2 Input Command (throttle: 30, steering_angle: 0.3)
                        ↓
               Unity Vehicle Moves
                        ↓
ROS 2 Feedback (linear_velocity, angular_velocity, individual wheel angles)
```

This confirms a functional two-way data feedback channel rather than just blindly dumping instructions down a one-way path.

[PLACE LIVE ROS FEEDBACK SCREENSHOT HERE]

---

## 18. Recording the ROS Data

You can save your entire multi-channel simulation session into an isolated, re-playable folder using a standard ROS bag configuration.

### Data Logging Command
To record your control paths and active sensor streams simultaneously during a run, execute:

```bash
ros2 bag record -o ros_test /input /feedback /gps /imu /camera /lidar /tf /joint_states
```

This saves your vehicle inputs, live engine telemetry, GPS tracks, IMU balances, camera feeds, LiDAR points, structural transform trees, and wheel joint updates safely in your workspace directory.

### Replaying your Data Packages
To audit your recorded session logs later without turning on Unity or resetting your environment parameters, run:

```bash
ros2 bag play ros_test
```

[PLACE ROS BAG RECORDING SCREENSHOT HERE]

---

## Troubleshooting

### 1. Unity Does Not Connect to ROS 2
* Ensure your network handshake engine is running inside your Linux shell container:
  ```bash
  ros2 run ros_tcp_endpoint default_server_endpoint
  ```
* Open the connection settings layout option window inside Unity. Ensure that your designated IP pointer points to your exact active WSL computer address and uses the correct open network port (default is usually `10000`).

### 2. Custom Message Is Not Found
* Confirm that your message folder workspace has been compiled cleanly without warnings:
  ```bash
  cd ~/ugv_ws && colcon build
  ```
* Refresh your shell paths to inherit the new changes:
  ```bash
  source install/setup.bash
  ```
* Check that your underlying data layout definition builds properly:
  ```bash
  ros2 interface show ugv_assignment_msgs/msg/Input
  ```

### 3. No Topics Are Visible
* Verify that Unity is actively running in **Play** mode.
* Verify your ROS-TCP Endpoint server tab is currently active.
* Ensure your open console shell handles both your root ROS 2 configuration and your local assignment workspace directory parameters:
  ```bash
  source /opt/ros/humble/setup.bash
  source ~/ugv_ws/install/setup.bash
  ros2 topic list
  ```

### 4. Vehicle Does Not Move
* Ensure that your terminals are publishing data packets onto the exact target channel names:
  ```bash
  ros2 topic echo /input
  ```
* Check your command syntax ranges to ensure your throttle, braking, and steering configurations are passing valid numeric ranges.
* Ensure the Unity scene is running and that your UGV vehicle root script nodes have their active input subscribers enabled in the inspector panel.
