﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using StarFox.Interop.BRR;

namespace StarFoxMapVisualizer.Controls2
{
    /// <summary>
    /// Interaction logic for WaveFormControl.xaml
    /// </summary>
    public partial class WaveFormControl : ContentControl
    {
        /// <summary>
        /// Measures how simple this waveform is in comparison to the amount of lateral space it can fill.
        /// <para>Default is 1.0 -- as in if the control is 200px wide it will display 200 samples as 1px wide lines.
        /// .5 would double this amount, and 2.0 would half it.</para>
        /// </summary>
        public double Simplicity { get; set; } = 1.0;

        public WaveFormControl()
        {
            InitializeComponent();
        }

        public WaveFormControl(BRRSample Sample) : this()
        {
            Loaded += delegate
            {
                Display(Sample);
            };
        }

        public void Display(BRRSample Sample)
        {
            WaveFormHost.Children.Clear();
            if (Sample.SampleData.Count < 1) return;
            double widthMeasurement = HOST.ActualWidth == 0 ? HOST.Width : HOST.ActualWidth;
            double heightMeasurement = HOST.ActualHeight == 0 ? HOST.Height : HOST.ActualHeight;
            double halfDesignHeight = 100; // distance from median to top / bottom of control
            double designWidth = 1;
            double totalSamples = (int)(Math.Max(widthMeasurement, Sample.SampleData.Count) * Simplicity);
            double step = totalSamples / widthMeasurement;
            short HighBound = Sample.SampleData.Max();
            short LowBound = 0;
            int distance = HighBound - LowBound;
            int currentX = -1;
            var addedSamples = new List<int>();
            void AddPoint(int index)
            {
                currentX++;
                var dataPoint = Sample.SampleData[index];
                if (dataPoint == short.MaxValue ||
                    dataPoint == short.MinValue)
                    return;
                var value = Math.Abs(dataPoint);
                var Percentage = (double)value / distance;
                var lineHeight = Percentage * halfDesignHeight;
                lineHeight *= 2;
                Rectangle rect = new Rectangle()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(currentX, 0, 0, 0),
                    Width = designWidth,
                    Height = lineHeight,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                WaveFormHost.Children.Add(rect);
            }
            for(double i = 0; i < Sample.SampleData.Count-1; i += step)
            {
                var index = (int)i;
                if (addedSamples.Contains(index)) continue;
                addedSamples.Add(index);
                AddPoint(index);
            }
            currentX++;
            AddPoint(Sample.SampleData.Count - 1);
            WaveFormHost.Height = halfDesignHeight;
            WaveFormHost.Width = totalSamples;
            WaveFormHost.LayoutTransform = new ScaleTransform(
                scaleX: widthMeasurement / currentX,
                scaleY: heightMeasurement / (halfDesignHeight)
            );
        }
    }
}
