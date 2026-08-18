# Sand Synthetic Data Scene

## Overview

The main goal is to create a controlled environment where sand particles are clearly visible to the Camera and actively detected by the LiDAR scanner.

This scene focuses purely on synthetic data generation rather than vehicle driving or handling. We placed the particle system directly in front of the vehicle's sensors so we could study how the exact same environment gets represented differently through a visual camera image versus a LiDAR point cloud.

The complete sensor output was logged into a ROS bag so you can easily replay and inspect the generated data later.

**Evaluation Video**

[PLACE SAND SYNTHETIC DATA YOUTUBE VIDEO EMBED / THUMBNAIL HERE]

---

# 1. Objective

This scenario proves that these features work:
* **Synthetic Camera Data:** Sand particles show up clearly in the simulated camera output.
* **Synthetic LiDAR Data:** LiDAR rays interact with the particle objects to generate accurate point cloud returns.
* **Controlled Environment:** The particle system can be fully tested without needing the vehicle to move.
* **ROS 2 Integration:** Visual camera frames and LiDAR data stream continuously through ROS 2.
* **Sensor Comparison:** You can analyze how two completely different sensor types view the exact same particle field.
* **Data Logging:** All sensor outputs are captured into a ROS bag for later playback and analysis.

Ultimately, this test shows how Unity can be used to generate highly controlled synthetic data that can later be fed directly into ROS 2 tools for processing or machine learning training.

[PLACE SAND SCENE WIDE SCREENSHOT HERE]

[PLACE SAND PARTICLE SIMULATION GIF HERE]

---

# 2. ROS Initialization and ROS Bag Creation

Before launching the scene in Unity, we need to spin up our ROS 2 environment and connect the network bridge. Open your terminal and run:

```bash
# Set up your ROS 2 environment
source /opt/ros/humble/setup.bash

# Start the connection bridge between Unity and ROS 2
ros2 run ros_tcp_endpoint default_server_endpoint
```

### Checking Active Topics
Once Unity links up with the bridge, the active sensor publishers will automatically start streaming data. To check if the topics are running, open a new terminal window and list them:

```bash
ros2 topic list
```

If you want to look at the raw Camera frames and LiDAR data points streaming across individual topics in real-time, use the `echo` command:

```bash
# Watch the live camera metadata stream
ros2 topic echo /camera

# Monitor incoming LiDAR scanning data
ros2 topic echo /lidar
```

You can check that the data is publishing at the correct rates by measuring their topic frequencies with:

```bash
ros2 topic hz /camera
ros2 topic hz /lidar
```

### Recording the Session
We log the entire session into a folder named `sand1` so we can inspect the synthetic data later. Since the vehicle doesn't need to move for this evaluation, the recording focuses purely on the data coming off the sensors:

```bash
ros2 bag record -o sand1 /camera /lidar
```

[PLACE ROS2 TERMINAL / ENDPOINT SCREENSHOT HERE]

[PLACE ROS BAG RECORDING SCREENSHOT HERE]

Once you are done recording, you can review the exact Camera and LiDAR outputs at any time without booting up Unity. Just run:

```bash
ros2 bag info ~/ugv_ros2_ws/sand1
```

---

# 3. Things Used and Created

### Synthetic Particle Environment
* A dedicated Unity testing scene configured specifically for sand data generation.
* Suspended sand particles placed directly inside the active sensor field of view.
* Balanced placement so that both the Camera and the LiDAR scan the exact same space.
* Zero vehicle movement required, ensuring a perfectly controlled baseline.

### Sensor Setup
The two primary modules under review are:
* **Camera:** Captures the exact visual appearance, color, and density of the floating sand particles.
* **LiDAR:** Sends out rays to detect particle boundaries whenever they enter the configured target layers.

The remaining vehicle sensors (`GPS`, `IMU`, `TF`, and `Joint States`) stay completely active and accessible in the background.

### LiDAR Debug Visualization
We rely heavily on Unity's debug rays to watch exactly how the laser beams interact with the sand volume. This debug overlay makes it easy to confirm:
* Whether individual LiDAR rays are hitting the floating particles.
* Whether the particle objects are assigned to the correct physics layer.

[PLACE UNITY SAND PARTICLE SCENE SCREENSHOT HERE]

[PLACE LIDAR DEBUG RAYS SCREENSHOT HERE]

[PLACE PARTICLE SYSTEM INSPECTOR SCREENSHOT HERE]

---

#4. Active Sensors

The core focus of this entire test is the synchronized data coming from the Camera and LiDAR modules:

* **Camera:** Broadcasts a synthetic image feed showing the floating sand. This verifies that visual distortions caused by sandstorms are accurately captured by the sensor.
* **LiDAR:** Scans the air volume and generates point cloud returns. The sand particles are specifically configured to reflect the laser beams, forcing them to show up as physical obstructions in the LiDAR output data.
* **GPS & IMU:** Stay turned on and broadcast constant, stationary baseline data.
* **TF & Joint States:** Continually maintain the spatial coordinate tree and static wheel states so the sensor frame relationships remain perfectly accurate.

[PLACE RVIZ SENSOR OUTPUT SCREENSHOT HERE]

[PLACE LIDAR PARTICLE OUTPUT SCREENSHOT HERE]

[PLACE CAMERA SENSOR OUTPUT SCREENSHOT HERE]

The most critical takeaway here is that both sensors look at the exact same sand cloud and translate it into two completely unique, highly realistic data types simultaneously.

---

# 5. Optimization

We intentionally kept this scene highly optimized. The goal was to build a clean testing loop for particle rendering and laser raycasts without overloading the system with massive environments.

To guarantee a stable framerate and clean data logging, we used these performance guidelines:
* Only essential particle systems and sensor rigs are active in the hierarchy.
* Particle counts are strictly capped to prevent rendering lag.
* The sand volume is tightly confined only to the area directly visible to the sensor lenses.
* Camera and LiDAR update rates are kept at controlled, steady intervals instead of firing haphazardly on every single engine frame.
* LiDAR raycast detection is strictly limited to the required object layers to minimize processing overhead.
* We completely left out heavy terrain assets and decorative scenery.

---

# 6. Challenges

* **Making Particles "Solid" to LiDAR:** A particle can easily be seen by a regular camera in Unity, but standard raycast based LiDAR systems will slice right through it undetected. We had to spend extra time configuring the particle collisions so that the laser beams would hit them and register a valid return distance.
* **Layer Configuration:** We had to ensure every floating element lived on a dedicated layer that matched the LiDAR sensor's target detection mask.
* **Balancing Particle Density:** Finding the right balance was tough. If we used too few particles, the sand didn't show up in the sensor logs. If we used too many, the camera view became a solid wall of color and bogged down the rendering frame rate.
* **Sensor Positioning:** The sand particles had to be positioned precisely at the intersection point where the Camera's field of view overlapped with the LiDAR's scanning plane.
* **Validating Output:** We had to continually cross reference live debug rays with RViz2 point clouds to confirm that the sand was actually registering as data points, rather than just looking pretty on the Unity screen.

---

# 8. Future Improvements
* Adding variable particle density presets to simulate everything from light morning haze to blinding desert sandstorms.

  
