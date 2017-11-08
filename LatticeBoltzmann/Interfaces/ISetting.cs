using System;
using System.Reflection;
using LatticeBoltzmann.Models;

namespace LatticeBoltzmann.Interfaces
{
    public interface ISetting
    {
        string SettingName { get; set; }
        Type Type { get; }

        SettingsHolder SettingsHolder { get; }
        MethodInfo SetMethod { get; }
    }
}
