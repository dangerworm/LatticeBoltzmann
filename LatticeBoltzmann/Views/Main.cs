using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using LatticeBoltzmann.Models;
using Rectangle = LatticeBoltzmann.Models.Rectangle;

namespace LatticeBoltzmann.Views
{
    public partial class Main : Form
    {
        private LatticeBoltzmannSimulator _simulator;
        private readonly Graphics _solidsGraphics;
        private readonly Bitmap _solidsImage;

        private bool _running;
        private int _counter;
        private double _time;
        private Collection<Particle> _particles;

        public Main()
        {
            InitializeComponent();
            InitializeSimulator();

            secSettingsEditor.SetDataSource(_simulator);

            _solidsImage = new Bitmap(pbxSolids.Width, pbxSolids.Height);
            _solidsGraphics = Graphics.FromImage(_solidsImage);
            _particles = new Collection<Particle>();

            for (int x = 0; x < 52; x++)
            {
                _particles.Add(new Particle(25, 6 * x, Color.LightBlue));
            };
        }

        private void InitializeSimulator()
        {
            _simulator = new LatticeBoltzmannSimulator();
            _simulator.PropertyChanged += SettingChanged;

            foreach (var shape in ShapeManager.GetColumns(_simulator.Resolution))
            {
                _simulator.AddShape(shape);
            }
            _simulator.SetBedShape(BedPointManager.GetBedPoints());
        }

        private void RunSimulation()
        {
            _simulator.Init();

            txtConsole.Clear();
            btnRun.Enabled = false;

            Application.DoEvents();

            _running = true;

            _counter = 0;
            while (_running && _simulator.Step())
            {
                _time = ++_counter * _simulator.Dt;

                txtConsole.Text += $@"Iteration {_counter:d4}: t = {_time:N2}, f[100, 80] = {_simulator.GetFunctionValueAtPoint(100, 80)}{Environment.NewLine}";
                txtConsole.SelectionStart = txtConsole.Text.Length - 1;
                txtConsole.ScrollToCaret();

                DrawDepths();
                DrawSolids();
                DrawArrows();
                DrawParticlePath();

                Application.DoEvents();
            }

            _running = false;
            btnRun.Enabled = true;
        }

        private void DrawDepths()
        {
            var depth = _simulator.Depths;

            for (var y = 0; y < _simulator.Ly; y++)
            {
                for (var x = 0; x < _simulator.Lx; x++)
                {
                    var depthValue = Convert.ToInt32(depth[x, y] * 255 / _simulator.H0);
                    var brush = new SolidBrush(Color.FromArgb(depthValue / 3, depthValue / 3, depthValue));

                    _solidsGraphics.FillRectangle(brush, x * 2, y * 2, 3, 3);
                }
            }

            _solidsGraphics.Flush();
            pbxSolids.Image = _solidsImage;
        }

        private void DrawSolids()
        {
            var isSolid = _simulator.Solids;

            for (var y = 0; y < _simulator.Ly; y++)
            {
                for (var x = 0; x < _simulator.Lx; x++)
                {
                    if (isSolid[x, y])
                    {
                        _solidsGraphics.FillRectangle(Brushes.Black, x * 2, y * 2, 2, 2);
                    }
                }
            }

            _solidsGraphics.Flush();
            pbxSolids.Image = _solidsImage;
        }

        private void DrawArrows()
        {
            const int spacing = 10;

            for (var y = 0; y < _simulator.Ly; y += spacing)
            {
                for (var x = 0; x < _simulator.Lx; x += spacing)
                {
                    var vector = _simulator.GetMovementVectorAtPoint(x, y);

                    var pen = new Pen(Color.Red) {EndCap = LineCap.ArrowAnchor};
                    var startPoint = new Point((x - vector.X) * 2, (y - vector.Y) * 2);
                    var endPoint = new Point((x + vector.X) * 2, (y + vector.Y) * 2);

                    if (IsWithinBounds(startPoint, 2) && IsWithinBounds(endPoint, 2))
                    {
                        _solidsGraphics.DrawLine(pen, startPoint, endPoint);
                    }
                }
            }
        }

        private void DrawParticlePath()
        {
            var solid = _simulator.Solids;

            foreach (var particle in _particles)
            {
                var x = particle.CurrentPosition.X / 2;
                var y = particle.CurrentPosition.Y / 2;

                if (IsWithinBounds(x, y))
                {
                    var vector = _simulator.GetMovementVectorAtPoint(x, y);
                    var newX = x + vector.X;
                    var newY = y + vector.Y;

                    if (IsWithinBounds(newX, newY) && !solid[x + vector.X, y + vector.Y])
                    {
                        particle.Move(vector);
                    }
                }

                for (var p = 0; p < particle.History.Count - 1; p++)
                {
                    _solidsGraphics.DrawLine(new Pen(particle.Colour), particle.History[p], particle.History[p+1]);
                }
            }
        }

        private bool IsWithinBounds(Point point, int correctionFactor = 1)
        {
            return IsWithinBounds(point.X, point.Y, correctionFactor);
        }

        private bool IsWithinBounds(int x, int y, int correctionFactor = 1)
        {
            return x / correctionFactor >= 0 && x / correctionFactor < _simulator.Lx &&
                   y / correctionFactor >= 0 && y / correctionFactor < _simulator.Ly;
        }

        #region UI hooks

        private void btnRun_Click(object sender, EventArgs e)
        {
            RunSimulation();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            _running = false;
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            _running = false;
            _simulator.Init();
        }

        private string GetSettingValue(string propertyName)
        {
            var properties = _simulator.GetType().GetProperties();
            return properties
                .FirstOrDefault(x => x.Name.Equals(propertyName))?
                .GetGetMethod()
                .Invoke(_simulator, null)
                .ToString();
        }

        private void SettingChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (_running)
            {
                return;
            }

            var value = GetSettingValue(eventArgs.PropertyName);
            tssStatus.Text = $@"{eventArgs.PropertyName} changed to {value}.";
            _simulator.Init();
        }

        #endregion UI hooks
    }
}
