using System;
using UnityEngine;

public class CarInput : MonoBehaviour
{
    [Header("Controlled from SimManager. \nThis class reads control input from keyboard/ROS and passes to carController.")]
    [Space]
    
    [Header("Input - View Only (Edit only from SimManager)")]
    [SerializeField]
    private CarInputSource inputSource = CarInputSource.Keyboard;
    public CarInputSource CurrentInputSource => inputSource;

    [Header("References")]
    [SerializeField]
    private CarController carController;

    private VehicleControls inputActions;
    private VehicleCommand rosCommand;

    internal void Begin(CarInputSource carInputSource)
    {
        Debug.Log(carInputSource);
        inputSource = carInputSource;
    }

    private void Awake()
    {
        inputActions = new VehicleControls();
    }


    private void OnEnable() => inputActions.Vehicle.Enable();
    private void OnDisable() => inputActions.Vehicle.Disable();


    private void Update()
    {
        switch (inputSource)
        {
            case CarInputSource.Keyboard:
                ReadKeyboardInput();
                break;

            case CarInputSource.ROS:
                ApplyRosInput();
                break;
        }
    }


    private void ReadKeyboardInput()
    {
        float throttl = inputActions.Vehicle.Drive.ReadValue<float>();
        float steerin = inputActions.Vehicle.Steer.ReadValue<float>();

        bool brak = inputActions.Vehicle.Brake.IsPressed();

        VehicleCommand command = new VehicleCommand()
        {
            throttle = throttl,
            brake = brak ? 1 : 0,
            steering_angle = steerin
        };

        carController.SetInput(command);
    }


    private void ApplyRosInput()
    {
        carController.SetInput(rosCommand);
    }


    // This will be called later by the ROS subscriber.
    public void SetRosInput(VehicleCommand command)
    {
        rosCommand = command;
        ApplyRosInput();
    }
}

[Serializable]
public enum CarInputSource
{
    Keyboard,
    ROS
}

[Serializable]
public struct VehicleCommand
{
    public float throttle;       // 0 to 100
    public float brake;          // 0 to 100
    public float steering_angle; // -0.5 to +0.5 rad
}