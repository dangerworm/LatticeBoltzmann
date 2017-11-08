using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using LatticeBoltzmann.Models;

namespace LatticeBoltzmann.Views
{
    public partial class Main : Form
    {
        private readonly SettingsHolder _settings;

        public Main()
        {
            InitializeComponent();

            _settings = new SettingsHolder(
                20, 5.5, 0.8, 0, 2.2, 201, 56, 1,
                9.81, 0.00248, 0.25, 0.5, 10);

            InitializeSettingsEditor();
        }

        private void InitializeSettingsEditor()
        {
            secSettingsEditor.SetDataSource(_settings);
            _settings.PropertyChanged += SettingChanged;
        }

        private void SettingChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            var value = GetSettingValue(eventArgs.PropertyName);
            tssStatus.Text = $@"{eventArgs.PropertyName} changed to {value}.";
        }

        private string GetSettingValue(string propertyName)
        {
            var properties = _settings.GetType().GetProperties();
            return properties
                .FirstOrDefault(x => x.Name.Equals(propertyName))?
                .GetGetMethod()
                .Invoke(_settings, null)
                .ToString();
        }
    }
}
