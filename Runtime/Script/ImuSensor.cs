using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Geometry;

public class ImuSensor : MonoBehaviour
{
    [Header("Publishes IMU topic which contains - \nOrientation, Angular velocity & Linear acceleration of the car.")]
    [Space]

    [Header("ROS")]
    [SerializeField]
    private string imuTopic = "/imu";

    [Header("Vehicle")]
    [SerializeField]
    private Rigidbody vehicleRigidbody;

    [Tooltip("Every that milli sec the data will be published")]
    [Header("Publishing")]
    [SerializeField]
    private float publishRate = 5f;

    private ROSConnection ros;
    private Vector3 previousVelocity;

    internal void Begin(ROSConnection connection)
    {
        ros = connection;

        ros.RegisterPublisher<ImuMsg>(imuTopic);

        previousVelocity = vehicleRigidbody.linearVelocity;

        InvokeRepeating(nameof(PublishImu), 0, 1/publishRate);
    }

    internal void Stop()
    {
        CancelInvoke();
    }

    private void PublishImu()
    {
        Vector3 acceleration = (vehicleRigidbody.linearVelocity - previousVelocity) / publishRate;

        previousVelocity = vehicleRigidbody.linearVelocity;

        Quaternion rotation = vehicleRigidbody.rotation;
        Vector3 angularVelocity = vehicleRigidbody.angularVelocity;

        ImuMsg imuMessage = new ImuMsg();

        // Orientation
        imuMessage.orientation = new QuaternionMsg(
            rotation.x,
            rotation.y,
            rotation.z,
            rotation.w
        );

        // Angular velocity (rad/s)
        imuMessage.angular_velocity = new Vector3Msg(
            angularVelocity.x,
            angularVelocity.y,
            angularVelocity.z
        );

        // Linear acceleration (m/s²)
        imuMessage.linear_acceleration = new Vector3Msg(
            acceleration.x,
            acceleration.y,
            acceleration.z
        );

        ros.Publish(imuTopic, imuMessage);
    }
}