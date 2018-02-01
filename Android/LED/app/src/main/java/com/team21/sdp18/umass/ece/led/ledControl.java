
package com.team21.sdp18.umass.ece.led;

import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;
import android.bluetooth.BluetoothSocket;
import android.content.Intent;
import android.view.View;
import android.widget.Button;
import android.widget.Toast;
import android.app.ProgressDialog;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.os.AsyncTask;

import com.jjoe64.graphview.GraphView;
import com.jjoe64.graphview.Viewport;
import com.jjoe64.graphview.series.DataPoint;
import com.jjoe64.graphview.series.LineGraphSeries;

import java.io.IOException;
import java.io.InputStream;
import java.util.Random;
import java.util.UUID;

public class ledControl extends AppCompatActivity {

    String address = null;
    private ProgressDialog progress;
    BluetoothAdapter myBluetooth = null;
    BluetoothSocket btSocket = null;
    private boolean isBtConnected = false;

    // Generic SPP UUID for connecting to a BT serial board
    static final UUID myUUID = UUID.fromString("00001101-0000-1000-8000-00805F9B34FB");

    private static final Random RANDOM = new Random();
    private LineGraphSeries<DataPoint> seriesax, seriesay, seriesaz, seriesgx, seriesgy, seriesgz;

    private int axlastX = 0;
    private int aylastX = 0;
    private int azlastX = 0;
    private int gxlastX = 0;
    private int gylastX = 0;
    private int gzlastX = 0;

    GraphView grapha;
    GraphView graphg;

    private double ax, ay, az, gx, gy, gz;

    private Button aButton,gButton;


