using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using System.Collections.Generic;

public class LidarSensor : MonoBehaviour
{
    [Header("Detects obstacles in 360 by raycast. \nSet object's to 'Obstacle' to get detected.")]
    [Space]

    [Header("Debug")]
    [SerializeField]
    private bool visualizeRaysInEditor = true;
    [SerializeField]
    private bool visualizeRaysInScene = false;

    [Header("ROS")]
    [SerializeField]
    private string lidarTopic = "/lidar";
    [SerializeField]
    private string frameId = "lidar";

    [Header("LiDAR Settings")]
    [SerializeField]
    private float minRange = 0.1f;
    [SerializeField]
    private float maxRange = 10f;
    [SerializeField]
    private int numberOfRays = 180;
    [Tooltip("Every that milli sec the data will be published")]
    [SerializeField]
    private float publishRate = 10f;

    [Header("Detection")]
    [SerializeField]
    private List<string> obstacleStartNames = new List<string>();
    [SerializeField]
    private List<string> particleStartNames = new List<string>();
    private LayerMask obstacleLayer;
    [SerializeField]
    private LayerMask particleLayer;

    [Header("Particle Detection")]
    [SerializeField, Range(0f, 1f)]
    private float particleDetectionProbability = 0.2f;
    [SerializeField]
    private float particleRangeNoise = 0.2f;
    [SerializeField]
    private float particleCloudDepth = 4f;

    private LidarVisualizer lidarVisualizer;
    private ROSConnection ros;
    private float[] ranges;
    private bool[] detected;

    private void Start()
    {
        if (visualizeRaysInScene)
        {
            lidarVisualizer = GetComponent<LidarVisualizer>();
            if (lidarVisualizer != null)
                lidarVisualizer.Begin(numberOfRays);
        }
    }

    internal void Begin(ROSConnection connection)
    {
        ros = connection;

        ros.RegisterPublisher<LaserScanMsg>(lidarTopic);

        ranges = new float[numberOfRays];
        detected = new bool[numberOfRays];

        StartPublishing();
    }

    internal void Stop()
    {
        CancelInvoke();
    }

    private void StartPublishing()
    {
        if (publishRate <= 0f)
        {
            Debug.LogError("LidarSensor: Publish Rate must be greater than 0.");
            return;
        }

        InvokeRepeating(nameof(PublishLidarScan), 0f, 1f / publishRate);
    }

    // private void PublishLidarScan()
    // {
    //     float angleMin = -Mathf.PI;
    //     float angleMax = Mathf.PI;

    //     //Creation of 360 angle with the no. of rays req given

    //     float angleIncrement = (angleMax - angleMin) / (numberOfRays - 1);

    //     for (int i = 0; i < numberOfRays; i++)
    //     {
    //         float rosAngle = angleMin + (i * angleIncrement);

    //         Vector3 localDirection = new Vector3(-Mathf.Sin(rosAngle), 0f, Mathf.Cos(rosAngle));
    //         Vector3 worldDirection = transform.TransformDirection(localDirection);

    //         // Detect real solid obstacles
    //         bool hitObstacle = Physics.Raycast(
    //             transform.position,
    //             worldDirection,
    //             out RaycastHit obstacleHit,
    //             maxRange,
    //             obstacleLayer,
    //             QueryTriggerInteraction.Ignore
    //         );

    //         // Detect the particle volume
    //         bool hitParticleVolume = Physics.Raycast(
    //             transform.position,
    //             worldDirection,
    //             out RaycastHit particleHit,
    //             maxRange,
    //             particleLayer,
    //             QueryTriggerInteraction.Collide
    //         );

    //         float finalRange = maxRange;
    //         bool detectedSomething = false;

    //         //Particle hit
    //         if (hitParticleVolume)
    //         {
    //             if (Random.value < particleDetectionProbability)
    //             {
    //                 float maxParticleDistance = particleHit.distance + particleCloudDepth;

    //                 //If both particle & obstacle got in range
    //                 if (hitObstacle)
    //                     maxParticleDistance = Mathf.Min(maxParticleDistance, obstacleHit.distance);

    //                 maxParticleDistance = Mathf.Max(particleHit.distance, maxParticleDistance);

    //                 float particleDistance = Random.Range(particleHit.distance, maxParticleDistance);

    //                 // Add noise
    //                 float noisyDistance = particleDistance + Random.Range(-particleRangeNoise, particleRangeNoise);

