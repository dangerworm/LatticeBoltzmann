using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
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

        private const double ManningsCoefficient = 0.012;

        private readonly ICollection<Shape> _shapes;

        private int _maxT;
        private int _iterations;
        private int _resolution;
        private double _accelerationDueToGravity;
        private double _length;
        private double _width;
        private double _h0;
        private double _v0;
        private double _q0;
        private double _r;
        private double _d;
        private double _e;

        private bool[,] _isSolid;
        private double[,] _bedShape;
        private double[,] _bedData;
        private double[][,] _bedSlopeData;

        private double[,] _h;
        private double[,] _u;
        private double[,] _v;
        private double[,] _particleVectors;

        private double[,,] _fEq;
        private double[,,] _fEqCurrent;
        private double[,,] _fTemp;

        private bool _initialised;

        public event PropertyChangedEventHandler PropertyChanged;

        public const int CENTRE = 0;
        public const int RIGHT = 1;
        public const int UP_RIGHT = 2;
        public const int UP = 3;
        public const int UP_LEFT = 4;
        public const int LEFT = 5;
        public const int DOWN_LEFT = 6;
        public const int DOWN = 7;
        public const int DOWN_RIGHT = 8;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        [Description("#Iterations")]
        public int MaxT
        {
            get { return _maxT; }
            set { SetPropertyField(nameof(MaxT), ref _maxT, value); }
        }

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

        public int Resolution
        {
            get { return _resolution; }
            set { SetPropertyField(nameof(Resolution), ref _resolution, value); }
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

        #endregion Properties

        #region Accessors

        // Calculated based on values above
        public int Lx => Convert.ToInt32(_length * _resolution);
        public int Ly => Convert.ToInt32(_width * _resolution);

        public double Delta => Length / Lx;
        public double U0 => Q0 / (H0 * Width * Delta);
        public double Fb => AccelerationDueToGravity / Math.Pow(Math.Pow(H0, 1.0 / 6.0) / ManningsCoefficient, 2); // Originally 0.00248
        public double Dt => Delta / E;
        public double Tau => 0.5 * (1 + 0.01 * 6 * Dt / Math.Pow(Delta, 2));
        public double Nu => E * Delta * (2 * Tau - 1) / 6;
        public double Nermax => (Lx - 1) * (Ly - 1);
        public double Fr => U0 / Math.Sqrt(AccelerationDueToGravity * H0);
        public double Re => U0 * H0 / Nu;
        public double ReD => U0 * D / Nu;

        public bool[,] Solids => _isSolid;
        public double[,] Depths => _h;
        public double[,,] Function => _fEq;

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

        public Point GetMovementVectorAtPoint(int x, int y)
        {
            var dx = _fEq[RIGHT, x, y] +
                     Math.Sqrt(2) * (_fEq[UP_RIGHT, x, y] + _fEq[DOWN_RIGHT, x, y]) -
                     _fEq[LEFT, x, y] -
                     Math.Sqrt(2) * (_fEq[UP_LEFT, x, y] + _fEq[DOWN_LEFT, x, y]);
            var dy = _fEq[UP, x, y] +
                     Math.Sqrt(2) * (_fEq[UP_LEFT, x, y] + _fEq[UP_RIGHT, x, y]) -
                     _fEq[DOWN, x, y] -
                     Math.Sqrt(2) * (_fEq[DOWN_LEFT, x, y] + _fEq[DOWN_RIGHT, x, y]);

            try
            {
                return new Point(Convert.ToInt32(dx * 50), Convert.ToInt32(dy * 50));
            }
            catch (Exception e)
            {
                return new Point(0, 0);
            }
        }

        #endregion Accessors

        public LatticeBoltzmannSimulator()
        {
            MaxT = 1000;
            AccelerationDueToGravity = 9.81;
            Resolution = 10;

            Length = 20.0;
            Width = 16.0;
            V0 = 0.0;
            Q0 = 2.2;
            D = 0.5;
            R = 0.25;
            E = 10;

            _shapes = new List<Shape>();

            _initialised = false;
        }

        public LatticeBoltzmannSimulator(double length, double width,
            int resolution, double v0, double q0,
            double accelerationDueToGravity, double r, double d, double e)
        {
            AccelerationDueToGravity = accelerationDueToGravity;
            Resolution = resolution;

            Length = length;
            Width = width;
            V0 = v0;
            Q0 = q0;
            D = d;
            R = r;
            E = e;

            _shapes = new List<Shape>();

            _initialised = false;
        }

        #region Modifiers

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
            int xMax = points.GetLength(0), yMax = points.GetLength(1);
            _bedShape = new double[xMax, yMax];

            var maxDepth = 0.0;
            for (var x = 0; x < xMax; x++)
            {
                if (points[x, 1] > maxDepth + 0.5)
                {
                    maxDepth = points[x, 1] + 0.5;
                }
            }

            H0 = maxDepth;

            for (var x = 0; x < xMax; x++)
            {
                _bedShape[x, 0] = Convert.ToInt32(points[x, 0] * Resolution);
                _bedShape[x, 1] = points[x, 1];
            }

            _initialised = false;
        }

        protected void SetPropertyField<T>(string propertyName, ref T field, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue)) return;
            field = newValue;
            OnPropertyChanged(propertyName);
        }

        #endregion Modifiers

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

            _iterations = 0;
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
                        _isSolid[x, y] = _isSolid[x, y] || shape.IsSolid(x, y);
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
            if (_bedShape[0, 0] > 0)
            {
                for (var y = 0; y < _bedShape[0, 0]; y++)
                {
                    for (var x = 0; x < Lx; x++)
                    {
                        _bedData[x, y] = _bedShape[1, 0];
                    }
                }
            }

            var maxP = _bedShape.GetLength(0) - 1;
            for (var p = 0; p < maxP; p++)
            {
                var y1 = Convert.ToInt32(_bedShape[p, 0]);
                var h1 = _bedShape[p, 1];
                var y2 = Convert.ToInt32(_bedShape[p + 1, 0]);
                var h2 = _bedShape[p + 1, 1];

                if (y1 < 0)
                {
                    y1 = 0;
                }
                if (y2 > Ly)
                {
                    y2 = Ly;
                }

                var m = (h2 - h1) / (y2 - y1);

                for (var y = y1; y < y2; y++)
                {
                    var depth = (m * (y - y1)) + h1;
                    for (var x = 0; x < Lx; x++)
                    {
                        _bedData[x, y] = depth;
                    }
                }
            }

            // If the last part of the bed shape data doesn't touch the other bank, use 
            // that value for the bed in between there and the bank.
            var endPoint = Convert.ToInt32(_bedShape[maxP, 0]);
            if (endPoint >= Ly)
            {
                return;
            }

            for (var y = endPoint + 1; y < Ly; y++)
            {
                for (var x = 0; x < Lx; x++)
                {
                    _bedData[x, y] = endPoint;
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

        public bool Step()
        {
            if (_iterations > _maxT)
            {
                return false;
            }

            ComputeEquilibriumDistributionFunction();
            CollideStream();
            BcBodySlipNs();
            BcBodyMain();
            BcInOut();
            Solution();
            BcInflowOutflow();

            _iterations++;

            return true;
        }

        private void ComputeEquilibriumDistributionFunction()
        {
            if (!_initialised)
            {
                throw new InvalidOperationException("You must initialise the simulator before computing the equilibrium function.");
            }

            // Optimisation
            var eSquared = Math.Pow(E, 2);

            var eSquared3 = eSquared * 3;
            var eSquared6 = eSquared * 6;
            var eSquared12 = eSquared * 12;
            var eSquared24 = eSquared * 24;

            var eToPowerFour = Math.Pow(E, 4);
            var eToPowerFour2 = eToPowerFour * 2;
            var eToPowerFour8 = eToPowerFour * 8;

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

                    // Optimisation
                    var uSquaredPlusVSquared = Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2);

                    for (var a = 1; a < 9; a += 2)
                    {
                        _fEqCurrent[a, x, y] =
                            AccelerationDueToGravity * Math.Pow(_h[x, y], 2) / eSquared6 +
                            _h[x, y] / eSquared3 *
                            (_particleVectors[X, a] * _u[x, y] + _particleVectors[Y, a] * _v[x, y]) + _h[x, y] /
                            eToPowerFour2 *
                            (_particleVectors[X, a] * _u[x, y] * _particleVectors[X, a] * _u[x, y] +
                             2 * _particleVectors[X, a] * _u[x, y] * _particleVectors[Y, a] * _v[x, y] +
                             _particleVectors[Y, a] * _v[x, y] * _particleVectors[Y, a] * _v[x, y]) - _h[x, y] /
                            eSquared6 * (uSquaredPlusVSquared);
                    }

                    for (var a = 2; a < 9; a += 2)
                    {
                        _fEqCurrent[a, x, y] =
                            AccelerationDueToGravity * Math.Pow(_h[x, y], 2) / eSquared24 +
                            _h[x, y] / eSquared12 *
                            (_particleVectors[X, a] * _u[x, y] + _particleVectors[Y, a] * _v[x, y]) + _h[x, y] /
                            eToPowerFour8 *
                            (_particleVectors[X, a] * _u[x, y] * _particleVectors[X, a] * _u[x, y] +
                             2 * _particleVectors[X, a] * _u[x, y] * _particleVectors[Y, a] * _v[x, y] +
                             _particleVectors[Y, a] * _v[x, y] * _particleVectors[Y, a] * _v[x, y]) - _h[x, y] /
                            eSquared24 * (uSquaredPlusVSquared);
                    }

                    _fEqCurrent[CENTRE, x, y] = _h[x, y] -
                                           5 * AccelerationDueToGravity * Math.Pow(_h[x, y], 2) / eSquared6 -
                                           2 * _h[x, y] / eSquared3 *
                                           (uSquaredPlusVSquared);
                }
            }

            if (_iterations == 0)
            {
                _fEq = _fEqCurrent.Clone() as double[,,];
            }
        }

        /// <summary>
        /// Collide streams
        /// </summary>
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

                    // Optimisation
                    var squareRootUSquaredPlusVSquared = Math.Sqrt(Math.Pow(_u[x, y], 2) + Math.Pow(_v[x, y], 2));
                    var dtOver6ESquared = Dt / (6 * E * E);


                    if (xp <= Lx && !_isSolid[xp, y])
                    {
                        _fTemp[RIGHT, xp, y] = _fEq[RIGHT, x, y] -
                                               (_fEq[RIGHT, x, y] - _fEqCurrent[RIGHT, x, y]) / Tau -
                                               dtOver6ESquared * AccelerationDueToGravity * 0.5 *
                                               (_h[x, y] + _h[xp, y]) *
                                               (_particleVectors[X, RIGHT] * _bedSlopeData[X][x, y] +
                                                _particleVectors[Y, RIGHT] * _bedSlopeData[Y][x, y]) -
                                               dtOver6ESquared * Fb * squareRootUSquaredPlusVSquared *
                                               (_particleVectors[X, RIGHT] * _u[x, y] +
                                                _particleVectors[Y, RIGHT] * _v[x, y]);
                    }

                    if (xp < Lx && yp < Ly && !_isSolid[xp, yp])
                    {
                        _fTemp[UP_RIGHT, xp, yp] = _fEq[UP_RIGHT, x, y] -
                                                   (_fEq[UP_RIGHT, x, y] - _fEqCurrent[UP_RIGHT, x, y]) / Tau -
                                                   dtOver6ESquared * AccelerationDueToGravity * 0.5 *
                                                   (_h[x, y] + _h[xp, yp]) *
                                                   (_particleVectors[X, UP_RIGHT] * _bedSlopeData[X][x, y] +
                                                    _particleVectors[Y, UP_RIGHT] * _bedSlopeData[Y][x, y]) -
                                                   dtOver6ESquared * Fb * squareRootUSquaredPlusVSquared *
                                                   (_particleVectors[X, UP_RIGHT] * _u[x, y] +
                                                    _particleVectors[Y, UP_RIGHT] * _v[x, y]);
                    }

                    if (yp < Ly && !_isSolid[x, yp])
                    {
                        _fTemp[UP, x, yp] = _fEq[UP, x, y] - (_fEq[UP, x, y] - _fEqCurrent[UP, x, y]) / Tau -
                                            dtOver6ESquared * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[x, yp]) *
                                            (_particleVectors[X, UP] * _bedSlopeData[X][x, y] +
                                             _particleVectors[Y, UP] * _bedSlopeData[Y][x, y]) -
                                             dtOver6ESquared * Fb * squareRootUSquaredPlusVSquared *
                                            (_particleVectors[X, UP] * _u[x, y] + _particleVectors[Y, UP] * _v[x, y]);
                    }

                    if (xn >= 0 && yp < Ly && !_isSolid[xn, yp])
                    {
                        _fTemp[UP_LEFT, xn, yp] = _fEq[UP_LEFT, x, y] - (_fEq[UP_LEFT, x, y] - _fEqCurrent[UP_LEFT, x, y]) / Tau -
                                            dtOver6ESquared * AccelerationDueToGravity * 0.5 *
                                            (_h[x, y] + _h[xn, yp]) *
                                            (_particleVectors[X, UP_LEFT] * _bedSlopeData[X][xn, y] +
                                             _particleVectors[Y, UP_LEFT] * _bedSlopeData[Y][x, y]) -
                                             dtOver6ESquared * Fb * squareRootUSquaredPlusVSquared *
                                            (_particleVectors[X, UP_LEFT] * _u[x, y] +
                                             _particleVectors[Y, UP_LEFT] * _v[x, y]);
                    }

                    if (xn >= 0 && !_isSolid[xn, y])
                    {
                        _fTemp[LEFT, xn, y] = _fEq[LEFT, x, y] - (_fEq[LEFT, x, y] - _fEqCurrent[LEFT, x, y]) / Tau -
                                           dtOver6ESquared * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[xn, y]) *
                                           (_particleVectors[X, LEFT] * _bedSlopeData[X][xn, y] +
                                            _particleVectors[Y, LEFT] * _bedSlopeData[Y][x, y]) -
                                            dtOver6ESquared * Fb * squareRootUSquaredPlusVSquared *
                                           (_particleVectors[X, LEFT] * _u[x, y] +
                                            _particleVectors[Y, LEFT] * _v[x, y]);
                    }

                    if (xn >= 0 && yn >= 0 && !_isSolid[xn, yn])
                    {
                        _fTemp[DOWN_LEFT, xn, yn] = _fEq[DOWN_LEFT, x, y] - (_fEq[DOWN_LEFT, x, y] - _fEqCurrent[DOWN_LEFT, x, y]) / Tau -
                                            dtOver6ESquared * AccelerationDueToGravity * 0.5 *
                                            (_h[x, y] + _h[xn, yn]) *
                                            (_particleVectors[X, DOWN_LEFT] * _bedSlopeData[X][xn, y] +
                                             _particleVectors[Y, DOWN_LEFT] * _bedSlopeData[Y][x, yn]) -
                                             dtOver6ESquared * Fb * squareRootUSquaredPlusVSquared *
                                            (_particleVectors[X, DOWN_LEFT] * _u[x, y] +
                                             _particleVectors[Y, DOWN_LEFT] * _v[x, y]);
                    }

                    if (yn >= 0 && !_isSolid[x, yn])
                    {
                        _fTemp[DOWN, x, yn] = _fEq[DOWN, x, y] - (_fEq[DOWN, x, y] - _fEqCurrent[DOWN, x, y]) / Tau -
                                           dtOver6ESquared * AccelerationDueToGravity * 0.5 * (_h[x, y] + _h[x, yn]) *
                                           (_particleVectors[X, DOWN] * _bedSlopeData[X][x, y] +
                                            _particleVectors[Y, DOWN] * _bedSlopeData[Y][x, yn]) -
                                            dtOver6ESquared * Fb * squareRootUSquaredPlusVSquared *
                                           (_particleVectors[X, DOWN] * _u[x, y] + _particleVectors[Y, DOWN] * _v[x, y]);
                    }

                    if (xp < Lx && yn >= 0 && !_isSolid[xp, yn])
                    {
                        _fTemp[DOWN_RIGHT, xp, yn] = _fEq[DOWN_RIGHT, x, y] - (_fEq[DOWN_RIGHT, x, y] - _fEqCurrent[DOWN_RIGHT, x, y]) / Tau -
                                            dtOver6ESquared * AccelerationDueToGravity * 0.5 *
                                            (_h[x, y] + _h[xp, yn]) *
                                            (_particleVectors[X, DOWN_RIGHT] * _bedSlopeData[X][x, y] +
                                             _particleVectors[Y, DOWN_RIGHT] * _bedSlopeData[Y][x, yn]) -
                                             dtOver6ESquared * Fb * squareRootUSquaredPlusVSquared *
                                            (_particleVectors[X, DOWN_RIGHT] * _u[x, y] + _particleVectors[Y, DOWN_RIGHT] * _v[x, y]);
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
                _fTemp[UP, x, 0] = _fTemp[DOWN, x, 0];
                _fTemp[UP_LEFT, x, 0] = _fTemp[DOWN_LEFT, x, 0];

                _fTemp[DOWN_RIGHT, x, Ly - 1] = _fTemp[UP_RIGHT, x, Ly - 1];
                _fTemp[DOWN, x, Ly - 1] = _fTemp[UP, x, Ly - 1];
                _fTemp[DOWN_LEFT, x, Ly - 1] = _fTemp[UP_LEFT, x, Ly - 1];
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
                _h[0, y] = _h[1, y]; // Zero gradients
                _v[0, y] = 0;
            }

            double qi = 0, area = 0;
            for (var y = 0; y < Ly - 2; y++)
            {
                qi += 0.5 * (_h[0, y] + _h[0, y + 1]) * Resolution * 0.5 * (_u[0, y] + _u[0, y + 1]);
                area += 0.5 * (_h[0, y] + _h[0, y + 1]) * Resolution;
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
