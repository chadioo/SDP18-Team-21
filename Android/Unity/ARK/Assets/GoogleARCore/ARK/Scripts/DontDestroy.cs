using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestroy : MonoBehaviour {

    private static bool created = false;

    void Awake()
    {
        if (!created)
        {
            DontDestroyOnLoad(this.gameObject);
            created = true;
            Debug.Log("Awake: " + this.gameObject);
        }
    }
    /*
    public void LoadScene()
    {
        if (SceneManager.GetActiveScene().name == "Soccer Game")
        {
            SceneManager.LoadScene("End Game", LoadSceneMode.Single);
        }
    }
    */
}
