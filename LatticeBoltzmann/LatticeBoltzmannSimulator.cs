using System;
using System.CodeDom;
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
        private const int X = 0;
        private const int Y = 1;

        private const int CENTRE = 0;
        private const int RIGHT = 1;
        private const int UP_RIGHT = 2;
        private const int UP = 3;
        private const int UP_LEFT = 4;
        private const int LEFT = 5;
        private const int DOWN_LEFT = 6;
        private const int DOWN = 7;
        private const int DOWN_RIGHT = 8;

        private const double ManningsCoefficient = 0.012;

        private readonly ICollection<Shape> _shapes;

        private double _accelerationDueToGravity;
        private double _length;
        private double _width;
        private double _delta;
        private double _h0;
        private double _v0;
        private double _q0;
        private double _r;
        private double _d;
        private double _e;

        private bool[,] _isSolid;
        private double[,] _bedShape;
        private double[,] _bedData;
        private double[,] _h;
        private double[,] _u;
        private double[,] _v;
        private double[,] _particleVectors;
        private double[][,] _bedSlopeData;

        private double[,,] _fEq;
        private double[,,] _fEqCurrent;
        private double[,,] _fTemp;

        private bool _initialised;

        #region Accessors

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
            set { SetPropertyField(nameof(Delta), ref _delta, value); }
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

        #endregion Accessors

        #region Properties

        // Calculated based on values above
        public int Lx => Convert.ToInt32(_length / _delta);
        public int Ly => Convert.ToInt32(_width / _delta);

        public double Fb => AccelerationDueToGravity / Math.Pow(Math.Pow(H0, 1.0 / 6.0) / ManningsCoefficient, 2);
        public double U0 => 0.8; //Q0 / (H0 * Width);
        public double Dt => Delta / E;
        public double Tau => 0.5 * (1 + 0.01 * 6 * Dt / (Delta * Delta));
        public double Nu => E * Delta * (2 * Tau - 1) / 6;
        public double Nermax => (Lx - 1) * (Ly - 1);
        public double Fr => U0 / Math.Sqrt(AccelerationDueToGravity * H0);
        public double Re => U0 * H0 / Nu;
        public double ReD => U0 * D / Nu;

        #endregion Properties

        public event PropertyChangedEventHandler PropertyChanged;

        public LatticeBoltzmannSimulator()
        {
            Length = 20.0;
            Width = 16.0;
            H0 = 1.5;
            V0 = 0.0;
            Q0 = 2.2;
            AccelerationDueToGravity = 9.81;
            //Fb = 0.00248;

            D = 0.5;
            Delta = 0.1;
            R = 0.25;
            E = 10;

            _shapes = new List<Shape>();

            _initialised = false;
        }

        public LatticeBoltzmannSimulator(double length, double width,
            double delta, double h0, double v0, double q0,
            double accelerationDueToGravity, double r, double d, double e)
        {
            Length = length;
            Width = width;
            H0 = h0;
            V0 = v0;
            Q0 = q0;
            AccelerationDueToGravity = accelerationDueToGravity;
            D = d;
            Delta = delta;
            R = r;
            E = e;

            _shapes = new List<Shape>();

            _initialised = false;
        }

        public void AddShape(Shape shape)
        {
            _shapes.Add(shape);

            _initialised = false;
        }

        public void RemoveShape(Shape shape)
        {
            _shapes.Remove(shape);

            _initialised = false;
        }

        public void SetBedShape(double[,] points)
        {
            _bedShape = points;

            _initialised = false;
        }

        public bool[,] GetSolids()
        {
            return _isSolid;
        }

        public double[,] GetDepths()
        {
            return _h;
        }

        public double[,,] GetFunction()
        {
            return _fEq;
        }

        public string GetFunctionValueAtPoint(int x, int y)
        {
            double sumF = 0.0;
            for (var a = 0; a < 9; a++)
            {
                sumF = _fEq[1, x, y] + _fEq[2, x, y] + _fEq[8, x, y];
                sumF -= _fEq[4, x, y] + _fEq[5, x, y] + _fEq[6, x, y];
            }

            return sumF.ToString("F2");
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

        #region Initialisation

        public void Init()
        {
            InitSolidValues();
            InitBedData();
            InitBedSlopeData();
            InitDepthAndVelocityFields();
            InitParticleVectors();

            _fEqCurrent = DataHelper.GetNew3DArray(9, Lx, Ly, 0.0);
            _fTemp = DataHelper.GetNew3DArray(9, Lx, Ly, 0.0);

            _initialised = true;
        }

        private void InitSolidValues()
        {
            _isSolid = DataHelper.GetNew2DArray(Lx, Ly, false);

            // Top and bottom boundary
            for (var x = 0; x < Lx; x++)
            {
                _isSolid[x, 0] = false;
                _isSolid[x, Ly - 1] = false;
            }

            // Shapes
            foreach (var shape in _shapes)
            {
                for (var y = 1; y < Ly - 1; y++)
                {
                    for (var x = 0; x < Lx; x++)
                    {
                        _isSolid[x, y] = _isSolid[x, y] || shape.IsSolid(x * Delta, y * Delta);
                    }
                }
            }
        }

        private void InitBedData()
        {
            _bedData = DataHelper.GetNew2DArray(Lx, Ly, 0.0);

            if (_bedShape == null)
            {
                return;
            }

            // If the first part of the bed shape data doesn't touch the first bank, use 
            // that value for the bed in between the bank and the start of the bed.
            if (_bedShape[0, 0] / Delta > 0)
            {
                for (var y = 0; y < _bedShape[0, 0] / Delta; y++)
                {
                    for (var x = 0; x < Lx; x++)
                    {
                        _bedData[x, y] = _bedShape[0, 0] / Delta;
                    }
                }
            }

            var maxP = (_bedShape.Length / _bedShape.Rank) - 1;
            for (var p = 0; p < maxP; p++)
            {
                // Metres
                var y1M = _bedShape[p, 0];
                var h1M = _bedShape[p, 1];
                var y2M = _bedShape[p + 1, 0];
                var h2M = _bedShape[p + 1, 1];

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

            // If the last part of the bed shape data doesn't touch the other bank, use 
            // that value for the bed in between there and the bank.
            var lastYPoint = Convert.ToInt32(_bedShape[maxP, 0] / Delta);
            if (lastYPoint >= Ly)
            {
                return;
            }

            for (var y = lastYPoint + 1; y < Ly; y++)
            {
                for (var x = 0; x < Lx; x++)
                {
                    _bedData[x, y] = _bedShape[maxP, 0] / Delta;
                }
            }
        }

        private void InitBedSlopeData()
        {
            _bedSlopeData = new double[2][,];
            _bedSlopeData[X] = DataHelper.GetNew2DArray(Lx, Ly, 0.0);
            _bedSlopeData[Y] = DataHelper.GetNew2DArray(Lx, Ly, 0.0);
        }

        private void InitDepthAndVelocityFields()
        {
            _h = new double[Lx, Ly];
            _u = new double[Lx, Ly];
            _v = new double[Lx, Ly];

            for (var y = 0; y < Ly; y++)
            {
                for (var x = 0; x < Lx; x++)
                {
                    // Depth and velocity fields
                    if (_isSolid[x, y])
                    {
                        _h[x, y] = 0;
                        _u[x, y] = 0;
                        _v[x, y] = 0;
                    }
                    else
                    {
                        _h[x, y] = H0 - _bedData[x, y];
                        _v[x, y] = 0;
                        _u[x, y] = U0;
                    }
                }
            }
        }

        private void InitParticleVectors()
        {
            _particleVectors = new double[2, 9];

            _particleVectors[X, CENTRE] = 0;
            _particleVectors[Y, CENTRE] = 0;

            _particleVectors[X, RIGHT] = E;
            _particleVectors[Y, RIGHT] = 0;

            _particleVectors[X, UP_RIGHT] = E;
            _particleVectors[Y, UP_RIGHT] = E;

            _particleVectors[X, UP] = 0;
            _particleVectors[Y, UP] = E;

            _particleVectors[X, UP_LEFT] = -E;
            _particleVectors[Y, UP_LEFT] = E;

            _particleVectors[X, LEFT] = -E;
            _particleVectors[Y, LEFT] = 0;

            _particleVectors[X, DOWN_LEFT] = -E;
            _particleVectors[Y, DOWN_LEFT] = -E;

            _particleVectors[X, DOWN] = 0;
            _particleVectors[Y, DOWN] = -E;

            _particleVectors[X, DOWN_RIGHT] = E;
            _particleVectors[Y, DOWN_RIGHT] = -E;
        }

        #endregion Initialisation

        public void ComputeEquilibriumDistributionFunction(bool setFEqToResult = false)
        {
            if (!_initialised)
            {
                throw new InvalidOperationException("You must initialise the simulator before computing the equilibrium function.");
            }

            for (var y = 0; y < Ly; y++)
            {
                for (var x = 0; x < Lx; x++)
                {
                    if (_isSolid[x, y])
                    {
                        for (var a = 0; a < 9; a++)
                        {
                            _fEqCurrent[a, x, y] = 0;
                        }

                        continue;
                    }

                    for (var a = 1; a < 9; a++)
                    {
                        if (a % 2 == 0)
                        {
                            _fEqCurrent[a, x, y] =
                                AccelerationDueToGravity * Math.Pow(_h[x, y], 2) / (24 * Math.Pow(E, 2)) +
                                _h[x, y] / (12 * Math.Pow(E, 2)) *
                                (_particleVectors[X, a] * _u[x, y] + _particleVectors[Y, a] * _v[x, y]) +
                                _h[x, y] / (8 * Math.Pow(E, 4)) *
                                (
                                    _particleVectors[X, a] * _u[x, y] * _particleVectors[X, a] * _u[x, y] +
                                    2 * _particleVectors[X, a] * _u[x, y] * _particleVectors[Y, a] *
                                    _v[x, y] +
                                    _particleVectors[Y, a] * _v[x, y] * _particleVectors[Y, a] * _v[x, y]
                                ) - _h[x, y] / (24 * Math.Pow(E, 2)) * (Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2));
                        }
                        else
                        {
                            _fEqCurrent[a, x, y] =
                                AccelerationDueToGravity * Math.Pow(_h[x, y], 2) / (6 * Math.Pow(E, 2)) +
                                _h[x, y] / (3 * Math.Pow(E, 2)) *
                                (_particleVectors[X, a] * _u[x, y] + _particleVectors[Y, a] * _v[x, y]) +
                                _h[x, y] / (2 * Math.Pow(E, 4)) *
                                (
                                    _particleVectors[X, a] * _u[x, y] * _particleVectors[X, a] * _u[x, y] +
                                    2 * _particleVectors[X, a] * _u[x, y] * _particleVectors[Y, a] *
                                    _v[x, y] +
                                    _particleVectors[Y, a] * _v[x, y] * _particleVectors[Y, a] * _v[x, y]
                                ) - _h[x, y] / (6 * Math.Pow(E, 2)) * (Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2));
                        }
                    }

                    _fEqCurrent[CENTRE, x, y] = _h[x, y] -
                                           5 * AccelerationDueToGravity * Math.Pow(_h[x, y], 2) / (6 * Math.Pow(E, 2)) -
                                           2 * _h[x, y] / (3 * Math.Pow(E, 2)) *
                                           (Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2));
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
            for (var y = 1; y < Ly - 1; y++)
            {
                var yp = y + 1;
                var yn = y - 1;

                for (var x = 1; x < Lx - 1; x++)
                {
                    if (_isSolid[x, y])
                    {
                        for (var a = 0; a < 9; a++)
                        {
                            _fTemp[a, x, y] = 0;
                        }
                        continue;
                    }

                    var xp = x + 1;
                    var xn = x - 1;

                    if (xp <= Lx && !_isSolid[xp, y])
                    {
                        _fTemp[RIGHT, xp, y] = _fEq[RIGHT, x, y] - (_fEq[RIGHT, x, y] - _fEqCurrent[RIGHT, x, y]) / Tau -
                                               Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 *
                                               (_h[x, y] + _h[xp, y]) *
                                               (_particleVectors[X, RIGHT] * _bedSlopeData[X][x, y] +
                                                _particleVectors[Y, RIGHT] * _bedSlopeData[Y][x, y]) -
                                               Dt / (6 * E * E) * Fb *
                                               Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) *
                                               (_particleVectors[X, RIGHT] * _u[x, y] +
                                                _particleVectors[Y, RIGHT] * _v[x, y]);
                    }

                    if (xp < Lx && yp < Ly && !_isSolid[xp, yp])
                    {
                        _fTemp[UP_RIGHT, xp, yp] = _fEq[UP_RIGHT, x, y] - (_fEq[UP_RIGHT, x, y] - _fEqCurrent[UP_RIGHT, x, y]) / Tau -
                                                   Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 *
                                                   (_h[x, y] + _h[xp, yp]) *
                                                   (_particleVectors[X, UP_RIGHT] * _bedSlopeData[X][x, y] +
                                                    _particleVectors[Y, UP_RIGHT] * _bedSlopeData[Y][x, y]) -
                                                   Dt / (6 * E * E) * Fb *
                                                   Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) *
                                                   (_particleVectors[X, UP_RIGHT] * _u[x, y] +
                                                    _particleVectors[Y, UP_RIGHT] * _v[x, y]);
                    }

                    if (yp < Ly && !_isSolid[x, yp])
                    {
                        _fTemp[UP, x, yp] = _fEq[UP, x, y] - (_fEq[UP, x, y] - _fEqCurrent[UP, x, y]) / Tau -
                                           Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[x, yp]) *
                                           (_particleVectors[X, UP] * _bedSlopeData[X][x, y] +
                                            _particleVectors[Y, UP] * _bedSlopeData[Y][x, y]) - Dt / (6 * E * E) * Fb *
                                           Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) *
                                           (_particleVectors[X, UP] * _u[x, y] + 
                                            _particleVectors[Y, UP] * _v[x, y]);
                    }

                    if (xn >= 0 && yp < Ly && !_isSolid[xn, yp])
                    {
                        _fTemp[UP_LEFT, xn, yp] = _fEq[UP_LEFT, x, y] - (_fEq[UP_LEFT, x, y] - _fEqCurrent[UP_LEFT, x, y]) / Tau -
                                            Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 *
                                            (_h[x, y] + _h[xn, yp]) *
                                            (_particleVectors[X, UP_LEFT] * _bedSlopeData[X][xn, y] +
                                             _particleVectors[Y, UP_LEFT] * _bedSlopeData[Y][x, y]) - Dt / (6 * E * E) * Fb *
                                            Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) *
                                            (_particleVectors[X, UP_LEFT] * _u[x, y] +
                                             _particleVectors[Y, UP_LEFT] * _v[x, y]);
                    }

                    if (xn >= 0 && !_isSolid[xn, y])
                    {
                        _fTemp[LEFT, xn, y] = _fEq[LEFT, x, y] - (_fEq[LEFT, x, y] - _fEqCurrent[LEFT, x, y]) / Tau -
                                           Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[xn, y]) *
                                           (_particleVectors[X, LEFT] * _bedSlopeData[X][xn, y] +
                                            _particleVectors[Y, LEFT] * _bedSlopeData[Y][x, y]) - Dt / (6 * E * E) * Fb *
                                           Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) *
                                           (_particleVectors[X, LEFT] * _u[x, y] + 
                                            _particleVectors[Y, LEFT] * _v[x, y]);
                    }

                    if (xn >= 0 && yn >= 0 && !_isSolid[xn, yn])
                    {
                        _fTemp[DOWN_LEFT, xn, yn] = _fEq[DOWN_LEFT, x, y] - (_fEq[DOWN_LEFT, x, y] - _fEqCurrent[DOWN_LEFT, x, y]) / Tau -
                                            Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 *
                                            (_h[x, y] + _h[xn, yn]) *
                                            (_particleVectors[X, DOWN_LEFT] * _bedSlopeData[X][xn, y] +
                                             _particleVectors[Y, DOWN_LEFT] * _bedSlopeData[Y][x, yn]) - Dt / (6 * E * E) * Fb *
                                            Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) *
                                            (_particleVectors[X, DOWN_LEFT] * _u[x, y] + 
                                             _particleVectors[Y, DOWN_LEFT] * _v[x, y]);
                    }

                    if (yn >= 0 && !_isSolid[x, yn])
                    {
                        _fTemp[DOWN, x, yn] = _fEq[DOWN, x, y] - (_fEq[DOWN, x, y] - _fEqCurrent[DOWN, x, y]) / Tau -
                                           Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[x, yn]) *
                                           (_particleVectors[X, DOWN] * _bedSlopeData[X][x, y] +
                                            _particleVectors[Y, DOWN] * _bedSlopeData[Y][x, yn]) - Dt / (6 * E * E) * Fb *
                                           Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) *
                                           (_particleVectors[X, DOWN] * _u[x, y] + 
                                            _particleVectors[Y, DOWN] * _v[x, y]);
                    }

                    if (xp < Lx && yn >= 0 && !_isSolid[xp, yn])
                    {
                        _fTemp[DOWN_RIGHT, xp, yn] = _fEq[DOWN_RIGHT, x, y] - (_fEq[DOWN_RIGHT, x, y] - _fEqCurrent[DOWN_RIGHT, x, y]) / Tau -
                                            Dt / (6 * E * E) * AccelerationDueToGravity * 0.5 *
                                            (_h[x, y] + _h[xp, yn]) *
                                            (_particleVectors[X, DOWN_RIGHT] * _bedSlopeData[X][x, y] +
                                             _particleVectors[Y, DOWN_RIGHT] * _bedSlopeData[Y][x, yn]) - Dt / (6 * E * E) * Fb *
                                            Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2)) *
                                            (_particleVectors[X, DOWN_RIGHT] * _u[x, y] +
                                             _particleVectors[Y, DOWN_RIGHT] * _v[x, y]);
                    }

                    _fTemp[CENTRE, x, y] = _fEq[CENTRE, x, y] - (_fEq[CENTRE, x, y] - _fEqCurrent[CENTRE, x, y]) / Tau;
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
                _fTemp[UP_RIGHT, x, 0] = _fTemp[DOWN_RIGHT, x, 0];
                _fTemp[UP, x, 0] =       _fTemp[DOWN, x, 0];
                _fTemp[UP_LEFT, x, 0] =  _fTemp[DOWN_LEFT, x, 0];

                _fTemp[DOWN_RIGHT, x, Ly - 1] = _fTemp[UP_RIGHT, x, Ly - 1];
                _fTemp[DOWN, x, Ly - 1] =       _fTemp[UP, x, Ly - 1];
                _fTemp[DOWN_LEFT, x, Ly - 1] =  _fTemp[UP_LEFT, x, Ly - 1];
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
            const int src = 0;
            const int dst = 1;
            var map = new[,]
            {
                { RIGHT,        LEFT },
                { UP_RIGHT,     DOWN_LEFT },
                { UP,           DOWN },
                { UP_LEFT,      DOWN_RIGHT },
                { LEFT,         RIGHT },
                { DOWN_LEFT,    UP_RIGHT },
                { DOWN,         UP },
                { DOWN_RIGHT,   UP_LEFT }
            };

            for (var y = 1; y < Ly - 1; y++)
            {
                for (var x = 1; x < Lx - 1; x++)
                {
                    if (_isSolid[x, y])
                    {
                        continue;
                    }

                    for (var d = 0; d < 8; d++)
                    {
                        if (_isSolid[x + dx[d], y + dy[d]])
                        {
                            _fTemp[map[d, dst], x, y] = _fTemp[map[d, src], x, y];
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
                    _fTemp[a, 0, y] =      _fTemp[a, 1, y];
                    _fTemp[a, Lx - 1, y] = _fTemp[a, Lx - 2, y];
                }
            }
        }

        /// <summary>
        /// Compute the velocity and depth
        /// </summary>
        private void Solution()
        {
            _fEq = _fTemp.Clone() as double[,,];

            if (_fEq == null)
            {
                throw new InvalidOperationException("The equilibrium distribution is null. That shouldn't happen; please inform the developer.");
            }

            var h = DataHelper.GetNew2DArray(Lx, Ly, 0.0);

            for (var y = 0; y < Ly; y++)
            {
                for (var x = 0; x < Lx; x++)
                {
                    if (_isSolid[x, y])
                    {
                        _h[x, y] = 0;
                        _u[x, y] = 0;
                        _v[x, y] = 0;

                        continue;
                    }

                    double sumU = 0, sumV = 0;
                    for (var a = 0; a < 9; a++)
                    {
                        h[x, y] += _fEq[a, x, y];
                        sumU += _particleVectors[X, a] * _fEq[a, x, y];
                        sumV += _particleVectors[Y, a] * _fEq[a, x, y];
                    }

                    _u[x, y] = sumU / _h[x, y];
                    _v[x, y] = sumV / _h[x, y];
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
                _h[Lx - 1, y] = _h[Lx - 2, y] - 0.01;
                _u[Lx - 1, y] = _u[Lx - 2, y];
                _v[Lx - 1, y] = 0;
            }

            // Inflow
            for (var y = 0; y < Ly; y++)
            {
                _h[0, y] = _h[1, y]; // Zero gradient
                _v[0, y] = 0;
            }

            double qi = 0, area = 0;
            for (var y = 0; y < Ly - 2; y++)
            {
                qi += 0.5 * (_h[0, y] + _h[0, y + 1]) * Delta * 0.5 * (_u[0, y] + _u[0, y + 1]);
                area += 0.5 * (_h[0, y] + _h[0, y + 1]) * Delta;
            }

            for (var y = 0; y < Ly; y++)
            {
                _u[0, y] += (Q0 - qi) / area;
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
