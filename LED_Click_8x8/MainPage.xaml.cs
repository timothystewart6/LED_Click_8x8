﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Devices.I2c;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using Windows.Devices.Enumeration;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LED_Click_8x8
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int MATRIX_SIZE = 8;

        private SolidColorBrush _on = new SolidColorBrush(Colors.Red);
        private SolidColorBrush _off = new SolidColorBrush(Colors.White);

        private TextBlock[] _matrixRowValue = new TextBlock[MATRIX_SIZE];

        private byte[,] _matrixData = new byte[MATRIX_SIZE, MATRIX_SIZE];

        // I2C device, display
        private I2cDevice _matrixDevice = null;



        public MainPage()
        {
            this.InitializeComponent();

            initLedMatrixDevice();

            for (int i = 0; i < MATRIX_SIZE; i++)
            {
                TextBlock tb = new TextBlock();
                tb.Text = "0x00";
                tb.HorizontalAlignment = HorizontalAlignment.Center;
                tb.VerticalAlignment = VerticalAlignment.Center;
                tb.SetValue(Grid.RowProperty, i);
                tb.SetValue(Grid.ColumnProperty, MATRIX_SIZE);
                _matrix.Children.Add(tb);

                _matrixRowValue[i] = tb;

                for (int j = 0; j < MATRIX_SIZE; j++)
                {
                    Ellipse led = new Ellipse();
                    led.Width = 40;
                    led.Height = 40;
                    led.HorizontalAlignment = HorizontalAlignment.Center;
                    led.VerticalAlignment = VerticalAlignment.Center;
                    led.Fill = _off;
                    led.SetValue(Grid.RowProperty, i);
                    led.SetValue(Grid.ColumnProperty, j);
                    led.PointerPressed += Led_PointerPressed;
                    _matrix.Children.Add(led);

                    setMatrixData(i, j, 0);
                }
            }
        }

        // viz http://robodoupe.cz/2013/maticovy-displej-8x8/
        private void setMatrixData(int row, int column, byte state)
        {
            // shift columns in grid to columns on device
            column = (column + 7) & 7;
            // write state to matrix
            _matrixData[row, column] = state;

            byte rowData = 0x00;

            // calculate byte            
            for (int i = 0; i < MATRIX_SIZE; i++)
            {
                rowData |= (byte)(_matrixData[row, i] << (byte)i);
            }

            // show byte value for row
            _matrixRowValue[row].Text = String.Format("0x{0:X2}", rowData);

            // if we have not real display return
            if (_matrixDevice == null)
            {
                return;
            }

            // write value to display
            _matrixDevice.Write(new byte[] { (byte)(row * 2), rowData });
        }

        private void Led_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Ellipse led = (Ellipse)sender;

            if (led.Fill == _off)
            {
                led.Fill = _on;
                setMatrixData((int)led.GetValue(Grid.RowProperty),
                    (int)led.GetValue(Grid.ColumnProperty),
                    0x01);
            }
            else
            {
                led.Fill = _off;
                setMatrixData((int)led.GetValue(Grid.RowProperty),
                    (int)led.GetValue(Grid.ColumnProperty),
                    0x00);
            }
        }

        private async void initLedMatrixDevice()
        {
            try
            {
                // I2C bus settings
                // Address of device is 0x70
                I2cConnectionSettings settings = new I2cConnectionSettings(0x70);
                // Using standard speed 100 kHZ
                settings.BusSpeed = I2cBusSpeed.StandardMode;

                // Get device on bus named I2C1
                string aqs = I2cDevice.GetDeviceSelector("I2C1");
                var dis = await DeviceInformation.FindAllAsync(aqs);
                _matrixDevice = await I2cDevice.FromIdAsync(dis[0].Id, settings);

                // diplay initialization
                _matrixDevice.Write(new byte[] { 0x21 });
                _matrixDevice.Write(new byte[] { 0x81 });

                // switch all LEDs off
                for (int i = 0; i < (MATRIX_SIZE * 2); i = i + 2)
                {
                    _matrixDevice.Write(new byte[] { (byte)i, 0x00 });
                }
            }
            catch
            {
                _matrixDevice = null;
            }
        }
    }
}