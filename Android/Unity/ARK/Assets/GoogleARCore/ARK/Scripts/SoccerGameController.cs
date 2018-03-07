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
    using System.IO;
    using System.Collections.Generic;
    using System.Collections;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.UI;
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
        private bool Bluetooth = false;         // determines if bluetooth implemented

        // FLOATS
        private float Threshold = 2f;           // acceleration threshold for determining kick
        //private float speed = 1f;               // speed of ball (not currently used)
        private float[] SensorData;             // array used to store sensor data from one input line

        // INTEGERS
        private int score = 0;

        // BLUETOOTH DEVICE
        private BluetoothDevice device;         // bluetooth device

        // TEXT BOXE
        public Text message;


        //
        // METHODS FOR HANDLING BLUETOOTH
        //

        // AWAKE METHOD (first method, initializes bluetooth)
        void Awake(){
            SideBySidePerspectiveCameraConfig();
            message.text = "Click Devices Button";
            BluetoothAdapter.askEnableBluetooth();                   //Ask user to enable Bluetooth
            BluetoothAdapter.OnDeviceOFF += HandleOnDeviceOff;
            BluetoothAdapter.OnDevicePicked += HandleOnDevicePicked; //To get what device the user picked out of the devices list
        }

        void HandleOnDeviceOff(BluetoothDevice dev){                // Bluetooth Handler
            if (!string.IsNullOrEmpty(dev.Name)){
                message.text = "Can't connect to " + dev.Name + ", device is OFF";
                Debug.Log("Can't connect to " + dev.Name + ", device is OFF");
            }
            else if (!string.IsNullOrEmpty(dev.Name)){
                message.text = "Can't connect to " + dev.MacAddress + ", device is OFF";
                Debug.Log("Can't connect to " + dev.MacAddress + ", device is OFF");
            }
        }


        //############### UI BUTTONS RELATED METHODS #####################
        public void showDevices() {
            BluetoothAdapter.showDevices();//show a list of all devices//any picked device will be sent to this.HandleOnDevicePicked()
        }


        public void connect(){ //Connect to the public global variable "device" if it's not null.
        
            if (device != null){
                message.text = "Connecting to device";
                device.connect();
            }
            message.text = "Connection successful";
            Bluetooth = true;
        }


        public void disconnect(){ //Disconnect the public global variable "device" if it's not null.
        
            if (device != null) {
                message.text = "Connection terminated";
                device.close();
                Bluetooth = false;
            }
        }


        void HandleOnDevicePicked(BluetoothDevice device){//Called when device is Picked by user
        
            this.device = device;//save a global reference to the device

            device.setEndByte(10);

            device.ReadingCoroutine = ManageConnection;

            Debug.Log("Decvice Name: "+device.Name);

            message.text = "Click Connect Button";
        }

        void OnDestroy(){
            BluetoothAdapter.OnDevicePicked -= HandleOnDevicePicked;
            BluetoothAdapter.OnDeviceOFF -= HandleOnDeviceOff;
        }

        // GAME FUNCTIONALITY



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

            if (!FoundPlane & Bluetooth){   // If you have not found plane, search for plane

                Frame.GetPlanes(m_NewPlanes, TrackableQueryFilter.New); // See if new plane exists

                if (m_NewPlanes.Count > 0){ // See if new plane exists

                    PlaneVector = m_NewPlanes[0].Position;
                    FoundPlane = true;
                }
            }

            if (FoundPlane && !SpawnGoal && ! SpawnBall){ // If plane is found and objects have not been instantiated

                // Set spawn location to be on plane certain distance in front of camera
                SoccerFieldVector = new Vector3(0, PlaneVector.y, 15);          // field vector is everywhere
                SoccerBallVector = new Vector3(0, PlaneVector.y+0.10f, 1);      // ball is 1 unit of distance forward
                SoccerGoalVector = new Vector3(0, PlaneVector.y, 30);           // goal is 15 units of distance forward, lower height to rest on plane

                // Spawn Objects
                Instantiate(SoccerFieldInput, SoccerFieldVector, Quaternion.identity);
                SoccerBallRigidbody = Instantiate(SoccerBallInput, SoccerBallVector, Quaternion.identity) as Rigidbody;
                Instantiate(SoccerGoalInput, SoccerGoalVector, Quaternion.Euler(270, 270, 180));

                Debug.Log("ARK LOG ********** Spawn ball and goal and field.");

                message.text = "Score: " + score;

                SpawnGoal = true;
                SpawnBall = true;
            }
            
            if (FoundPlane && SpawnGoal && !SpawnBall){ // If ball needs to be respawn

                // Set spawn location to be on plane certain distance in front of camera
                SoccerBallVector = new Vector3(0, PlaneVector.y + 1, 1);     // ball is 1 unit of distance forward

                // Spawn Objects
                SoccerBallRigidbody = Instantiate(SoccerBallInput, SoccerBallVector, Quaternion.identity) as Rigidbody;

                Debug.Log("ARK LOG ********** Respawn ball.");

                message.text = "Score: " + score;

                SpawnBall = true;
            }

        } // end of Update


        
        // FIXED UPDATE METHOD
        void FixedUpdate() {
            
            if (FoundPlane && SpawnBall && SpawnGoal && KickDetected){

                //Debug.Log("Soccer ball position: x " + SoccerBallRigidbody.position.x + " y " + SoccerBallRigidbody.position.y + " z " + SoccerBallRigidbody.position.z);

                if (SensorData.Length >= 10) {

                    //_ShowAndroidToastMessage("Move");
                    // Determines ball movement
                    float xAcc = SensorData[2];     // sensor x axis (in current orientation) is unity y axis
                    float yAcc = SensorData[0];     // sensor x axis (in current orientation) is unity y axis
                    float zAcc = SensorData[4];     // sensor z axis (in current orientation) is unity z axis

                    Vector3 acc = new Vector3(xAcc, yAcc, zAcc);

                    float xAng = SensorData[8];     // sensor x axis (in current orientation) is unity y axis
                    float yAng = SensorData[6];     // sensor x axis (in current orientation) is unity y axis
                    float zAng = SensorData[10];     // sensor z axis (in current orientation) is unity z axis

                    Vector3 angle = new Vector3(xAng, yAng, zAng);

                    Vector3 force = new Vector3(xAcc * SoccerBallRigidbody.mass, yAcc * SoccerBallRigidbody.mass, zAcc * SoccerBallRigidbody.mass);

                    Debug.Log("Acceleration: "+acc+" Anglular Velocity: "+angle+" Force: "+ force);

                    SoccerBallRigidbody.AddForce(force);

                    if (SoccerBallRigidbody.position.x > 15 | SoccerBallRigidbody.position.x < -15 | SoccerBallRigidbody.position.z < -5) {
                        Destroy(SoccerBallRigidbody, 1.0f);// remove object
                        Debug.Log("Destroyed ball oob");
                        _ShowAndroidToastMessage("Out of Bounds");
                        SpawnBall = false;
                        KickDetected = false;
                    }
                    else if (SoccerBallRigidbody.position.x < 5 && SoccerBallRigidbody.position.x > -5 && SoccerBallRigidbody.position.z > 30 && SoccerBallRigidbody.position.y < 10) {
                        Destroy(SoccerBallRigidbody, 1.0f);// remove object
                        Debug.Log("Destroyed ball goal");
                        score++;    // increase score
                        _ShowAndroidToastMessage("Score: " + score);
                        SpawnBall = false;
                        KickDetected = false;
                    }
                    else if (SoccerBallRigidbody.position.z > 30) {
                        Destroy(SoccerBallRigidbody, 1.0f);// remove object
                        Debug.Log("Destroyed ball oob");
                        _ShowAndroidToastMessage("Out of Bounds");
                        SpawnBall = false;
                        KickDetected = false;
                    }
                }
            }

        }



        // READING DATA
        IEnumerator ManageConnection(BluetoothDevice device) {
            
            //Debug.Log("ARK LOG ********** ManageConnection: Device is reading: " + device.IsReading + " Data is available: " + device.IsDataAvailable);

            while (device.IsReading){

                if (device.IsDataAvailable){

                    byte[] msg = device.read();
                    string content = "";
                    string[] subStrings;

                    if (msg != null && msg.Length > 0){

                        content = System.Text.ASCIIEncoding.ASCII.GetString(msg);
                        Debug.Log("ARK LOG ********** Content: " + content);

                        content = content.Replace(",", "");     // Remove commas
                        subStrings = content.Split(' ');        // Split up by spaces

                        sensorSave(content);

                        SensorData = new float[subStrings.Length];

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

                        if (FoundPlane && SpawnBall && SpawnGoal && !KickDetected){ // If no kick has been detected and data exists, check if threshold has been reached 
                            
                            if (SensorData[0] >= Threshold){ // If threshold has been reached, kick has been detected
                                Debug.Log("Kick Detected!");
                                _ShowAndroidToastMessage("Kick Detected!");
                                KickDetected = true;
                            }
                        }
                    }
                }
                yield return null;
            }
        }



        //############### Deregister Events  #####################


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




        int dataCount = 0;

		public void sensorSave(string data){
			System.IO.StreamWriter writer;
			System.IO.StreamReader reader;
			string line;
            //Debug.Log("ARK LOG ********** sensorSave");
            if (!File.Exists(Application.persistentDataPath + "/sensorCache.txt")){
				FileStream fileStr = File.Create(Application.persistentDataPath + "/sensorCache.txt");
                //Debug.Log("ARK LOG ********** Create File");
            }
			if (dataCount < 10) {
                //Debug.Log("ARK LOG ********** Write Data");
                using (writer = new System.IO.StreamWriter (Application.persistentDataPath + "/sensorCache.txt", true)) {
					writer.WriteLine (data);
                    dataCount++;
				}
			}
			else {
                //Debug.Log("ARK LOG ********** Read Data");
                reader = new System.IO.StreamReader(Application.persistentDataPath + "/sensorCache.txt");  
				while((line = reader.ReadLine()) != null) {  
					Debug.Log("ARK LOG ********** Data: " + line);
				}  
				reader.Close();
                dataCount = 0;
				using (writer = new System.IO.StreamWriter (Application.persistentDataPath + "/sensorCache.txt", false)) {
					writer.WriteLine (data);
				}
			}
		}



    }
}
