using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
using LatticeBoltzmann.Annotations;
using LatticeBoltzmann.Helpers;
using LatticeBoltzmann.Interfaces;
using LatticeBoltzmann.Models;

namespace LatticeBoltzmann
{
    public class LatticeBoltzmannSimulator : ISimulator
    {
        private const int XIndex = 0;
        private const int YIndex = 1;

        private const double ManningsCoefficient = 0.012;
        private const double SolidValuesDefault = 2;

        private const int ParticleGridBox0 = 0;
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
        private double _delta;
        private double _h0;
        private double _v0;
        private double _q0;
        private double _maxT;
        private double _r;
        private double _d;
        private double _e;

        private double[,] _solidValues;
        private double[,] _bedData;
        private double[][,] _bedSlopeData;
        private double[,] _h;
        private double[,] _u;
        private double[,] _v;
        private double[,] _particleVectors;

        private double[,,] _fEq;
        private double[,,] _fEqCurrent;
        private double[,,] _fTemp;

        private readonly ICollection<Shape> _shapes;

        [Description("Acceleration due to Gravity")]
        public double AccelerationDueToGravity
        {
            get { return _accelerationDueToGravity; }
            set { SetPropertyField(nameof(AccelerationDueToGravity), ref _accelerationDueToGravity, value); }
        }

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

        public double Delta
        {
            get { return _delta; }
            set { SetPropertyField(nameof(Delta), ref _delta, value);}
        }

        [Description("Water Depth")]
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

        public double MaxT
        {
            get { return _maxT; }
            set { SetPropertyField(nameof(MaxT), ref _maxT, value); }
        }

        [Description("Solid Radius")]
        public double R
        {
            get { return _r; }
            set { SetPropertyField(nameof(R), ref _r, value); }
        }

        public double D
        {
            get { return _d; }
            set { SetPropertyField(nameof(D), ref _d, value); }
        }

        public double E
        {
            get { return _e; }
            set { SetPropertyField(nameof(E), ref _e, value); }
        }

        // Calculated based on values above
        public int Lx => Convert.ToInt32(_length / _delta);
        public int Ly => Convert.ToInt32(_width / _delta);
        
        public double Fb => AccelerationDueToGravity / Math.Pow(Math.Pow(H0, 1.0/6.0) / ManningsCoefficient, 2);
        public double U0 => Q0 / (H0 * Width);
        public double Dt => Delta / E;
        public double Tau => 0.5 * (1 + 0.01 * 6 * Dt / (Delta * Delta));
        public double Nu => E * Delta * (2 * Tau - 1) / 6;
        public double Xs => 1;
        public double Xe => Lx;
        public double Ys => 1;
        public double Ye => Ly;
        public double Nermax => (Lx - 1) * (Ly - 1);
        public double Fr => U0 / Math.Sqrt(AccelerationDueToGravity * H0);
        public double Re => U0 * H0 / Nu;
        public double ReD => U0 * D / Nu;

        public event PropertyChangedEventHandler PropertyChanged;

        public LatticeBoltzmannSimulator()
        {
            Length = 20.0;
            Width = 16.0;
            Delta = 0.1;
            H0 = 1.5;
            V0 = 0.0;
            Q0 = 2.2;
            MaxT = 1;
            AccelerationDueToGravity = 9.81;
            //Fb = 0.00248;
            R = 0.25;
            D = 0.5;
            E = 10;

            _shapes = new List<Shape>();

            InitValues();
        }

        public LatticeBoltzmannSimulator(double length, double width,
            double delta, double h0, double v0, double q0, double maxT, 
            double accelerationDueToGravity, double r, double d, double e)
        {
            Length = length;
            Width = width;
            Delta = delta;
            H0 = h0;
            V0 = v0;
            Q0 = q0;
            MaxT = maxT;
            AccelerationDueToGravity = accelerationDueToGravity;
            R = r;
            D = d;
            E = e;

            _shapes = new List<Shape>();

            InitValues();
        }

        public void AddShape(Shape shape)
        {
            _shapes.Add(shape);
        }

        public void RemoveShape(Shape shape)
        {
            _shapes.Remove(shape);
        }

        public void SetBedShape(double[,] points)
        {
            _bedData = points;
        }

        public string GetTrackedPointDepth()
        {
            return _h[98, 51].ToString("F2");
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
            InitSolidValues();
            InitBedData();
            InitBedSlopeData();
            InitDepthAndVelocityFields();
            InitParticleVectors(E);
        }

