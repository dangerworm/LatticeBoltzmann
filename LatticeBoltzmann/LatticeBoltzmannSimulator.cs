using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LatticeBoltzmann.Annotations;
using LatticeBoltzmann.Helpers;
using LatticeBoltzmann.Interfaces;
using LatticeBoltzmann.Models;

namespace LatticeBoltzmann
{
    public class LatticeBoltzmannSimulator : ISimulator
    {
        private const double SolidValuesDefault = 2;
        private const double BedDataStartX = 0;

        private const int DirectionX = 0;
        private const int DirectionY = 1;

        private const int ParticleGridBox9 = 0;
        private const int ParticleGridBox1 = 1;
        private const int ParticleGridBox2 = 2;
        private const int ParticleGridBox3 = 3;
        private const int ParticleGridBox4 = 4;
        private const int ParticleGridBox5 = 5;
        private const int ParticleGridBox6 = 6;
        private const int ParticleGridBox7 = 7;
        private const int ParticleGridBox8 = 8;

        private double _accelerationDueToGravity;
        private double _length;
        private double _width;
        private double _h0;
        private double _v0;
        private double _q0;
        private int _Lx;
        private int _Ly;
        private double _max_t;
        private double _fb;
        private double _r;
        private double _D;
        private double _e;

        private double[,] _solidValues;
        private double[,] _bedData;
        private double[][,] _bedSlopeData;
        private double[,] _h;
        private double[,] _u;
        private double[,] _v;

        private double[][] _particleVectors;

        private ICollection<Shape> _shapes;

        // Must be set by user
        [Description("Length")]
        public double Length
        {
            get { return _length; }
            set { SetPropertyField(nameof(Length), ref _length, value); }
        }

        [Description("Width")]
        public double Width
        {
            get { return _width; }
            set { SetPropertyField(nameof(Width), ref _width, value); }
        }

        [Description("Height?")]
        public double H0
        {
            get { return _h0; }
            set { SetPropertyField(nameof(H0), ref _h0, value); }
        }

        public double V0
        {
            get { return _v0; }
            set { SetPropertyField(nameof(V0), ref _v0, value); }
        }

        public double Q0
        {
            get { return _q0; }
            set { SetPropertyField(nameof(Q0), ref _q0, value); }
        }

        public int Lx
        {
            get { return _Lx; }
            set { SetPropertyField(nameof(Lx), ref _Lx, value); }
        }

        public int Ly
        {
            get { return _Ly; }
            set { SetPropertyField(nameof(Ly), ref _Ly, value); }
        }

        public double MaxT
        {
            get { return _max_t; }
            set { SetPropertyField(nameof(MaxT), ref _max_t, value); }
        }

        public double AccelerationDueToGravity
        {
            get { return _accelerationDueToGravity; }
            set { SetPropertyField(nameof(AccelerationDueToGravity), ref _accelerationDueToGravity, value); }
        }

        public double Fb
        {
            get { return _fb; }
            set { SetPropertyField(nameof(Fb), ref _fb, value); }
        }

        [Description("Solid Radius")]
        public double R
        {
            get { return _r; }
            set { SetPropertyField(nameof(R), ref _r, value); }
        }

        public double D
        {
            get { return _D; }
            set { SetPropertyField(nameof(D), ref _D, value); }
        }

        public double E
        {
            get { return _e; }
            set { SetPropertyField(nameof(E), ref _e, value); }
        }

        // Calculated based on values above
        public double U0 => Q0 / (H0 * Width);

        public double Dx => Length / (Lx - 1);
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

        public LatticeBoltzmannSimulator(double length, double width, double h0,
            double v0, double q0, int lx, int ly, double maxT,
            double accelerationDueToGravity, double fb,
            double r, double d, double e)
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
            R = r;
            D = d;
            E = e;

            _shapes = new List<Shape>();

            InitValues();
            InitParticleVectors(E);
        }

        public void AddShape(Shape shape)
        {
            _shapes.Add(shape);
        }

