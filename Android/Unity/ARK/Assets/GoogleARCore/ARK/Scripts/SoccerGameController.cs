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
        /// A list to hold all planes ARCore is tracking in the current frame. This object is used across
        /// the application to avoid per-frame allocations.
        /// </summary>
        private List<TrackedPlane> m_AllPlanes = new List<TrackedPlane>();

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

        void Start()
        {
            SoccerBallRigidbody = SoccerBallPrefab.GetComponent<Rigidbody>();
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

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
            if (!FoundPlane) {

                // See if new plane exists
                Frame.GetPlanes(m_NewPlanes, TrackableQueryFilter.New);

                // If there is a new plane, stop searching
                if (m_NewPlanes.Count > 0) {
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
            if (FoundPlane && !SpawnObjects) {

                // Saple plane location
                //Vector3 PlaneVector = m_NewPlanes[0].Position;

                // Set spawn location to be on plane certain distance in front of camera
                SoccerBallVector = new Vector3(0, PlaneVector.y, 1);      // ball is 1 unit of distance forward
                SoccerGoalVector = new Vector3(0, PlaneVector.y, 10);     // goal is 10 units of distance forward

                // Spawn Objects
                Instantiate(SoccerBallPrefab, SoccerBallVector, Quaternion.identity);
                Instantiate(SoccerGoalPrefab, SoccerGoalVector, Quaternion.identity);

                SpawnObjects = true;
            }

            // If objects have been spawn, move soccer ball
            if (FoundPlane && SpawnObjects) {

                // Determines ball movement
                float moveHorizontal = Input.GetAxis("Horizontal");
                float moveVertical = Input.GetAxis("Vertical");

                Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

                // Applies force to rigidbody, makes movement
                SoccerBallRigidbody.AddForce(movement * speed);
            }

/*
            // Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
            Frame.GetPlanes(m_NewPlanes, TrackableQueryFilter.New);
            for (int i = 0; i < m_NewPlanes.Count; i++)
            {
                // Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
                // the origin with an identity rotation since the mesh for our prefab is updated in Unity World
                // coordinates.
                GameObject planeObject = Instantiate(TrackedPlanePrefab, Vector3.zero, Quaternion.identity,
                    transform);
                planeObject.GetComponent<TrackedPlaneVisualizer>().Initialize(m_NewPlanes[i]);
            }
*/
/*            
            // Disable the snackbar UI when no planes are valid.
            Frame.GetPlanes(m_AllPlanes);
            bool showSearchingUI = true;
            for (int i = 0; i < m_AllPlanes.Count; i++)
            {
                if (m_AllPlanes[i].TrackingState == TrackingState.Tracking)
                {
                    showSearchingUI = false;
                    break;
                }
            }
*/
/*
            SearchingForPlaneUI.SetActive(showSearchingUI);

            // If the player has not touched the screen, we are done with this update.
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }
*/
/*
            // Raycast against the location the player touched to search for planes.
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinBounds | TrackableHitFlags.PlaneWithinPolygon;

            if (Session.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
            {
                var andyObject = Instantiate(AndyAndroidPrefab, hit.Pose.position, hit.Pose.rotation);

                // Create an anchor to allow ARCore to track the hitpoint as understanding of the physical
                // world evolves.
                var anchor = hit.Trackable.CreateAnchor(hit.Pose);

                // Andy should look at the camera but still be flush with the plane.
                andyObject.transform.LookAt(FirstPersonCamera.transform);
                andyObject.transform.rotation = Quaternion.Euler(0.0f,
                    andyObject.transform.rotation.eulerAngles.y, andyObject.transform.rotation.z);

                // Make Andy model a child of the anchor.
                andyObject.transform.parent = anchor.transform;
            }
*/
        } // end of Update




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
    }
}
