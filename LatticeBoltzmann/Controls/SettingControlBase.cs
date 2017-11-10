using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LatticeBoltzmann.Annotations;
using LatticeBoltzmann.Helpers;
using LatticeBoltzmann.Interfaces;

namespace LatticeBoltzmann.Controls
{
    public class SettingControlBase<T> : UserControl, ISetting<T>, INotifyPropertyChanged
    {
        public Type Type => typeof(T);
        public ISimulator Simulator { get; }
        public MethodInfo SetMethod { get; }

        protected string _settingName { get; set; }
        protected T _value { get; set; }

        protected Binding _lblBinding { get; set; }
        protected Binding _txtBinding { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public string SettingName
        {
            get { return _settingName; }
            set
            {
                _settingName = value;
                OnPropertyChanged(nameof(SettingName));
            }
        }

        public T Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
                SetMethod.Invoke(Simulator, new object[] { value });
            }
        }

        public string ValueText
        {
            get { return _value.ToString(); }
            set { Value = TypeHelper.Convert<T>(value); }
        }

        public SettingControlBase(string settingName, T value, 
            ISimulator simulator, MethodInfo setMethod)
        {
            //var name = $@"{settingName.Trim()} ({typeof(T)})";
            //Name = name;
            Simulator = simulator;
            SetMethod = setMethod;

            Name = settingName.Trim();
            SettingName = Name;
            Value = value;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null, string value = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
