using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

public class JointStatePublisher : MonoBehaviour
{
    [Header("Publishes wheel stats")]
    [Space]

    [Header("ROS")]
    [SerializeField]
    private string jointStateTopic = "/joint_states";

    [Header("Wheel Colliders")]
    [SerializeField]
    private WheelCollider frontLeftWheel;
    [SerializeField]
    private WheelCollider frontRightWheel;
    [SerializeField]
    private WheelCollider rearLeftWheel;
    [SerializeField]
    private WheelCollider rearRightWheel;

    [Header("Publish Settings")]
    [Tooltip("Every that milli sec the data will be published")]
    [SerializeField]
    private float publishRate = 10f;

    private ROSConnection ros;

    private float frontLeftPosition;
    private float frontRightPosition;
    private float rearLeftPosition;
    private float rearRightPosition;

    //Called from SimManager
    internal void Begin(ROSConnection connection)
    {
        ros = connection;
        ros.RegisterPublisher<JointStateMsg>(jointStateTopic);

        StartPublishing();
    }

    //Called from SimManager
    internal void Stop()
    {
        CancelInvoke();
    }

    private void StartPublishing()
    {
        if (publishRate <= 0f)
        {
            Debug.LogError("JointStatePublisher: Publish Rate must be greater than 0.");
            return;
        }

        InvokeRepeating(nameof(PublishJointStates), 0f, 1f / publishRate);
    }

    private void PublishJointStates()
    {
        if (frontLeftWheel == null || frontRightWheel == null || rearLeftWheel == null || rearRightWheel == null)
        {
            Debug.LogError("JointStatePublisher: One or more WheelColliders are missing.");
            CancelInvoke();
            return;
        }

        // Convert RPM to radians per second.
        float frontLeftVelocity = frontLeftWheel.rpm * Mathf.Deg2Rad * 6f;
        float frontRightVelocity = frontRightWheel.rpm * Mathf.Deg2Rad * 6f;
        float rearLeftVelocity = rearLeftWheel.rpm * Mathf.Deg2Rad * 6f;
        float rearRightVelocity = rearRightWheel.rpm * Mathf.Deg2Rad * 6f;

        // Integrate wheel rotation.
        frontLeftPosition += frontLeftVelocity * (1f / publishRate);
        frontRightPosition += frontRightVelocity * (1f / publishRate);
        rearLeftPosition += rearLeftVelocity * (1f / publishRate);
        rearRightPosition += rearRightVelocity * (1f / publishRate);

        JointStateMsg message = new JointStateMsg
        {
            header = new HeaderMsg
            {
                stamp = GetCurrentTime(),
                frame_id = "base_link"
            },
            name = new string[]
            {
                "front_left_wheel_joint",
                "front_right_wheel_joint",
                "rear_left_wheel_joint",
                "rear_right_wheel_joint"
            },
            position = new double[]
            {
                frontLeftPosition,
                frontRightPosition,
                rearLeftPosition,
                rearRightPosition
            },
            velocity = new double[]
            {
                frontLeftVelocity,
                frontRightVelocity,
                rearLeftVelocity,
                rearRightVelocity
            },
            effort = new double[]
            {
                0.0, 0.0, 0.0, 0.0
            }
        };

        ros.Publish(jointStateTopic, message);
    }


    private TimeMsg GetCurrentTime()
    {
        float currentTime = Time.realtimeSinceStartup;
        int seconds = Mathf.FloorToInt(currentTime);
        uint nanoseconds = (uint)((currentTime - seconds) * 1000000000);

        return new TimeMsg((uint)seconds, nanoseconds);
    }


    private void OnDestroy()
    {
        CancelInvoke();
    }
}
