using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MailSender : MonoBehaviour {

    public void SendEmail() {

        string email = "";

        string subject = MyEscapeURL("Your ARK Score");

        string body = MyEscapeURL("Congratulations!\r\nHere is your score from your most recent game.\r\nScore "+"\r\n"+"Thank you for playing!");

        Application.OpenURL("mailto:" + email + "?subject=" + subject + "&body=" + body);

    }

    public string MyEscapeURL(string url)
    {
        return WWW.EscapeURL(url).Replace("+", "%20");
    }
}