        private void InitSolidValues()
        {
            _solidValues = DataHelper.GetNew2DArray(Lx, Ly, 0.0);

            // Top and bottom boundary
            for (var x = 0; x < Lx; x++)
            {
                _solidValues[x, 0] = 1.0;
                _solidValues[x, Ly - 1] = 1.0;
            }

            // Shapes
            foreach (var shape in _shapes)
            {
                for (var y = 1; y < Ly - 1; y++)
                {
                    for (var x = 0; x < Lx; x++)
                    {
                        if (shape.IsSolid(x * Delta, y * Delta))
                        {
                            _solidValues[x, y] = SolidValuesDefault;
                        }
                    }
                }
            }
        }

        private void InitBedData()
        {
            _bedData = DataHelper.GetNew2DArray(Lx, Ly, 0.0);

            for (var p = 0; p < _bedData.Length % Lx - 1; p++)
            {
                // Metres
                var y1M = _bedData[p, 0];
                var h1M = _bedData[p, 1];
                var y2M = _bedData[p + 1, 0];
                var h2M = _bedData[p + 1, 1];

                // Array units
                var y1 = Convert.ToInt32(y1M / Delta);
                var y2 = Convert.ToInt32(y2M / Delta);

                if (y1 < 0)
                {
                    y1 = 0;
                }
                if (y2 > Ly)
                {
                    y2 = Ly;
                }

                var m = (h2M - h1M) / (y2M - y1M);

                for (var y = y1; y < y2; y++)
                {
                    for (var x = 0; x < Lx; x++)
                    {
                        _bedData[x, y] = (m * ((y * Delta) - y1M)) + h1M;
                    }
                }
            }
        }

        private void InitBedSlopeData()
        {
            _bedSlopeData = new double[2][,];
            _bedSlopeData[XIndex] = DataHelper.GetNew2DArray(Lx, Ly, 0.0);
            _bedSlopeData[YIndex] = DataHelper.GetNew2DArray(Lx, Ly, 0.0);
        }

        private void InitDepthAndVelocityFields()
        {
            _h = DataHelper.GetNew2DArray(Lx, Ly, 0.0);
            _u = DataHelper.GetNew2DArray(Lx, Ly, 0.0);
            _v = DataHelper.GetNew2DArray(Lx, Ly, 0.0);

            for (var y = 0; y < Ly; y++)
            {
                for (var x = 0; x < Lx; x++)
                {
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
            }
        }

        private void InitParticleVectors(double e)
        {
            _particleVectors = new double[2, 9];

            _particleVectors[XIndex, ParticleGridBox0] = 0; _particleVectors[YIndex, ParticleGridBox0] = 0;
            _particleVectors[XIndex, ParticleGridBox1] = e; _particleVectors[YIndex, ParticleGridBox1] = 0;
            _particleVectors[XIndex, ParticleGridBox2] = e; _particleVectors[YIndex, ParticleGridBox2] = e;
            _particleVectors[XIndex, ParticleGridBox3] = 0; _particleVectors[YIndex, ParticleGridBox3] = e;
            _particleVectors[XIndex, ParticleGridBox4] = -e; _particleVectors[YIndex, ParticleGridBox4] = e;
            _particleVectors[XIndex, ParticleGridBox5] = -e; _particleVectors[YIndex, ParticleGridBox5] = 0;
            _particleVectors[XIndex, ParticleGridBox6] = -e; _particleVectors[YIndex, ParticleGridBox6] = -e;
            _particleVectors[XIndex, ParticleGridBox7] = 0; _particleVectors[YIndex, ParticleGridBox7] = -e;
            _particleVectors[XIndex, ParticleGridBox8] = e; _particleVectors[YIndex, ParticleGridBox8] = -e;
        }

        public void ComputeEquilibriumDistributionFunction(bool setFEqToResult = false)
        {
            _fEqCurrent = DataHelper.GetNew3DArray(9, Lx, Ly, 0.0);

            for (var y = 0; y < Ly; y++)
            {
                for (var x = 0; x < Lx; x++)
                {
                    if (!(_solidValues[x, y] <= 1.0))
                    {
                        continue;
                    }

                    _fEqCurrent[0, x, y] = _h[x, y] -
                                           5 * AccelerationDueToGravity * Math.Pow(_h[x, y], 2) / (6 * Math.Pow(E, 2)) -
                                           2 * _h[x, y] / (3 * Math.Pow(E, 2)) *
                                           (Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2));

                    for (var a = 1; a < 9; a++)
                    {
                        if (a % 2 == 0)
                        {
                            _fEqCurrent[a, x, y] =
                                AccelerationDueToGravity * Math.Pow(_h[x, y], 2) / (24 * Math.Pow(E, 2)) +
                                _h[x, y] / (12 * Math.Pow(E, 2)) *
                                (_particleVectors[XIndex, a] * _u[x, y] + _particleVectors[YIndex, a] * _v[x, y]) +
                                _h[x, y] / (8 * Math.Pow(E, 4)) *
                                (
                                    _particleVectors[XIndex, a] * _u[x, y] * _particleVectors[XIndex, a] * _u[x, y] +
                                    2 * _particleVectors[XIndex, a] * _u[x, y] * _particleVectors[YIndex, a] *
                                    _v[x, y] +
                                    _particleVectors[YIndex, a] * _v[x, y] * _particleVectors[YIndex, a] * _v[x, y]
                                ) - _h[x, y] / (24 * Math.Pow(E, 2)) * (Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2));
                        }
                        else
                        {
                            _fEqCurrent[a, x, y] =
                                AccelerationDueToGravity * Math.Pow(_h[x, y], 2) / (6 * Math.Pow(E, 2)) +
                                _h[x, y] / (3 * Math.Pow(E, 2)) *
                                (_particleVectors[XIndex, a] * _u[x, y] + _particleVectors[YIndex, a] * _v[x, y]) +
                                _h[x, y] / (2 * Math.Pow(E, 4)) *
                                (
                                    _particleVectors[XIndex, a] * _u[x, y] * _particleVectors[XIndex, a] * _u[x, y] +
                                    2 * _particleVectors[XIndex, a] * _u[x, y] * _particleVectors[YIndex, a] *
                                    _v[x, y] +
                                    _particleVectors[YIndex, a] * _v[x, y] * _particleVectors[YIndex, a] * _v[x, y]
                                ) - _h[x, y] / (6 * Math.Pow(E, 2)) * (Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2));
                        }
                    }
                }
            }