    @Override
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);

        Intent newint = getIntent();
        address = newint.getStringExtra(DeviceList.EXTRA_ADDRESS); // Receive the address of the Bluetooth device

        new ConnectBT().execute(); // Call the class to connect

        setContentView(R.layout.activity_graphs);

        // Accelerometer Graph
        grapha = (GraphView) findViewById(R.id.grapha);
        seriesax = new LineGraphSeries<DataPoint>();
        seriesay = new LineGraphSeries<DataPoint>();
        seriesaz = new LineGraphSeries<DataPoint>();
        grapha.addSeries(seriesax);
        grapha.addSeries(seriesay);
        grapha.addSeries(seriesaz);
        Viewport viewportx = grapha.getViewport();
        viewportx.setYAxisBoundsManual(true);
        viewportx.setMinY(-5);
        viewportx.setMaxY(5);
        viewportx.setScrollable(true);

        // Gyroscope Graph
        graphg = (GraphView) findViewById(R.id.graphg);
        seriesgx = new LineGraphSeries<DataPoint>();
        seriesgy = new LineGraphSeries<DataPoint>();
        seriesgz = new LineGraphSeries<DataPoint>();
        graphg.addSeries(seriesgx);
        graphg.addSeries(seriesgy);
        graphg.addSeries(seriesgz);
        Viewport viewporty = graphg.getViewport();
        viewporty.setYAxisBoundsManual(true);
        viewporty.setMinY(-5);
        viewporty.setMaxY(5);
        viewporty.setScrollable(true);

        // List all buttons and their functions

        aButton = findViewById(R.id.buttona);

        aButton.setOnClickListener(new View.OnClickListener()
        {
            @Override
            public void onClick(View v) {
                try {
                    getData();      //method to turn on
                    getData();      //method to turn on
                    getData();      //method to turn on
                    System.out.println("ax: "+ax+" ay: "+ay+" az: "+az);
                    printGraph('a', ax, ay, az);

                } catch (IOException e1) {
                    e1.printStackTrace();
                }

            }
        });

        gButton = findViewById(R.id.buttong);

        gButton.setOnClickListener(new View.OnClickListener()
        {
            @Override
            public void onClick(View v) {
                try {
                    getData();      //method to turn on
                    System.out.println("gx: "+gx+" gy: "+gy+" gz: "+gz);
                    printGraph('g',gx,gy,gz);

                } catch (IOException e1) {
                    e1.printStackTrace();
                }

            }
        });

    }



    //////////////////////////
    // FUNCTIONS FOR GRAPHS //
    //////////////////////////


    // print value to graph
    protected void printGraph(char axis, double x, double y, double z) {
        final char Axis = axis;
        final double X = x;
        final double Y = y;
        final double Z = z;

        // we're going to simulate real time with thread that append data to the graph
        new Thread(new Runnable() {

            @Override
            public void run() {
                // we add 100 new entries

                runOnUiThread(new Runnable() {

                    @Override
                    public void run() {
                        if(Axis=='a'){
                            seriesax.appendData(new DataPoint(axlastX++, X), false, 20);
                            seriesay.appendData(new DataPoint(aylastX++, Y), false, 20);
                            seriesaz.appendData(new DataPoint(azlastX++, Z), false, 20);
                            /*
                            graphg.addSeries(seriesax);
                            graphg.addSeries(seriesay);
                            graphg.addSeries(seriesaz);
                            */
                        }
                        else if(Axis=='g'){
                            seriesgx.appendData(new DataPoint(gxlastX++, X), false, 20);
                            seriesgy.appendData(new DataPoint(gylastX++, Y), false, 20);
                            seriesgz.appendData(new DataPoint(gzlastX++, Z), false, 20);
                            /*
                            graphg.addSeries(seriesgx);
                            graphg.addSeries(seriesgx);
                            graphg.addSeries(seriesgx);
                            */
                        }
                    }

                });

                // sleep to slow down the add of entries
                try {
                    Thread.sleep(600);
                } catch (InterruptedException e) {
                    // manage error ...
                }

            }
        }).start();
    }


    ///////////////////////
    // Method to getData //
    ///////////////////////

    // use this to read a string
    private String getMessage() throws IOException{
        String fullMessage = " ";
        if (btSocket!=null) {
            btSocket.getOutputStream().write("0".toString().getBytes());
            InputStream socketInputStream = btSocket.getInputStream();
            byte[] byteArray = new byte[512];
            int length;
            char end = '\n';
            char currentChar = ' ';

            // Keep looping to listen for received messages
            while (currentChar != end) {
                try {
                    length = socketInputStream.read(byteArray);                     //read bytes from input buffer
                    System.out.println("Length: " + length);
                    String readMessage = new String(byteArray, 0, length);
                    for (int i = 0; i < length; i++) {
                        currentChar = readMessage.charAt(i);
                    }
                    fullMessage = fullMessage + readMessage;
                    // Send the obtained bytes to the UI Activity via handler
                    Log.i("logging", readMessage + "");
                } catch (IOException e) {
                    break;
                }
            }
            Log.i("logging", fullMessage + "");
        }
        return fullMessage;
    }

    // use this to read a data point for graph
    private void getData() throws IOException{
        if (btSocket!=null) {

            // write 1 to sensor to tell sensor to send data
            btSocket.getOutputStream().write("1".toString().getBytes());

            // reads incoming data
            InputStream socketInputStream = btSocket.getInputStream();

            byte[] byteArray = new byte[512];
            int length;

            // Keep looping to listen for received messages
            while (true) {
                try {
                    length = socketInputStream.read(byteArray);                     //read bytes from input buffer
                    System.out.println("Length: " + length);
                    String readMessage = new String(byteArray, 0, length);
                    System.out.println("Line: "+readMessage);
                    if(length>2) {
                        System.out.println("Line Parsed: ID: "+readMessage.substring(0,2)+" Value: "+readMessage.substring(2,length));
                        if(readMessage.substring(0,2).contentEquals("Ax")){
                            ax = Double.parseDouble(readMessage.substring(2,8).replaceAll("[^\\d.]", ""));
                        }
                        if(readMessage.substring(0,2).contentEquals("Ay")){
                            ay = Double.parseDouble(readMessage.substring(2,8).replaceAll("[^\\d.]", ""));
                        }
                        if(readMessage.substring(0,2).contentEquals("Az")){
                            az = Double.parseDouble(readMessage.substring(2,8).replaceAll("[^\\d.]", ""));
                        }
                        if(readMessage.substring(0,2).contentEquals("Gx")){
                            gx = Double.parseDouble(readMessage.substring(2,8).replaceAll("[^\\d.]", ""));
                        }
                        if(readMessage.substring(0,2).contentEquals("Gy")){
                            gy = Double.parseDouble(readMessage.substring(2,8).replaceAll("[^\\d.]", ""));
                        }
                        if(readMessage.substring(0,2).contentEquals("Gz")){
                            gz = Double.parseDouble(readMessage.substring(2,8).replaceAll("[^\\d.]", ""));
                        }
                        break;
                    }

                } catch (IOException e) {
                    break;
                }
            }

        }
    }


    /////////////////////////////
    // CODE FROM INSTRUCTABLES //
    /////////////////////////////


    // fast way to call Toast
    private void msg(String s)
    {
        Toast.makeText(getApplicationContext(),s,Toast.LENGTH_LONG).show();
    }

    private class ConnectBT extends AsyncTask<Void, Void, Void>  // UI thread
    {

        private boolean ConnectSuccess = true; // If it's here, it's almost connected

        @Override
        protected void onPreExecute()
        {
            progress = ProgressDialog.show(ledControl.this, "Connecting...", "Please wait!");  // Show a progress dialog
        }

        @Override
        protected Void doInBackground(Void... devices) // While the progress dialog is shown, the connection is done in background
        {
            try
            {
                if (btSocket == null || !isBtConnected)
                {
                    myBluetooth = BluetoothAdapter.getDefaultAdapter(); // Get the mobile Bluetooth device
                    BluetoothDevice dispositivo = myBluetooth.getRemoteDevice(address); // Connects to the device's address and checks if it's available
                    btSocket = dispositivo.createInsecureRfcommSocketToServiceRecord(myUUID); // Create a RFCOMM (SPP) connection
                    BluetoothAdapter.getDefaultAdapter().cancelDiscovery();
                    btSocket.connect(); // Start connection

                }
            }
            catch (IOException e) {
                ConnectSuccess = false; // If the try failed, check the exception here
            }
            return null;
        }
        @Override
        protected void onPostExecute(Void result) // After the doInBackground, checks if everything went fine
        {
            super.onPostExecute(result);

            if (!ConnectSuccess)
            {
                msg("Connection Failed. Is it a SPP Bluetooth? Try again.");
                finish();
            }
            else
            {
                msg("Connected.");
                isBtConnected = true;
            }
            progress.dismiss();
        }
    }

} // END