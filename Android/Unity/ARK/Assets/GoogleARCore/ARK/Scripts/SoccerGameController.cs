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

namespace GoogleARCore.HelloAR{

    // IMPORTS
    using System.Collections.Generic;
    using System.Collections;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.Rendering;
    using TechTweaking.Bluetooth;

    public class SoccerGameController : MonoBehaviour{
        
        // CAMERAS
        public Camera FirstPersonCamera;        // main camera
        public Camera camLeft;                  // left camera for side by side
        public Camera camRight;                 // left camera for side by side

       // GAMEOBJECTS
        public GameObject SoccerGoalInput;     // soccer goal input
        public GameObject SoccerFieldInput;    // soccer field input

        // RIGIDBODY
        public Rigidbody SoccerBallInput;      // soccer ball input
        private Rigidbody SoccerBallRigidbody;  // instantiated soccer ball

        // VECTORS
        private Vector3 SoccerBallVector;       // soccer ball position vector
        private Vector3 SoccerGoalVector;       // soccer goal position vector
        private Vector3 SoccerFieldVector;      // soccer field position vector
        private Vector3 PlaneVector;            // plane position vector

        // LISTS
        private List<TrackedPlane> m_NewPlanes = new List<TrackedPlane>();  // used to store plane data

        // BOOLEAN
        private bool m_IsQuitting = false;      // determines if quitting
        private bool FoundPlane = false;        // determines if plane found
        private bool SpawnGoal = false;         // determines if spawned objects
        private bool SpawnBall = false;         // determines if spawned objects
        private bool KickDetected = false;      // determines if kick detected
        private bool ResetBall = false;         // determines if ball needs to be reset
        private bool GoalScored = false;        // determines if goal scored
        private bool OutOfBounds = false;       // determines if out of bounds

        // FLOATS
        private float Threshold = 2f;           // acceleration threshold for determining kick
        private float speed = 1f;               // speed of ball (not currently used)
        private float[] SensorData;             // array used to store sensor data from one input line

        // INTEGERS
        private int score = 0;

        // BLUETOOTH DEVICE
        private BluetoothDevice device;         // bluetooth device



        // AWAKE METHOD (first method, initializes bluetooth)
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



        // START METHOD (runs after awake, handles bluetooth exceptions)
        void Start(){

            //Debug.Log("ARK LOG ********** Running Start");

            BluetoothAdapter.OnDeviceOFF += HandleOnDeviceOff;//This would mean a failure in connection! the reason might be that your remote device is OFF

            BluetoothAdapter.OnDeviceNotFound += HandleOnDeviceNotFound; //Because connecting using the 'Name' property is just searching, the Plugin might not find it!.

            //SideBySidePerspectiveCameraConfig();

        }



