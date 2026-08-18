using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

/// <summary>
/// Main Class, 1st one from where communication starts
/// </summary>
public class SimulationManager : MonoBehaviour
{
    [Header("Class from where code starts. \nAll settings are controlled from here")]
    [Space]

    [Header("Input Source for Car")]

    [SerializeField]
    private CarInputSource inputSource = CarInputSource.Keyboard;

    [Space]
    [Header("ROS connection is required for communication")]
    [SerializeField]
    private bool connectToRos = false;

    [Space]
    [Header("Publish Vehicle Stats. \n(For 'ROSInputToCar' it turns 'ON' when 'InputSource' is set to 'ROS')")]
    [SerializeField]
    private bool publishFeedbackOut = false;

    [Header("Sensors")]
    [SerializeField]
    private bool connectToGPSSensor = false;
    [SerializeField]
    private bool connectToIMUSensor = false;
    [SerializeField]
    private bool connectToCamera = false;
    [SerializeField]
    private bool connectToLiDAR = false;
    [SerializeField]
    private bool connectToTFs = false;
    [SerializeField]
    private bool connectToJointStates = false;

    [Space]
    [Header("References")]
    [SerializeField]
    private CarInput carInput;
    [SerializeField]
    private ROSConnection connection;
    [SerializeField]
    private RosInput rInput;
    [SerializeField]
    private RosOutput rOutput;
    [SerializeField]
    private GpsSensor gpsSensor;
    [SerializeField]
    private ImuSensor imuSensor;
    [SerializeField]
    private CameraSensor cameraSensor;
    [SerializeField]
    private LidarSensor lidarSensor;
    [SerializeField]
    private TfPublisher tfPublisher;
    [SerializeField]
    private JointStatePublisher jointState;

    //These variables remember and avoid multi calling
    private bool isFeedbackOutOn = false;
    private bool isGpsOn = false;
    private bool isEmuOn = false;
    private bool isCameraOn = false;
    private bool isLidarOn = false;
    private bool isTfOn = false;
    private bool isJointStateOn = false;
    private CarInputSource currentInputSource;

    private void Start()
    {
        //Init push to default val
        carInput.Begin(inputSource);
        currentInputSource = inputSource;

        if (connectToRos)
            connection.Connect();
    }

    private void Update()
    {
        CheckInputSource();

        if (!connectToRos)
            return;

        CheckOutPublisher();
        CheckGPSSensor();
        CheckIMUSensor();
        CheckCameraSensor();
        CheckLidarSensor();
        CheckTfSensor();
        CheckJointStateSensor();
    }

    private void CheckInputSource()
    {
        //Debug.Log("INIT: " +currentInputSource + ", ISF: "+inputSource);
        if (inputSource == currentInputSource)
            return;

        carInput.Begin(inputSource);
        currentInputSource = inputSource;

        if (inputSource == CarInputSource.ROS)
            rInput.Begin(connection);
        else
            rInput.Stop();
        //Debug.Log("CHANG: " + currentInputSource + ", ISF: " + inputSource);
    }

    private void CheckOutPublisher()
    {
        if (publishFeedbackOut == isFeedbackOutOn)
            return;

        if (publishFeedbackOut)
            rOutput.Begin(connection);
        else
            rOutput.Stop();

        isFeedbackOutOn = !isFeedbackOutOn;
    }

    private void CheckGPSSensor()
    {
        if (connectToGPSSensor == isGpsOn)
            return;

        if (connectToGPSSensor)
            gpsSensor.Begin(connection);
        else
            gpsSensor.Stop();

        isGpsOn = !isGpsOn;
    }

    private void CheckIMUSensor()
    {
        if (connectToIMUSensor == isEmuOn)
            return;

        if (connectToIMUSensor)
            imuSensor.Begin(connection);
        else
            imuSensor.Stop();

        isEmuOn = !isEmuOn;
    }

    private void CheckCameraSensor()
    {
        if (connectToCamera == isCameraOn)
            return;

        if (connectToCamera)
            cameraSensor.Begin(connection);
        else
            cameraSensor.Stop();

        isCameraOn = !isCameraOn;
    }

    private void CheckLidarSensor()
    {
        if (connectToLiDAR == isLidarOn)
            return;

        if (connectToLiDAR)
            lidarSensor.Begin(connection);
        else
            lidarSensor.Stop();

        isLidarOn = !isLidarOn;
    }

    private void CheckTfSensor()
    {
        if (connectToTFs == isTfOn)
            return;

        if (connectToTFs)
            tfPublisher.Begin(connection);
        else
            tfPublisher.Stop();

        isTfOn = !isTfOn;
    }

    private void CheckJointStateSensor()
    {
        if (connectToJointStates == isJointStateOn)
            return;

        if (connectToJointStates)
            jointState.Begin(connection);
        else
            jointState.Stop();

        isJointStateOn = !isJointStateOn;
    }
}