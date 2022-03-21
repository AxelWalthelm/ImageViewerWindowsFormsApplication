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
    // Display an image similar to PictureBox but allows interactive zoom operations to view parts of the image enlarged.
    // Always shows image as large as possible, but to preserve aspect ratio empty border regions are added at two sides.
    // If maximum pixel size does not allow the image to be displayed larger (e.g. for very small images), empty border is added at all siedes.
    // Resizing the control will try to keep center of image visible and adjusts zoom settings accordingly.
    // When a new image is set, the control tries to preserve zoom settings;
    // if the new image has different size, zoom settings are adjusted to show a similar region of the image.
    [ToolboxItem(true)]
    //[ToolboxItemFilter("System.Windows.Forms", ToolboxItemFilterType.Custom)]
    [ToolboxBitmap(typeof(PictureBox))]
    public partial class ImageZoomView : Control
    {
        private class Transform
        {
            private double _relativeZoomFactorRaw = 1.0; // [0,1] 1: full image; 0: maximum zoom in (special value, see also RelativeZoomFactor)
            private double _relativeSrcCenterX = 0.5; // [0,1] 0.5: centered
            private double _relativeSrcCenterY = 0.5; // [0,1] 0.5: centered

            public void Reset()
            {
                _relativeZoomFactorRaw = 1;
                _relativeSrcCenterX = 0.5;
                _relativeSrcCenterY = 0.5;

                Update();
            }

            private Size _imageSize;
            private Size _clientSize;

            private double _zoomFactor;

            // image2client pixel resize factor; <1: multiple pixels are drawn in one pixel; >1 pixel are enlarged
            public double ZoomFactor => _zoomFactor;
            public double RelativeZoomFactor => Math.Max(_relativeZoomFactorRaw, MinimumRelativeZoomFactor);

            public bool IsValid => _zoomFactor > 0 && MinimumRelativeZoomFactor < 1;
            public bool IsActive => IsValid && RelativeZoomFactor < 1;

            public double MaximumPixelSize = _maximumPixelSizeDefault;

            public double NeutralRelativeZoomFactor => _imageSize.Width <= 0 || _imageSize.Height <= 0 ? 0 : Math.Min(
                    _clientSize.Width / (double)_imageSize.Width,
                    _clientSize.Height / (double)_imageSize.Height);

            public double MinimumRelativeZoomFactor => NeutralRelativeZoomFactor / MaximumPixelSize;

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

                public Rectangle ToInt()
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

            public Rectangle Src => _src.ToInt();
            public Rectangle Dst => _dst.ToInt();

            public enum VisualizationArea { Zoom, Client, Image, Move }
            private RectangleD GetZoomAreaVisualizationD(double relativeSize, VisualizationArea area)
            {
                relativeSize = Math.Abs(relativeSize);

                double border = 8;
                double maxRelativeSize = Math.Min(
                    (_clientSize.Width - 2 * border) / _clientSize.Width,
                    (_clientSize.Height - 2 * border) / _clientSize.Height);
                relativeSize = Math.Max(Math.Min(relativeSize, maxRelativeSize), 0);
                var clientArea = new RectangleD(border, border, relativeSize * _clientSize.Width, relativeSize * _clientSize.Height);

                if (area == VisualizationArea.Client)
                    return clientArea;

                //double aspect = (_imageSize.Width / _imageSize.Height) / (_clientSize.Width / _clientSize.Height);
                double aspect = (_imageSize.Width * (double)_clientSize.Height) / (_imageSize.Height * (double)_clientSize.Width);
                RectangleD imageArea =
                    aspect < 1
                    ? new RectangleD(clientArea.X + clientArea.Width * (1 - aspect) * 0.5, clientArea.Y, clientArea.Width * aspect, clientArea.Height)
                    : new RectangleD(clientArea.X, clientArea.Y + clientArea.Height * (1 - 1 / aspect) * 0.5, clientArea.Width, clientArea.Height / aspect);

                if (area == VisualizationArea.Image)
                    return imageArea;

                double factorX = imageArea.Width / _imageSize.Width;
                double factorY = imageArea.Height / _imageSize.Height;

                if (area == VisualizationArea.Move)
                {
                    double moveBorder = GetRelativeZoomMoveBorder();
                    double w = _clientSize.Width / _zoomFactor;
                    double srcX0 = moveBorder * (_imageSize.Width - 1) - (w - 1) * 0.5;
                    double srcX1 = (1 - moveBorder) * (_imageSize.Width - 1) - (w - 1) * 0.5 + w;
                    double h = _clientSize.Height / _zoomFactor;
                    double srcY0 = moveBorder * (_imageSize.Height - 1) - (h - 1) * 0.5;
                    double srcY1 = (1 - moveBorder) * (_imageSize.Height - 1) - (h - 1) * 0.5 + h;
                    return new RectangleD(
                        imageArea.X + srcX0 * factorX,
                        imageArea.Y + srcY0 * factorY,
                        (srcX1 - srcX0) * factorX,
                        (srcY1 - srcY0) * factorY);
                }

                return new RectangleD(
                    imageArea.X + _src.X * factorX,
                    imageArea.Y + _src.Y * factorY,
                    _src.Width * factorX,
                    _src.Height * factorY);
            }

            public RectangleF GetZoomAreaVisualizationF(double relativeSize, VisualizationArea area) => GetZoomAreaVisualizationD(relativeSize, area).ToFloat();
            public Rectangle GetZoomAreaVisualization(double relativeSize, VisualizationArea area) => GetZoomAreaVisualizationD(relativeSize, area).ToInt();

            public bool UpdateSizes(Size imageSize, Size clientSize)
            {
                if (_imageSize != imageSize || _clientSize != clientSize)
                {
                    _imageSize = imageSize;
                    _clientSize = clientSize;
                    LimitRelativeSrcCenter();
                }

                return Update();
            }

            private bool Update()
            {
                if (_imageSize.Width <= 0 || _imageSize.Height <= 0 || _clientSize.Width <= 0 || _clientSize.Height <= 0)
                    return false;

                _zoomFactor = NeutralRelativeZoomFactor / RelativeZoomFactor;
                double srcCenterX = _relativeSrcCenterX * (_imageSize.Width - 1);
                double srcCenterY = _relativeSrcCenterY * (_imageSize.Height - 1);
                double w = _clientSize.Width / _zoomFactor;
                double h = _clientSize.Height / _zoomFactor;
                _src = new RectangleD(srcCenterX - (w - 1) * 0.5, srcCenterY - (h - 1) * 0.5, w, h);
                _dst = new RectangleD(0, 0, _clientSize.Width, _clientSize.Height);

                return true;
            }

            public void UpdateZoom(double newRelativeZoomFactor, Point? clientFocus = null)
            {
                UpdateZoom(newRelativeZoomFactor,
                    clientFocus.HasValue ? clientFocus.Value.X / (double)(_clientSize.Width - 1) : 0.5,
                    clientFocus.HasValue ? clientFocus.Value.Y / (double)(_clientSize.Height - 1) : 0.5);
            }

            private double SrcRelative2AbsoluteX(double relativeX) => _src.X + relativeX * (_src.Width - 1);
            private double SrcRelative2AbsoluteY(double relativeY) => _src.Y + relativeY * (_src.Height - 1);

            private double SrcAbsolute2RelativeX(double srcX) => (srcX - _src.X) / (_src.Width - 1);
            private double SrcAbsolute2RelativeY(double srcY) => (srcY - _src.Y) / (_src.Height - 1);

            public void UpdateZoom(double newRelativeZoomFactor, double relativeFocusX, double relativeFocusY)
            {
                if (newRelativeZoomFactor > 1)
                    newRelativeZoomFactor = 1;
                else
                if (newRelativeZoomFactor <= MinimumRelativeZoomFactor)
                    newRelativeZoomFactor = 0; // store as "maximum zoom in"

                if (_relativeZoomFactorRaw == newRelativeZoomFactor)
                    return;

                Update();
                double focusSrcX = SrcRelative2AbsoluteX(relativeFocusX);
                double focusSrcY = SrcRelative2AbsoluteY(relativeFocusY);

                _relativeZoomFactorRaw = newRelativeZoomFactor;
                Update();

                // adust _relativeSrcCenterX/Y so that src pixel at relativeFocusX/Y stays at the same client position
                //
                // SrcAbsolute2RelativeX(focusSrcX) = relativeFocusX
                // (focusSrcX - _src.X) / (_src.Width - 1) = relativeFocusX
                //   _src.X = srcCenterX - (_src.Width - 1) * 0.5
                // (focusSrcX - srcCenterX + (_src.Width - 1) * 0.5) / (_src.Width - 1) = relativeFocusX
                //   srcCenterX = _relativeSrcCenterX * (_imageSize.Width - 1)
                // (focusSrcX - _relativeSrcCenterX * (_imageSize.Width - 1) + (_src.Width - 1) * 0.5) / (_src.Width - 1) = relativeFocusX
                // _relativeSrcCenterX * (_imageSize.Width - 1) = (focusSrcX + (_src.Width - 1) * 0.5 - relativeFocusX * (_src.Width - 1))
                _relativeSrcCenterX = (focusSrcX + (0.5 - relativeFocusX) * (_src.Width - 1)) / (_imageSize.Width - 1);
                _relativeSrcCenterY = (focusSrcY + (0.5 - relativeFocusY) * (_src.Height - 1)) / (_imageSize.Height - 1);

                // adust _relativeSrcCenterX/Y so that displayed image does not move outside of client area when zooming out
                LimitRelativeSrcCenter();

                Update();
            }

            public void UpdateMoveFromClientStep(double clientPixelDeltaX, double clientPixelDeltaY)
            {
                _relativeSrcCenterX += clientPixelDeltaX * RelativeZoomFactor;
                _relativeSrcCenterY += clientPixelDeltaY * RelativeZoomFactor;

                LimitRelativeSrcCenter();

                Update();
            }

            public void UpdateMoveFromClientDrag(int clientPixelDeltaX, int clientPixelDeltaY, double zoomAreaVisualizationSize = 0)
            {
                Update();

                if (zoomAreaVisualizationSize <= 0)
                {
                    // convert client to image relative
                    double dX = clientPixelDeltaX / (_zoomFactor * (_imageSize.Width - 1));
                    double dY = clientPixelDeltaY / (_zoomFactor * (_imageSize.Height - 1));

                    _relativeSrcCenterX -= dX;
                    _relativeSrcCenterY -= dY;
                }
                else
                {
                    var imageArea = GetZoomAreaVisualizationD(zoomAreaVisualizationSize, VisualizationArea.Image);
                    // var zoomArea = GetZoomAreaVisualizationD(zoomAreaVisualizationSize, ZoomAreaVisualization.Zoom);
                    //// expand formula until it contains _relativeSrcCenterX
                    // zoomArea.X = imageArea.X + (_relativeSrcCenterX * (_imageSize.Width - 1) - (_clientSize.Width / _zoomFactor - 1) * 0.5) * imageArea.Width / _imageSize.Width
                    //// simplify formula by substitunting all terms not depending on _relativeSrcCenterX
                    // zoomArea.X = a + (_relativeSrcCenterX * b - c) * d
                    // a := imageArea.X
                    // b := _imageSize.Width - 1
                    // c := (_clientSize.Width / _zoomFactor - 1) * 0.5
                    // d := imageArea.Width / _imageSize.Width;
                    //// we are only interested in change, so we switch to delta by substracting old from new
                    // clientPixelX = zoomArea.X{new} - zoomArea.X{old}
                    // clientPixelX = a + (_relativeSrcCenterX{new} * b - c) * d - (a + (_relativeSrcCenterX{old} * b - c) * d)
                    // clientPixelX = (_relativeSrcCenterX{new} - _relativeSrcCenterX{old}) * b * d
                    //// dX := _relativeSrcCenterX{new} - _relativeSrcCenterX{old}
                    // clientPixelX = dX * b * d
                    // dX = clientPixelX / (b * d)
                    //// re-substitute
                    // dX = clientPixelX / (imageArea.Width * (_imageSize.Width - 1) / _imageSize.Width)
                    // dX = clientPixelX * _imageSize.Width / ((_imageSize.Width - 1) * imageArea.Width)
                    double dX = clientPixelDeltaX * (_imageSize.Width / ((_imageSize.Width - 1) * imageArea.Width));
                    double dY = clientPixelDeltaY * (_imageSize.Height / ((_imageSize.Height - 1) * imageArea.Height));

                    _relativeSrcCenterX += dX;
                    _relativeSrcCenterY += dY;
                }

                LimitRelativeSrcCenter();

                Update();
            }

            // adust _relativeSrcCenterX/Y so that displayed image does not move outside of client area
            private void LimitRelativeSrcCenter()
            {
                double border = GetRelativeZoomMoveBorder();

                if (_relativeSrcCenterX < border)
                    _relativeSrcCenterX = border;
                else
                if (_relativeSrcCenterX > 1 - border)
                    _relativeSrcCenterX = 1 - border;

                if (_relativeSrcCenterY < border)
                    _relativeSrcCenterY = border;
                else
                if (_relativeSrcCenterY > 1 - border)
                    _relativeSrcCenterY = 1 - border;
            }

            private double GetRelativeZoomMoveBorder()
            {
                // _src.X{_relativeSrcCenterX=borderX} = 0
                // _relativeSrcCenterX * (_imageSize.Width - 1) - (_clientSize.Width / _zoomFactor - 1) * 0.5 = 0
                // borderX = (_clientSize.Width / _zoomFactor - 1) * 0.5 / (_imageSize.Width - 1)
                double zoomFactor = NeutralRelativeZoomFactor / RelativeZoomFactor;
                double borderX = (_clientSize.Width / zoomFactor - 1) * 0.5 / (_imageSize.Width - 1);
                double borderY = (_clientSize.Height / zoomFactor - 1) * 0.5 / (_imageSize.Height - 1);
                // apply the smaller border to all sides to allow some filling border to preserve aspect ratio and simplify navigation
                // border > 0.5 means we have filling border at all sides and should display centered
                return Math.Min(Math.Min(borderX, borderY), 0.5);
            }
        }

        private readonly Transform _transform = new Transform();

        public ImageZoomView()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
        }

        private Image _image;

        [Browsable(true)]
        [Category("Appearance")]
        [Description("The image to be displayed.")]
        public Image Image
        {
            get { return _image; }
            set { _image = value; this.Invalidate(); }
        }

        private const double _maximumPixelSizeDefault = 10;

        [Browsable(true)]
        [Category("Behavior")]
        [Description("Maximum allowed pixel size when zooming in. Must be at least 1.")]
        [DefaultValue(_maximumPixelSizeDefault)]
        public double MaximumPixelSize
        {
            get { return _transform.MaximumPixelSize; }
            set { _transform.MaximumPixelSize = Math.Max(1, value); this.Invalidate(); }
        }

        [Browsable(true)]
        [Category("Behavior")]
        [Description("Indicate pixel borders by drawing them as squares without any smoothing.")]
        [DefaultValue(true)]
        public bool ShowPixelBorders
        {
            get { return _transform.ShowPixelBorders; }
            set { _transform.ShowPixelBorders = value; this.Invalidate(); }
        }

        private const double _zoomAreaVisualizationSizeDefault = 0.2;
        private double _zoomAreaVisualizationSize = _zoomAreaVisualizationSizeDefault;

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Relative size of zoom area visualization. 1 is largest, 0 is invisible.")]
        [DefaultValue(_zoomAreaVisualizationSizeDefault)]
        public double ZoomAreaVisualizationSize
        {
            get { return _zoomAreaVisualizationSize; }
            set { _zoomAreaVisualizationSize = Math.Min(Math.Max(value, 0), 1); this.Invalidate(); }
        }

        public enum ZoomVisualizationMode { Off, AreasWhenZoomed, Areas, AreasAndScale }
        private const ZoomVisualizationMode _zoomVisualizationDefault = ZoomVisualizationMode.AreasWhenZoomed;
        private ZoomVisualizationMode _zoomVisualization = _zoomVisualizationDefault;

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Determines how zoom is visualized. Display zoom areas? Display zoom scale?")]
        [DefaultValue(_zoomVisualizationDefault)]
        public ZoomVisualizationMode ZoomVisualization
        {
            get { return _zoomVisualization; }
            set { _zoomVisualization = value; this.Invalidate(); }
        }

        public double RelativeZoom => _transform.RelativeZoomFactor;
        public double Zoom => _transform.ZoomFactor;

        public void SetRelativeZoom(double newRelativeZoomFactor, double relativeFocusX = 0.5, double relativeFocusY = 0.5)
        {
            if (_image == null || !_transform.UpdateSizes(_image.Size, this.ClientSize))
                return;

            _transform.UpdateZoom(newRelativeZoomFactor, relativeFocusX, relativeFocusY);
            this.Invalidate();
        }

        protected bool IsZoomVisualization => _image != null && _transform.IsValid && this.ClientSize.Width > 20 && this.ClientSize.Height > 20;
        protected bool IsZoomScaleVisualization => IsZoomVisualization && _zoomVisualization == ZoomVisualizationMode.AreasAndScale;
        protected bool IsZoomAreaVisualization => IsZoomVisualization && _zoomVisualization != ZoomVisualizationMode.Off && (_zoomVisualization != ZoomVisualizationMode.AreasWhenZoomed || _transform.IsActive);

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (_image != null)
            {
                _transform.UpdateSizes(_image.Size, this.ClientSize);
            }

            this.Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            this.Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);

            this.Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs pe)
        {
            // When overriding [...] it is not necessary to call the base class's OnPaintBackground(PaintEventArgs).

            Graphics graphics = pe.Graphics;

            using (var brush = new SolidBrush(this.BackColor))
            {
                graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            // When overriding [...], be sure to call the base class's OnPaint(PaintEventArgs) method so that registered delegates receive the event.
            base.OnPaint(pe);

            Graphics graphics = pe.Graphics;

            Color grayText = this.BackColor == SystemColors.Control && this.ForeColor == SystemColors.ControlText
                ? SystemColors.GrayText
                : MiddleColor(this.BackColor, this.ForeColor, 0.543);
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

            ControlPaint.DrawBorder(graphics, this.ClientRectangle, grayText, ButtonBorderStyle.Dotted);

            if (hasImage && _transform.UpdateSizes(_image.Size, this.ClientSize))
            {
                graphics.InterpolationMode = _transform.ZoomFactor < 2 || !_transform.ShowPixelBorders ? InterpolationMode.HighQualityBilinear : InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half; // important for InterpolationMode.NearestNeighbor at high zoom in to draw all border pixels
                graphics.DrawImage(_image, _transform.DstF, _transform.SrcF, GraphicsUnit.Pixel);
                graphics.InterpolationMode = InterpolationMode.Bilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.Default;

                if (IsZoomAreaVisualization)
                {
                    var outer = _transform.GetZoomAreaVisualizationF(_zoomAreaVisualizationSize, Transform.VisualizationArea.Client);
                    var inner = _transform.GetZoomAreaVisualizationF(_zoomAreaVisualizationSize, Transform.VisualizationArea.Zoom);
                    var image = _transform.GetZoomAreaVisualizationF(_zoomAreaVisualizationSize, Transform.VisualizationArea.Image);
                    //var zmove = _transform.GetZoomAreaVisualizationF(_zoomAreaVisualizationSize, Transform.VisualizationArea.Move);

                    using (var backPen = new Pen(this.BackColor, 5))
                    using (var forePen = new Pen(this.ForeColor, 1))
                    using (var palePen = new Pen(MiddleColor(this.BackColor, this.ForeColor, 0.2), 1))
                    using (var highlightPen = _dragLeftMouse.IsFastDrag ? new Pen(SystemColors.Highlight, backPen.Width) : null)
                    {
                        var dragPen = highlightPen ?? backPen;

                        graphics.SmoothingMode = SmoothingMode.AntiAlias;

                        graphics.DrawRectangle(backPen, outer.X, outer.Y, outer.Width, outer.Height);
                        graphics.DrawRectangle(backPen, image.X, image.Y, image.Width, image.Height);
                        graphics.DrawRectangle(dragPen, inner.X, inner.Y, inner.Width, inner.Height);
                        //graphics.DrawRectangle(backPen, zmove.X, zmove.Y, zmove.Width, zmove.Height);

                        graphics.DrawRectangle(palePen, outer.X, outer.Y, outer.Width, outer.Height);
                        graphics.DrawRectangle(forePen, image.X, image.Y, image.Width, image.Height);
                        graphics.DrawRectangle(forePen, inner.X, inner.Y, inner.Width, inner.Height);
                        //graphics.DrawRectangle(Pens.Red, zmove.X, zmove.Y, zmove.Width, zmove.Height);

                        graphics.SmoothingMode = SmoothingMode.None;
                    }
                }
            }

            if (IsZoomScaleVisualization)
            {
                Rectangle displayArea = this.ClientRectangle;
                displayArea.Inflate(-5, -5);

                using (var foreBrush = new SolidBrush(this.ForeColor))
                using (var backBrush = new SolidBrush(this.BackColor))
                using (var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near })
                {
                    string to = "\u2009:\u2009"; // 0x2009 is narrow space
                    string text = _transform.ZoomFactor > 1 ? $"{_transform.ZoomFactor:0.##}{to}1" : $"1{to}{1 / _transform.ZoomFactor:0.##}";
                    foreach (var offset in new[] {
                            new Point(1, 1), new Point(1, -1), new Point(-1, 1), new Point(-1, -1),
                            new Point(-1, 0), new Point(1, 0), new Point(0, 1), new Point(0, -1) })
                    {
                        Rectangle r = displayArea;
                        r.Offset(offset);
                        graphics.DrawString(text, this.Font, backBrush, r, format);
                    }
                    graphics.DrawString(text, this.Font, foreBrush, displayArea, format);
                }
            }

            if (!hasImage)
            {
                using (var brush = new SolidBrush(grayText))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    graphics.DrawString(this.Text, this.Font, brush, this.ClientRectangle, format);
                }
            }
        }

        private static Color MiddleColor(Color color1, Color color2, double weight2 = 0.5)
        {
            double weight1 = 1 - weight2;
            return Color.FromArgb(
                Math.Min(Math.Max((int)(color1.R * weight1 + color2.R * weight2), 0), 255),
                Math.Min(Math.Max((int)(color1.G * weight1 + color2.G * weight2), 0), 255),
                Math.Min(Math.Max((int)(color1.B * weight1 + color2.B * weight2), 0), 255));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            TrackDragMouse(e, MouseEventType.Down);

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            TrackDragMouse(e, MouseEventType.Move);

            base.OnMouseMove(e);
        }

        // we do not expect to get this event (cursor could leave without mouse up)
        // but if we get it, we use it
        protected override void OnMouseUp(MouseEventArgs e)
        {
            TrackDragMouse(e, MouseEventType.Up);

            base.OnMouseUp(e);
        }

        private struct MouseDragInfo
        {
            public Point LastPosition;
            public bool IsValid;
            public bool IsStartedOnZoomAreaVisualization;

            public bool IsFastDrag => IsValid && IsStartedOnZoomAreaVisualization;
        }
        private MouseDragInfo _dragLeftMouse;

        private enum MouseEventType { Move, Up, Down };
        private void TrackDragMouse(MouseEventArgs e, MouseEventType eventType)
        {
            if ((e.Button & MouseButtons.Left) != 0 && eventType != MouseEventType.Up && _transform.IsActive)
            {
                if (_dragLeftMouse.IsValid)
                {
                    // move zoom location
                    int dX = e.Location.X - _dragLeftMouse.LastPosition.X;
                    int dY = e.Location.Y - _dragLeftMouse.LastPosition.Y;

                    if (dX != 0 || dY != 0)
                    {
                        _transform.UpdateMoveFromClientDrag(dX, dY, _dragLeftMouse.IsFastDrag ? _zoomAreaVisualizationSize : 0);

                        this.Invalidate();
                    }
                }
                else if (eventType == MouseEventType.Down)
                {
                    Rectangle zoomArea = _transform.GetZoomAreaVisualization(_zoomAreaVisualizationSize, Transform.VisualizationArea.Zoom);
                    zoomArea.Inflate(3, 3);
                    Point clientPoint = e.Location;
                    bool isFastDrag = zoomArea.Contains(clientPoint);
                    _dragLeftMouse.IsValid = true;
                    _dragLeftMouse.IsStartedOnZoomAreaVisualization = isFastDrag;
                    if (isFastDrag)
                    {
                        // limit cursor movement
                        Rectangle moveArea = _transform.GetZoomAreaVisualization(_zoomAreaVisualizationSize, Transform.VisualizationArea.Move);
                        Cursor.Clip = this.RectangleToScreen(moveArea);
                    }
                    // set move cursor
                    Cursor.Current = isFastDrag ? Cursors.Cross : Cursors.SizeAll;

                    this.Invalidate();
                }

                _dragLeftMouse.LastPosition = e.Location;
            }
            else if (_dragLeftMouse.IsValid)
            {
                _dragLeftMouse.IsValid = false;

                // release any limitation of cursor movement
                Cursor.Clip = new Rectangle();
                // unset move cursor
                Cursor.Current = Cursors.Default;

                this.Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            this.Focus();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            this.Focus();

            if (e.Button == MouseButtons.Left)
            {
                Zoom3Steps(true, true, e.Location);

                Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                _transform.Reset();

                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs mea)
        {
            base.OnMouseWheel(mea);

            this.FindForm()?.Activate();
            this.Focus();

            if (mea.Delta != 0)
            {
                ZoomScroll(mea.Location, mea.Delta > 0, ModifierKeys);
                Invalidate();
            }
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                case Keys.Up:
                case Keys.Left:
                case Keys.Right:
                    e.IsInputKey = true;
                    break;

                case Keys.Tab:
                    if (e.Modifiers == Keys.Control || e.Modifiers == (Keys.Shift | Keys.Control))
                        e.IsInputKey = true;
                    break;
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                case Keys.D0:
                case Keys.NumPad0:
                    _transform.Reset();
                    Invalidate();
                    e.Handled = true;
                    break;

                case Keys.D1:
                case Keys.D2:
                case Keys.D3:
                case Keys.D4:
                case Keys.D5:
                case Keys.D6:
                case Keys.D7:
                case Keys.D8:
                case Keys.D9:
                case Keys.NumPad1:
                case Keys.NumPad2:
                case Keys.NumPad3:
                case Keys.NumPad4:
                case Keys.NumPad5:
                case Keys.NumPad6:
                case Keys.NumPad7:
                case Keys.NumPad8:
                case Keys.NumPad9:
                    int digit = Math.Min(Math.Max(e.KeyCode <= Keys.D9 ? e.KeyCode - Keys.D0 : e.KeyCode - Keys.NumPad0, 0), 9);
                    double min = _transform.MinimumRelativeZoomFactor;
                    // newRelativeZoomFactor = stepIn^(digit / -9.0)
                    // if digit = 9 then newRelativeZoomFactor = min
                    // min = stepIn^(-1)
                    // stepIn = 1 / min
                    double newRelativeZoomFactor = digit == 9 ? 0 : Math.Pow(1 / min, digit / -9.0);
                    _transform.UpdateZoom(newRelativeZoomFactor);
                    Invalidate();
                    e.Handled = true;
                    break;

                case Keys.Home:
                    Zoom3Steps(false);
                    Invalidate();
                    e.Handled = true;
                    break;

                case Keys.End:
                    Zoom3Steps(true);
                    Invalidate();
                    e.Handled = true;
                    break;

                case Keys.Space:
                    if (e.Modifiers == Keys.None)
                    {
                        Zoom3Steps(true, true);
                        Invalidate();
                    }
                    else if (e.Modifiers == Keys.Shift)
                    {
                        Zoom3Steps(false, true);
                        Invalidate();
                    }
                    e.Handled = true;
                    break;

                case Keys.Enter:
                case Keys.PageUp:
                case Keys.Oemplus:
                case Keys.Add:
                case Keys.Q:
                    ZoomStep(1, e.Modifiers);
                    Invalidate();
                    e.Handled = true;
                    break;

                case Keys.Back:
                case Keys.PageDown:
                case Keys.OemMinus:
                case Keys.Subtract:
                case Keys.E:
                case Keys.Control | Keys.Down:
                case Keys.Control | Keys.Left:
                    ZoomStep(-1, e.Modifiers);
                    Invalidate();
                    e.Handled = true;
                    break;

                case Keys.Up:
                case Keys.W:
                    ZoomMove(0, -1, e.Modifiers);
                    Invalidate();
                    break;

                case Keys.Down:
                case Keys.S:
                    ZoomMove(0, 1, e.Modifiers);
                    Invalidate();
                    break;

                case Keys.Left:
                case Keys.A:
                    ZoomMove(-1, 0, e.Modifiers);
                    Invalidate();
                    break;

                case Keys.Right:
                case Keys.D:
                    ZoomMove(1, 0, e.Modifiers);
                    Invalidate();
                    break;

                case Keys.Tab:
                    if (_image != null)
                    {
                        if (e.Modifiers == Keys.Control)
                        {
                            var values = (ZoomVisualizationMode[])Enum.GetValues(typeof(ZoomVisualizationMode));
                            ZoomVisualization = values[(Array.IndexOf(values, ZoomVisualization) + 1) % values.Length];
                        }
                        else if (e.Modifiers == (Keys.Shift | Keys.Control))
                        {
                            var values = (ZoomVisualizationMode[])Enum.GetValues(typeof(ZoomVisualizationMode));
                            ZoomVisualization = values[(Array.IndexOf(values, ZoomVisualization) + values.Length - 1) % values.Length];
                        }
                    }
                    break;
            }

            base.OnKeyDown(e);
        }

        private void Zoom3Steps(bool zoomIn, bool cyclic = false, Point? clientFocus = null)
        {
            if (_image == null)
            {
                _transform.Reset();
                return;
            }

            if (zoomIn)
            {
                if (_transform.RelativeZoomFactor > _transform.NeutralRelativeZoomFactor)
                {
                    _transform.UpdateZoom(_transform.NeutralRelativeZoomFactor, clientFocus);
                }
                else if (_transform.RelativeZoomFactor > _transform.MinimumRelativeZoomFactor)
                {
                    _transform.UpdateZoom(0, clientFocus);
                }
                else if (cyclic)
                {
                    _transform.Reset();
                }
            }
            else
            {
                if (_transform.RelativeZoomFactor < _transform.NeutralRelativeZoomFactor)
                {
                    _transform.UpdateZoom(_transform.NeutralRelativeZoomFactor, clientFocus);
                }
                else if (_transform.RelativeZoomFactor < 1)
                {
                    _transform.Reset();
                }
                else if (cyclic)
                {
                    _transform.UpdateZoom(0, clientFocus);
                }
            }
        }

        private void ZoomScroll(Point focus, bool zoomIn, Keys modifiers)
        {
            if (_image == null || !_transform.UpdateSizes(_image.Size, this.ClientSize))
                return;

            double stepFactor = GetZoomSpeed(zoomIn ? 1 : -1, modifiers);
            double newRelativeZoomFactor = _transform.RelativeZoomFactor * stepFactor;

            _transform.UpdateZoom(newRelativeZoomFactor, focus);
        }

        private double GetSpeed(Keys modifiers, double slow, double normal, double fast)
        {
            if ((modifiers & Keys.Control) != 0)
                return fast;
            if ((modifiers & Keys.Shift) != 0)
                return slow;
            return normal;
        }

        private double GetMoveSpeed(Keys modifiers) => GetSpeed(modifiers, 0.05, 0.5, 0.9);

        private const double _scrollZoomFactor = 1.15;

        private double GetZoomSpeed(double stepIn, Keys modifiers) => Math.Pow(_scrollZoomFactor, stepIn * -GetSpeed(modifiers, 0.25, 1, 4));

        private void ZoomStep(double stepIn, Keys modifiers)
        {
            if (_image == null)
                return;

            double stepFactor = GetZoomSpeed(stepIn, modifiers);
            double newRelativeZoomFactor = _transform.RelativeZoomFactor * stepFactor;
            _transform.UpdateZoom(newRelativeZoomFactor);
        }

        private void ZoomMove(double dX, double dY, Keys modifiers)
        {
            if (_image == null)
                return;

            // if we are fully zoomed out move would have no effect => zoom in a bit
            if (!_transform.IsActive)
                _transform.UpdateZoom(0.25);

            double speed = GetMoveSpeed(modifiers);
            _transform.UpdateMoveFromClientStep(dX * speed, dY * speed);
        }
    }
}
