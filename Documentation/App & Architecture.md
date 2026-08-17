# Ackermann ROS Unity Simulation

This repository contains a Unmanned Ground Vehicle (UGV) simulation in Unity 3D integrated with ROS 2. The project demonstrates how a simulated ground vehicle moves through physics based desert environments, receives remote control commands, and publishes synthetic sensor and state data back to ROS.

---

## Technical Stack

* **Engine:** Unity 3D
* **Language:** C#
* **Robotics Middleware:** ROS 2 Humble
* **ROS Bridge:** Unity Robotics ROS TCP Connector
* **Visualization:** RViz2
* **Recording:** ros2 bag, OBS Studio, LiCEcap
* **Editing:** DaVinci Resolve

---

## Core Architecture

The simulation isolates control logic, physics calculations, and sensor publishing to maintain clean communication boundaries. 

### Execution Flow
1. **Input Selection:** `SimulationManager` determines whether control commands come from the local Keyboard or ROS.
2. **Vehicle Input Read:** `CarInput` reads the commands from the chosen source.
3. **Vehicle Physics:** `CarController` translates commands into steering, motor torque, and braking.
4. **State Assessment:** Vehicle movement and active inputs determine the current operational state.
5. **Sensor Publishing:** Active sensors fetch the physical simulation state and publish it to designated ROS topics.
6. **ROS Validation:** Topics are verified and inspected using standard terminal tools, RViz2, or ros2 bag.

```
Unity Simulation
└─ SimulationManager
   ├─ CarInput → CarController
   ├─ RosInput ← /input
   ├─ RosOutput → /feedback
   ├─ Camera → /camera
   ├─ LiDAR → /lidar
   ├─ GPS → /gps
   ├─ IMU → /imu
   └─ JointState → /joint_states
        ↓
ROS 2 / RViz2 / ros2 bag
```

### Component Breakdown

| Component Name | File / Script | Core Function |
| :--- | :--- | :--- |
| Car | Vehicle Prefab | Root physical entity containing Rigidbody, WheelColliders, and subsystems. |
| Simulation Manager | SimulationManager.cs | Central coordinator for ROS connection, input selection, and sensor initialization. |
| Car Input | CarInput.cs | Selects the active input source (Keyboard or ROS) and forwards commands to the controller. |
| Car Controller | CarController.cs | Computes vehicle physics, handling steering, power delivery, and suspension. |
| ROS Input / Output | RosInput.cs / RosOutput.cs | RosInput receives remote `/input` commands; RosOutput publishes local `/feedback`. |
| Sensor Layers | Camera, LiDAR, GPS, IMU, JointState | Individual modules that gather environmental and internal state data to publish. |

---

## Vehicle Model and Control

Vehicle motion is managed entirely inside `CarController.cs`, which processes the active command set and distributes values to the wheels.

### Steering System
* **Ackermann Steering:** The system automatically calculates necessary track width and wheelbase using the configured WheelColliders.
* **Inner / Outer Wheel Angles:** To eliminate skid during turns, the inside front wheel receives a larger steering angle than the outside wheel.
* **Speed Sensitivity:** Steering tension scales down at higher velocities to preserve vehicle stability.

### Motor, Brakes, and Stability
* **Motor Torque:** throttle percentage maps directly to wheel motor torque.
* **Braking:** Brake percentage maps directly to brake torque across the configurations.
* **Rest Stabilization:** When the vehicle is nearly stationary and receives no active user input, angular velocity is actively damped to prevent drifting or sliding on uneven terrain.

---

## ROS Communication

Unity and ROS 2 exchange data over a network bridge using the Unity Robotics ROS TCP Connector. Communication relies on a mix of standard robotics messages and custom project specific definitions.

### Topic Registry

