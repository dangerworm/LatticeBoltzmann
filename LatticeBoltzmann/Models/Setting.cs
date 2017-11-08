using System;
using System.Reflection;
using LatticeBoltzmann.Interfaces;

namespace LatticeBoltzmann.Models
{
    public class Setting<T> : ISetting<T>
    {
        public string SettingName { get; set; }
        public T Value { get; set; }
        public Type Type => typeof(T);

        public SettingsHolder SettingsHolder { get; }
        public MethodInfo SetMethod { get; }

        public Setting(string settingName, T value,
            SettingsHolder settingsHolder, MethodInfo setMethod)
        {
            SettingName = settingName;
            Value = value;

            SettingsHolder = settingsHolder;
            SetMethod = setMethod;
        }
    }
}
