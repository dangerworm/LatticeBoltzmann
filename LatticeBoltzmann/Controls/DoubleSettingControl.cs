using System.Reflection;
using System.Windows.Forms;
using LatticeBoltzmann.Interfaces;
using LatticeBoltzmann.Models;

namespace LatticeBoltzmann.Controls
{
    public partial class DoubleSettingControl : SettingControlBase<double>
    {
        public DoubleSettingControl(ISetting<double> setting)
           : this(setting.SettingName, setting.Value,
            setting.SettingsHolder, setting.SetMethod)
        {
        }

        public DoubleSettingControl(string settingName, double value,
            SettingsHolder settingsHolder, MethodInfo setMethod)
            : base(settingName, value, settingsHolder, setMethod)
        {
            InitializeComponent();

            var lblBinding = new Binding(nameof(lblSettingName.Text), this, nameof(SettingName));
            var txtBinding = new Binding(nameof(txtSettingValue.Text), this, nameof(ValueText));

            lblSettingName.DataBindings.Add(lblBinding);
            txtSettingValue.DataBindings.Add(txtBinding);
        }
    }
}