| Direction | Topic | Message Type | Data Content |
| :--- | :--- | :--- | :--- |
| ROS → Unity | `/input` | `ugv_assignment_msgs/Input` | Throttle, brake, and steering control values. |
| Unity → ROS | `/feedback` | `ugv_assignment_msgs/Output` | Vehicle state and active motion feedback. |
| Unity → ROS | `/gps` | `sensor_msgs/NavSatFix` | Approximate geographic coordinates. |
| Unity → ROS | `/imu` | `sensor_msgs/Imu` | Orientation, angular velocity, and linear acceleration. |
| Unity → ROS | `/camera` | `sensor_msgs/Image` | Synthetic RGB camera frames. |
| Unity → ROS | `/lidar` | `sensor_msgs/LaserScan` | Obstacle and sand particle ranges. |
| Unity → ROS | `/tf` | `sensor_msgs/Imu` | Transform of /gps, /imu, /camera, /lidar. |
| Unity → ROS | `/joint_states` | `sensor_msgs/JointState` | Physical wheel positions and rotational velocities. |

### Custom Message Definitions

The custom message files were built in Unity using the ROS Message Browser, generating native `InputMsg.cs` and `OutputMsg.cs` parsing scripts.

#### Input.msg
```msg
float64 throttle         # Range: 0.0 to 100.0 (percent)
float64 brake            # Range: 0.0 to 100.0 (percent)
float64 steering_angle   # Range: -0.5 to 0.5 (radians, negative = left)
```

#### Output.msg
```msg
string state                         # One of: "Parking", "Manual", ...
float64 linear_velocity              # m/s
float64 angular_velocity             # # rad/s  
float64 front_left_steering_angle    # radians
float64 front_right_steering_angle   # radians
```

### Control and Feedback Pipeline

#### ROS Input Handling
1. An external ROS node publishes control values over the `/input` topic.
2. `RosInput.cs` intercepts the `InputMsg` data and maps the values to a local `VehicleCommand` structure.
3. `CarInput.SetRosInput()` passes this command.
4. If ROS input mode is active, `CarInput` forwards the data to `CarController.cs`.
5. The car executes the movement via the same physics engine path used by the keyboard.

#### Vehicle State Machine
The `RosOutput.cs` publishes vehicle feedback at a configurable frequency.

| Condition | Reported State |
| :--- | :--- |
| Vehicle velocity is ≤ 0.1 m/s | `Parking` |
| Vehicle is in motion powered by keyboard input | `Manual` |
| Vehicle is in motion powered by remote ROS commands | `Auto` |

---

### Control & Optimization 
* Each sensor exposes Begin() and Stop() methods.
* Methods are called from SimulationManager.cs when requested.
* The scripts use InvokeRepeating(0, 1/Rate) rather than Update().
* Fewer calls result in better optimization.
* Only sensors required for the active scenario need to be enabled.
* Publish rates are configurable directly from the Unity Editor.

---

## Sensor Layer Architecture

Each sensor registers its own ROS publisher and can be independently started or stopped by SimulationManager. The architecture relies on configuration parameters and structured flows for data publishing. 

---

### Subsystem Specifications

### 1. Simulation Manager (`SimulationManager.cs`)
The central controller of the Unity simulation. It manages the active input source, ROS connection, feedback publishing, and enables or disables each sensor publisher when its setting changes.

##### A. Configuration Parameters
* **inputSource** (CarInputSource enum): Select Keyboard or ROS as the active vehicle input source.
* **connectToRos** (bool): Connects Unity to the configured ROS connection.
* **publishFeedbackOut** (bool): Enables publishing of vehicle feedback through RosOutput.
* **connectToGPSSensor** (bool): Enables or disables the GPS sensor publisher.
* **connectToIMUSensor** (bool): Enables or disables the IMU sensor publisher.
* **connectToCamera** (bool): Enables or disables the camera image publisher.
* **connectToLiDAR** (bool): Enables or disables the LiDAR scan publisher.
* **connectToTFs** (bool): Enables or disables the TF publisher.
* **connectToJointStates** (bool): Enables or disables the joint state publisher.

