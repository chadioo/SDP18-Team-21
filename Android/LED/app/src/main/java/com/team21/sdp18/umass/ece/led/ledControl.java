
package com.team21.sdp18.umass.ece.led;

import android.app.Activity;
import android.os.Handler;
import android.os.Message;
import android.support.design.widget.Snackbar;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;

import android.bluetooth.BluetoothSocket;
import android.content.Intent;
import android.view.View;
import android.widget.Button;
import android.widget.CompoundButton;
import android.widget.EditText;
import android.widget.SeekBar;
import android.widget.TextView;
import android.widget.Toast;
import android.app.ProgressDialog;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.os.AsyncTask;
import android.widget.ToggleButton;

import com.jjoe64.graphview.GraphView;
import com.jjoe64.graphview.Viewport;
import com.jjoe64.graphview.series.DataPoint;
import com.jjoe64.graphview.series.LineGraphSeries;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.DataInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.Random;
import java.util.UUID;

import com.jjoe64.graphview.GraphView;
import com.jjoe64.graphview.series.DataPoint;
import com.jjoe64.graphview.series.LineGraphSeries;


public class ledControl extends AppCompatActivity {

    String address = null;
    String message;
    private ProgressDialog progress;
    BluetoothAdapter myBluetooth = null;
    BluetoothSocket btSocket = null;
    private boolean isBtConnected = false;
    Toast LEDinfo;
    // Generic SPP UUID for connecting to a BT serial board
    static final UUID myUUID = UUID.fromString("00001101-0000-1000-8000-00805F9B34FB");

    private static final Random RANDOM = new Random();
    private LineGraphSeries<DataPoint> seriesx, seriesy, seriesz;
    private int xlastX = 0;
    private int ylastX = 0;
    private int zlastX = 0;

    private Button xButton,yButton,zButton;


    @Override
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);

        Intent newint = getIntent();
        address = newint.getStringExtra(DeviceList.EXTRA_ADDRESS); // Receive the address of the Bluetooth device

        new ConnectBT().execute(); // Call the class to connect

        setContentView(R.layout.activity_graphs);

        // X Graph
        GraphView graphx = (GraphView) findViewById(R.id.graphx);
        seriesx = new LineGraphSeries<DataPoint>();
        graphx.addSeries(seriesx);
        Viewport viewportx = graphx.getViewport();
        viewportx.setYAxisBoundsManual(true);
        viewportx.setMinY(0);
        viewportx.setMaxY(100);
        viewportx.setScrollable(true);

        // X Graph
        GraphView graphy = (GraphView) findViewById(R.id.graphy);
        seriesy = new LineGraphSeries<DataPoint>();
        graphy.addSeries(seriesy);
        Viewport viewporty = graphy.getViewport();
        viewporty.setYAxisBoundsManual(true);
        viewporty.setMinY(0);
        viewporty.setMaxY(100);
        viewporty.setScrollable(true);

        // X Graph
        GraphView graphz = (GraphView) findViewById(R.id.graphz);
        seriesz = new LineGraphSeries<DataPoint>();
        graphz.addSeries(seriesz);
        Viewport viewportz = graphz.getViewport();
        viewportz.setYAxisBoundsManual(true);
        viewportz.setMinY(0);
        viewportz.setMaxY(100);
        viewportz.setScrollable(true);


        // List all buttons and their functions

        xButton = (Button)findViewById(R.id.buttonx);

        xButton.setOnClickListener(new View.OnClickListener()
        {
            @Override
            public void onClick(View v) {
                try {
                    int data = getData('x');      //method to turn on
                    System.out.println(data);
                    printGraph('x',data);

                } catch (IOException e1) {
                    e1.printStackTrace();
                }

            }
        });

        yButton = (Button)findViewById(R.id.buttony);

        yButton.setOnClickListener(new View.OnClickListener()
        {
            @Override
            public void onClick(View v) {
                try {
                    int data = getData('y');      //method to turn on
                    System.out.println(data);
                    printGraph('y',data);

                } catch (IOException e1) {
                    e1.printStackTrace();
                }

            }
        });

        zButton = (Button)findViewById(R.id.buttonz);

        zButton.setOnClickListener(new View.OnClickListener()
        {
            @Override
            public void onClick(View v) {
                try {
                    int data = getData('z');      //method to turn on
                    System.out.println(data);
                    printGraph('z',data);

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
    protected void printGraph(char axis, int data) {
        final char Axis = axis;
        final int Data = data;

        // we're going to simulate real time with thread that append data to the graph
        new Thread(new Runnable() {

            @Override
            public void run() {
                // we add 100 new entries

                runOnUiThread(new Runnable() {

                    @Override
                    public void run() {
                        if(Axis=='x'){
                            seriesx.appendData(new DataPoint(xlastX++, Data), false, 20);
                        }
                        else if(Axis=='y'){
                            seriesy.appendData(new DataPoint(ylastX++, Data), false, 20);
                        }
                        else if(Axis=='z'){
                            seriesz.appendData(new DataPoint(zlastX++, Data), false, 20);
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
    private int getData(char axis) throws IOException{
        final char Axis = axis;
        char getChar = ' ';
        if (btSocket!=null) {
            if(Axis=='x'){
                btSocket.getOutputStream().write("0".toString().getBytes());
            }
            else if(Axis=='y'){
                btSocket.getOutputStream().write("1".toString().getBytes());
            }
            else if(Axis=='z'){
                btSocket.getOutputStream().write("2".toString().getBytes());
            }
            btSocket.getOutputStream().write("0".toString().getBytes());
            InputStream socketInputStream = btSocket.getInputStream();


            byte[] byteArray = new byte[512];
            int length;

            // Keep looping to listen for received messages
            while (true) {
                try {
                    length = socketInputStream.read(byteArray);                     //read bytes from input buffer
                    System.out.println("Length: " + length);
                    String readMessage = new String(byteArray, 0, length);
                    if(length>0) {
                        getChar = readMessage.charAt(0);
                        break;
                    }
                } catch (IOException e) {
                    break;
                }
            }
            Log.i("logging", getChar + "");
        }
        return (int) getChar;
    }


    /////////////////////////////
    // SHIT FROM INSTRUCTABLES //
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