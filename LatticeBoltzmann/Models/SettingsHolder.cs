using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LatticeBoltzmann.Annotations;

namespace LatticeBoltzmann.Models
{
    public class SettingsHolder : INotifyPropertyChanged
    {
        private double _accelerationDueToGravity;
        private double _length;
        private double _width;
        private double _h0;
        private double _v0;
        private double _q0;
        private double _Lx;
        private double _Ly;
        private double _max_t;
        private double _fb;
        private double _r;
        private double _D;
        private double _e;

        // Must be set by user
        public double Length
        {
            get => _length;
            set
            {
                _length = value;
                OnPropertyChanged(nameof(Length));
            }
        }

        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged(nameof(Width));
            }
        }

        public double H0
        {
            get => _h0;
            set
            {
                _h0 = value;
                OnPropertyChanged(nameof(H0));
            }
        }

        public double V0
        {
            get => _v0;
            set
            {
                _v0 = value;
                OnPropertyChanged(nameof(V0));
            }
        }

        public double Q0
        {
            get => _q0;
            set
            {
                _q0 = value;
                OnPropertyChanged(nameof(Q0));
            }
        }

        public double Lx
        {
            get => _Lx;
            set
            {
                _Lx = value;
                OnPropertyChanged(nameof(Lx));
            }
        }

        public double Ly
        {
            get => _Ly;
            set
            {
                _Ly = value;
                OnPropertyChanged(nameof(Ly));
            }
        }

        public double MaxT
        {
            get => _max_t;
            set
            {
                _max_t = value;
                OnPropertyChanged(nameof(MaxT));
            }
        }

        public double AccelerationDueToGravity
        {
            get => _accelerationDueToGravity;
            set
            {
                _accelerationDueToGravity = value;
                OnPropertyChanged(nameof(AccelerationDueToGravity));
            }
        }

        public double Fb
        {
            get => _fb;
            set
            {
                _fb = value;
                OnPropertyChanged(nameof(Fb));
            }
        }

        public double SolidRadius
        {
            get => _r;
            set
            {
                _r = value;
                OnPropertyChanged(nameof(SolidRadius));
            }
        }

        public double D
        {
            get => _D;
            set
            {
                _D = value;
                OnPropertyChanged(nameof(D));
            }
        }

        public double E
        {
            get => _e;
            set
            {
                _e = value;
                OnPropertyChanged(nameof(E));
            }
        }

        // Calculated based on values above
        public double Dx => Length / (Lx - 1);
        public double U0 => Q0 / (H0 * Width);
        public double Dy => Dx;
        public double Dt => Dx / E;
        public double Tau => 0.5 * (1 + 0.01 * 6 * Dt / (Dx * Dx));
        public double Nu => E * Dx * (2 * Tau - 1) / 6;
        public double Xs => 1;
        public double Xe => Lx;
        public double Ys => 1;
        public double Ye => Ly;
        public double Nermax => (Lx - 1) * (Ly - 1);
        public double Fr => U0 / Math.Sqrt(AccelerationDueToGravity * H0);
        public double Re => U0 * H0 / Nu;
        public double ReD => U0 * D / Nu;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SettingsHolder(double length, double width, double h0,
            double v0, double q0, double lx, double ly, double maxT,
            double accelerationDueToGravity, double fb,
            double solidRadius, double d, double e)
        {
            Length = length;
            Width = width;
            H0 = h0;
            V0 = v0;
            Q0 = q0;
            Lx = lx;
            Ly = ly;
            MaxT = maxT;
            AccelerationDueToGravity = accelerationDueToGravity;
            Fb = fb;
            SolidRadius = solidRadius;
            D = d;
            E = e;
        }
    }
}