##### B. Publishing Flow
1. Initialize CarInput using the selected inputSource.
2. Store the active input source as currentInputSource.
3. Connect to ROS when connectToRos is enabled.
4. Check whether the selected input source has changed.
5. When ROS is selected, start RosInput; otherwise unsubscribe from it.
6. Check feedback, GPS, IMU, Camera, LiDAR, TF, and Joint State toggle values.
7. Call Begin() for sensors only when a component changes from OFF to ON.
8. Call Stop() for sensors only when a component changes from ON to OFF.

### 2. Car Input (`CarInput.cs`)
Acts as the input bridge for the vehicle. It reads control commands either from the keyboard or ROS and passes the final VehicleCommand to the CarController.

##### A. Configuration Parameters
* **inputSource** (CarInputSource enum): Selects the current vehicle input source: Keyboard or ROS.
* **carController** (CarController): Reference to the vehicle controller that receives the drive command.

##### B. Supported Input Sources
* **Keyboard:** Vehicle is controlled using input actions named VehicleControls (W - throttle, A/D - Steering, Space - Brake).
* **ROS:** Vehicle is controlled using commands received through ROS by reading through /input, parsing with InputMsg, and sending commands to CarController.cs.

##### C. Publishing Flow
1. Create the VehicleControls input action system when the script starts.
2. Read the current inputSource every frame.
3. If the source is Keyboard, read Drive, Steer, and Brake inputs and convert these values into a VehicleCommand.
4. Send the command to CarController.
5. If the source is ROS, apply the latest ROS command instead.
6. SetRosInput() receives and stores a new ROS command from the ROS subscriber and parses to VehicleCommand.
7. The received command is immediately passed to CarController.

### 3. ROS Input (`RosInput.cs`)
The ROS input component subscribes to the custom /input topic, receives throttle, brake, and steering commands, and converts them to VehicleCommand that is passed to CarInput for vehicle control.

##### A. Configuration Parameters
* **inputTopic** (string): ROS topic used to receive Input.msg commands; default /input.
* **carInput** (CarInput): Reference used to forward the received vehicle command.

##### B. Publishing Flow
1. From SimManager -> Begin() Get or receive the active ROSConnection.
2. Subscribe to the configured /input topic using InputMsg.
3. Wait for a message containing throttle, brake, and steering_angle values.
4. Log the received command values for runtime checking.
5. Convert the ROS message values into a VehicleCommand.
6. Pass the VehicleCommand to CarInput through SetRosInput().
7. CarInput applies the command when the active input source is ROS.
8. Unsubscribe from /input when Stop() is called.

### 4. ROS Output (`RosOutput.cs`)
The ROS output component reads the current vehicle state from CarController and publishes the result as the custom /feedback topic using OutputMsg.

##### A. Configuration Parameters
* **outputTopic** (string): ROS topic used for vehicle feedback.
* **showDebugMsg** (bool): Prints the published vehicle feedback in the Unity Console.
* **publishingRepeatInterval** (float): Time interval in seconds between feedback messages; default 1 sec.
* **carController** (CarController): Reference used to read vehicle state, velocity, and steering values.

##### B. Publishing Flow
1. Register OutputMsg as a publisher on /feedback.
2. Start InvokeRepeating() using publishingRepeatInterval.
3. Read the current CarMode from CarController.
4. Read linear velocity and angular velocity.
5. Read the front left and front right steering angles.
6. Populate an OutputMsg with the current vehicle feedback.
7. Publish the completed message on /feedback.

### 5. GPS Sensor (`GpsSensor.cs`)
The GPS sensor converts the Unity vehicle position into latitude, longitude, and altitude values using a configurable GPS origin. It maps local position to geographic reference.

##### A. Configuration Parameters
* **gpsTopic** (string): ROS topic used for GPS publication.
* **originLatitude** (double): Reference latitude used as the GPS origin.
* **originLongitude** (double): Reference longitude used as the GPS origin.
* **originAltitude** (double): Reference altitude used as the GPS origin.
* **positionNoise** (float): Small random variation added to latitude and longitude.
* **publishRate** (float): GPS coordinates published per 1/publishRate seconds; default 10 Hz.