        public void RemoveShape(Shape shape)
        {
            _shapes.Remove(shape);
        }

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

        private void InitValues()
        {
            _solidValues = DataHelper.GetNew2DArray(_Lx, _Ly, 0.0);

            _bedData = DataHelper.GetNew2DArray(_Lx, _Ly, 0.0);

            _bedSlopeData = new double[2][,];
            _bedSlopeData[DirectionX] = DataHelper.GetNew2DArray(_Lx, _Ly, 0.0);
            _bedSlopeData[DirectionY] = DataHelper.GetNew2DArray(_Lx, _Ly, 0.0);

            _h = DataHelper.GetNew2DArray(_Lx, _Ly, 0.0);
            _u = DataHelper.GetNew2DArray(_Lx, _Ly, 0.0);
            _v = DataHelper.GetNew2DArray(_Lx, _Ly, 0.0);

            for (var y = 0; y < _Ly; y++)
            {
                for (var x = 0; x < _Lx; x++)
                {
                    // Solid values
                    foreach (var shape in _shapes)
                    {
                        if (shape.IsSolid((x - 1) * Dx, (y - 1) * Dy, _r))
                        {
                            _solidValues[x, y] = SolidValuesDefault;
                        }
                    }

                    // Bed data
                    _bedData[x, y] = BedDataStartX * (x - 1) * Dx + 0.0025; // Negative

                    // Bed slope data
                    /*
                     * Nothing here yet
                     */

                    // Depth and velocity fields
                    if (_solidValues[x, y] <= 1)
                    {
                        _h[x, y] = H0 - _bedData[x, y];
                        _v[x, y] = 0;
                        _u[x, y] = U0;
                    }
                    else
                    { 
                        _h[x, y] = 0;
                        _u[x, y] = 0;
                        _v[x, y] = 0;
                    }
                }

                // Boundary data (y)
                _bedSlopeData[DirectionX][_Lx-1, y] = 0.0;
                _bedSlopeData[DirectionY][_Lx-1, y] = 0.0;
            }

            // Boundary data (x)
            for (var x = 0; x < _Lx; x++)
            {
                _solidValues[x, 1] = 1;
                _solidValues[x, _Ly-1] = 1;

                _bedSlopeData[DirectionX][x, _Ly-1] = 0.0;
                _bedSlopeData[DirectionY][x, _Ly-1] = 0.0;
            }

            _bedSlopeData[DirectionX][_Lx-1, _Ly-1] = 0.0;
            _bedSlopeData[DirectionY][_Lx-1, _Ly-1] = 0.0;
        }

        private void InitParticleVectors(double e)
        {
            _particleVectors = new double[2][];
            _particleVectors[DirectionX] = new double[9];
            _particleVectors[DirectionY] = new double[9];

            _particleVectors[DirectionX][ParticleGridBox1] = e;  _particleVectors[DirectionY][ParticleGridBox1] = 0;
            _particleVectors[DirectionX][ParticleGridBox2] = e;  _particleVectors[DirectionY][ParticleGridBox2] = e;
            _particleVectors[DirectionX][ParticleGridBox3] = 0;  _particleVectors[DirectionY][ParticleGridBox3] = e;
            _particleVectors[DirectionX][ParticleGridBox4] = -e; _particleVectors[DirectionY][ParticleGridBox4] = e;
            _particleVectors[DirectionX][ParticleGridBox5] = -e; _particleVectors[DirectionY][ParticleGridBox5] = 0;
            _particleVectors[DirectionX][ParticleGridBox6] = -e; _particleVectors[DirectionY][ParticleGridBox6] = -e;
            _particleVectors[DirectionX][ParticleGridBox7] = 0;  _particleVectors[DirectionY][ParticleGridBox7] = -e;
            _particleVectors[DirectionX][ParticleGridBox8] = e;  _particleVectors[DirectionY][ParticleGridBox8] = -e;
            _particleVectors[DirectionX][ParticleGridBox9] = 0;  _particleVectors[DirectionY][ParticleGridBox9] = 0;
        }
    }
}