        // UPDATE METHOD
        public void Update(){

            //Debug.Log("ARK LOG ********** Running Update.");
            _QuitOnConnectionErrors();

            // Check that motion tracking is tracking.
            if (Frame.TrackingState != TrackingState.Tracking){

                const int lostTrackingSleepTimeout = 15;
                Screen.sleepTimeout = lostTrackingSleepTimeout;
                return;
            }

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // If you have not found plane, search for plane
            if (!FoundPlane){

                // See if new plane exists
                Frame.GetPlanes(m_NewPlanes, TrackableQueryFilter.New);

                // If there is a new plane, stop searching
                if (m_NewPlanes.Count > 0){

                    //Debug.Log("ARK LOG ********** Plane has been found.");
                    PlaneVector = m_NewPlanes[0].Position;
                    FoundPlane = true;
                }
            }

            // If plane is found and objects have not been instantiated
            if (FoundPlane && !SpawnGoal && ! SpawnBall){

                //Debug.Log("ARK LOG ********** Spawn objects onto plane.");

                // Sample plane location
                //Vector3 PlaneVector = m_NewPlanes[0].Position;

                // Set spawn location to be on plane certain distance in front of camera
                SoccerFieldVector = new Vector3(0, PlaneVector.y, 15);     // field vector is everywhere
                SoccerBallVector = new Vector3(0, PlaneVector.y+1, 1);     // ball is 1 unit of distance forward
                SoccerGoalVector = new Vector3(0, PlaneVector.y, 30);      // goal is 15 units of distance forward, lower height to rest on plane

                // Spawn Objects
                Instantiate(SoccerFieldInput, SoccerFieldVector, Quaternion.identity);
                SoccerBallRigidbody = Instantiate(SoccerBallInput, SoccerBallVector, Quaternion.identity) as Rigidbody;
                Instantiate(SoccerGoalInput, SoccerGoalVector, Quaternion.Euler(270, 270, 180));

                //Debug.Log("ARK LOG ********** Objects have been spawn.");

                SpawnGoal = true;
                SpawnBall = true;
            }

            // If plane is found and objects have not been instantiated
            if (FoundPlane && SpawnGoal && !SpawnBall){

                // Set spawn location to be on plane certain distance in front of camera
                SoccerBallVector = new Vector3(0, PlaneVector.y + 1, 1);     // ball is 1 unit of distance forward

                // Spawn Objects
                SoccerBallRigidbody = Instantiate(SoccerBallInput, SoccerBallVector, Quaternion.identity) as Rigidbody;

                //Debug.Log("ARK LOG ********** Objects have been spawn.");

                SpawnBall = true;
            }


        } // end of Update


        
        // FIXED UPDATE METHOD
        void FixedUpdate() {
            
            if (FoundPlane && SpawnBall && SpawnGoal && KickDetected){

                Debug.Log("Soccer ball position: x " + SoccerBallRigidbody.position.x + " y " + SoccerBallRigidbody.position.y + " z " + SoccerBallRigidbody.position.z);

                if (SensorData.Length >= 3) {

                    //_ShowAndroidToastMessage("Move");
                    // Determines ball movement
                    float moveHorizontal = SensorData[0];
                    float moveVertical = SensorData[2];

                    Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

                    SoccerBallRigidbody.AddForce(movement);

                    if (SoccerBallRigidbody.position.x > 15 | SoccerBallRigidbody.position.x < -15 | SoccerBallRigidbody.position.z < -5) {
                        OutOfBounds = true;
                        _ShowAndroidToastMessage("Out of Bounds");
                    }
                    else if (SoccerBallRigidbody.position.x < 5 && SoccerBallRigidbody.position.x > -5 && SoccerBallRigidbody.position.z > 30 && SoccerBallRigidbody.position.y < 10) {
                        GoalScored = true;
                    }
                    else if (SoccerBallRigidbody.position.z > 30) {
                        OutOfBounds = true;
                    }

                    if (OutOfBounds) {
                        Destroy(SoccerBallRigidbody, 1.0f);// remove object
                        _ShowAndroidToastMessage("Out of Bounds");
                        OutOfBounds = false;
                    }

                    if (GoalScored) {
                        Destroy(SoccerBallRigidbody, 1.0f);// remove object
                        score++;    // increase score
                        _ShowAndroidToastMessage("Score: " + score);
                        GoalScored = false; // reset boolean
                    }

                }
            }

        }



