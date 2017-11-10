using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using LatticeBoltzmann.Models;

namespace LatticeBoltzmann.Views
{
    public partial class Main : Form
    {
        private LatticeBoltzmannSimulator _simulator;

        public Main()
        {
            InitializeComponent();
            InitializeSimulator();
        }

        private void InitializeSimulator()
        {
            _simulator = new LatticeBoltzmannSimulator(
                20, 5.5, 0.8, 0, 2.2, 201, 56, 1,
                9.81, 0.00248, 0.25, 0.5, 10);

            _simulator.PropertyChanged += SettingChanged;

            var circle = new Circle(10, 5.5 / 2);
            _simulator.AddShape(circle);

            secSettingsEditor.SetDataSource(_simulator);

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
            
        }
    }
}
