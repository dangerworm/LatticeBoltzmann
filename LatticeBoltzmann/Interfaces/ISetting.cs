using System;
using System.Reflection;

namespace LatticeBoltzmann.Interfaces
{
    public interface ISetting
    {
        string SettingName { get; set; }
        Type Type { get; }

        ISimulator Simulator { get; }
        MethodInfo SetMethod { get; }
    }
}