        // READING DATA
        IEnumerator ManageConnection(BluetoothDevice device) {
        //Manage Reading Coroutine
            //Debug.Log("ARK LOG ********** ManageConnection: Device is reading: " + device.IsReading + " Data is available: " + device.IsDataAvailable);

            while (device.IsReading){

                if (device.IsDataAvailable){

                    //because we called setEndByte(10)..read will always return a packet excluding the last byte 10.
                    byte[] msg = device.read();
                    string content = "";
                    string[] subStrings;

                    if (msg != null && msg.Length > 0){

                        content = System.Text.ASCIIEncoding.ASCII.GetString(msg);
                        //Debug.Log("ARK LOG ********** Content: " + content);

                        // Split up by spaces
                        content = content.Replace(",", "");
                        subStrings = content.Split(' ');
                        //Debug.Log("ARK LOG ********** Sensor Data Length:" + subStrings.Length + " 0: " + subStrings[0]);
                        //_ShowAndroidToastMessage("Sensor Data Length: "+subStrings.Length);

                        SensorData = new float[subStrings.Length];
                        //Debug.Log("ARK LOG ********** Instantiate SensorData Array to Length of 6");

                        for (int i = 0; i < subStrings.Length; i++){

                            //Debug.Log("Substring: "+ subStrings[i] + " i:"+i);
                            float temp;
                            if (float.TryParse(subStrings[i], out temp)){
                                SensorData[i] = temp;
                                //Debug.Log("Parsed Value " + temp + " at " + i);
                            }
                            else {
                                //Debug.Log("Could not parse!");
                            }
                        }
                        //_ShowAndroidToastMessage("Sensor Data: " + SensorData.Length + "Kick Detection: " + KickDetected);

                        // If no kick has been detected and data exists, check if threshold has been reached 
                        if (FoundPlane && SpawnBall && SpawnGoal && !KickDetected){
                            //Debug.Log("ARK LOG ********** Sensor Value: " + SensorData[0] + " >= ? Threshold: "+Threshold);
                            //_ShowAndroidToastMessage("Acc: "+ SensorData[0] + " g");
                            // If threshold has been reached, kick has been detected
                            if (SensorData[0] >= Threshold){
                                
                                _ShowAndroidToastMessage("Kick Detected!");
                                KickDetected = true;
                            }
                        }

                    }

                }
                yield return null;
            }
        }



        // CONNECT METHOD (conencts to bluetooth)
        private void connect(){

            Debug.Log("Status : Trying To Connect");

            device.MacAddress = "98:D3:35:71:0B:15";

            device.setEndByte(10);

            device.ReadingCoroutine = ManageConnection;

            device.connect();

        }



        // DICONNECT METHOD
        public void disconnect(){

            if (device != null){
                Debug.Log("Status : Device Disconnected");
                device.close();
            }
        }



        //############### Handlers/Recievers #####################
        void HandleOnBluetoothStateChanged(bool isBtEnabled){

            if (isBtEnabled){
                connect();
                //We now don't need our recievers
                BluetoothAdapter.OnBluetoothStateChanged -= HandleOnBluetoothStateChanged;
                BluetoothAdapter.stopListenToBluetoothState();
            }
        }



        //This would mean a failure in connection! the reason might be that your remote device is OFF
        void HandleOnDeviceOff(BluetoothDevice dev){

            if (!string.IsNullOrEmpty(dev.Name)){
                Debug.Log("Status : can't connect to '" + dev.Name + "', device is OFF ");
            }
            else if (!string.IsNullOrEmpty(dev.MacAddress)){
                Debug.Log("Status : can't connect to '" + dev.MacAddress + "', device is OFF ");
            }
        }



        //Because connecting using the 'Name' property is just searching, the Plugin might not find it!.
        void HandleOnDeviceNotFound(BluetoothDevice dev){

            if (!string.IsNullOrEmpty(dev.Name)){

                Debug.Log("Status : Can't find a device with the name '" + dev.Name + "', device might be OFF or not paird yet ");
            }
        }



        //############### Deregister Events  #####################
        void OnDestroy(){

            BluetoothAdapter.OnDeviceOFF -= HandleOnDeviceOff;
            BluetoothAdapter.OnDeviceNotFound -= HandleOnDeviceNotFound;

        }



        /// <summary>
        /// Quit the application if there was a connection error for the ARCore session.
        /// </summary>
        private void _QuitOnConnectionErrors(){

            if (m_IsQuitting){
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.ConnectionState == SessionConnectionState.UserRejectedNeededPermission){

                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("DoQuit", 0.5f);
            }
            else if (Session.ConnectionState == SessionConnectionState.ConnectToServiceFailed){

                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("DoQuit", 0.5f);
            }
        }



        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void DoQuit(){
            Application.Quit();
        }



        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        private void _ShowAndroidToastMessage(string message){

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null){

                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");

                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>{

                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, 0);
                    toastObject.Call("show");
                }));
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        /// <summary>
        /// Sets the cameras parameters to side by side configuration.
        /// </summary>
        void SideBySidePerspectiveCameraConfig(){

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
