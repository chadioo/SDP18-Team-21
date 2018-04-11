using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class Scoreboard : MonoBehaviour {

    // IMPORT SCORE
    /*
    public GameObject scoreObj;
    private SoccerGameController originalScript;
    private int originalScore;
    */
    // SCORE LIST
    public Text Score1;
    public Text Score2;
    public Text Score3;
    public Text Score4;
    public Text Score5;
    public Text Score6;
    public Text Score7;
    public Text Score8;
    public Text Score9;
    public Text Score10;

    public Text Name1;
    public Text Name2;
    public Text Name3;
    public Text Name4;
    public Text Name5;
    public Text Name6;
    public Text Name7;
    public Text Name8;
    public Text Name9;
    public Text Name10;

    int newScore;
    int newRank;

    int[] scoreBoardScores = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    string[] scoreBoardNames = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
    //string[] scoreBoardNames = { "", "", "", "", "", "", "", "", "", "" };

    string newScorePath = "/ARKscore.txt";
    string scoreBoardPath = "/ARKscoreBoard.txt";

    bool gotHighScore = false;

    public InputField usernameInput;
    public static string username;


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    void Start() {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        calculateScoreBoard();
        initNames();
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    void Update() {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public void calculateScoreBoard()
    {
        System.IO.StreamReader reader;

        string line;

        // check if new score has been saved
        if (!File.Exists(Application.persistentDataPath + newScorePath))
        {
            Debug.Log("No score has been saved");
        }
        // retrieve new score and store in var
        else
        {
            Debug.Log("A score has been saved");
            reader = new System.IO.StreamReader(Application.persistentDataPath + newScorePath);
            while ((line = reader.ReadLine()) != null)
            {
                Debug.Log("ARK LOG ********** Saved Score: " + line);
                newScore = int.Parse(line);
            }
            reader.Close();
            Debug.Log("ARK LOG ********** Value of newScore: " + newScore);

            // check if scoreboard exists
            if (!File.Exists(Application.persistentDataPath + scoreBoardPath))
            {
                Debug.Log("No high scores have been saved");
                //File.Create(Application.persistentDataPath + scoreBoardPath);
                writeFromArrays();
                // scoreBoardScores[0] = newScore;
            }
            // retrieve old scores and store in array
            Debug.Log("A scoreboard has been saved");
            reader = new System.IO.StreamReader(Application.persistentDataPath + scoreBoardPath);
            int count = 0;
            while ((line = reader.ReadLine()) != null && count<20)
            {
                Debug.Log("ARK LOG ********** High Score "+count+": "+(count/2)+": "+(count%2)+": " + line);

                if (count % 2 == 0)
                {
                    scoreBoardNames[count / 2] = line;
                }
                else
                {
                    scoreBoardScores[count / 2] = int.Parse(line);  // might switch to try parse
                }
                count++;
                // need to include code for saving names strings
            }
            reader.Close();

            // insert new score into this list and sort
            sortList();

            //File.Delete(Application.persistentDataPath + scoreBoardPath); // delete old score file to pevent people from using previous score
            File.Delete(Application.persistentDataPath + newScorePath); // delete old score file to pevent people from using previous score
        }
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public void sortList() {
        for (int i=0; i<10; i++) {
            if (newScore >= scoreBoardScores[i])
            {
                Debug.Log("ARK LOG ********** Got A High Score!");
                newRank = i;
                Debug.Log("ARK LOG ********** Score ranking is: " + newRank);
                gotHighScore = true;
                break;
            }
        }
        if (gotHighScore) {
            for (int j = 9; j>newRank; j--)
            {
                Debug.Log("value "+ scoreBoardNames[j]+" of index " +j+" equals value "+ scoreBoardNames[j - 1] + " of index "+(j-1));
                scoreBoardScores[j] = scoreBoardScores[j-1];
                scoreBoardNames[j] = scoreBoardNames[j-1];
            }
            scoreBoardScores[newRank] = newScore;
            scoreBoardNames[newRank] = "Enter your name!";
            Debug.Log("ARK LOG ********** The score saved was: " + scoreBoardScores[newRank]);
            writeFromArrays();
        }
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public void writeFromArrays()
    {
        System.IO.StreamWriter writer;

        using (writer = new System.IO.StreamWriter(Application.persistentDataPath + scoreBoardPath, false))
        {
            for (int i = 0; i < 10; i++)
            {
                writer.WriteLine(scoreBoardNames[i]);
                writer.WriteLine(scoreBoardScores[i]);
                Debug.Log("writeFromArray index "+i+" name "+ scoreBoardNames[i] + " score"+ scoreBoardScores[i]);
            }
        }
        writer.Close();
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public void saveName() {
        username = usernameInput.text;
        Debug.Log("Call saveName: username "+username+" gotHighScore "+gotHighScore);
        if (gotHighScore)
        {
            scoreBoardNames[newRank] = username;
            initNames();
            writeFromArrays();
            Debug.Log("Saving name");
        }
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public void initNames()
    {
        Score1.text = scoreBoardScores[0].ToString();
        Score2.text = scoreBoardScores[1].ToString();
        Score3.text = scoreBoardScores[2].ToString();
        Score4.text = scoreBoardScores[3].ToString();
        Score5.text = scoreBoardScores[4].ToString();
        Score6.text = scoreBoardScores[5].ToString();
        Score7.text = scoreBoardScores[6].ToString();
        Score8.text = scoreBoardScores[7].ToString();
        Score9.text = scoreBoardScores[8].ToString();
        Score10.text = scoreBoardScores[9].ToString();

        Name1.text = scoreBoardNames[0];
        Name2.text = scoreBoardNames[1];
        Name3.text = scoreBoardNames[2];
        Name4.text = scoreBoardNames[3];
        Name5.text = scoreBoardNames[4];
        Name6.text = scoreBoardNames[5];
        Name7.text = scoreBoardNames[6];
        Name8.text = scoreBoardNames[7];
        Name9.text = scoreBoardNames[8];
        Name10.text = scoreBoardNames[9];
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public void SendEmail()
    {
        string email = "";

        string subject = MyEscapeURL("Your ARK Score");

        string body = MyEscapeURL("Congratulations!\r\nHere is your score from your most recent game.\r\nScore " + newScore + "\r\n" + "Thank you for playing!");

        Application.OpenURL("mailto:" + email + "?subject=" + subject + "&body=" + body);

    }

    public string MyEscapeURL(string url)
    {
        return WWW.EscapeURL(url).Replace("+", "%20");
    }


}
