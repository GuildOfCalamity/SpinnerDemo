using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpinnerDemo.Controls;

public class GlowDecorator : Decorator
{
    #region [Dependency Properties]
    /// <summary>
    /// DependencyProperty for <see cref="GlowDecorator.CornerRadius" /> property.
    /// </summary>
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius),
        typeof(CornerRadius),
        typeof(GlowDecorator),
        new FrameworkPropertyMetadata(new CornerRadius(), FrameworkPropertyMetadataOptions.AffectsRender, null),
        new ValidateValueCallback(IsCornerRadiusValid));
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
    static bool IsCornerRadiusValid(object value)
    {
        CornerRadius cr = (CornerRadius)value;
        return !(cr.TopLeft < 0.0 || cr.TopRight < 0.0 || cr.BottomLeft < 0.0 || cr.BottomRight < 0.0 ||
                 double.IsNaN(cr.TopLeft) || double.IsNaN(cr.TopRight) || double.IsNaN(cr.BottomLeft) || double.IsNaN(cr.BottomRight) ||
                 double.IsInfinity(cr.TopLeft) || double.IsInfinity(cr.TopRight) || double.IsInfinity(cr.BottomLeft) || double.IsInfinity(cr.BottomRight));
    }


    /// <summary>
    /// DependencyProperty for <see cref="GlowDecorator.BlurRadius" /> property.
    /// </summary>
    public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.Register(
        nameof(BlurRadius),
        typeof(double),
        typeof(GlowDecorator),
        new FrameworkPropertyMetadata(6d, FrameworkPropertyMetadataOptions.AffectsRender));
    public double BlurRadius
    {
        get => (double)GetValue(BlurRadiusProperty);
        set => SetValue(BlurRadiusProperty, value);
    }

    /// <summary>
    /// DependencyProperty for <see cref="GlowDecorator.Color" /> property.
    /// </summary>
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color),
        typeof(Color),
        typeof(GlowDecorator),
        new FrameworkPropertyMetadata(Color.FromArgb(255, 0x00, 0x00, 0x00), FrameworkPropertyMetadataOptions.AffectsRender, null));
    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    /// DependencyProperty for <see cref="GlowDecorator.PenThickness" /> property.
    /// </summary>
    public static readonly DependencyProperty PenThicknessProperty = DependencyProperty.Register(
        nameof(PenThickness),
        typeof(double),
        typeof(GlowDecorator),
        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender));
    public double PenThickness
    {
        get => (double)GetValue(PenThicknessProperty);
        set => SetValue(PenThicknessProperty, value);
    }

    /// <summary>
    /// DependencyProperty for <see cref="GlowDecorator.Flood" /> property.
    /// </summary>
    public static readonly DependencyProperty FloodProperty = DependencyProperty.Register(
        nameof(Flood),
        typeof(bool),
        typeof(GlowDecorator),
        new PropertyMetadata(false));
    public bool Flood
    {
        get => (bool)GetValue(FloodProperty);
        set => SetValue(FloodProperty, value);
    }
    #endregion

    public GlowDecorator()
    {
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        int blurRadius = (int)Math.Round(BlurRadius, 0);
        Color color = Color;
        double[] weights = GetWeights(blurRadius);
        Rect shadowBounds = new Rect(new Point(blurRadius, blurRadius), new Size(RenderSize.Width, RenderSize.Height));
        if (shadowBounds.Width > 0 && shadowBounds.Height > 0 && color.A > 0)
        {
            for (int i = 0; i < blurRadius; i++)
            {
                var cornerRadius = CornerRadius;
                var geometry = CreateGeometry(RenderSize.Width, RenderSize.Height, cornerRadius, -i - 1, Flood);
                Brush brush = new SolidColorBrush(Color.FromArgb((byte)(Math.Min(1, weights[i]) * 255), Color.R, Color.G, Color.B));
                drawingContext.DrawGeometry(brush, new Pen(brush, PenThickness), geometry);
            }
        }
    }

    static ArcSegment CreateArcSegment(Point point, double radius)
    {
        return new ArcSegment
        {
            Point = point,
            Size = new Size(radius, radius),
            IsLargeArc = false,
            SweepDirection = SweepDirection.Clockwise
        };
    }

    static PathGeometry CreateGeometry(double width, double height, CornerRadius cornerRadius, double margin, bool flood)
    {
        double top = margin;
        double bottom = height - margin;
        double left = margin;
        double right = width - margin;

        var actualCornerRadius = new CornerRadius(Math.Max(0, cornerRadius.TopLeft - margin), Math.Max(0, cornerRadius.TopRight - margin), Math.Max(0, cornerRadius.BottomRight - margin), Math.Max(0, cornerRadius.TopLeft - margin));

        var figure = new PathFigure { IsClosed = true, IsFilled = flood, StartPoint = new Point(left, cornerRadius.TopLeft) };
        figure.Segments.Add(CreateArcSegment(new Point(cornerRadius.TopLeft, top), actualCornerRadius.TopLeft));
        figure.Segments.Add(new LineSegment { Point = new Point(width - cornerRadius.TopRight, margin) });
        figure.Segments.Add(CreateArcSegment(new Point(right, cornerRadius.TopRight), actualCornerRadius.TopRight));
        figure.Segments.Add(new LineSegment { Point = new Point(right, height - cornerRadius.TopRight) });
        figure.Segments.Add(CreateArcSegment(new Point(width - cornerRadius.BottomRight, bottom), actualCornerRadius.BottomRight));
        figure.Segments.Add(new LineSegment { Point = new Point(cornerRadius.BottomLeft, bottom) });
        figure.Segments.Add(CreateArcSegment(new Point(left, height - cornerRadius.BottomLeft), actualCornerRadius.BottomLeft));

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(figure);

        return pathGeometry;
    }

    /// <summary>
    /// The weights are used to drop-off the alpha as the <see cref="ArcSegment"/>s recede.
    /// </summary>
    double[] GetWeights(int radius = 6)
    {
        double sum = 0.0;
        var samplingWeights = new double[radius + 1];

        for (var i = 0; i < samplingWeights.Length; i++)
        {
            // Choosing a standard deviation of 1/3rd the radius is standard for a discrete approximation of the gaussian function.
            double sd = radius / 3.0;
            double ind = (double)i;
            double weight = (1.0 / (sd * Math.Sqrt(Math.PI * 2))) * Math.Exp(-(ind * ind) / (2.0 * sd * sd));

            // Sum the weights as we go so we can normalize them at the end to ensure conservation of intensity.
            if (i == 0)
                sum += weight;
            else
                sum += weight * 2d;

            samplingWeights[i] = weight;
        }

        double sumWeight = 0.0;
        var result = new double[radius];
        for (int i = radius - 1; i >= 0; i--)
        {
            sumWeight += samplingWeights[i];
            result[i] = sumWeight;
        }

        return result;
    }
}
