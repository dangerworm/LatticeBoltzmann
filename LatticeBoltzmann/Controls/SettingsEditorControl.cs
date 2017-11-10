using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using LatticeBoltzmann.Helpers;
using LatticeBoltzmann.Interfaces;
using LatticeBoltzmann.Models;

namespace LatticeBoltzmann.Controls
{
    public partial class SettingsEditorControl : UserControl
    {
        public Collection<UserControl> SettingControls { get; private set; }

        private ISimulator _simulator { get; set; }

        public SettingsEditorControl()
        {
            InitializeComponent();
        }

        public void SetDataSource(ISimulator settings)
        {
            _simulator = settings;
            InitializeControls();
        }

        private void InitializeControls()
        {
            SettingControls = new Collection<UserControl>();
            
            // Reflection
            var properties = _simulator
                .GetType()
                .GetProperties()
                .Where(x => x.CanWrite);

            foreach (var property in properties)
            {
                AddSettingControl(property);
            }

            SettingControls = new Collection<UserControl>(SettingControls
                .OrderBy(x => (x as ISetting)?.SettingName)
                .ToList());

            foreach (var control in SettingControls)
            {
                flpSettings.Controls.Add(control);
            }
        }

        private void AddSettingControl(PropertyInfo property)
        {
            UserControl control = null;

            if (property.PropertyType == typeof(double))
            {
                var setting = GetSetting<double>(property);
                control = new DoubleSettingControl(setting);
            }
            else if (property.PropertyType == typeof(int))
            {
                var setting = GetSetting<int>(property);
                control = new IntSettingControl(setting);
            }
            else if (property.PropertyType == typeof(string))
            {
                var setting = GetSetting<string>(property);
                control = new StringSettingControl(setting);
            }

            if (control == null)
            {
                return;
            }

            control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            control.Width = flpSettings.Width - 25;

            SettingControls.Add(control);
        }

        private ISetting<T> GetSetting<T>(PropertyInfo property)
        {
            var value = (T)property.GetGetMethod().Invoke(_simulator, null);
            return new Setting<T>(property.GetDescription(), value, _simulator, property.GetSetMethod());
        }
    }
}
