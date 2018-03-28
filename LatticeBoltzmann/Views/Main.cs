using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LatticeBoltzmann.Models;
using Rectangle = LatticeBoltzmann.Models.Rectangle;

namespace LatticeBoltzmann.Views
{
    public partial class Main : Form
    {
        private LatticeBoltzmannSimulator _simulator;
        private readonly Graphics _solidsGraphics;
        private readonly Bitmap _solidsImage;

        private bool _running;
        private int _counter;
        private double _maxT;
        private double _time;

        public Main()
        {
            InitializeComponent();
            InitializeSimulator();

            _solidsImage = new Bitmap(pbxSolids.Width, pbxSolids.Height);
            _solidsGraphics = Graphics.FromImage(_solidsImage);
            DrawSolids();
        }

        private void InitializeSimulator()
        {
            _simulator = new LatticeBoltzmannSimulator();
            _simulator.PropertyChanged += SettingChanged;

            AddSolids();

            var bedPoints = new[,]
            {
                {  0.0, 0.4 },
                {  5.0, 0.9 },
                {  6.0, 0.9 },
                { 16.0, 0.3 }
            };
            _simulator.SetBedShape(bedPoints);

            _simulator.Init();

            secSettingsEditor.SetDataSource(_simulator);
        }

        private void AddSolids()
        {
            /*
            _simulator.AddShape(new Rectangle(5.0, 4.5, 1, 1));
            _simulator.AddShape(new Rectangle(5.0, 11.5, 1, 1));
            _simulator.AddShape(new Rectangle(7.0, 4.5, 1, 1));
            _simulator.AddShape(new Rectangle(7.0, 11.5, 1, 1));

            // Top left, top right, bottom left, bottom right
            var trapeziumPoints = new[,]
            {
                { 5.0, 5.0 },
                { 7.0, 5.0 },
                { 7.0, 4.0 },
                { 5.0, 4.0 }
            };
            _simulator.AddShape(new Trapezium(trapeziumPoints));

            trapeziumPoints = new[,]
            {
                { 5.0, 12.0 },
                { 7.0, 12.0 },
                { 7.0, 11.0 },
                { 5.0, 11.0 }
            };
            _simulator.AddShape(new Trapezium(trapeziumPoints));
            */

            _simulator.AddShape(new Circle(8.0, 2.0, 0.5));
            _simulator.AddShape(new Circle(8.0, 5.0, 0.5));
            _simulator.AddShape(new Circle(8.0, 8.0, 0.5));
            _simulator.AddShape(new Circle(8.0, 11.0, 0.5));
            _simulator.AddShape(new Circle(8.0, 14.0, 0.5));
        }

        private void CalculateValues()
        {
            btnRun.Enabled = false;
            _running = true;

            txtConsole.Clear();

            Application.DoEvents();

            _simulator.ComputeEquilibriumDistributionFunction(setFEqToResult: true);

            _counter = 0;
            _maxT = Convert.ToInt32(nudIterations.Value);
            while (_counter < _maxT && _running)
            {
                _counter++;

                _time = _counter * _simulator.Dt;

                _simulator.ComputeEquilibriumDistributionFunction();
                _simulator.ComputeNewValues();

                txtConsole.Text += $@"Iteration {_counter:d4}: t = {_time:N2}, f[100, 80] = {_simulator.GetFunctionValueAtPoint(100, 80)}{Environment.NewLine}";
                txtConsole.SelectionStart = txtConsole.Text.Length - 1;
                txtConsole.ScrollToCaret();

                DrawFunction();
                DrawSolids();

                Application.DoEvents();
            }

            _running = false;
            btnRun.Enabled = true;
        }

        private void DrawSolids()
        {
            var isSolid = _simulator.GetSolids();

            for (var y = 0; y < _simulator.Ly; y++)
            {
                for (var x = 0; x < _simulator.Lx; x++)
                {
                    if (isSolid[x, y])
                    {
                        _solidsGraphics.DrawRectangle(Pens.Black, x, y, 1, 1);
                    }
                }
            }

            _solidsGraphics.Flush();
            pbxSolids.Image = _solidsImage;
        }

        private void DrawFunction()
        {
            var depth = _simulator.GetDepths();
            var f = _simulator.GetFunction();
            var sumF = new double[_simulator.Lx,_simulator.Ly];
            var max = 0.00001;

            for (var y = 0; y < _simulator.Ly; y++)
            {
                for (var x = 0; x < _simulator.Lx; x++)
                {
                    sumF[x, y] = f[1, x, y] + f[2, x, y] + f[8, x, y];
                    sumF[x, y] -= f[4, x, y] + f[5, x, y] + f[6, x, y];

                    if (double.IsNaN(sumF[x, y]) || double.IsInfinity(sumF[x, y]))
                    {
                        sumF[x, y] = 0;
                        _running = false;
                        return;
                    }

                    if (sumF[x, y] > max)
                    {
                        max = sumF[x, y];
                    }
                }
            }

            if (max > 0.2)
            {
                _running = false;
                return;
            }

            var correction = Convert.ToInt32(128 / max);

            for (var y = 0; y < _simulator.Ly; y++)
            {
                for (var x = 0; x < _simulator.Lx; x++)
                {
                    var correctedFunctionValue = sumF[x, y] > 0
                        ? 128 + Convert.ToInt32(sumF[x, y] * correction)
                        : 0;
                
                    if (correctedFunctionValue > 255)
                    {
                        correctedFunctionValue = 255;
                    }

                    var pen = correctedFunctionValue > 0 
                        ? new Pen(Color.FromArgb(correctedFunctionValue, 128, 192 + Convert.ToInt32(depth[x, y] * 20))) 
                        : Pens.Transparent;

                    _solidsGraphics.DrawRectangle(pen, x, y, 1, 1);
                }
            }

            _solidsGraphics.Flush();
            pbxSolids.Image = _solidsImage;
        }

        #region UI hooks

        private void btnRun_Click(object sender, EventArgs e)
        {
            CalculateValues();
        }

        private string GetSettingValue(string propertyName)
        {
            var properties = _simulator.GetType().GetProperties();
            return properties
                .FirstOrDefault(x => x.Name.Equals(propertyName))?
                .GetGetMethod()
                .Invoke(_simulator, null)
                .ToString();
        }

        private void SettingChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (_running)
            {
                return;
            }

            var value = GetSettingValue(eventArgs.PropertyName);
            tssStatus.Text = $@"{eventArgs.PropertyName} changed to {value}.";
            _simulator.Init();
        }

        #endregion UI hooks

    }
}
