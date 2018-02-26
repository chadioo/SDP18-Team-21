//-----------------------------------------------------------------------
// <copyright file="HelloARController.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.HelloAR
{
    using System.Collections.Generic;
    using System.Collections;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.Rendering;
    using TechTweaking.Bluetooth;

    /// <summary>
    /// Controls the HelloAR example.
    /// </summary>
    public class SoccerGameController : MonoBehaviour
    {
        /// <summary>
        /// The first-person camera being used to render the passthrough camera image (i.e. AR background).
        /// </summary>
        public Camera FirstPersonCamera;

        /// <summary>
        /// A prefab for tracking and visualizing detected planes.
        /// </summary>
        public GameObject TrackedPlanePrefab;

        /// <summary>
        /// A model of the soccer ball.
        /// </summary>
        public GameObject SoccerBallPrefab;

        /// <summary>
        /// A model of the soccer goal.
        /// </summary>
        public GameObject SoccerGoalPrefab;

        /// <summary>
        /// A model of the soccer ball.
        /// </summary>
        private Vector3 SoccerBallVector;

        /// <summary>
        /// A model of the soccer goal.
        /// </summary>
        private Vector3 SoccerGoalVector;

        /// <summary>
        /// A list to hold new planes ARCore began tracking in the current frame. This object is used across
        /// the application to avoid per-frame allocations.
        /// </summary>
        private List<TrackedPlane> m_NewPlanes = new List<TrackedPlane>();

        /// <summary>
        /// True if the app is in the process of quitting due to an ARCore connection error, otherwise false.
        /// </summary>
        private bool m_IsQuitting = false;

        /// <summary>
        /// True if the app found plane, otherwise false.
        /// </summary>
        private bool FoundPlane = false;

        /// <summary>
        /// True if the app spawned soccer goal and soccer ball, otherwise false.
        /// </summary>
        private bool SpawnObjects = false;

        /// <summary>
        /// True if threshold for kick was detected, otherwise false.
        /// </summary>
        private bool KickDetected = false;

        /// <summary>
        /// Amount of g (acceleration) needed to detect a kick.
        /// </summary>
        private float Threshold = 1.5f;

        /// <summary>
        /// PLane vector to use for anchoring objects.
        /// </summary>
        private Vector3 PlaneVector;

        /// <summary>
        /// Speed is value used to control speed of soccer ball
        /// </summary>
        public float speed = 1f;

        /// <summary>
        /// Rigidbody is object that contains soccer goal, allows it to use physics.
        /// </summary>
        private Rigidbody SoccerBallRigidbody;

        /// <summary>
        /// Bluetooth device is used to connect to bluetooth module.
        /// </summary>
        private BluetoothDevice device;

        /// <summary>
        /// Sensor data storage.
        /// </summary>
        private float[] SensorData;



        /// <summary>
        /// The Unity Start() method.
        /// </summary>
        void Awake(){

            device = new BluetoothDevice();

            if (BluetoothAdapter.isBluetoothEnabled()){
                connect();
            }
            else{

                //BluetoothAdapter.enableBluetooth(); //you can by this force enabling Bluetooth without asking the user
                Debug.Log("Status : Please enable your Bluetooth");

                BluetoothAdapter.OnBluetoothStateChanged += HandleOnBluetoothStateChanged;
                BluetoothAdapter.listenToBluetoothState(); // if you want to listen to the following two events  OnBluetoothOFF or OnBluetoothON

                BluetoothAdapter.askEnableBluetooth(); // Ask user to enable Bluetooth

            }

        }



        /// <summary>
        /// The Unity Start() method.
        /// </summary>
        void Start(){

            //Debug.Log("ARK LOG ********** Running Start");

            BluetoothAdapter.OnDeviceOFF += HandleOnDeviceOff;//This would mean a failure in connection! the reason might be that your remote device is OFF

            BluetoothAdapter.OnDeviceNotFound += HandleOnDeviceNotFound; //Because connecting using the 'Name' property is just searching, the Plugin might not find it!.

            SideBySidePerspectiveCameraConfig();

            // Create rigidbody for soccer ball
            SoccerBallRigidbody = SoccerBallPrefab.GetComponent<Rigidbody>();
            //Debug.Log("ARK LOG ********** Instantiate SoccerBallRigidbody");

            SensorData = new float[6];
            //Debug.Log("ARK LOG ********** Instantiate SensorData Array to Length of 6");
        }



        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            //Debug.Log("ARK LOG ********** Running Update.");
            _QuitOnConnectionErrors();

            // Check that motion tracking is tracking.
            if (Frame.TrackingState != TrackingState.Tracking)
            {
                const int lostTrackingSleepTimeout = 15;
                Screen.sleepTimeout = lostTrackingSleepTimeout;
                return;
            }

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // If you have not found plane, search for plane
            if (!FoundPlane)
            {
                // See if new plane exists
                Frame.GetPlanes(m_NewPlanes, TrackableQueryFilter.New);

                // If there is a new plane, stop searching
                if (m_NewPlanes.Count > 0)
                {
                    Debug.Log("ARK LOG ********** Plane has been found.");
                    FoundPlane = true;
                }

                for (int i = 0; i < m_NewPlanes.Count; i++)
                {
                    PlaneVector = m_NewPlanes[i].Position;
                    // Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
                    // the origin with an identity rotation since the mesh for our prefab is updated in Unity World
                    // coordinates.
                    GameObject planeObject = Instantiate(TrackedPlanePrefab, Vector3.zero, Quaternion.identity,
                        transform);
                    planeObject.GetComponent<TrackedPlaneVisualizer>().Initialize(m_NewPlanes[i]);
                }

            }

            // If plane is found and objects have not been instantiated
            if (FoundPlane && !SpawnObjects){

                //Debug.Log("ARK LOG ********** Spawn objects onto plane.");

                // Sample plane location
                //Vector3 PlaneVector = m_NewPlanes[0].Position;

                // Set spawn location to be on plane certain distance in front of camera
                SoccerBallVector = new Vector3(0, PlaneVector.y, 1);      // ball is 1 unit of distance forward
                SoccerGoalVector = new Vector3(0, PlaneVector.y - 10, 40);   // goal is 15 units of distance forward, lower height to rest on plane

                // Spawn Objects
                Instantiate(SoccerBallPrefab, SoccerBallVector, Quaternion.identity);
                Instantiate(SoccerGoalPrefab, SoccerGoalVector, Quaternion.Euler(270, 270, 180));



                //Debug.Log("ARK LOG ********** Objects have been spawn.");

                SpawnObjects = true;
            }

            // If objects have been spawn, move soccer ball
            if (FoundPlane && SpawnObjects)
            {

                //Debug.Log("ARK LOG ********** Read data from device.");

                //StartCoroutine("ManageConnection", device);
                /*
                                // Read data from Bluetooth file
                                //Debug.Log("ARK LOG ********** Device is reading: " + device.IsReading);
                                //Debug.Log("ARK LOG ********** Data is available: " + device.IsDataAvailable);
                                while (device.IsReading)
                                {
                                    if (device.IsDataAvailable)
                                    {

                                        //because we called setEndByte(10)..read will always return a packet excluding the last byte 10.
                                        byte[] msg = device.read();
                                        string content = "";
                                        string[] subStrings;

                                        // Read string
                                        if (msg != null && msg.Length > 0)
                                        {
                                            content = System.Text.ASCIIEncoding.ASCII.GetString(msg);
                                            //Debug.Log("ARK LOG ********** Content: " + content);

                                            // Split up by spaces
                                            content = content.Replace(",", "");
                                            subStrings = content.Split(' ');
                                            //Debug.Log("ARK LOG ********** Sensor Data Length:" + subStrings.Length);
                                            //_ShowAndroidToastMessage("Sensor Data Length: "+subStrings.Length);

                                            for (int i = 0; i < subStrings.Length; i++)
                                            {

                                                double value = double.Parse(subStrings[i]);
                                                SensorData[i] = (float)value;
                                            }
                                            _ShowAndroidToastMessage("Sensor Data: "+SensorData.Length+"Kick Detection: "+KickDetected);

                                            // If no kick has been detected and data exists, check if threshold has been reached 
                                            if (!KickDetected)
                                            {
                                                //Debug.Log("ARK LOG ********** Sensor Value: " + SensorData[0]);
                                                //_ShowAndroidToastMessage("Acc: "+ SensorData[0] + " g");
                                                // If threshold has been reached, kick has been detected
                                                if (SensorData[0] >= Threshold)
                                                {

                                                    _ShowAndroidToastMessage("Kick Detected!");
                                                    KickDetected = true;
                                                }
                                            }

                                            // If kick has been detected and data exists, move ball
                                            if (KickDetected) {

                                                //_ShowAndroidToastMessage("Move");
                                                // Determines ball movement
                                                float moveHorizontal = SensorData[0];
                                                float moveVertical = SensorData[1];

                                                Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

                                                // Applies force to rigidbody, makes movement
                                                SoccerBallRigidbody.AddForce(movement * speed);                
                                            }

                                        }
                                        //break;
                                    }
                                }
                */
            }

        } // end of Update



        //############### Reading Data  #####################
        //Please note that you don't have to use Couroutienes, you can just put your code in the Update() method
        IEnumerator ManageConnection(BluetoothDevice device)
        {//Manage Reading Coroutine
            Debug.Log("ARK LOG ********** ManageConnection: Device is reading: " + device.IsReading + " Data is available: " + device.IsDataAvailable);

            while (device.IsReading)
            {
                if (device.IsDataAvailable)
                {
                    //because we called setEndByte(10)..read will always return a packet excluding the last byte 10. 10 equals '\n' so it will return lines. 
                    byte[] msg = device.read();

                    if (msg != null && msg.Length > 0)
                    {
                        string content = System.Text.ASCIIEncoding.ASCII.GetString(msg);
                        Debug.Log("ARK LOG ********** Content: " + content);
                    }
                }

                yield return null;
            }
        }



        private void connect(){

            Debug.Log("Status : Trying To Connect");

            device.MacAddress = "98:D3:35:71:0B:15";

            device.setEndByte(10);

            device.ReadingCoroutine = ManageConnection;

            Debug.Log("Status : trying to connect");

            device.connect();

        }


        //############### Handlers/Recievers #####################
        void HandleOnBluetoothStateChanged(bool isBtEnabled)
        {
            if (isBtEnabled)
            {
                connect();
                //We now don't need our recievers
                BluetoothAdapter.OnBluetoothStateChanged -= HandleOnBluetoothStateChanged;
                BluetoothAdapter.stopListenToBluetoothState();
            }
        }



        //This would mean a failure in connection! the reason might be that your remote device is OFF
        void HandleOnDeviceOff(BluetoothDevice dev)
        {
            if (!string.IsNullOrEmpty(dev.Name))
            {
                Debug.Log("Status : can't connect to '" + dev.Name + "', device is OFF ");
            }
            else if (!string.IsNullOrEmpty(dev.MacAddress))
            {
                Debug.Log("Status : can't connect to '" + dev.MacAddress + "', device is OFF ");
            }
        }



        //Because connecting using the 'Name' property is just searching, the Plugin might not find it!.
        void HandleOnDeviceNotFound(BluetoothDevice dev)
        {
            if (!string.IsNullOrEmpty(dev.Name))
            {
                Debug.Log("Status : Can't find a device with the name '" + dev.Name + "', device might be OFF or not paird yet ");

            }
        }



        public void disconnect()
        {
            if (device != null)
                device.close();
        }



        //############### Deregister Events  #####################
        void OnDestroy()
        {
            BluetoothAdapter.OnDeviceOFF -= HandleOnDeviceOff;
            BluetoothAdapter.OnDeviceNotFound -= HandleOnDeviceNotFound;

        }



        /// <summary>
        /// Quit the application if there was a connection error for the ARCore session.
        /// </summary>
        private void _QuitOnConnectionErrors()
        {
            if (m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.ConnectionState == SessionConnectionState.UserRejectedNeededPermission)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("DoQuit", 0.5f);
            }
            else if (Session.ConnectionState == SessionConnectionState.ConnectToServiceFailed)
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("DoQuit", 0.5f);
            }
        }



        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void DoQuit()
        {
            Application.Quit();
        }



        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public Camera camLeft;

        public Camera camRight;

        /// <summary>
        /// Sets the cameras parameters to side by side configuration.
        /// </summary>
        void SideBySidePerspectiveCameraConfig()
        {
            //sets cameras to perspective
            this.camLeft.orthographic = false;
            this.camRight.orthographic = false;

            //camera positioning
            this.camLeft.pixelRect = new Rect(0, 0, Screen.width / 2, Screen.height);
            this.camRight.pixelRect = new Rect(Screen.width / 2, 0, Screen.width, Screen.height);

            float nRatio = (camLeft.pixelWidth) / camLeft.pixelHeight * 2;
            Debug.Log(string.Format("Current res = {0}x{1}, ratio of {2}.", camLeft.pixelWidth, camLeft.pixelHeight, nRatio));
            this.camLeft.aspect = nRatio;
            this.camRight.aspect = nRatio;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



    }
}
