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

    // IMPORTS
    using System.Linq;
    using System.IO;
    using System.Collections.Generic;
    using System.Collections;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Rendering;
    using TechTweaking.Bluetooth;

    public class SoccerGameController : MonoBehaviour
    {

        // CAMERAS
        public Camera FirstPersonCamera;        // main camera
        public Camera camLeft;                  // left camera for side by side
        public Camera camRight;                 // left camera for side by side

        // GAMEOBJECTS
        public GameObject SoccerGoalInput;     // soccer goal input
        public GameObject SoccerFieldInput;    // soccer field input
        public GameObject ScoreProfile;

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
        private bool KickExecuted = false;      // determines if kick detected
        private bool Bluetooth = false;         // determines if bluetooth implemented
        private bool BluetoothInit = false;     // determines if bluetooth connection successful
        private bool LeftFoot = false;          // determines if on left or right foot

        // FLOATS
        float xAcc, yAcc, zAcc, xAng, yAng, zAng;

        // FLOAT ARRAYS
        private float[] StraightLow =   { 1.50f, 0.75f, -0.50f };  // acceleration threshold for determining kick  1.50f, 1.40f, 0.30f 
        private float[] StraightHigh =  { 2.00f, 1.50f, 0.75f };  // acceleration threshold for determining kick  1.50f, 1.40f, 0.30f 
        private float[] AngleLow =      { 1.40f, 0.6f, 0.15f };     // acceleration threshold for determining kick  1.40f, 1.28f, 0.50f 
        private float[] AngleHigh =     { 2.00f, 1.50f, 0.75f };     // acceleration threshold for determining kick  1.40f, 1.28f, 0.50f 
        private float[] NinetyLow =     { 1.50f, 0.00f, 0.50f };    // acceleration threshold for determining kick  10f, 10f, 10f 
        private float[] NinetyHigh =    { 2.00f, 1.00f, 1.50f };    // acceleration threshold for determining kick  10f, 10f, 10f 
        private float[] DigLow =        { 10f, 10f, 10f };       // acceleration threshold for determining kick  10f, 10f, 10f 
        private float[] DigHigh =       { 10f, 10f, 10f };       // acceleration threshold for determining kick  10f, 10f, 10f 
        private float[] SensorData;                         // array used to store sensor data from one input line

        private float[] xAvg = { 0f, 0f, 0f, 0f, 0f};    // moving average for x
        private float[] xTemp = { 0f, 0f, 0f, 0f};    // for copying purposes
        private float[] yAvg = { 0f, 0f, 0f, 0f, 0f};    // moving average for z
        private float[] yTemp = { 0f, 0f, 0f, 0f};    // for copying purposes
        private float[] zAvg = { 0f, 0f, 0f, 0f, 0f};    // moving average for y
        private float[] zTemp = { 0f, 0f, 0f, 0f};    // for copying purposes

        // INTEGERS
        private int score = 0;
        private int kickType = 1;

        // BLUETOOTH DEVICE
        private BluetoothDevice device;         // bluetooth device

        // TEXT BOXES
        public Text message;
        public Text scoreText;
        public Text countText;
        public Text avgX_display;
        public Text avgY_display;
        public Text avgZ_display;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // FIRST METHOD TO RUN, INITIALIZES BLUETOOTH FUNCTIONALITY

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            SideBySidePerspectiveCameraConfig();
            message.text = "Click Devices Button";
            BluetoothAdapter.askEnableBluetooth();                   //Ask user to enable Bluetooth
            BluetoothAdapter.OnDeviceOFF += HandleOnDeviceOff;
            BluetoothAdapter.OnDevicePicked += HandleOnDevicePicked; //To get what device the user picked out of the devices list
            showDevices();
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // BLUETOOTH CONNECTION HANDLERS


        void HandleOnDeviceOff(BluetoothDevice dev)
        {                // Bluetooth Handler
            if (!string.IsNullOrEmpty(dev.Name))
            {
                message.text = "Can't connect to " + dev.Name + ", device is OFF";
                Debug.Log("Can't connect to " + dev.Name + ", device is OFF");
            }
            else if (!string.IsNullOrEmpty(dev.Name))
            {
                message.text = "Can't connect to " + dev.MacAddress + ", device is OFF";
                Debug.Log("Can't connect to " + dev.MacAddress + ", device is OFF");
            }
        }


        public void showDevices()
        {
            BluetoothAdapter.showDevices(); //show a list of all devices//any picked device will be sent to this.HandleOnDevicePicked()
        }


        public void connect() //Connect to the public global variable "device" if it's not null.
        {
            if (device != null)
            {
                message.text = "Connecting to device";
                device.connect();
            }
            message.text = "Connection initializing";
            Bluetooth = true;
        }


        public void disconnect() //Disconnect the public global variable "device" if it's not null.
        {
            if (device != null)
            {
                message.text = "Connection terminated";
                device.close();
                Bluetooth = false;
                BluetoothInit = false;
            }
        }


        void HandleOnDevicePicked(BluetoothDevice device) // Called when device is Picked by user
        {
            this.device = device; //save a global reference to the device
            device.setEndByte(10);
            device.ReadingCoroutine = ManageConnection;
            Debug.Log("Decvice Name: " + device.Name);
            message.text = "Click Connect Button";
        }


        void OnDestroy()
        {
            BluetoothAdapter.OnDevicePicked -= HandleOnDevicePicked;
            BluetoothAdapter.OnDeviceOFF -= HandleOnDeviceOff;
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // UPDATE IF PLANE IS FOUND, SPAWNING BALL AND NET


        public void Update()
        {
            if (BluetoothInit == false) {
                connect();
            }
            _QuitOnConnectionErrors();

            if (Frame.TrackingState != TrackingState.Tracking) // Check that motion tracking is tracking.
            {

                const int lostTrackingSleepTimeout = 15;
                Screen.sleepTimeout = lostTrackingSleepTimeout;
                return;
            }

            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (!FoundPlane & Bluetooth) // If you have not found plane, search for plane
            {   
                Frame.GetPlanes(m_NewPlanes, TrackableQueryFilter.New); // See if new plane exists

                if (m_NewPlanes.Count > 0) // See if new plane exists
                { 
                    PlaneVector = m_NewPlanes[0].Position;
                    FoundPlane = true;
                }
            }
            if (FoundPlane && !SpawnGoal && !SpawnBall) // If plane is found and objects have not been instantiated
            { 
                // NOTE: UNITY AXIS X = left / right, Y = up/down, Z = forwards/backwards
                // Set spawn location to be on plane certain distance in front of camera
                SoccerFieldVector = new Vector3(0, PlaneVector.y, 15);          // field vector is everywhere
                SoccerBallVector = new Vector3(0, PlaneVector.y + 0.10f, 1);    // ball is 1 unit of distance forward
                SoccerGoalVector = new Vector3(0, PlaneVector.y, 6);            // goal is 15 units of distance forward, lower height to rest on plane

                // Spawn Objects
                Instantiate(SoccerFieldInput, SoccerFieldVector, Quaternion.identity);
                SoccerBallRigidbody = Instantiate(SoccerBallInput, SoccerBallVector, Quaternion.identity) as Rigidbody;
                Instantiate(SoccerGoalInput, SoccerGoalVector, Quaternion.Euler(270, 270, 180));

                Debug.Log("ARK LOG ********** Spawn ball and goal and field.");

                message.text = "Score: " + score;
                scoreText.text = "Score: "+score.ToString();

                SpawnGoal = true;
                SpawnBall = true;
                StartCountdown();
            }
            if (FoundPlane && SpawnGoal && !SpawnBall) // If ball needs to be respawn
            {
                // Set spawn location to be on plane certain distance in front of camera
                SoccerBallVector = new Vector3(0, PlaneVector.y + 1, 1);     // ball is 1 unit of distance forward

                // Spawn Objects
                SoccerBallRigidbody = Instantiate(SoccerBallInput, SoccerBallVector, Quaternion.identity) as Rigidbody;
                Debug.Log("ARK LOG ********** Respawn ball.");
                message.text = "Score: " + score;
                scoreText.text = "Score: " + score.ToString();
                SpawnBall = true;
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // UPDATING PHYSICS INCLUDING BALL MOVEMENT AND IF BALL IS OUT OF BOUNDS OR IN GOAL

        
        void FixedUpdate()
        {
            if (FoundPlane && SpawnBall && SpawnGoal && KickDetected && !KickExecuted)
            {
                if (kickType == 1) // 90 degree kick has flipped coordinates
                {
                    Vector3 acc = new Vector3(-zAcc * 50 * 1.1f, yAcc * 50 * 1.1f, xAcc * 50 * 1.1f);
                    SoccerBallRigidbody.AddForce(acc);
                    KickExecuted = true;
                }
                else if (kickType == 2) // angle
                {
                    Vector3 acc = new Vector3(-xAcc * 50 * 1.2f, yAcc * 50 * 1.2f, zAcc * 50 * 1.2f);
                    SoccerBallRigidbody.AddForce(acc);
                    KickExecuted = true;
                }
                else if (kickType == 3) // straight
                {
                    Vector3 acc = new Vector3(-xAcc * 50 * 1.3f, yAcc * 50 * 1.3f, zAcc * 50 * 1.3f);
                    SoccerBallRigidbody.AddForce(acc);
                    KickExecuted = true;
                }
            }

            if (FoundPlane && SpawnBall && SpawnGoal && KickDetected && KickExecuted)
            {
                if (SoccerBallRigidbody.position.x > 6 | SoccerBallRigidbody.position.x < -6 | SoccerBallRigidbody.position.z < -2 | SoccerBallRigidbody.position.z > 6.5)
                {
                    Destroy(SoccerBallRigidbody);// remove object
                    Debug.Log("Destroyed ball oob");
                    _ShowAndroidToastMessage("Out of Bounds");
                    SpawnBall = false;
                    KickDetected = false;
                    KickExecuted = false;
                }
                else if (SoccerBallRigidbody.position.x < 1.49 && SoccerBallRigidbody.position.x > -1.49 && SoccerBallRigidbody.position.z > 6 && SoccerBallRigidbody.position.y < 1)
                {
                    Destroy(SoccerBallRigidbody);// remove object
                    Debug.Log("Destroyed ball goal");
                    score++;    // increase score
                    SpawnBall = false;
                    KickDetected = false;
                    KickExecuted = false;
                }
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // READING DATA FROM THE SENSOR AND DETERMINING THRESHOLD


        int connectionLostCount = 0;
        IEnumerator ManageConnection(BluetoothDevice device)
        {
            while (device.IsReading)
            {
                if (device.IsDataAvailable)
                {
                    if (BluetoothInit == false)
                    {
                        message.text = "Bluetooth successful, receiving data";
                        BluetoothInit = true;
                    }

                    byte[] msg = device.read();
                    string content = "";
                    string[] subStrings;
                    connectionLostCount = 0;

                    if (msg != null && msg.Length > 0)
                    {
                        content = System.Text.ASCIIEncoding.ASCII.GetString(msg);
                        //Debug.Log("ARK LOG ********** Content: " + content);

                        content = content.Replace(",", "");     // Remove commas
                        subStrings = content.Split(' ');        // Split up by spaces

                        //sensorSave(content);

                        SensorData = new float[subStrings.Length];
                        //Debug.Log("subStrings: "+ (string.Join(",", subStrings)));
                        for (int i = 0; i < subStrings.Length; i++)
                        {
                            //Debug.Log("Substring: "+ subStrings[i] + " i:"+i);
                            float temp;
                            if(float.TryParse(subStrings[i], out temp))
                            {
                                SensorData[i] = temp;
                                //Debug.Log("Parsed Value " + temp + " at " + i);
                            }

                        }
                        if (SensorData.Length >= 5)
                        {
                            System.Array.Copy(xAvg, 1, xTemp, 0, 4);
                            System.Array.Copy(yAvg, 1, yTemp, 0, 4);
                            System.Array.Copy(zAvg, 1, zTemp, 0, 4);

                            System.Array.Copy(xTemp, xAvg, 4);
                            System.Array.Copy(yTemp, yAvg, 4);
                            System.Array.Copy(zTemp, zAvg, 4);

                            if (LeftFoot)
                            {
                                xAvg[4] = -SensorData[4];    // sensor x axis (in current orientation) is unity y axis
                                yAvg[4] = SensorData[0];     // sensor x axis (in current orientation) is unity y axis
                                zAvg[4] = SensorData[2];     // sensor z axis (in current orientation) is unity z axis
                            }

                            else
                            {
                                xAvg[4] = SensorData[4];     // sensor x axis (in current orientation) is unity y axis
                                yAvg[4] = SensorData[0];     // sensor x axis (in current orientation) is unity y axis
                                zAvg[4] = SensorData[2];     // sensor z axis (in current orientation) is unity z axis

                                //xAng = SensorData[8];     // sensor x axis (in current orientation) is unity y axis
                                //yAng = SensorData[6];     // sensor x axis (in current orientation) is unity y axis
                                //zAng = SensorData[10];     // sensor z axis (in current orientation) is unity z axis
                            }

                            xAcc = xAvg.Sum() / 5;
                            yAcc = yAvg.Sum() / 5;
                            zAcc = zAvg.Sum() / 5;
                            //Debug.Log("ARK LOG ********** Content: " +yAcc+" "+zAcc+" "+xAcc);
                            avgX_display.text = string.Join(" \t ", xAvg);
                            avgY_display.text = string.Join(" \t ", yAvg);
                            avgZ_display.text = string.Join(" \t ", zAvg);
                        }

                        if (FoundPlane && SpawnBall && SpawnGoal && !KickDetected)
                        { // If no kick has been detected and data exists, check if threshold has been reached 
                            // NOTE: ACCEL AXIS     X = up/down,        Y = forwards/backwards, Z = left/right
                            // NOTE: UNITY AXIS     X = left / right,   Y = up/down,            Z = forwards/backwards
                            if (zAcc >= StraightLow[1] && zAcc <= StraightHigh[1] && xAcc >= StraightLow[2] && xAcc <= StraightHigh[2] && yAcc >= StraightLow[0] && yAcc <= StraightHigh[0])
                            {
                                Debug.Log("Straight Kick Detected!");
                                _ShowAndroidToastMessage("Straight Kick Detected!");
                                kickType = 3;
                                KickDetected = true;
                            }
                            else if (zAcc >= AngleLow[1] && zAcc <= AngleHigh[1] && xAcc >= AngleLow[2] && xAcc <= AngleHigh[2] && yAcc >= AngleLow[0] && yAcc <= AngleHigh[0])
                            {
                                Debug.Log("Angle Kick Detected!");
                                _ShowAndroidToastMessage("Angle Kick Detected!");
                                kickType = 2;
                                KickDetected = true;
                            }
                            else if (zAcc >= NinetyLow[1] && zAcc <= NinetyHigh[1] && xAcc >= NinetyLow[2] && xAcc <= NinetyHigh[2] && yAcc >= NinetyLow[0] && yAcc <= NinetyHigh[0])
                            {
                                Debug.Log("Ninety Kick Detected!");
                                _ShowAndroidToastMessage("Ninety Kick Detected!");
                                kickType = 1;
                                KickDetected = true;
                            }
                            else if (false)
                            {
                                Debug.Log("Dig Kick Detected!");
                                _ShowAndroidToastMessage("Dig Kick Detected!");
                                KickDetected = true;
                            }
                        }
                    }
                }
                else // if no data is returned, make sure it is abnormal amount, then display error message
                {
                    connectionLostCount++;
                    if (connectionLostCount > 5) {
                        message.text = "Connection Lost";
                        Debug.Log("Connection Lost");
                        BluetoothInit = false;
                    }
                }
                yield return null;
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // SWAP LEFT / RIGHT FOOT COORDINATES
            

        public void SwapFoot()
        {
            LeftFoot = !LeftFoot;
            if (LeftFoot)
            {
                Debug.Log("Left Foot Mode");
                message.text = "Left Foot Mode";
                _ShowAndroidToastMessage("Left Foot Mode");
            }
            else {
                Debug.Log("Right Foot Mode");
                message.text = "Right Foot Mode";
                _ShowAndroidToastMessage("Right Foot Mode");
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // COUNTDOWN USED TO TIME GAME LENGTH (initiated when arena spawned)


        private int duration = 90;   // units are seconds
        private int timeRemaining;
        private bool isCountingDown = false;


        public void StartCountdown()
        {
            if (!isCountingDown)
            {
                message.text = "Start Count";
                isCountingDown = true;
                timeRemaining = duration;
                Invoke("_tick", 1f);
            }
        }

        private void _tick()
        {
            timeRemaining--;
            if (timeRemaining > 0)
            {
                message.text = timeRemaining.ToString();
                countText.text = timeRemaining.ToString();
                Invoke("_tick", 1f);
            }
            else
            {
                saveScore(score.ToString());
                message.text = "Press Me!";
                countText.text = "Press Me!";
                isCountingDown = false;
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // DEMO USED TO VERIFY FUNCTIONING OF APP WITHOUT HARDWARE


        public void demo()
        {
            if (FoundPlane && SpawnBall && SpawnGoal && !KickDetected && !KickExecuted)
            {
                KickDetected = true;
                Vector3 demo = new Vector3(0, 50, 100);
                SoccerBallRigidbody.AddForce(demo);
                KickExecuted = true;
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ERROR HANDLERS

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


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // FOR DISPLAYING ANDROID TOAST MESSAGES


        private void _ShowAndroidToastMessage(string message)
        {

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {

                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");

                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {

                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, 0);
                    toastObject.Call("show");
                }));
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // SIDE BY SIDE CAMERA FOR DEPTH


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
        // SAVE SCORE


        public void saveScore(string data)
        {
            System.IO.StreamWriter writer;
            string filePath = "/ARKscore.txt";

            if (!File.Exists(Application.persistentDataPath + filePath))
            {
                Debug.Log("No score previously saved, saving new score");
                //FileStream fileStr = File.Create(Application.persistentDataPath + filePath);

                using (writer = new System.IO.StreamWriter(Application.persistentDataPath + filePath, false))
                {
                    Debug.Log("Saving Data: "+data);
                    writer.WriteLine(data);
                }
                writer.Close();
            }
            else
            {
                Debug.Log("Score previously saved, saving new score");
                using (writer = new System.IO.StreamWriter(Application.persistentDataPath + filePath, false))
                {
                    writer.WriteLine(data);
                }
                writer.Close();
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // SAVE DATA TO FILE FOR USE IN DATA AVERAGING


        int dataCount = 0;

        public void sensorSave(string data)
        {
            System.IO.StreamWriter writer;
            System.IO.StreamReader reader;
            string line;
            //Debug.Log("ARK LOG ********** sensorSave");
            if (!File.Exists(Application.persistentDataPath + "/sensorCache.txt"))
            {
                //FileStream fileStr = File.Create(Application.persistentDataPath + "/sensorCache.txt");
                //Debug.Log("ARK LOG ********** Create File");
            }
            if (dataCount < 10)
            {
                //Debug.Log("ARK LOG ********** Write Data");
                using (writer = new System.IO.StreamWriter(Application.persistentDataPath + "/sensorCache.txt", true))
                {
                    writer.WriteLine(data);
                    dataCount++;
                }
                writer.Close();
            }
            else
            {
                //Debug.Log("ARK LOG ********** Read Data");
                reader = new System.IO.StreamReader(Application.persistentDataPath + "/sensorCache.txt");
                while ((line = reader.ReadLine()) != null)
                {
                    //Debug.Log("ARK LOG ********** Data: " + line);
                }
                reader.Close();
                dataCount = 0;
                using (writer = new System.IO.StreamWriter(Application.persistentDataPath + "/sensorCache.txt", false))
                {
                    writer.WriteLine(data);
                }
                writer.Close();
            }
        }



    }

}