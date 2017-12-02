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

        private readonly ICollection<Shape> _shapes;

        private double _gAcl;
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

        private double[,,] _f;
        private double[,,] _fEq;
        private double[,,] _fTemp;

        private double[][] _particleVectors;

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

        [Description("Acceleration due to gravity")]
        public double GAcl
        {
            get { return _gAcl; }
            set { SetPropertyField(nameof(_gAcl), ref _gAcl, value); }
        }

        public double Fb
        {
            get { return _fb; }
            set { SetPropertyField(nameof(_fb), ref _fb, value); }
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
        public double Xs => 0;
        public double Xe => Lx - 1;
        public double Ys => 0;
        public double Ye => Ly - 1;
        public double Nermax => (Lx - 1) * (Ly - 1);
        public double Fr => U0 / Math.Sqrt(_gAcl * H0);
        public double Re => U0 * H0 / Nu;
        public double ReD => U0 * D / Nu;

        public event PropertyChangedEventHandler PropertyChanged;

        public LatticeBoltzmannSimulator(double length, double width, double h0,
            double v0, double q0, int lx, int ly, double maxT,
            double gAcl, double fb, double d, double e)
        {
            Length = length;
            Width = width;
            H0 = h0;
            V0 = v0;
            Q0 = q0;
            Lx = lx;
            Ly = ly;
            MaxT = maxT;
            GAcl = gAcl;
            Fb = fb;
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

        public void Calculate()
        {
            _f = new double[9, Lx, Ly];
            _fEq = new double[9, Lx, Ly];

            ComputeFeq();

            Array.Copy(_fTemp, _f, Lx * Ly);

            var additionalIterations = 0;
            var iteration = 0;
            //var time = 0.0;
            while (iteration < MaxT + additionalIterations)
            {
                iteration++;
                //time = iteration * Dt;

                ComputeFeq();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetPropertyField<T>(string propertyName, ref T field, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return;
            }
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
                        if (shape.IsSolid((x - 1) * Dx, (y - 1) * Dy))
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
                _bedSlopeData[DirectionX][_Lx - 1, y] = 0.0;
                _bedSlopeData[DirectionY][_Lx - 1, y] = 0.0;
            }

            // Boundary data (x)
            for (var x = 0; x < _Lx; x++)
            {
                _solidValues[x, 1] = 1;
                _solidValues[x, _Ly - 1] = 1;

                _bedSlopeData[DirectionX][x, _Ly - 1] = 0.0;
                _bedSlopeData[DirectionY][x, _Ly - 1] = 0.0;
            }

            _bedSlopeData[DirectionX][_Lx - 1, _Ly - 1] = 0.0;
            _bedSlopeData[DirectionY][_Lx - 1, _Ly - 1] = 0.0;
        }

        private void InitParticleVectors(double e)
        {
            _particleVectors = new double[2][];
            _particleVectors[DirectionX] = new double[9];
            _particleVectors[DirectionY] = new double[9];

            _particleVectors[DirectionX][ParticleGridBox1] = e; _particleVectors[DirectionY][ParticleGridBox1] = 0;
            _particleVectors[DirectionX][ParticleGridBox2] = e; _particleVectors[DirectionY][ParticleGridBox2] = e;
            _particleVectors[DirectionX][ParticleGridBox3] = 0; _particleVectors[DirectionY][ParticleGridBox3] = e;
            _particleVectors[DirectionX][ParticleGridBox4] = -e; _particleVectors[DirectionY][ParticleGridBox4] = e;
            _particleVectors[DirectionX][ParticleGridBox5] = -e; _particleVectors[DirectionY][ParticleGridBox5] = 0;
            _particleVectors[DirectionX][ParticleGridBox6] = -e; _particleVectors[DirectionY][ParticleGridBox6] = -e;
            _particleVectors[DirectionX][ParticleGridBox7] = 0; _particleVectors[DirectionY][ParticleGridBox7] = -e;
            _particleVectors[DirectionX][ParticleGridBox8] = e; _particleVectors[DirectionY][ParticleGridBox8] = -e;
            _particleVectors[DirectionX][ParticleGridBox9] = 0; _particleVectors[DirectionY][ParticleGridBox9] = 0;
        }

        /// <summary>
        /// Compute the equilibrium distribution function fEq
        /// </summary>
        private void ComputeFeq()
        {
            for (var y = 0; y < Ly; y++)
            {
                for (var x = 0; x < Lx; x++)
                {
                    if (_solidValues[x, y] <= 1)
                    {
                        for (var a = 1; a <= 8; a++)
                        {
                            if (a % 2 == 0)
                            {
                                _fEq[a - 1, x, y] =
                                    GAcl * Math.Pow(_h[x, y], 2) / (24 * Math.Pow(E, 2)) +          // gacl*h(x,y)^2/(24*e^2)+
                                    _h[x, y] / (12 * Math.Pow(E, 2)) *                              // h(x,y)/(12*e^2)*
                                        (_particleVectors[DirectionX][a] * _u[x, y] +               //   (ex(a)*u(x,y)+
                                         _particleVectors[DirectionY][a] * _v[x, y]) +              //    ey(a)*v(x,y))+
                                    _h[x, y] / (8 * Math.Pow(E, 4)) *                               // h(x,y)/(8*e^4)*
                                        (Math.Pow(_particleVectors[DirectionX][a] * _u[x, y], 2) +  //   (ex(a)*u(x,y)*ex(a)*u(x,y)+
                                         2 * _particleVectors[DirectionX][a] * _u[x, y] *           //    2*ex(a)*u(x,y)*
                                         _particleVectors[DirectionY][a] * _v[x, y] +               //    ey(a)*v(x,y)+
                                         Math.Pow(_particleVectors[DirectionY][a] * _v[x, y], 2)) - //    ey(a)*v(x,y)*ey(a)*v(x,y))-
                                    _h[x, y] / (24 * Math.Pow(E, 2)) *                              // h(x,y)/(24*e^2)*
                                    (Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2));                // (u(x,y)^2+v(x,y)^2);
                            }
                            else
                            {
                                _fEq[a - 1, x, y] =
                                    GAcl * Math.Pow(_h[x, y], 2) / (6 * Math.Pow(E, 2)) +           // gacl*h(x,y)^2/(6*e^2)+
                                    _h[x, y] / (3 * Math.Pow(E, 2)) *                               // h(x,y)/(3*e^2)*
                                    (_particleVectors[DirectionX][a] * _u[x, y] +                   //   (ex(a)*u(x,y)+
                                     _particleVectors[DirectionY][a] * _v[x, y]) +                  //    ey(a)*v(x,y))+
                                    _h[x, y] / (2 * Math.Pow(E, 4)) *                               // h(x,y)/(2*e^4)*
                                    (Math.Pow(_particleVectors[DirectionX][a] * _u[x, y], 2) +      //   (ex(a)*u(x,y)*ex(a)*u(x,y)+
                                     2 * _particleVectors[DirectionX][a] * _u[x, y] *               //    2*ex(a)*u(x,y)*
                                     _particleVectors[DirectionY][a] * _v[x, y] +                   //    ey(a)*v(x,y)+
                                     Math.Pow(_particleVectors[DirectionY][a] * _v[x, y], 2)) -     //    ey(a)*v(x,y)*ey(a)*v(x,y))-
                                    _h[x, y] / (6 * Math.Pow(E, 2)) *                               // h(x,y)/(6*e^2)*
                                    (Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2));                // (u(x,y)^2+v(x,y)^2);
                            }
                        }
                        _fEq[8, x, y] =
                            _h[x, y] - 5 * GAcl * Math.Pow(_h[x, y], 2) / (6 * Math.Pow(E, 2)) -    // h(x,y)-5*gacl*h(x,y)^2/(6*e^2)-
                            2 * _h[x, y] / (3 * Math.Pow(E, 2)) *                                   // 2*h(x,y)/(3*e^2)*
                            Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2);                          // (u(x,y)^2+v(x,y)^2);  
                    }
                    else
                    {
                        for (var a = 0; a < 9; a++)
                        {
                            _fEq[a, x, y] = 0.0;
                        }
                    }
                }
            }
        }


        private void CollideStream()
        {
            for (var y = 0; y < Ly; y++)
            {
                var yp = y + 1;
                var yn = y - 1;

                for (var x = 0; x < Lx; x++)
                {
                    if (_solidValues[x, y] <= 1)
                    {
                        var xp = x + 1;
                        var xn = x - 1;

                        if (xp < Lx && _solidValues[xp, y] <= 1)
                        {
                            _fTemp[0, xp, y] =  _f[0, x, y] - (_f[0, x, y] - _fEq[0, x, y]) / Tau - Dt / (6 * E * E) * GAcl * 0.5 * (_h[x, y] + _h[xp, y]) *  (_particleVectors[DirectionX][0] * _bedSlopeData[DirectionX][x, y] +  _particleVectors[DirectionY][0] * _bedSlopeData[DirectionY][x, y]) - Dt /  (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[DirectionX][0] * _u[x, y] + _particleVectors[DirectionY][0] * _v[x, y]);
                        }

                        if (xp < Lx && yp < Ly && _solidValues[xp, yp] <= 1)
                        {
                            _fTemp[1, xp, yp] = _f[1, x, y] - (_f[1, x, y] - _fEq[1, x, y]) / Tau - Dt / (6 * E * E) * GAcl * 0.5 * (_h[x, y] + _h[xp, yp]) * (_particleVectors[DirectionX][1] * _bedSlopeData[DirectionX][x, y] +  _particleVectors[DirectionY][1] * _bedSlopeData[DirectionY][x, y]) - Dt /  (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[DirectionX][1] * _u[x, y] + _particleVectors[DirectionY][1] * _v[x, y]);
                        }

                        if (yp < Ly && _solidValues[x, yp] <= 1)
                        {
                            _fTemp[2, x, yp] =  _f[2, x, y] - (_f[2, x, y] - _fEq[2, x, y]) / Tau - Dt / (6 * E * E) * GAcl * 0.5 * (_h[x, y] + _h[x, yp]) *  (_particleVectors[DirectionX][2] * _bedSlopeData[DirectionX][x, y] +  _particleVectors[DirectionY][2] * _bedSlopeData[DirectionY][x, y]) - Dt /  (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[DirectionX][2] * _u[x, y] + _particleVectors[DirectionY][2] * _v[x, y]);
                        }

                        if (xn >= 0 && yp < Ly && _solidValues[xn, yp] <= 1)
                        {
                            _fTemp[3, xn, yp] = _f[3, x, y] - (_f[3, x, y] - _fEq[3, x, y]) / Tau - Dt / (6 * E * E) * GAcl * 0.5 * (_h[x, y] + _h[xn, yp]) * (_particleVectors[DirectionX][3] * _bedSlopeData[DirectionX][xn, y] + _particleVectors[DirectionY][3] * _bedSlopeData[DirectionY][x, y]) - Dt /  (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[DirectionX][3] * _u[x, y] + _particleVectors[DirectionY][3] * _v[x, y]);
                        }

                        if (xn >= 0 && _solidValues[xn, y] <= 1)
                        {
                            _fTemp[4, xn, y] =  _f[4, x, y] - (_f[4, x, y] - _fEq[4, x, y]) / Tau - Dt / (6 * E * E) * GAcl * 0.5 * (_h[x, y] + _h[xn, y]) *  (_particleVectors[DirectionX][4] * _bedSlopeData[DirectionX][xn, y] + _particleVectors[DirectionY][4] * _bedSlopeData[DirectionY][x, y]) - Dt /  (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[DirectionX][4] * _u[x, y] + _particleVectors[DirectionY][4] * _v[x, y]);
                        }

                        if (xn >= 0 && yn >= 0 && _solidValues[xn, yn] <= 1)
                        {
                            _fTemp[5, xn, yn] = _f[5, x, y] - (_f[5, x, y] - _fEq[5, x, y]) / Tau - Dt / (6 * E * E) * GAcl * 0.5 * (_h[x, y] + _h[xn, yn]) * (_particleVectors[DirectionX][5] * _bedSlopeData[DirectionX][xn, y] + _particleVectors[DirectionY][5] * _bedSlopeData[DirectionY][x, yn]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[DirectionX][5] * _u[x, y] + _particleVectors[DirectionY][5] * _v[x, y]);
                        }

                        if (yn >= 0 && _solidValues[x, yn] <= 1)
                        {
                            _fTemp[6, x, yn] =  _f[6, x, y] - (_f[6, x, y] - _fEq[6, x, y]) / Tau - Dt / (6 * E * E) * GAcl * 0.5 * (_h[x, y] + _h[x, yn]) *  (_particleVectors[DirectionX][6] * _bedSlopeData[DirectionX][x, y] +  _particleVectors[DirectionY][6] * _bedSlopeData[DirectionY][x, yn]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[DirectionX][6] * _u[x, y] + _particleVectors[DirectionY][6] * _v[x, y]);
                        }

                        if (xp < Lx && yn >= 0 && _solidValues[xp, yn] <= 1)
                        {
                            _fTemp[7, xp, yn] =  _f[7, x, y] - (_f[7, x, y] - _fEq[7, x, y]) / Tau - Dt / (6 * E * E) * GAcl * 0.5 * (_h[x, y] + _h[xp, yn]) * (_particleVectors[DirectionX][7] * _bedSlopeData[DirectionX][x, y] +  _particleVectors[DirectionY][7] * _bedSlopeData[DirectionY][x, yn]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[DirectionX][7] * _u[x, y] + _particleVectors[DirectionY][7] * _v[x, y]);
                        }

                        _fTemp[8, x, y] = _f[8, x, y] - (_f[8, x, y] - _fEq[8, x, y]) / Tau;
                    }
                    else
                    {
                        for (var a = 0; a < 9; a++)
                        {
                            _fTemp[a, x, y] = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Slip
        /// </summary>
        private void BcBodySlipNs()
        {
            for (var x = 0; x < Lx; x++)
            {
                _fTemp[2, x, 0] = _fTemp[8, x, 0];
                _fTemp[3, x, 0] = _fTemp[7, x, 0];
                _fTemp[4, x, 0] = _fTemp[6, x, 0];
                
                _fTemp[8, x, Ly - 1] = _fTemp[2, x, Ly - 1];
                _fTemp[7, x, Ly - 1] = _fTemp[3, x, Ly - 1];
                _fTemp[6, x, Ly - 1] = _fTemp[4, x, Ly - 1];
            }
        }

        /// <summary>
        /// No slip
        /// </summary>
        private void BcBodyMain()
        {
            // x/y differences per loop
            var dx = new[] {1, 1, 0, -1, -1, -1, 0, 1};
            var dy = new[] {0, 1, 1, 1, 0, -1, -1, -1};

            // Map sources/destinations
            var src = new[] {0, 1, 2, 3, 4, 5, 6, 7};
            var dst = new[] {4, 5, 6, 7, 0, 1, 2, 3};

            for (var y = 1; y < Ly - 1; y++)
            {
                for (var x = 1; y < Lx - 1; x++)
                {
                    if (!_solidValues[x, y].Equals(0))
                    {
                        continue;
                    }

                    for (var d = 0; d < 8; d++)
                    {
                        if (_solidValues[x + dx[d], y + dy[d]].Equals(2))
                        {
                            _fTemp[dst[d], x, y] = _fTemp[src[d], x, y];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Specify the inlet and outlet boundary conditions
        /// </summary>
        private void BcInOut()
        {
            for (var y = 0; y < Ly; y++)
            {
                for (var a = 0; a < 9; a++)
                {
                    _fTemp[a, 0, y] = _fTemp[a, 1, y];
                    _fTemp[a, Lx - 1, y] = _fTemp[a, Lx - 2, y];
                }
            }
        }

        /// <summary>
        /// Compute the velocity and depth
        /// </summary>
        private void Solution()
        {
            Array.Copy(_fTemp, _f, Lx * Ly);

            for (var y = 0; y < Ly; y++)
            {
                for (var x = 0; x < Lx; x++)
                {
                    if (_solidValues[x, y] <= 1)
                    {
                        var h = DataHelper.GetNew2DArray(_Lx, _Ly, 0.0);
                        var sumU = DataHelper.GetNew2DArray(_Lx, _Ly, 0.0);
                        var sumV = DataHelper.GetNew2DArray(_Lx, _Ly, 0.0);

                        for (var a = 0; a < 9; a++)
                        {
                            h[x, y] = h[x, y] + _f[a, x, y];
                            sumU[x, y] = sumU[x, y] + _particleVectors[DirectionX][a] * _f[a, x, y];
                            sumV[x, y] = sumV[x, y] + _particleVectors[DirectionY][a] * _f[a, x, y];
                        }

                        _u[x, y] = sumU[x, y] / _h[x, y];
                        _v[x, y] = sumV[x, y] / _h[x, y];
                    }
                    else if (_solidValues[x, y].Equals(2))
                    {
                        _h[x, y] = 0;
                        _u[x, y] = 0;
                        _v[x, y] = 0;
                    }
                }
            }
        }
        
        /// <summary>
        /// Specify the boundary conditions
        /// </summary>
        private void BcInflowOutflow()
        {
            // Outflow
            for (var y = 0; y < Ly; y++)
            {
                _h[Lx - 1, y] = _h0;
                _u[Lx - 1, y] = _u[Lx - 2, y];
                _v[Lx - 1, y] = 0;
            }

            // Inflow
            for (var y = 0; y < Ly; y++)
            {
                _h[0, y] = _h[1, y]; // Zero gradient
                _v[0, y] = 0;
            }

            var qi = 0.0;
            var area = 0.0;
            for (var y = 0; y < Ly - 2; y++)
            {
                qi = qi + 0.5 * (_h[0, y] + _h[0, y + 1]) * Dy * 0.5 * (_u[0, y] + _u[0, y + 1]);
                area = area + 0.5 * (_h[0, y] + _h[0, y + 1]) * Dy;
            }

            for (var y = 0; y < Ly; y++)
            {
                _u[0, y] = _u[0, y] + (Q0 - qi) / area;
            }

            // Wall
            for (var x = 0; x < Lx; x++)
            {
                _v[x, 0] = 0;
                _v[x, Ly - 1] = 0;
            }
        }
    }
}
