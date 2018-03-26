using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using LatticeBoltzmann.Models;

namespace LatticeBoltzmann.Views
{
    public partial class Main : Form
    {
        private LatticeBoltzmannSimulator _simulator;

        private bool _running;
        private int _counter;
        private double _time;

        public Main()
        {
            InitializeComponent();
            InitializeSimulator();
        }

        private void InitializeSimulator()
        {
            _simulator = new LatticeBoltzmannSimulator();

            _simulator.PropertyChanged += SettingChanged;

            _simulator.AddShape(new Rectangle(5.0,  4.5, 1, 1));
            _simulator.AddShape(new Rectangle(5.0, 11.5, 1, 1));
            _simulator.AddShape(new Rectangle(7.0,  4.5, 1, 1));
            _simulator.AddShape(new Rectangle(7.0, 11.5, 1, 1));

            // Top left, top right, bottom left, bottom right
            var trapeziumPoints = new[,]
            {
                { 5.0, 5.0 },
                { 7.0, 5.0 },
                { 5.0, 4.0 },
                { 7.0, 4.0 },
            };
            _simulator.AddShape(new Trapezium(trapeziumPoints));

            trapeziumPoints = new[,]
            {
                { 5.0, 12.0 },
                { 7.0, 12.0 },
                { 5.0, 11.0 },
                { 7.0, 11.0 },
            };
            _simulator.AddShape(new Trapezium(trapeziumPoints));

            var bedPoints = new[,]
            {
                {  0.0, 0.4 },
                {  5.0, 0.9 },
                {  6.0, 0.9 },
                { 16.0, 0.3 }
            };
            _simulator.SetBedShape(bedPoints);

            secSettingsEditor.SetDataSource(_simulator);
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            CalculateValues();
        }

        private void SettingChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (_running)
            {
                return;
            }

            var value = GetSettingValue(eventArgs.PropertyName);
            tssStatus.Text = $@"{eventArgs.PropertyName} changed to {value}.";
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

        private void CalculateValues()
        {
            txtConsole.Clear();

            btnRun.Enabled = false;
            _running = true;

            Application.DoEvents();

            _simulator.ComputeEquilibriumDistributionFunction(setFEqToResult: true);

            _simulator.MaxT = Convert.ToInt32(nudIterations.Value);

            _counter = 0;
            while (_counter < _simulator.MaxT)
            {
                _counter++;

                _time = _counter * _simulator.Delta;

                _simulator.ComputeEquilibriumDistributionFunction();
                _simulator.ComputeNewValues();

                txtConsole.Text += $@"Iteration {_counter:d4}: h[98, 51] = {_simulator.GetTrackedPointDepth()}{Environment.NewLine}";

                Application.DoEvents();
            }

            _running = false;
            btnRun.Enabled = true;
        }
    }
}
