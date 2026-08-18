using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.UgvAssignment;

public class RosInput : MonoBehaviour
{
    [Header("Responsible for hearing /Input.msg. \nCar is controlled via code which is sent from Linux to Unity.")]
    [Space]

    [Header("ROS")]
    [SerializeField] private string inputTopic = "/input";

    [Header("References")]
    [SerializeField] private CarInput carInput;

    private ROSConnection ros;

    void Start()
    {
        Begin(ROSConnection.GetOrCreateInstance()); //Temp for recording
    }

    internal void Begin(ROSConnection connection)
    {
        ros = connection;
        ros.Subscribe<InputMsg>(inputTopic, ReceivedInputMsg);
    }

    internal void Stop()
    {
        ros.Unsubscribe(inputTopic);
    }

    private void ReceivedInputMsg(InputMsg message)
    {
        Debug.Log("Throttle: " + message.throttle + ", brake: " + message.brake + ", Sterring_angle: " + message.steering_angle);
        
        VehicleCommand command = new VehicleCommand
        {
            throttle = (float)message.throttle,
            brake = (float)message.brake,
            steering_angle = (float)message.steering_angle
        };

        carInput.SetRosInput(command);
    }
}