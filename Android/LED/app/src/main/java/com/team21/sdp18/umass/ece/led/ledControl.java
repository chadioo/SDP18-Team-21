package com.team21.sdp18.umass.ece.led;

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
import android.widget.SeekBar;
import android.widget.TextView;
import android.widget.Toast;
import android.app.ProgressDialog;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.os.AsyncTask;
import android.widget.ToggleButton;

import com.jjoe64.graphview.GraphView;
import com.jjoe64.graphview.series.DataPoint;
import com.jjoe64.graphview.series.LineGraphSeries;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.util.UUID;

import com.jjoe64.graphview.GraphView;
import com.jjoe64.graphview.series.DataPoint;
import com.jjoe64.graphview.series.LineGraphSeries;


public class ledControl extends AppCompatActivity {

    ToggleButton toggle;
    String address = null;
    String message;
    private ProgressDialog progress;
    BluetoothAdapter myBluetooth = null;
    BluetoothSocket btSocket = null;
    private boolean isBtConnected = false;
    Toast LEDinfo;
    // Generic SPP UUID for connecting to a BT serial board
    static final UUID myUUID = UUID.fromString("00001101-0000-1000-8000-00805F9B34FB");
    LineGraphSeries<DataPoint> series;


    @Override
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);

        Intent newint = getIntent();
        address = newint.getStringExtra(DeviceList.EXTRA_ADDRESS); // Receive the address of the Bluetooth device

        new ConnectBT().execute(); // Call the class to connect

        // Call code to connect with graphs here
        setContentView(R.layout.activity_graphs);

        // Call the widgets
        toggle = (ToggleButton) findViewById(R.id.toggleButton);

        //new ConnectBT().execute(); // Call the class to connect

        toggle.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
            public void onCheckedChanged(CompoundButton buttonView, boolean isChecked) {
                if (isChecked) {
                    // The toggle is enabled
                    turnOnToggle();
                } else {
                    // The toggle is disabled
                    turnOffToggle();
                }
            }
        });

    }

    private void Disconnect()
    {
        if (btSocket!=null) // If the btSocket is busy
        {
            try
            {
                btSocket.close(); // Close connection
            }
            catch (IOException e)
            { msg("Error");}
        }
        finish(); // Return to the first layout

    }

    private void turnOffToggle()
    {
        //


    }

    private void turnOnToggle()
    {
        //



    }

    private void rejectMessage()
    {
        if (btSocket!=null)
        {
            try
            {
                btSocket.getOutputStream().write("f".toString().getBytes());
                ledStatus();
            }
            catch (IOException e)
            {
                msg("Error");
            }
        }
    }

    // fast way to call Toast
    private void msg(String s)
    {
        Toast.makeText(getApplicationContext(),s,Toast.LENGTH_LONG).show();
    }

    private void ledStatus() throws IOException {
        InputStream socketInputStream =  btSocket.getInputStream();

    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        /*getMenuInflater().inflate(R.menu.menu_led_control, menu);*/
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        // noinspection SimplifiableIfStatement
        /*if (id == R.id.action_settings) {
            return true;
        }*/

        return super.onOptionsItemSelected(item);
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
            catch (IOException e)
            {
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
}