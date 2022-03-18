using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageViewerWindowsFormsApplication
{
    [ToolboxItem(true)]
    //[ToolboxItemFilter("System.Windows.Forms", ToolboxItemFilterType.Custom)]
    [ToolboxBitmap(typeof(PictureBox))]
    public partial class ImageZoomView : Control
    {
        public const double _scrollZoomFactor = 1.15;
        private const double _maximumPixelSizeDefault = 10;

        private class Transform
        {
            private double _relativeZoomFactorRaw = 1.0; // [0,1] 1: full image; 0: maximum zoom in
            private double _relativeSrcCenterX = 0.5; // [0,1] 0.5: centered
            private double _relativeSrcCenterY = 0.5; // [0,1] 0.5: centered

            public void Reset()
            {
                _relativeZoomFactorRaw = 1;
                _relativeSrcCenterX = 0.5;
                _relativeSrcCenterY = 0.5;

                UpdateAbsolute();
            }

            private Size _imageSize;
            private Size _clientSize;

            private double _zoomFactor;

            // image2client pixel resize factor; <1: multiple pixels are drawn in one pixel; >1 pixel are enlarged
            public double ZoomFactor => _zoomFactor;
            public double RelativeZoomFactor => Math.Max(_relativeZoomFactorRaw, MinimumRelativeZoomFactor);

            public double MaximumPixelSize = _maximumPixelSizeDefault;

            public double MinimumRelativeZoomFactor => _imageSize.Width <= 0 || _imageSize.Height <= 0 ? 0 : Math.Min(
                    _clientSize.Width / (double)_imageSize.Width,
                    _clientSize.Height / (double)_imageSize.Height) / MaximumPixelSize;

            private struct RectangleD
            {
                public double X;
                public double Y;
                public double Width;
                public double Height;

                public RectangleD(double x, double y, double width, double height)
                {
                    X = x; Y = y; Width = width; Height = height;
                }

                public RectangleF ToFloat()
                {
                    return new RectangleF((float)X, (float)Y, (float)Width, (float)Height);
                }

                public RectangleF ToInt()
                {
                    int x0 = (int)Math.Round(X, MidpointRounding.AwayFromZero);
                    int y0 = (int)Math.Round(Y, MidpointRounding.AwayFromZero);
                    int x1 = (int)Math.Round(X + Width, MidpointRounding.AwayFromZero);
                    int y1 = (int)Math.Round(Y + Height, MidpointRounding.AwayFromZero);
                    return new Rectangle(x0, y0, x1 - x0, y1 - y0);
                }
            }

            private RectangleD _src;
            private RectangleD _dst;

            public bool ShowPixelBorders = true;

            public RectangleF SrcF => _src.ToFloat();
            public RectangleF DstF => _dst.ToFloat();

            public RectangleF Src => _src.ToInt();
            public RectangleF Dst => _dst.ToInt();

            public void UpdateSizes(Size imageSize, Size clientSize)
            {
                _imageSize = imageSize;
                _clientSize = clientSize;

                UpdateAbsolute();
            }

            private void UpdateAbsolute()
            {
                if (_imageSize.Width <= 0 || _imageSize.Height <= 0 || _clientSize.Width <= 0 || _clientSize.Height <= 0)
                    return;

                _zoomFactor = Math.Min(
                    _clientSize.Width / (double)_imageSize.Width,
                    _clientSize.Height / (double)_imageSize.Height) / RelativeZoomFactor;
                double srcCenterX = _relativeSrcCenterX * (_imageSize.Width - 1);
                double srcCenterY = _relativeSrcCenterY * (_imageSize.Height - 1);
                double w = _clientSize.Width / _zoomFactor;
                double h = _clientSize.Height / _zoomFactor;
                _src = new RectangleD(srcCenterX - (w - 1) * 0.5, srcCenterY - (h - 1) * 0.5, w, h);
                _dst = new RectangleD(0, 0, _clientSize.Width, _clientSize.Height);
            }

            public void AdjustRelativeZoom(double newRelativeZoomFactor, Point clientFocus)
            {
                UpdateRelativeZoom(newRelativeZoomFactor, clientFocus.X / (double)(_clientSize.Width - 1), clientFocus.Y / (double)(_clientSize.Height - 1));
            }

            public void UpdateRelativeZoom(double newRelativeZoomFactor, double relativeFocusX, double relativeFocusY)
            {
                if (newRelativeZoomFactor > 1)
                    newRelativeZoomFactor = 1;
                else
                if (newRelativeZoomFactor <= MinimumRelativeZoomFactor)
                    newRelativeZoomFactor = 0; // store as "maximum zoom in"

                if (_relativeZoomFactorRaw == newRelativeZoomFactor)
                    return;

                // TODO: move _relativeSrcCenterX/Y
                _relativeZoomFactorRaw = newRelativeZoomFactor;

                UpdateAbsolute();
            }
        }

        private readonly Transform _transform = new Transform();

        public ImageZoomView()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            this.GotFocus += (object sender, EventArgs e) => this.Invalidate();
            this.LostFocus += (object sender, EventArgs e) => this.Invalidate();
            this.SizeChanged += (object sender, EventArgs e) => this.Invalidate();
        }

        private Image _image;

        [Browsable(true)]
        [Category("Appearance")]
        [Description("The image displayed.")]
        public Image Image
        {
            get { return _image; }
            set { _image = value; this.Invalidate(); }
        }

        [Browsable(true)]
        [Category("Behavior")]
        [Description("Maximum pixel size.")]
        [DefaultValue(_maximumPixelSizeDefault)]
        public double MaximumPixelSize
        {
            get { return _transform.MaximumPixelSize; }
            set { _transform.MaximumPixelSize = value; this.Invalidate(); }
        }

        [Browsable(true)]
        [Category("Behavior")]
        [Description("Indicate pixel borders by drawing them crisp.")]
        [DefaultValue(true)]
        public bool ShowPixelBorders
        {
            get { return _transform.ShowPixelBorders; }
            set { _transform.ShowPixelBorders = value; this.Invalidate(); }
        }

        protected override void OnPaintBackground(PaintEventArgs pe)
        {
            Graphics graphics = pe.Graphics;

            using (var brush = new SolidBrush(this.BackColor))
            {
                graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Graphics graphics = pe.Graphics;

            Color color = SystemColors.GrayText;
            bool isActive = this.Focused;
            bool hasImage = this._image != null;

            if (isActive)
            {
                const int highlightBorderThickness = 3;
                using (var pen = new Pen(SystemColors.Highlight, (highlightBorderThickness - 1) * 2 + 1))
                {
                    graphics.DrawRectangle(pen, new Rectangle(0, 0, this.Width - 1, this.Height - 1));
                }
            }

            ControlPaint.DrawBorder(graphics, this.ClientRectangle, color, ButtonBorderStyle.Dotted);

            if (hasImage)
            {
                _transform.UpdateSizes(_image.Size, this.ClientSize);

                graphics.InterpolationMode = _transform.ZoomFactor < 1 || !_transform.ShowPixelBorders ? InterpolationMode.HighQualityBilinear : InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half; // important for InterpolationMode.NearestNeighbor at high zoom in to draw all border pixels
                graphics.DrawImage(_image, _transform.DstF, _transform.SrcF, GraphicsUnit.Pixel);
                graphics.InterpolationMode = InterpolationMode.Bilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.Default;
            }

            //var r1 = this.ClientRectangle;
            //var r2 = r1;
            //r1.Inflate(-5, -5);
            //ControlPaint.DrawSelectionFrame(graphics, isActive, r2, r1, this.BackColor);

            if (!hasImage)
            {
                //using (var pen = new Pen(color))
                //{
                //    graphics.DrawRectangle(pen, new Rectangle(0, 0, this.Width - 1, this.Height - 1));
                //}

                using (var brush = new SolidBrush(color))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    graphics.DrawString(this.Text, this.Font, brush, this.ClientRectangle, format);
                }

                //using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                //{
                //    ControlPaint.DrawStringDisabled(graphics, this.Text, this.Font, this.ForeColor, this.ClientRectangle, format);
                //}
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            this.Focus();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _transform.Reset();
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs mea)
        {
            this.Focus();

            if (mea.Delta != 0)
            {
                Point clientPoint = PointToClient(this.PointToScreen(mea.Location));
                ZoomScroll(clientPoint, mea.Delta > 0);
                Invalidate();
            }
        }

        private void ZoomScroll(Point focus, bool zoomIn)
        {
            _transform.UpdateSizes(_image.Size, this.ClientSize);

            double newRelativeZoomFactor = _transform.RelativeZoomFactor * (zoomIn ? 1 / _scrollZoomFactor : _scrollZoomFactor);

            _transform.AdjustRelativeZoom(newRelativeZoomFactor, focus);
        }
    }
}
