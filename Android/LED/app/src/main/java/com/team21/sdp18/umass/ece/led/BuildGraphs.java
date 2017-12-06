package com.team21.sdp18.umass.ece.led;

/**
 * Created by Jackie Lagasse on 12/6/2017.
 */
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;

import com.jjoe64.graphview.GraphView;
import com.jjoe64.graphview.series.DataPoint;
import com.jjoe64.graphview.series.LineGraphSeries;

public class BuildGraphs extends AppCompatActivity {

    LineGraphSeries<DataPoint> series;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_graphs);

        double y,x;

        GraphView graphx = (GraphView) findViewById(R.id.graphx);
        series = new LineGraphSeries<DataPoint>();
        x = -5.0;
        for(int i=0; i<500; i++){
            x = x + 0.1;
            y = Math.sin(x);
            series.appendData(new DataPoint(x, y), true, 500);
        }
        graphx.addSeries(series);

        GraphView graphy = (GraphView) findViewById(R.id.graphy);
        series = new LineGraphSeries<DataPoint>();
        x = -5.0;
        for(int i=0; i<500; i++){
            x = x + 0.1;
            y = Math.sin(x);
            series.appendData(new DataPoint(x, y), true, 500);
        }
        graphy.addSeries(series);

        GraphView graphz = (GraphView) findViewById(R.id.graphz);
        series = new LineGraphSeries<DataPoint>();
        x = -5.0;
        for(int i=0; i<500; i++){
            x = x + 0.1;
            y = Math.sin(x);
            series.appendData(new DataPoint(x, y), true, 500);
        }
        graphz.addSeries(series);

    }
}