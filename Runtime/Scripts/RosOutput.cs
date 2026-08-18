using RosMessageTypes.UgvAssignment;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class RosOutput : MonoBehaviour
{
    [Header("Responsible for publishing car stats as data type: Output.msg")]
    [Space]

    [Header("ROS")]
    [SerializeField]
    private string outputTopic = "/feedback";
    [Tooltip("Its the car stats which are printed in log window for checking")]
    [SerializeField]
    private bool showDebugMsg = true;

    [Header("Publishing Settings")]
    [Tooltip("Every sec data is published to the topic")]
    [SerializeField]
    private float publishingRepeatInterval = 1f;

    [Header("Car")]
    [SerializeField] private CarController carController;

    private ROSConnection ros;

    internal void Begin(ROSConnection connection)
    {
        ros = connection;
        ros.RegisterPublisher<OutputMsg>(outputTopic);

        InvokeRepeating(nameof(PublishOutputMsg), 0, publishingRepeatInterval);
    }

    internal void Stop()
    {
        CancelInvoke();
    }

    private void PublishOutputMsg()
    {
        OutputMsg message = new OutputMsg
        {
            state = carController.CurrentCarMode.ToString(),
            linear_velocity = carController.LinearVelocity,
            angular_velocity = carController.AngularVelocity,
            front_left_steering_angle = carController.FrontLeftSteeringAngle,
            front_right_steering_angle = carController.FrontRightSteeringAngle
        };

        //Publishing car stats as Output.msg for parsed from ROS settings in Unity
        ros.Publish(outputTopic, message);

        if (showDebugMsg)
            Debug.Log(
                $"ROS OUTPUT -> " +
                $"State: {message.state}, " +
                $"Linear: {message.linear_velocity}, " +
                $"Angular: {message.angular_velocity}, " +
                $"FL Steering: {message.front_left_steering_angle}, " +
                $"FR Steering: {message.front_right_steering_angle}");
    }
}