            if (setFEqToResult)
            {
                _fEq = _fEqCurrent.Clone() as double[,,];
            }
        }

        public void ComputeNewValues()
        {
            CollideStream();
            BcBodySlipNs();
            BcBodyMain();
            BcInOut();
            Solution();
            BcInflowOutflow();
        }

        private void CollideStream()
        {
            if (_fTemp == null)
            {
                _fTemp = DataHelper.GetNew3DArray(9, Lx, Ly, 0.0);
            }

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
                            _fTemp[0, xp, y] = _fEq[0, x, y] - (_fEq[0, x, y] - _fEqCurrent[0, x, y]) / Tau - Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[xp, y]) * (_particleVectors[XIndex, 0] * _bedSlopeData[XIndex][x, y] + _particleVectors[YIndex, 0] * _bedSlopeData[YIndex][x, y]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[XIndex, 0] * _u[x, y] + _particleVectors[YIndex, 0] * _v[x, y]);
                        }

                        if (xp < Lx && yp < Ly && _solidValues[xp, yp] <= 1)
                        {
                            _fTemp[1, xp, yp] = _fEq[1, x, y] - (_fEq[1, x, y] - _fEqCurrent[1, x, y]) / Tau - Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[xp, yp]) * (_particleVectors[XIndex, 1] * _bedSlopeData[XIndex][x, y] + _particleVectors[YIndex, 1] * _bedSlopeData[YIndex][x, y]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[XIndex, 1] * _u[x, y] + _particleVectors[YIndex, 1] * _v[x, y]);
                        }

                        if (yp < Ly && _solidValues[x, yp] <= 1)
                        {
                            _fTemp[2, x, yp] = _fEq[2, x, y] - (_fEq[2, x, y] - _fEqCurrent[2, x, y]) / Tau - Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[x, yp]) * (_particleVectors[XIndex, 2] * _bedSlopeData[XIndex][x, y] + _particleVectors[YIndex, 2] * _bedSlopeData[YIndex][x, y]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[XIndex, 2] * _u[x, y] + _particleVectors[YIndex, 2] * _v[x, y]);
                        }

                        if (xn >= 0 && yp < Ly && _solidValues[xn, yp] <= 1)
                        {
                            _fTemp[3, xn, yp] = _fEq[3, x, y] - (_fEq[3, x, y] - _fEqCurrent[3, x, y]) / Tau - Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[xn, yp]) * (_particleVectors[XIndex, 3] * _bedSlopeData[XIndex][xn, y] + _particleVectors[YIndex, 3] * _bedSlopeData[YIndex][x, y]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[XIndex, 3] * _u[x, y] + _particleVectors[YIndex, 3] * _v[x, y]);
                        }

                        if (xn >= 0 && _solidValues[xn, y] <= 1)
                        {
                            _fTemp[4, xn, y] = _fEq[4, x, y] - (_fEq[4, x, y] - _fEqCurrent[4, x, y]) / Tau - Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[xn, y]) * (_particleVectors[XIndex, 4] * _bedSlopeData[XIndex][xn, y] + _particleVectors[YIndex, 4] * _bedSlopeData[YIndex][x, y]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[XIndex, 4] * _u[x, y] + _particleVectors[YIndex, 4] * _v[x, y]);
                        }

                        if (xn >= 0 && yn >= 0 && _solidValues[xn, yn] <= 1)
                        {
                            _fTemp[5, xn, yn] = _fEq[5, x, y] - (_fEq[5, x, y] - _fEqCurrent[5, x, y]) / Tau - Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[xn, yn]) * (_particleVectors[XIndex, 5] * _bedSlopeData[XIndex][xn, y] + _particleVectors[YIndex, 5] * _bedSlopeData[YIndex][x, yn]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[XIndex, 5] * _u[x, y] + _particleVectors[YIndex, 5] * _v[x, y]);
                        }

                        if (yn >= 0 && _solidValues[x, yn] <= 1)
                        {
                            _fTemp[6, x, yn] = _fEq[6, x, y] - (_fEq[6, x, y] - _fEqCurrent[6, x, y]) / Tau - Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[x, yn]) * (_particleVectors[XIndex, 6] * _bedSlopeData[XIndex][x, y] + _particleVectors[YIndex, 6] * _bedSlopeData[YIndex][x, yn]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[XIndex, 6] * _u[x, y] + _particleVectors[YIndex, 6] * _v[x, y]);
                        }

                        if (xp < Lx && yn >= 0 && _solidValues[xp, yn] <= 1)
                        {
                            _fTemp[7, xp, yn] = _fEq[7, x, y] - (_fEq[7, x, y] - _fEqCurrent[7, x, y]) / Tau - Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[xp, yn]) * (_particleVectors[XIndex, 7] * _bedSlopeData[XIndex][x, y] + _particleVectors[YIndex, 7] * _bedSlopeData[YIndex][x, yn]) - Dt / (6 * E * E) * Fb * Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) * (_particleVectors[XIndex, 7] * _u[x, y] + _particleVectors[YIndex, 7] * _v[x, y]);
                        }

                        _fTemp[8, x, y] = _fEq[8, x, y] - (_fEq[8, x, y] - _fEqCurrent[8, x, y]) / Tau;
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
            var dx = new[] { 1, 1, 0, -1, -1, -1, 0, 1 };
            var dy = new[] { 0, 1, 1, 1, 0, -1, -1, -1 };

            // Map sources/destinations
            var src = new[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            var dst = new[] { 4, 5, 6, 7, 0, 1, 2, 3 };

            for (var y = 1; y < Ly - 1; y++)
            {
                for (var x = 1; x < Lx - 1; x++)
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
            Array.Copy(_fTemp, _fEq, Lx * Ly);

            var h = DataHelper.GetNew2DArray(Lx, Ly, 0.0);
            var sumU = DataHelper.GetNew2DArray(Lx, Ly, 0.0);
            var sumV = DataHelper.GetNew2DArray(Lx, Ly, 0.0);

            for (var y = 0; y < Ly; y++)
            {
                for (var x = 0; x < Lx; x++)
                {
                    if (_solidValues[x, y] <= 1)
                    {
                        for (var a = 0; a < 9; a++)
                        {
                            h[x, y] = h[x, y] + _fEq[a, x, y];
                            sumU[x, y] = sumU[x, y] + _particleVectors[XIndex, a] * _fEq[a, x, y];
                            sumV[x, y] = sumV[x, y] + _particleVectors[YIndex, a] * _fEq[a, x, y];
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
                qi = qi + 0.5 * (_h[0, y] + _h[0, y + 1]) * Delta * 0.5 * (_u[0, y] + _u[0, y + 1]);
                area = area + 0.5 * (_h[0, y] + _h[0, y + 1]) * Delta;
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
