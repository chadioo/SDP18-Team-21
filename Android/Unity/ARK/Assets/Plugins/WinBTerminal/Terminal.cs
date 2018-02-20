using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System;
using System.ComponentModel;
using HoV;
using System.Threading;

class DeviceFinderHelper
{
    public string transferedText;
    public bool performFastFind;
    public bool performLoopFind;
}
class DeviceConnectionHelper
{
    public string status;
    public bool receiveHexData;
}
class DataCommunicationHelper
{
    public string dataToSend;
    public string receivedData;
}
class ByteDataCommunicationHelper
{
    public byte[] dataToSend;
    public byte[] receivedData;
    public int maxLength;
}

public class Terminal : MonoBehaviour
{

    public Text txtAnswer;
    public Text txtInput;
    public Text txtHexInput;
    public Dropdown ddDevices;

    private bool isBTDevicesSearching = false;
    private int maxByteLength = 81920;
    private UnityBackgroundWorker ubwDeviceFinder;
    private UnityBackgroundWorker ubwDeviceConnector;
    private UnityBackgroundWorker ubwDataSenderAndReceiver;
    private UnityBackgroundWorker ubwDataSender;
    private UnityBackgroundWorker ubwByteDataSender;
    private UnityBackgroundWorker ubwDataReceiver;
    private UnityBackgroundWorker ubwByteDataReceiver;

    private DeviceFinderHelper deviceFinderHelper;
    private DeviceConnectionHelper deviceConnectionHelper;
    private DataCommunicationHelper dataSenderAndReceiverHelper;
    private DataCommunicationHelper dataSenderHelper;
    private DataCommunicationHelper dataReceiverHelper;
    private ByteDataCommunicationHelper byteDataSenderHelper;
    private ByteDataCommunicationHelper byteDataReceiverHelper;

    [DllImport("BTManagerLibrary")]
    private static extern IntPtr BTM_GetDevicesNames();

    [DllImport("BTManagerLibrary")]
    private static extern IntPtr BTM_GetDevicesNamesFast();

    [DllImport("BTManagerLibrary")]
    private static extern IntPtr BTM_SendAndReceiveDataFast(string data);

    [DllImport("BTManagerLibrary")]
    private static extern IntPtr BTM_ReceiveDataFast(string data);

    [DllImport("BTManagerLibrary")]
    private static extern IntPtr BTM_SendDataFast(string data);

    [DllImport("BTManagerLibrary")]
    private static extern int BTM_ReceiveByteDataFast(byte[] bytes, int size);

    [DllImport("BTManagerLibrary")]
    private static extern IntPtr BTM_SendByteDataFast(IntPtr data, int size);

    [DllImport("BTManagerLibrary")]
    private static extern IntPtr BTM_ConnectToDevice(string data);

    [DllImport("BTManagerLibrary")]
    private static extern bool BTM_IsConnected();

    [DllImport("BTManagerLibrary")]
    private static extern bool BTM_IsBusy();

    [DllImport("BTManagerLibrary")]
    private static extern bool BTM_IsReceiving();

    [DllImport("BTManagerLibrary")]
    private static extern IntPtr BTM_DisconnectFromDevice();

    void Start()
    {
        deviceFinderHelper = new DeviceFinderHelper() { performFastFind = true, performLoopFind=false };
        deviceConnectionHelper = new DeviceConnectionHelper();
        dataSenderAndReceiverHelper = new DataCommunicationHelper();
        dataReceiverHelper = new DataCommunicationHelper();
        byteDataSenderHelper = new ByteDataCommunicationHelper();
        byteDataReceiverHelper = new ByteDataCommunicationHelper() { dataToSend=new byte[maxByteLength], receivedData=new byte[maxByteLength], maxLength=maxByteLength };

        ubwDeviceFinder = new UnityBackgroundWorker(this, BGW_DeviceFinder, BGW_DeviceFinder_Progress, BGW_DeviceFinder_Done, deviceFinderHelper);
        ubwDeviceConnector = new UnityBackgroundWorker(this, BGW_DeviceConnector, BGW_DeviceConnector_Progress, BGW_DeviceConnector_Done, deviceConnectionHelper);
        ubwDataSenderAndReceiver = new UnityBackgroundWorker(this, BGW_SendAndReceiveData, BGW_SendAndReceiveData_Progress, BGW_SendAndReceiveData_Done, dataSenderAndReceiverHelper);
        ubwDataReceiver = new UnityBackgroundWorker(this, BGW_ReceiveData, BGW_ReceiveData_Progress, BGW_ReceiveData_Done, dataReceiverHelper);
        ubwByteDataReceiver = new UnityBackgroundWorker(this, BGW_ReceiveByteData, BGW_ReceiveByteData_Progress, BGW_ReceiveByteData_Done, byteDataReceiverHelper);

        FindDevices(true);
        StartCoroutine(StatusCoroutine());
    }