##### B. Publishing Flow
1. Register a GPS publisher on /gps.
2. Read the current Unity position of the vehicle.
3. Convert the vehicle's Z position into a latitude offset.
4. Convert the vehicle's X position into a longitude offset.
5. Add the vehicle's Y position to the reference altitude.
6. Apply the configured GPS position noise.
7. Populate the message with latitude, longitude, and altitude.
8. Publish the GPS message to /gps.
9. Repeat the process at the configured publish rate.

### 6. IMU Sensor (`ImuSensor.cs`)
The IMU sensor reads the vehicle Rigidbody and publishes its orientation, angular velocity, and linear acceleration as a sensor_msgs/Imu message on the /imu topic.

##### A. Configuration Parameters
* **imuTopic** (string): ROS topic used for IMU publication.
* **vehicleRigidbody** (RigidBody): Rigidbody used to read vehicle movement and rotation.
* **publishRate** (float): IMU data published per 1/publishRate seconds; default 10 Hz (Note: Editor UI default is 5 Hz).

##### B. Publishing Flow
1. Register an IMU publisher on /imu.
2. Store the current vehicle velocity as the previous velocity.
3. Read the vehicle's current Rigidbody velocity.
4. Calculate linear acceleration from the change in velocity.
5. Read the current vehicle rotation.
6. Read the current angular velocity.
7. Convert orientation into a ROS quaternion.
8. Populate the ImuMsg with orientation, angular velocity, and linear acceleration.
9. Publish the IMU message to /imu.
10. Repeat the process at the configured publish rate.

### 7. Camera Sensor (`CameraSensor.cs`)
The camera sensor renders the attached Unity camera data into a RenderTexture, reads the pixels into a Texture2D, and publishes the result as sensor_msgs/Image on /camera.

##### A. Configuration Parameters
* **cameraTopic** (string): ROS topic used for image publication.
* **imageWidth** (int): Published image width; default 640.
* **imageHeight** (int): Published image height; default 480.
* **publishRate** (float): Images published per 1/publishRate seconds; default 10 Hz.

##### B. Publishing Flow
1. Register Image publisher on /camera.
2. Create RenderTexture and assign it to the attached Camera.
3. Render the camera into the target texture.
4. Read pixels into Texture2D using RGB24.
5. Flip pixel rows to correct RViz orientation.
6. Populate ImageMsg and publish raw RGB data.
7. Repeat the process at the configured publish rate.

### 8. LiDAR Sensor (`LidarSensor.cs`)
The LiDAR sensor performs a 360° raycast scan around the vehicle, detects obstacles and particle volume, and publishes the resulting distance data as a ROS message on /lidar. It also supports optional debug visualization inside Unity.

##### A. Scan Configuration
* **visualizeRaysInEditor** (bool): Shows LiDAR rays in Unity Editor.
* **visualizeRaysInScene** (bool): Enables the in scene LiDAR ray visualizer.
* **lidarTopic** (string): ROS topic used to publish the LiDAR scan; default /lidar.
* **frameId** (string): Frame identifier attached to the published scan; default lidar.
* **minRange** (float): Minimum valid LiDAR detection distance; default 0.1 m.
* **maxRange** (float): Maximum LiDAR detection distance; default 10 m.
* **numberOfRays** (int): Total number of rays used to create the 360° scan; default 180.
* **publishRate** (float): Number of LiDAR scans published per second; default 10 Hz.
* **obstacleLayer** (LayerMask): Layer containing solid objects that should be detected as obstacles.
* **particleLayer** (LayerMask): Layer used to detect the synthetic sand/particle volume.
* **particleDetectionProbability** (float): Probability that a ray detects the particle volume; default 0.2.
* **particleRangeNoise** (float): Random distance noise applied to a particle detection; default 0.2 m.
* **particleCloudDepth** (float): Maximum depth range used when generating detection inside particle clouds; default 4m.

