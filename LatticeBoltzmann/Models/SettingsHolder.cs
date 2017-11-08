using System;
using System.Collections.Generic;
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
            set => SetPropertyField(nameof(Length), ref _length, value);
        }

        public double Width
        {
            get => _width;
            set => SetPropertyField(nameof(Width), ref _width, value);
        }

        public double H0
        {
            get => _h0;
            set => SetPropertyField(nameof(H0), ref _h0, value);
        }

        public double V0
        {
            get => _v0;
            set => SetPropertyField(nameof(V0), ref _v0, value);
        }

        public double Q0
        {
            get => _q0;
            set => SetPropertyField(nameof(Q0), ref _q0, value);
        }

        public double Lx
        {
            get => _Lx;
            set => SetPropertyField(nameof(Lx), ref _Lx, value);
        }

        public double Ly
        {
            get => _Ly;
            set => SetPropertyField(nameof(Ly), ref _Ly, value);
        }

        public double MaxT
        {
            get => _max_t;
            set => SetPropertyField(nameof(MaxT), ref _max_t, value);
        }

        public double AccelerationDueToGravity
        {
            get => _accelerationDueToGravity;
            set => SetPropertyField(nameof(AccelerationDueToGravity), ref _accelerationDueToGravity, value);
        }

        public double Fb
        {
            get => _fb;
            set => SetPropertyField(nameof(Fb), ref _fb, value);
        }

        public double SolidRadius
        {
            get => _r;
            set => SetPropertyField(nameof(SolidRadius), ref _r, value);
        }

        public double D
        {
            get => _D;
            set => SetPropertyField(nameof(D), ref _D, value);
        }

        public double E
        {
            get => _e;
            set => SetPropertyField(nameof(E), ref _e, value);
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

        protected void SetPropertyField<T>(string propertyName, ref T field, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue)) return;
            field = newValue;
            OnPropertyChanged(propertyName);
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