    //                 finalRange = Mathf.Clamp(noisyDistance, minRange, maxRange);

    //                 detectedSomething = true;
    //             }
    //         }


    //         //Obstacle hit
    //         if (hitObstacle)
    //         {
    //             if (!detectedSomething || obstacleHit.distance < finalRange)
    //             {
    //                 finalRange = Mathf.Max(obstacleHit.distance, minRange);
    //                 detectedSomething = true;
    //             }
    //         }

    //         //Storing ray distances from sensor to hit area
    //         ranges[i] = finalRange;
    //         detected[i] = detectedSomething; // Store whether this ray detected something

    //         //Debug view in editor
    //         if (visualizeRaysInEditor)
    //         {
    //             Color rayColor = detectedSomething ? Color.red : Color.green;
    //             Debug.DrawRay(transform.position, worldDirection * finalRange, rayColor, 0.1f);
    //         }
    //     }

    //     if (lidarVisualizer != null)    //Top view cam debug
    //         lidarVisualizer.UpdateRays(ranges, detected, angleMin, angleIncrement);


    //     LaserScanMsg message = new LaserScanMsg
    //     {
    //         header = new HeaderMsg { frame_id = frameId },
    //         angle_min = angleMin,
    //         angle_max = angleMax,
    //         angle_increment = angleIncrement,
    //         range_min = minRange,
    //         range_max = maxRange,
    //         ranges = ranges
    //     };

    //     ros.Publish(lidarTopic, message);
    // }

    private void PublishLidarScan()
    {
        float angleMin = -Mathf.PI;
        float angleMax = Mathf.PI;

        // Create 360 degree scan
        float angleIncrement = (angleMax - angleMin) / (numberOfRays - 1);

        for (int i = 0; i < numberOfRays; i++)
        {
            float rosAngle = angleMin + (i * angleIncrement);

            Vector3 localDirection = new Vector3(-Mathf.Sin(rosAngle), 0f, Mathf.Cos(rosAngle));
            Vector3 worldDirection = transform.TransformDirection(localDirection);

            // Default result: nothing detected
            float finalRange = maxRange;
            bool detectedSomething = false;

            // Get EVERYTHING hit by this ray
            RaycastHit[] hits = Physics.RaycastAll(
                transform.position,
                worldDirection,
                maxRange,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide
            );

            // Process closest object first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                string hitObjectName = hit.collider.gameObject.name;

                //Valid obstacle hit check
                if (StartsWithAny(hitObjectName, obstacleStartNames))
                {
                    finalRange = Mathf.Max(hit.distance, minRange);
                    detectedSomething = true;
                    break;
                }

                //Particle hit check
                if (StartsWithAny(hitObjectName, particleStartNames))
                {
                    if (Random.value < particleDetectionProbability)
                    {
                        float particleStartDistance = hit.distance;
                        float maxParticleDistance = particleStartDistance + particleCloudDepth;

                        maxParticleDistance = Mathf.Min(maxParticleDistance, maxRange);

                        // Point inside particle cloud
                        float particleDistance = Random.Range(particleStartDistance, maxParticleDistance);
                        // Add  noise
                        float noisyDistance = particleDistance + Random.Range(-particleRangeNoise, particleRangeNoise);
                        finalRange = Mathf.Clamp(noisyDistance, minRange, maxRange);

                        detectedSomething = true;
                    }
                }
            }

            ranges[i] = finalRange;
            detected[i] = detectedSomething;

            if (visualizeRaysInEditor)
            {
                Color rayColor = detectedSomething ? Color.red : Color.green;
                Debug.DrawRay(transform.position, worldDirection * finalRange, rayColor, 0.1f);
            }
        }

        //Visual update
        if (lidarVisualizer != null)
        {
            lidarVisualizer.UpdateRays(
                ranges,
                detected,
                angleMin,
                angleIncrement
            );
        }


        LaserScanMsg message = new LaserScanMsg
        {
            header = new HeaderMsg { frame_id = frameId },
            angle_min = angleMin,
            angle_max = angleMax,
            angle_increment = angleIncrement,
            range_min = minRange,
            range_max = maxRange,
            ranges = ranges
        };


        ros.Publish(lidarTopic, message);
    }

    private bool StartsWithAny(string objectName, List<string> startNames
    )
    {
        foreach (string startName in startNames)
        {
            if (!string.IsNullOrEmpty(startName) && objectName.StartsWith(startName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}