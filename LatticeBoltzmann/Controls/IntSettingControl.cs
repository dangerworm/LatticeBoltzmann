using System.Reflection;
using System.Windows.Forms;
using LatticeBoltzmann.Interfaces;
using LatticeBoltzmann.Models;

namespace LatticeBoltzmann.Controls
{
    public partial class IntSettingControl : SettingControlBase<int>
    {
        public IntSettingControl(ISetting<int> setting)
        : this(setting.SettingName, setting.Value,
              setting.Simulator, setting.SetMethod)
        {
        }

        public IntSettingControl(string settingName, int value,
            ISimulator simulator , MethodInfo setMethod)
            : base(settingName, value, simulator, setMethod)
        {
            InitializeComponent();

            _lblBinding = new Binding(nameof(lblSettingName.Text), this, nameof(SettingName));
            _txtBinding = new Binding(nameof(txtSettingValue.Text), this, nameof(ValueText));

            lblSettingName.DataBindings.Add(_lblBinding);
            txtSettingValue.DataBindings.Add(_txtBinding);
        }
    }
}