    public void FindDevices(bool performFastFind)
    {
        if (!ubwDeviceFinder.IsBusy)
        {
            txtAnswer.text += "Searching for the devices..\n";
            deviceFinderHelper.performFastFind = performFastFind;
            deviceFinderHelper.performLoopFind = false;
            ubwDeviceFinder.Run();
        }
    }
    public void SendAndReceiveData()
    {
        if (!ubwDataSenderAndReceiver.IsBusy)
        {
            Debug.Log("SendData");
            dataSenderAndReceiverHelper.dataToSend = txtInput.text;
            ubwDataSenderAndReceiver.Run();
        }
    }
    public void ReceiveData()
    {
        if (ubwByteDataReceiver != null && ubwByteDataReceiver.IsBusy)
        {
            ubwByteDataReceiver.Abort();
        }

        ubwDataReceiver.Run();

    }
    public void ReceiveByteData()
    {
        if (ubwDataReceiver != null && ubwDataReceiver.IsBusy)
        {
            ubwDataReceiver.Abort();
        }

        ubwByteDataReceiver.Run();

        //int maxLength = 512;
        //byte[] data = new byte[maxLength];
        //int byteArrayLength=BTM_ReceiveByteDataFast(data, maxLength);
        //if(byteArrayLength==-1)
        //{
        //    Debug.Log("Error!");
        //}
        //for(int i=0;i<byteArrayLength;i++)
        //{
        //    Debug.Log(i + ": " +(int)data[i]);
        //}
    }
    public void SendData()
    {
        Marshal.PtrToStringAnsi(BTM_SendDataFast(txtInput.text));
    }
    public void SendByteData()
    {
        //byte[] byteArr = new byte[] { 0xFF, 0xAA, 0xFF };
        //Debug.Log(BitConverter.ToString(byteArr));
        //Debug.Log((BTM_SendByteDataFast(byteArr)));
        //Debug.Log((BTM_SendByteDataFast(byteArr)));

        //byte[] byteArr = new byte[] { 0xAA, 0xFF, 0xFF, 0xAA, 0xAA, 0xAA };
        //Marshal.PtrToStringAnsi(BTM_SendByteDataFast(BitConverter.ToString(byteArr), byteArr.Length));

        //byte[] data = new byte[] { 0xFF, 0xAB, 0xCD };
        //byte[] data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        string formattedString = txtHexInput.text.Replace("$", "");
        byte[] data = StringToByteArrayFastest(formattedString);
        //for(int i=0;i<data.Length;i++)
        //{
        //    Debug.Log(data[i]);
        //}
        IntPtr unmanagedArray = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, unmanagedArray, data.Length);
        Debug.Log(Marshal.PtrToStringAnsi(BTM_SendByteDataFast(unmanagedArray,data.Length)));
        Marshal.FreeHGlobal(unmanagedArray);
    }
    public void Connect(bool receiveHexData)
    {
        if(!ubwDeviceConnector.IsBusy)
        {
            deviceConnectionHelper.receiveHexData = receiveHexData;
            ubwDeviceConnector.Run();
        }
    }
    public void Disconnect()
    {
        txtAnswer.text += "\n" + Marshal.PtrToStringAnsi(BTM_DisconnectFromDevice());

        if (ubwDataReceiver != null && ubwDataReceiver.IsBusy)
        {
            ubwDataReceiver.Abort();
        }

        if (ubwByteDataReceiver != null && ubwByteDataReceiver.IsBusy)
        {
            ubwByteDataReceiver.Abort();
        }
    }
    public void CheckConnection()
    {
        txtAnswer.text += "Connection status: " + BTM_IsConnected() + "\n";
    }
    public void CheckReceiving()
    {
        txtAnswer.text += "Receiving status: " + BTM_IsReceiving() + "\n";
    }
    public void CheckBusy()
    {
        txtAnswer.text += "Busy status: " + BTM_IsBusy() + "\n";
    }
    public void Clear()
    {
        txtAnswer.text = "";
    }

    void BGW_DeviceFinder(object CustomData, UnityBackgroundWorkerArguments e)
    {
        try
        {
            if (!isBTDevicesSearching)
            {
                isBTDevicesSearching = true;

                DeviceFinderHelper temp = (DeviceFinderHelper)CustomData;

                do
                {
                    if (temp.performFastFind)
                    {
                        temp.transferedText = Marshal.PtrToStringAnsi(BTM_GetDevicesNamesFast());
                    }
                    else
                    {
                        temp.transferedText = Marshal.PtrToStringAnsi(BTM_GetDevicesNames());
                    }
                    e.Progress++;
                } while (isBTDevicesSearching && temp.performLoopFind);
               
            }
        }
        catch (Exception error)
        {
            e.HasError = true;
            e.ErrorMessage = error.Message;
        }
    }
    void BGW_DeviceFinder_Progress(object CustomData, int Progress)
    {
        txtAnswer.text += "Devices search completed!\n";
        DeviceFinderHelper temp = (DeviceFinderHelper)CustomData;
        Debug.Log(temp.transferedText);
        //temp.transferedText = temp.transferedText.Replace(" ", "");
        string[] strDeviceFormattedList = temp.transferedText.Split('\n');
        ddDevices.ClearOptions();
        foreach (var item in strDeviceFormattedList)
        {
            if (item.Length != 0)
            {
                ddDevices.options.Add(new Dropdown.OptionData(item));
            }
        }

        ddDevices.RefreshShownValue();

    }
    void BGW_DeviceFinder_Done(object CustomData, UnityBackgroundWorkerInformation Information)
    {
        isBTDevicesSearching = false;
        if (Information.Status == UnityBackgroundWorkerStatus.Done)
            Debug.Log("Done");
        else if (Information.Status == UnityBackgroundWorkerStatus.Aborted)
            Debug.Log("Aborted");
        else if (Information.Status == UnityBackgroundWorkerStatus.HasError)
            Debug.Log(Information.ErrorMessage);
    }

    void BGW_DeviceConnector(object CustomData, UnityBackgroundWorkerArguments e)
    {
        try
        {
            DeviceConnectionHelper temp = (DeviceConnectionHelper)CustomData;
            temp.status = "Connecting to the device";
            e.Progress++;

            temp.status = Marshal.PtrToStringAnsi(BTM_ConnectToDevice(ddDevices.options[ddDevices.value].text));
            e.Progress++;
        }
        catch (Exception error)
        {
            e.HasError = true;
            e.ErrorMessage = error.Message;
        }
    }
    void BGW_DeviceConnector_Progress(object CustomData, int Progress)
    {
        DeviceConnectionHelper temp = (DeviceConnectionHelper)CustomData;
        txtAnswer.text += temp.status + "\n";
        if(temp.status.Contains("Connected"))
        {
            if(temp.receiveHexData)
            {
                ReceiveByteData();
            }
            else
            {
                ReceiveData();
            }
        }
    }
    void BGW_DeviceConnector_Done(object CustomData, UnityBackgroundWorkerInformation Information)
    {
        if (Information.Status == UnityBackgroundWorkerStatus.Done)
            Debug.Log("Done");
        else if (Information.Status == UnityBackgroundWorkerStatus.Aborted)
            Debug.Log("Aborted");
        else if (Information.Status == UnityBackgroundWorkerStatus.HasError)
            Debug.Log(Information.ErrorMessage);
    }

    void BGW_SendAndReceiveData(object CustomData, UnityBackgroundWorkerArguments e)
    {
        try
        {
            DataCommunicationHelper temp = (DataCommunicationHelper)CustomData;
            Debug.Log(temp.dataToSend);
            temp.receivedData = Marshal.PtrToStringAnsi(BTM_SendAndReceiveDataFast("WEL_REF"));
            e.Progress++;
        }
        catch (Exception error)
        {
            e.HasError = true;
            e.ErrorMessage = error.Message;
        }
    }
    void BGW_SendAndReceiveData_Progress(object CustomData, int Progress)
    {
        Debug.Log("BGW_SendData_Progress");
        DataCommunicationHelper temp = (DataCommunicationHelper)CustomData;
        Debug.Log("Sent: " + temp.dataToSend + " Received: " + temp.receivedData);

        txtAnswer.text += temp.receivedData;
    }
    void BGW_SendAndReceiveData_Done(object CustomData, UnityBackgroundWorkerInformation Information)
    {
        if (Information.Status == UnityBackgroundWorkerStatus.Done)
            Debug.Log("Done");
        else if (Information.Status == UnityBackgroundWorkerStatus.Aborted)
            Debug.Log("Aborted");
        else if (Information.Status == UnityBackgroundWorkerStatus.HasError)
            Debug.Log(Information.ErrorMessage);
    }

    void BGW_ReceiveData(object CustomData, UnityBackgroundWorkerArguments e)
    {
        try
        {
            //if(BTM_IsReceiving())
            //{
            //    Disconnect();
            //    Connect();
            //}

            while (true && BTM_IsConnected())
            {
                Debug.Log("BGW_ReceiveData");
                DataCommunicationHelper temp = (DataCommunicationHelper)CustomData;
                temp.receivedData = Marshal.PtrToStringAnsi(BTM_ReceiveDataFast(temp.dataToSend));
                e.Progress++;
                Thread.Sleep(1000);
            }
        }
        catch (Exception error)
        {
            e.HasError = true;
            e.ErrorMessage = error.Message;
        }
    }
    void BGW_ReceiveData_Progress(object CustomData, int Progress)
    {
        Debug.Log("BGW_ReceiveData_Progress");
        DataCommunicationHelper temp = (DataCommunicationHelper)CustomData;
        txtAnswer.text += temp.receivedData;
    }
    void BGW_ReceiveData_Done(object CustomData, UnityBackgroundWorkerInformation Information)
    {
        if (Information.Status == UnityBackgroundWorkerStatus.Done)
            Debug.Log("Done");
        else if (Information.Status == UnityBackgroundWorkerStatus.Aborted)
            Debug.Log("Aborted");
        else if (Information.Status == UnityBackgroundWorkerStatus.HasError)
            Debug.Log(Information.ErrorMessage);
    }

    void BGW_ReceiveByteData(object CustomData, UnityBackgroundWorkerArguments e)
    {
        try
        {
            //if (BTM_IsReceiving())
            //{
            //    Disconnect();
            //    Connect();
            //}

            while (true && BTM_IsConnected())
            {
                ByteDataCommunicationHelper temp = (ByteDataCommunicationHelper)CustomData;
                byte[] receivedArray = new byte[temp.maxLength];
                int byteArrayLength = BTM_ReceiveByteDataFast(receivedArray, receivedArray.Length);

                if (byteArrayLength == -1)
                {
                    Debug.Log("BGW_ReceiveByteData data error");
                    return;
                }

                temp.receivedData = new byte[byteArrayLength];
                for(int i=0;i<byteArrayLength;i++)
                {
                    temp.receivedData[i] = receivedArray[i];
                }

                //for (int i = 0; i < byteArrayLength; i++)
                //{
                //    Debug.Log(temp.receivedData[i] + " ");
                //}

                e.Progress++;

                Thread.Sleep(1000);
            }
        }
        catch (Exception error)
        {
            e.HasError = true;
            e.ErrorMessage = error.Message;
        }
    }
    void BGW_ReceiveByteData_Progress(object CustomData, int Progress)
    {
        Debug.Log("BGW_ReceiveByteData_Progress");
        ByteDataCommunicationHelper temp = (ByteDataCommunicationHelper)CustomData;
        txtAnswer.text += BitConverter.ToString(temp.receivedData);
        Debug.Log(BitConverter.ToString(temp.receivedData));
    }
    void BGW_ReceiveByteData_Done(object CustomData, UnityBackgroundWorkerInformation Information)
    {
        if (Information.Status == UnityBackgroundWorkerStatus.Done)
            Debug.Log("BGW_ReceiveByteData_Done");
        else if (Information.Status == UnityBackgroundWorkerStatus.Aborted)
            Debug.Log("BGW_ReceiveByteData Aborted");
        else if (Information.Status == UnityBackgroundWorkerStatus.HasError)
            Debug.Log(Information.ErrorMessage);
    }

    void OnApplicationQuit()
    {
        Disconnect();

        if (ubwDeviceFinder != null && ubwDeviceFinder.IsBusy)
        {
            ubwDeviceFinder.Abort();
        }

        Thread.Sleep(1000);
    }

    public static byte[] StringToByteArrayFastest(string hex)
    {
        if (hex.Length % 2 == 1)
            throw new Exception("The binary key cannot have an odd number of digits");

        byte[] arr = new byte[hex.Length >> 1];

        for (int i = 0; i < hex.Length >> 1; ++i)
        {
            arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
        }

        return arr;
    }
    public static int GetHexVal(char hex)
    {
        int val = (int)hex;
        //For uppercase A-F letters:
        return val - (val < 58 ? 48 : 55);
        //For lowercase a-f letters:
        //return val - (val < 58 ? 48 : 87);
        //Or the two combined, but a bit slower:
        //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
    }


    public Image imgConnection;
    public Image imgReceiving;
    public Image imgFree;
    public Color onColor;
    public Color offColor;

    public IEnumerator StatusCoroutine()
    {
        while(true)
        {
            imgConnection.color = BTM_IsConnected() ? onColor : offColor;
            imgReceiving.color = BTM_IsReceiving() ? onColor : offColor;
            imgFree.color = BTM_IsBusy() ? offColor : onColor;
            yield return new WaitForSeconds(.3f);
        }

    }
}