##### B. Publishing Flow
1. Register a LaserScanMsg publisher on /lidar.
2. Create arrays to store the range and detection state for every ray.
3. Generate a scan from −π to +π, providing a full 360° field of view.
4. Calculate the angular increment based on the configured number of rays.
5. Convert each ROS scan angle into a Unity local direction.
6. Cast a ray against the configured obstacle layer. Objects need to be in these layers in order to get detected in LiDAR.
7. Cast the same ray against the configured particle layer. Particle volumes need to be in these layers in order to get detected in LiDAR.
8. For particle detection, use the configured probability to determine whether a synthetic return is generated.
9. Generate a distance within the configured particle cloud depth.
10. Apply configurable random range noise to simulate imperfect sensor readings.
11. If both a particle and a solid obstacle are detected, keep the closest valid detection.
12. Store the final range and detection state for that ray.
13. Optionally draw debug rays in Unity: red for detection and green for no detection.
14. Update the optional LidarVisualizer for the top view debug display.
15. Populate LaserScanMsg with scan angles, range limits, frame ID, and range data. Publish the completed scan to ROS on /lidar.
16. Repeat the process at the configured publish rate.

##### C. LidarVisualizer (`LidarVisualizer.cs`)
The LidarVisualizer script provides a visual debug representation of the LiDAR scan inside Unity. It creates LineRenderer rays and updates their length and colour (green - no object, red - obstacle detected) according to the LiDAR detection results.

### 9. Transform Publisher (`TfPublisher.cs`)
The TF publisher sends the vehicle and sensor coordinate relationships as a TFMessageMsg on /tf. It publishes the vehicle transform from world to base_link, then publishes the GPS, IMU, camera and LiDAR frames relative to base_link.

##### A. Configuration Parameters
* **tfTopic** (string): ROS topic used for TF (Transform Frame) publication.
* **baseLink** (Transform): Vehicle reference used as the base_link frame.
* **gps** (Transform): Optional GPS sensor frame published relative to base_link.
* **imu** (Transform): Optional IMU sensor frame published relative to base_link.
* **cameraSensor** (Transform): Optional camera frame published relative to base_link.
* **lidar** (Transform): Optional LiDAR frame published relative to base_link.
* **publishRate** (float): Transforms published per 1/publishRate seconds; default 10 Hz.

##### B. Publishing Flow
1. Register the TFMessage publisher on /tf.
2. Check that the baseLink reference is available.
3. Create the vehicle transform from world to base_link.
4. Convert Unity position and rotation values to the coordinate convention used by the ROS message.
5. Check whether GPS, IMU, camera and LiDAR transforms are assigned.
6. Calculate each assigned sensor pose relative to base_link.
7. Create a TransformStamped message for every available frame using the current runtime timestamp.
8. Combine all transforms into one TFMessageMsg.
9. Publish the complete transform set on /tf.

### 10. Joint State Publisher (`JointStatePublisher.cs`)
The joint state publisher reads wheel rotation from the four WheelColliders, converts RPM to radians per second, integrates the wheel position, and publishes the result as sensor_msgs/JointState on /joint_states.

##### A. Configuration Parameters
* **jointStateTopic** (string): ROS topic used for Joint State publication.
* **frontLeftWheel** (WheelCollider): Reference to the front left wheel collider.
* **frontRightWheel** (WheelCollider): Reference to the front right wheel collider.
* **rearLeftWheel** (WheelCollider): Reference to the rear left wheel collider.
* **rearRightWheel** (WheelCollider): Reference to the rear right wheel collider.
* **publishRate** (float): Images published per 1/publishRate seconds; default 10 Hz.

##### B. Publishing Flow
1. Register the JointState publisher on /joint_states.
2. Verify that all four WheelCollider references are available.
3. Read the current RPM from each wheel collider.
4. Convert RPM to radians per second.
5. Integrate angular velocity over the publish interval to estimate wheel position.
6. Create the JointStateMsg with wheel names, positions, velocities and zero effort values.
7. Add the base_link frame ID and current runtime timestamp.
8. Publish the completed joint state message and keep repeating the process at the configured publish rate.
