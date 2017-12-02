using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LatticeBoltzmann.Models;
using MLApp;

namespace LatticeBoltzmann.Views
{
    public partial class Main : Form
    {
        private LatticeBoltzmannSimulator _simulator;
        private MLApp.MLApp _matLabApp;

        private const string PlotVelocityVectors = @"PlotVelocityVectors.m";
        private const string MatLabCodeDirectory = @"MatLabCode";

        public Main()
        {
            InitializeComponent();
            InitializeSimulator();
            InitializeMatLabApp();
        }

        private void InitializeSimulator()
        {
            _simulator = new LatticeBoltzmannSimulator(
                20, 5.5, 0.8, 0, 2.2, 201, 56, 1,
                9.81, 0.00248, 0.5, 10);

            _simulator.PropertyChanged += SettingChanged;

            var circle = new Circle(10, 5.5 / 2, 0.25);
            _simulator.AddShape(circle);

            secSettingsEditor.SetDataSource(_simulator);
        }

        private void InitializeMatLabApp()
        {
            _matLabApp = new MLApp.MLApp();

            var currentDirectory= Path.Combine(Directory.GetCurrentDirectory(), "..", "..");
            var matLabCodeDirectory = Path.Combine(currentDirectory, MatLabCodeDirectory);
            var path = Path.Combine(matLabCodeDirectory, PlotVelocityVectors);
            
            /*
            object result = null;
            _matLabApp.Execute(path);
            _matLabApp.Feval("plot_velocity_vectors", 1, out result);

            // Display result 
            var matLabResults = result as object[];

            MessageBox.Show(matLabResults[0].ToString());
            MessageBox.Show(matLabResults[1].ToString());
            //*/
        }

        private void SettingChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
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
            _simulator.Calculate();
        }
    }
}
