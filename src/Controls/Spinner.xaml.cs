using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SpinnerDemo.Controls;

/**
 **   🛠️🛠️ THE ULTIMATE REUSABLE SPINNER CONTROL 🛠️🛠️
 **
 **           Copyright © The Guild 2024-2025
 **/

public enum SpinnerRenderMode
{
    RotateCanvas,    // if using single color with dot circle (simpler mode, but less versatile)
    AnimatePositions // if using gradient brush and versatile animations/shapes
}

public enum SpinnerRenderShape
{
    Dots,      // for standard/classic spinner
    Worm,      // for wiggle worm animation
    Spiral,    // for spiral rotation animation
    Polys,     // for spinner with more complex shapes
    Snow,      // for raining/snowing animation
    Wind,      // for horizontal animation
    Wave,      // for sine wave animation
    Space,     // for starfield animation
    Line,      // for line warp animation
    Stripe,    // for exaggerated line animation
    Bounce,    // for dot bouncing animation
    Square,    // for walking square animation
    Rings,     // for concentric ring animation
    Pulse,     // for ring pulse animation
    Twinkle1,  // for twinkling star animation
    Twinkle2,  // for twinkling star animation (with enhanced gradient brushes)
    Meteor1,   // for shooting star animation
    Meteor2,   // for shooting star animation (with enhanced color palette)
    Falling,   // for drop animation
    Explode,   // for explosion animation
    Fountain,  // for fountain animation
}

/// <summary>
///   If mode is set to <see cref="SpinnerRenderMode.RotateCanvas"/> then some of<br/>
///   the more advanced animations will not render correctly, it's<br/>
///   recommended to keep the mode set to <see cref="SpinnerRenderMode.AnimatePositions"/><br/>
///   which employs the <see cref="CompositionTarget.Rendering"/> surface event.<br/>
///   Visibility determines if animation runs.
/// </summary>
/// <remarks>
///   Most render methods have their own data elements, however some are<br/>
///   shared, e.g. the Snow/Wind/Space modes all use the _rain arrays.<br/>
///   The opacity for each dot is currently set per-frame in the render<br/>
///   phase, but this could be moved to the creation phase if desired.
/// </remarks>
public partial class Spinner : UserControl
{
    bool hasAppliedTemplate = false;
    bool _renderHooked = false;
    double _angle = 0.0;
    const double Tau = 2 * Math.PI;
    const double Epsilon = 0.000000000001;

    public int DotCount { get; set; } = 10;
    public double DotSize { get; set; } = 8;
    public Brush DotBrush { get; set; } = Brushes.DodgerBlue;
    public SpinnerRenderMode RenderMode { get; set; } = SpinnerRenderMode.AnimatePositions; // more versatile
    public SpinnerRenderShape RenderShape { get; set; } = SpinnerRenderShape.Wave;

    public Spinner()
    {
        InitializeComponent();
        Loaded += Spinner_Loaded;
        IsVisibleChanged += Spinner_IsVisibleChanged;
    }

    #region [Overrides]
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        hasAppliedTemplate = true;
        Debug.WriteLine($"[INFO] {nameof(Spinner)} template has been applied.");
    }

    protected override Size MeasureOverride(Size constraint)
    {
        // The width/height is used in render object calculations, so we must have some value.
        if (constraint.Width.IsInvalidOrZero()) { Width = 50; }
        if (constraint.Height.IsInvalidOrZero()) { Height = 50; }

        Debug.WriteLine($"[INFO] {nameof(Spinner)} is measured to be {constraint}");
        return base.MeasureOverride(constraint);
    }
    #endregion

    #region [Events]
    void Spinner_Loaded(object sender, RoutedEventArgs e)
    {
        if (RenderShape == SpinnerRenderShape.Dots || RenderShape == SpinnerRenderShape.Wave || RenderShape == SpinnerRenderShape.Worm)
            CreateDots();
        else if (RenderShape == SpinnerRenderShape.Polys)
            CreatePolys();
        else if (RenderShape == SpinnerRenderShape.Snow || RenderShape == SpinnerRenderShape.Wind || RenderShape == SpinnerRenderShape.Space)
            CreateSnow();
        else if (RenderShape == SpinnerRenderShape.Line)
            CreateLines();
        else if (RenderShape == SpinnerRenderShape.Stripe)
            CreateStripe();
        else if (RenderShape == SpinnerRenderShape.Bounce)
            CreateBounce();
        else if (RenderShape == SpinnerRenderShape.Spiral || RenderShape == SpinnerRenderShape.Pulse || RenderShape == SpinnerRenderShape.Rings)
            CreateSpiral();
        else if (RenderShape == SpinnerRenderShape.Square)
            CreateSquare();
        else if (RenderShape == SpinnerRenderShape.Twinkle1)
            CreateTwinkle1();
        else if (RenderShape == SpinnerRenderShape.Twinkle2)
            CreateTwinkle2();
        else if (RenderShape == SpinnerRenderShape.Meteor1)
            CreateMeteors1();
        else if (RenderShape == SpinnerRenderShape.Meteor2)
            CreateMeteors2();
        else if (RenderShape == SpinnerRenderShape.Falling)
            CreateFalling();
        else if (RenderShape == SpinnerRenderShape.Explode)
            CreateExplosion();
        else if (RenderShape == SpinnerRenderShape.Fountain)
            CreateFountain();
        else
            CreateDots();

        if (IsVisible)
        {
            if (RenderMode == SpinnerRenderMode.RotateCanvas)
                StartAnimationStandard();
            else
                StartAnimationCompositionTarget();
        }
    }

    void Spinner_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        RunFade(IsVisible);
        if (IsVisible)
        {
            if (RenderMode == SpinnerRenderMode.RotateCanvas)
                StartAnimationStandard();
            else
                StartAnimationCompositionTarget();
        }
        else
        {
            if (RenderMode == SpinnerRenderMode.RotateCanvas)
                StopAnimationStandard();
            else
                StopAnimationCompositionTarget();
        }
    }
    #endregion

    /// <summary>
    /// Starts the <see cref="DoubleAnimation"/> for <see cref="RotateTransform.AngleProperty"/> for the <see cref="Canvas"/>.
    /// </summary>
    void StartAnimationStandard()
    {
        // Always rebuild the animation fresh
        RotateTransform rotate = new RotateTransform();
        PART_Canvas.RenderTransform = rotate;
        PART_Canvas.RenderTransformOrigin = new Point(0.5, 0.5);
        DoubleAnimation anim = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(WaveDuration), // RotationDuration
            RepeatBehavior = RepeatBehavior.Forever
        };
        rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
    }

    /// <summary>
    /// Stops the <see cref="DoubleAnimation"/> for <see cref="RotateTransform.AngleProperty"/> for the <see cref="Canvas"/>.
    /// </summary>
    void StopAnimationStandard()
    {
        PART_Canvas.RenderTransform?.BeginAnimation(RotateTransform.AngleProperty, null);
    }

    /// <summary>
    /// Starts the animation rendering process for the current composition target based on the specified render shape.
    /// </summary>
    /// <remarks>
    /// This method hooks the appropriate rendering event handler to the <see cref="CompositionTarget.Rendering"/>
    /// event based on the value of the <see cref="SpinnerRenderShape"/>. Each render shape corresponds to a 
    /// specific animation style.
    /// </remarks>
    void StartAnimationCompositionTarget()
    {
        if (_renderHooked) { return; }
        _renderHooked = true;
        if (RenderShape == SpinnerRenderShape.Wave)
            CompositionTarget.Rendering += OnSineWaveRendering;
        else if (RenderShape == SpinnerRenderShape.Snow)
            CompositionTarget.Rendering += OnSnowRendering;
        else if (RenderShape == SpinnerRenderShape.Wind)
            CompositionTarget.Rendering += OnWindRendering;
        else if (RenderShape == SpinnerRenderShape.Space)
            CompositionTarget.Rendering += OnStarfieldRendering;
        else if (RenderShape == SpinnerRenderShape.Line)
            CompositionTarget.Rendering += OnLineRendering;
        else if (RenderShape == SpinnerRenderShape.Stripe)
            CompositionTarget.Rendering += OnStripeRendering;
        else if (RenderShape == SpinnerRenderShape.Bounce)
            CompositionTarget.Rendering += OnBounceRendering;
        else if (RenderShape == SpinnerRenderShape.Worm)
            CompositionTarget.Rendering += OnWormRendering;
        else if (RenderShape == SpinnerRenderShape.Spiral)
            CompositionTarget.Rendering += OnSpiralRendering;
        else if (RenderShape == SpinnerRenderShape.Square)
            CompositionTarget.Rendering += OnSquareRendering;
        else if (RenderShape == SpinnerRenderShape.Rings)
            CompositionTarget.Rendering += OnRingsRendering;
        else if (RenderShape == SpinnerRenderShape.Pulse)
            CompositionTarget.Rendering += OnPulseRendering;
        else if (RenderShape == SpinnerRenderShape.Twinkle1)
            CompositionTarget.Rendering += OnTwinkleRendering1;
        else if (RenderShape == SpinnerRenderShape.Twinkle2)
            CompositionTarget.Rendering += OnTwinkleRendering2;
        else if (RenderShape == SpinnerRenderShape.Meteor1)
            CompositionTarget.Rendering += OnMeteorRendering1;
        else if (RenderShape == SpinnerRenderShape.Meteor2)
            CompositionTarget.Rendering += OnMeteorRendering2;
        else if (RenderShape == SpinnerRenderShape.Falling)
            CompositionTarget.Rendering += OnFallingRendering;
        else if (RenderShape == SpinnerRenderShape.Explode)
            CompositionTarget.Rendering += OnExplosionRendering;
        else if (RenderShape == SpinnerRenderShape.Fountain)
            CompositionTarget.Rendering += OnFountainRendering;
        else // default is basic spinner circle
            CompositionTarget.Rendering += OnCircleRendering;
    }

    /// <summary>
    /// Stops the animation rendering process for the current composition target based on the specified render shape.
    /// </summary>
    void StopAnimationCompositionTarget()
    {
        if (!_renderHooked) { return; }
        _renderHooked = false;
        if (RenderShape == SpinnerRenderShape.Wave)
            CompositionTarget.Rendering -= OnSineWaveRendering;
        else if (RenderShape == SpinnerRenderShape.Snow)
            CompositionTarget.Rendering -= OnSnowRendering;
        else if (RenderShape == SpinnerRenderShape.Wind)
            CompositionTarget.Rendering -= OnWindRendering;
        else if (RenderShape == SpinnerRenderShape.Space)
            CompositionTarget.Rendering -= OnStarfieldRendering;
        else if (RenderShape == SpinnerRenderShape.Line)
            CompositionTarget.Rendering -= OnLineRendering;
        else if (RenderShape == SpinnerRenderShape.Stripe)
            CompositionTarget.Rendering -= OnStripeRendering;
        else if (RenderShape == SpinnerRenderShape.Bounce)
            CompositionTarget.Rendering -= OnBounceRendering;
        else if (RenderShape == SpinnerRenderShape.Worm)
            CompositionTarget.Rendering -= OnWormRendering;
        else if (RenderShape == SpinnerRenderShape.Spiral)
            CompositionTarget.Rendering -= OnSpiralRendering;
        else if (RenderShape == SpinnerRenderShape.Square)
            CompositionTarget.Rendering -= OnSquareRendering;
        else if (RenderShape == SpinnerRenderShape.Rings)
            CompositionTarget.Rendering -= OnRingsRendering;
        else if (RenderShape == SpinnerRenderShape.Pulse)
            CompositionTarget.Rendering -= OnPulseRendering;
        else if (RenderShape == SpinnerRenderShape.Twinkle1)
            CompositionTarget.Rendering -= OnTwinkleRendering1;
        else if (RenderShape == SpinnerRenderShape.Twinkle2)
            CompositionTarget.Rendering -= OnTwinkleRendering2;
        else if (RenderShape == SpinnerRenderShape.Meteor1)
            CompositionTarget.Rendering -= OnMeteorRendering1;
        else if (RenderShape == SpinnerRenderShape.Meteor2)
            CompositionTarget.Rendering -= OnMeteorRendering2;
        else if (RenderShape == SpinnerRenderShape.Falling)
            CompositionTarget.Rendering -= OnFallingRendering;
        else if (RenderShape == SpinnerRenderShape.Explode)
            CompositionTarget.Rendering -= OnExplosionRendering;
        else if (RenderShape == SpinnerRenderShape.Fountain)
            CompositionTarget.Rendering -= OnFountainRendering;
        else
            CompositionTarget.Rendering -= OnCircleRendering;
    }

    /// <summary>
    /// If <paramref name="fadeIn"/> is <c>true</c>, the <see cref="UserControl"/> will be animated to 1 opacity.<br/>
    /// If <paramref name="fadeIn"/> is <c>false</c>, the <see cref="UserControl"/> will be animated to 0 opacity.<br/>
    /// </summary>
    /// <remarks>animation will run for 250 milliseconds</remarks>
    void RunFade(bool fadeIn)
    {
        var anim = new DoubleAnimation
        {
            To = fadeIn ? 1.0 : 0.0,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        this.BeginAnimation(OpacityProperty, anim);
    }

    #region [Composition Rendering]

    void CreateDots(bool pulse = false)
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();
        double radius = Math.Min(ActualWidth, ActualHeight) / 2 - DotSize;

        // Fetch a brush from the local UserControl
        //Brush? brsh = (Brush)FindResource("DotBrush");

        for (int i = 0; i < DotCount; i++)
        {
            double angle = i * 360.0 / DotCount;
            double rad = angle * Math.PI / 180;
            double x = radius * Math.Cos(rad) + ActualWidth / 2 - DotSize / 2;
            double y = radius * Math.Sin(rad) + ActualHeight / 2 - DotSize / 2;
            //Rectangle dot = new Rectangle { Width = DotSize, Height = DotSize, RadiusX = 2, RadiusY = 2, Fill = DotBrush, Opacity = (double)i / DotCount };
            Ellipse dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = (double)i / (double)DotCount + 0.01 // fade each consecutive dot
            };

            if (pulse)
            {   // Pulsing effect
                dot.RenderTransform = new RotateTransform(angle + 90, DotSize / 3, DotSize / 3);
            }

            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
            PART_Canvas.Children.Add(dot);
        }
    }

    /// <summary>
    /// Create path geometry instead of a standard <see cref="Ellipse"/>.
    /// </summary>
    /// <param name="pointOutward"></param>
    void CreatePolys(bool pointOutward = true)
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();
        double radius = Math.Min(ActualWidth, ActualHeight) / 2 - DotSize;

        for (int i = 0; i < DotCount; i++)
        {
            double angle = i * 360.0 / DotCount;
            double rad = angle * Math.PI / 180;

            double x = radius * Math.Cos(rad) + ActualWidth / 2 - DotSize / 2;
            double y = radius * Math.Sin(rad) + ActualHeight / 2 - DotSize / 2;

            var triangle = Geometry.Parse("M 0,0 L 6,0 3,6 Z");
            var equilateral = Geometry.Parse("M 0,1 L 0.5,0 1,1 Z");
            var diamond = Geometry.Parse("M 0.5,0 L 1,0.5 0.5,1 0,0.5 Z");
            var star = Geometry.Parse("M 0.5,0 L 0.61,0.35 1,0.35 0.68,0.57 0.81,0.91 0.5,0.7 0.19,0.91 0.32,0.57 0,0.35 0.39,0.35 Z");
            var circle = Geometry.Parse("M 0,0.5 A 0.5,0.5 0 1 0 1,0.5 A 0.5,0.5 0 1 0 0,0.5");
            var tick = Geometry.Parse("M 0,0 L 0,1");
            var chevronRight = Geometry.Parse("M 0,0 L 0.6,0.5 L 0,1 L 0.2,1 L 0.8,0.5 L 0.2,0 Z");
            var chevronLeft = Geometry.Parse("M 1,0 L 0.4,0.5 L 1,1 L 0.8,1 L 0.2,0.5 L 0.8,0 Z");
            var chevronUp = Geometry.Parse("M 0,1 L 0.5,0.4 L 1,1 L 1,0.8 L 0.5,0.2 L 0,0.8 Z");
            var chevronDown = Geometry.Parse("M 0,0 L 0.5,0.6 L 1,0 L 1,0.2 L 0.5,0.8 L 0,0.2 Z");
            var path = new System.Windows.Shapes.Path
            {
                Data = triangle,
                Fill = DotBrush,
                Width = DotSize,
                Stroke = DotBrush, // new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                Height = DotSize,
                Stretch = Stretch.Uniform,
                Opacity = (double)i / (double)DotCount + 0.01 // fade each consecutive shape
            };

            if (pointOutward)
            {   // Keep the shape’s orientation consistent around the circle
                path.RenderTransform = new RotateTransform(angle + 90, DotSize / 2, DotSize / 2);
            }

            Canvas.SetLeft(path, x);
            Canvas.SetTop(path, y);
            PART_Canvas.Children.Add(path);
        }
    }

    /// <summary>
    /// Keep each dot’s gradient fixed by moving the dots around the circle every frame. 
    /// This avoids rotating any gradients which can cause a wobble effect.
    /// </summary>
    void OnCircleRendering(object? sender, EventArgs e)
    {
        // 360 degrees per RotationDuration seconds
        double degPerSec = 360.0 / WaveDuration;

        // Use a steady clock
        _angle = (_angle + degPerSec * GetDeltaSeconds()) % 360.0;

        double radius = Math.Min(ActualWidth, ActualHeight) / 2 - DotSize;
        int count = PART_Canvas.Children.Count;

        for (int i = 0; i < count; i++)
        {
            double baseAngle = i * 360.0 / count;
            double a = (baseAngle + _angle) * Math.PI / 180.0;

            double x = radius * Math.Cos(a) + ActualWidth / 2 - DotSize / 2;
            double y = radius * Math.Sin(a) + ActualHeight / 2 - DotSize / 2;

            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
        }
    }


    public double WaveDuration { get; set; } = 1.0; // seconds (A.K.A. Rotation Duration)
    public double WaveAmplitude { get; set; } = 14;     // pixels
    public double WaveFrequency { get; set; } = 1;      // cycles across width (shouldn't be less than 1)

    void OnSineWaveRendering(object? sender, EventArgs e)
    {
        double speed = ActualWidth / WaveDuration; // px/sec
        double delta = speed * GetDeltaSeconds();

        // Move the phase offset over time
        _angle = (_angle + delta) % ActualWidth;

        // Reverse direction
        //_angle = (_angle - delta) % ActualWidth;

        int count = PART_Canvas.Children.Count;
        double spacing = ActualWidth / count;

        for (int i = 0; i < count; i++)
        {
            double x = i * spacing;
            double phase = (x + _angle) / ActualWidth * WaveFrequency * Tau;
            double y = (ActualHeight - DotSize) / 2 + Math.Sin(phase) * WaveAmplitude;
            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
        }
    }


    Ellipse[] _dots;
    void CreateSpiral()
    {
        if (PART_Canvas == null)
            return;

        if (SpiralArmCount <= 0)
            SpiralArmCount = 1;

        PART_Canvas.Children.Clear();

        _dots = new Ellipse[DotCount];

        for (int i = 0; i < DotCount; i++)
        {
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = (double)i / (double)DotCount + 0.01 // fade each consecutive dot
            };
            _dots[i] = dot;
            PART_Canvas.Children.Add(dot);
        }
    }


    public int SpiralArmCount { get; set; } = 1;
    public bool SpiralLowOpacity { get; set; } = false;
    public bool SpiralFadeOut { get; set; } = true;
    public bool SpiralClockwise { get; set; } = false;
    public double SpiralDotSpacing { get; set; } = 5;
    public double SpiralTwistDensity { get; set; } = 0.3;
    public double SpiralRotationAngle { get; set; } = 0.06;  // radians per frame

    void OnSpiralRendering(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateSpiral();
            return;
        }

        int count = PART_Canvas.Children.Count;
        double cx = ActualWidth * 0.5;
        double cy = ActualHeight * 0.5;

        // Increment rotation angle
        if (SpiralClockwise)
            _angle += SpiralRotationAngle;
        else
            _angle -= SpiralRotationAngle;

        if (_angle > Tau)
            _angle -= Tau;
        if (_angle < 0)
            _angle += Tau;

        // Equal angular offset per arm
        double armSlice = Tau / SpiralArmCount;

        for (int i = 0; i < count; i++)
        {
            // Assign dot to an arm and its index along that arm
            int armIndex = i % SpiralArmCount;

            // Position along the arm (0,1,2,etc)
            int k = i / SpiralArmCount;

            // Spiral along the arm with per-arm offset
            double radius = k * SpiralDotSpacing;
            double theta = k * SpiralTwistDensity + _angle + armIndex * armSlice;

            double x = cx + radius * Math.Cos(theta);
            double y = cy + radius * Math.Sin(theta);

            var dot = _dots[i];

            if (SpiralLowOpacity)
            {
                dot.Opacity = RandomLowOpacity();
            }
            else
            {
                if (SpiralFadeOut)
                {
                    //dot.Opacity = Math.Min(1.0, ((double)count - (double)i) * 0.1d); // fade from outside inward
                    dot.Opacity = GetOpacityEaseInOut(i, count); // fade from outside inward
                }
                else
                {
                    //dot.Opacity = ((double)i / count) + 0.01; // fade from inside outward
                    dot.Opacity = GetOpacityEaseInOut(count - i, count); // fade from outside inward
                }
            }

            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);
        }
    }

    void OnSingleSpiralRendering(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateSpiral();
            return;
        }

        int count = PART_Canvas.Children.Count;

        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        // Increment rotation angle
        if (SpiralClockwise)
            _angle += SpiralRotationAngle;
        else
            _angle -= SpiralRotationAngle;

        if (_angle > Tau)
            _angle -= Tau;
        if (_angle < 0)
            _angle += Tau;

        // Spiral parameters
        double spacing = SpiralDotSpacing; // radial spacing between dots
        double twist = SpiralTwistDensity; // how tightly the spiral winds

        for (int i = 0; i < count; i++)
        {
            double radius = i * spacing;
            double theta = i * twist + _angle;

            double x = centerX + radius * Math.Cos(theta);
            double y = centerY + radius * Math.Sin(theta);

            var dot = (UIElement)PART_Canvas.Children[i];

            if (SpiralFadeOut)
                dot.Opacity = Math.Min(1.0, ((double)count - (double)i) * 0.1d); // fade each consecutive
            else
                dot.Opacity = ((double)i / count) + 0.01; // fade each consecutive

            Canvas.SetLeft(_dots[i], x - _dots[i].Width / 2);
            Canvas.SetTop(_dots[i], y - _dots[i].Height / 2);
        }
    }

    public double SpiralGrowthRate { get; set; } = 8;     // pixels/sec
    public double SpiralMaxRadius { get; set; } = 40;     // px
    public double SpiralAngularSpeed { get; set; } = 90;  // deg/sec
    public double SpiralInOutSpeed { get; set; } = 0.75;  // cycles/sec

    void OnSpiralRenderingOld(object? sender, EventArgs e)
    {
        double dt = GetDeltaSeconds();
        _angle = (_angle + SpiralAngularSpeed * dt) % 360.0;

        int count = PART_Canvas.Children.Count;
        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < count; i++)
        {
            // Each dot’s time offset
            double t = i * 0.1 + _angle / SpiralAngularSpeed;

            // Spiral radius grows over time
            double radius = SpiralGrowthRate * t; // * 0.5;

            // Spiral angle
            double a = (SpiralAngularSpeed * t) * Math.PI / 180.0;
            double x = centerX + radius * Math.Cos(a) - DotSize / 2;
            double y = centerY + radius * Math.Sin(a) - DotSize / 2;

            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
        }
    }


    double _radiusPhase = 0.0; // Worm spiral in/out phase tracking
    public double WormAngularSpeed { get; set; } = 90;   // deg/sec
    public double WormInOutSpeed { get; set; } = 0.75;   // cycles/sec
    public double WormMaxRadius { get; set; } = 40;      // px

    void OnWormRendering(object? sender, EventArgs e)
    {
        double dt = GetDeltaSeconds();

        // Angle for rotation
        _angle = (_angle + WormAngularSpeed * dt) % 360.0;

        // Phase for radius oscillation
        double phase = _radiusPhase + WormInOutSpeed * Tau * dt;
        _radiusPhase = phase;

        int count = PART_Canvas.Children.Count;
        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < count; i++)
        {
            // Offset phase per dot for staggered spiral arms
            double dotPhase = phase + i * (Math.PI / count);

            // Oscillating radius
            double radius = (WormMaxRadius / 2) * (1 + Math.Sin(dotPhase));

            // Dot angle offset
            double a = (_angle + i * (360.0 / count)) * Math.PI / 180.0;

            double x = centerX + radius * Math.Cos(a) - DotSize / 2;
            double y = centerY + radius * Math.Sin(a) - DotSize / 2;

            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
        }
    }


    // Drift effect for snow/rain
    public double WindAmplitude { get; set; } = 5;   // max horizontal sway in px
    public double WindFrequency { get; set; } = 0.6; // cycles/sec
    public double WindBias { get; set; } = 2;        // constant drift px/sec

    public bool SnowSizeRandom { get; set; } = true;
    public double SnowBaseSpeed { get; set; } = 50;
    public bool SnowLowOpacity { get; set; } = false; // for subtle backgrounds

    double[] _rainX;
    double[] _rainY;
    double[] _rainSpeed;
    double[] _rainPhase; // for wind sway offset
    double[] _rainSize;
    bool _fixedSnowSize = false;
    /// <summary>
    /// Creates an array of dots to apply wind/gravity pressure on.
    /// </summary>
    void CreateSnow()
    {
        if (PART_Canvas == null)
            return;

        _rainX = new double[DotCount];
        _rainY = new double[DotCount];
        _rainSpeed = new double[DotCount];
        _rainPhase = new double[DotCount];
        _rainSize = new double[DotCount];

        PART_Canvas.Children.Clear();

        for (int i = 0; i < DotCount; i++)
        {
            _rainX[i] = Random.Shared.NextDouble() * (ActualWidth - DotSize);
            _rainY[i] = Random.Shared.NextDouble() * ActualHeight;            // start at random vertical position
            _rainSpeed[i] = SnowBaseSpeed + Random.Shared.NextDouble() * 50;  // px/sec
            _rainPhase[i] = Random.Shared.NextDouble() * Tau;                 // random sway/drift start
            _rainSize[i] = SnowSizeRandom ? 1 + Random.Shared.NextDouble() * DotSize : DotSize;

            var dot = new Ellipse
            {
                Width = _rainSize[i],
                Height = _rainSize[i],
                Fill = DotBrush,
                Opacity = SnowLowOpacity ? RandomLowOpacity() : Random.Shared.NextDouble() + 0.09,
                //Opacity = (double)i / DotCount, // ⇦ use this to fade each consecutive dot
            };

            Canvas.SetLeft(dot, _rainX[i]);
            Canvas.SetTop(dot, _rainY[i]);
            PART_Canvas.Children.Add(dot);
        }
    }

    void OnSnowRendering(object? sender, EventArgs e)
    {
        if (_rainX == null || _rainY == null)
        {
            CreateSnow();
            return;
        }

        double dt = GetDeltaSeconds();

        for (int i = 0; i < DotCount; i++)
        {
            // move down
            _rainY[i] += _rainSpeed[i] * dt;

            if (_rainY[i] > ActualHeight)
            {
                // Re-spawn at top
                _rainY[i] = -DotSize;
                _rainX[i] = Random.Shared.NextDouble() * (ActualWidth - DotSize);
                _rainSpeed[i] = SnowBaseSpeed + Random.Shared.NextDouble() * 50;
                _rainPhase[i] = Random.Shared.NextDouble() * Tau;
            }

            // Advance sway phase
            _rainPhase[i] += WindFrequency * Tau * dt;

            // Horizontal sway + bias
            double sway = Math.Sin(_rainPhase[i]) * WindAmplitude;
            double x = _rainX[i] + sway + WindBias * (_rainY[i] / ActualHeight);

            //var dot = (Ellipse)PART_Canvas.Children[i]; // assumes the Canvas contains Ellipse elements
            var dot = (UIElement)PART_Canvas.Children[i];

            //Canvas.SetLeft(dot, _rainX[i]); // ⇦ use this if you want no sway/drift
            Canvas.SetLeft(dot, x); // place the sway/drift + bias
            Canvas.SetTop(dot, _rainY[i]);
        }
    }

    void OnWindRendering(object? sender, EventArgs e)
    {
        if (_rainX == null || _rainY == null)
        {
            CreateSnow();
            return;
        }

        double dt = GetDeltaSeconds();

        for (int i = 0; i < DotCount; i++)
        {
            // move right
            _rainX[i] += _rainSpeed[i] * dt;

            if (_rainX[i] > ActualWidth)
            {
                // Re-spawn at side
                _rainY[i] = Random.Shared.NextDouble() * (ActualHeight - DotSize);
                _rainX[i] = -DotSize;
                _rainSpeed[i] = SnowBaseSpeed + Random.Shared.NextDouble() * 50;
                _rainPhase[i] = Random.Shared.NextDouble() * Tau;

            }

            // Advance sway/drift phase
            _rainPhase[i] += WindFrequency * Tau * dt;

            // Horizontal sway/drift + bias
            double sway = Math.Sin(_rainPhase[i]) * WindAmplitude;
            double x = _rainY[i] + sway + WindBias * (_rainX[i] / ActualWidth);

            //var dot = (Ellipse)PART_Canvas.Children[i]; // assumes the Canvas contains Ellipse elements
            var dot = (UIElement)PART_Canvas.Children[i];

            Canvas.SetLeft(dot, _rainX[i]);
            //Canvas.SetTop(dot, _rainY[i]); // ⇦ use this if you want no sway/drift
            Canvas.SetTop(dot, x); // place the sway/drift + bias
        }
    }

    void OnStarfieldRendering(object? sender, EventArgs e)
    {
        if (_rainX == null || _rainY == null)
        {
            CreateSnow();
            return;
        }

        double dt = GetDeltaSeconds();
        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < DotCount; i++)
        {
            var dot = (Ellipse)PART_Canvas.Children[i]; // assumes the Canvas contains Ellipse elements

            // Direction vector from center
            double dx = _rainX[i] - centerX;
            double dy = _rainY[i] - centerY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // Normalize direction
            if (dist == 0)
                dist = 0.0001;
            dx /= dist;
            dy /= dist;

            // Move outward
            _rainX[i] += dx * _rainSpeed[i] * dt;
            _rainY[i] += dy * _rainSpeed[i] * dt;

            // Scale size based on distance
            double scale = 1 + dist / (ActualWidth / 2);
            dot.Width = _rainSize[i] * scale;
            dot.Height = _rainSize[i] * scale;

            // Opacity increases with distance
            dot.Opacity = Math.Min(1.0, 0.4 + dist / (ActualWidth / 2));

            Canvas.SetLeft(dot, _rainX[i] - dot.Width / 2);
            Canvas.SetTop(dot, _rainY[i] - dot.Height / 2);

            // Re-spawn immediately when out of bounds
            if (_rainX[i] < -DotSize || _rainX[i] > ActualWidth + DotSize ||
                _rainY[i] < -DotSize || _rainY[i] > ActualHeight + DotSize)
            {
                double angle = Random.Shared.NextDouble() * Tau;
                _rainX[i] = centerX + Math.Cos(angle) * 2; // small offset so they don't overlap exactly
                _rainY[i] = centerY + Math.Sin(angle) * 2;
                _rainSpeed[i] = SnowBaseSpeed + Random.Shared.NextDouble() * 100;
                _rainSize[i] = 1 + Random.Shared.NextDouble() * DotSize;
            }
        }
    }


    double[] _starX;
    double[] _starY;
    double[] _starSpeed;
    double[] _starSize;
    double[] _starDirX;
    double[] _starDirY;
    /// <summary>
    /// Creates an array of dots to apply wind/gravity pressure on.
    /// </summary>
    void CreateLines()
    {
        if (PART_Canvas == null)
            return;

        _starX = new double[DotCount];
        _starY = new double[DotCount];
        _starSpeed = new double[DotCount];
        _starSize = new double[DotCount];
        _starDirX = new double[DotCount];
        _starDirY = new double[DotCount];

        PART_Canvas.Children.Clear();

        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < DotCount; i++)
        {
            double angle = Random.Shared.NextDouble() * Tau;
            _starX[i] = centerX;
            _starY[i] = centerY;
            _starDirX[i] = Math.Cos(angle);
            _starDirY[i] = Math.Sin(angle);
            _starSpeed[i] = LineBaseSpeed + Random.Shared.NextDouble() * 50;
            _starSize[i] = 1 + Random.Shared.NextDouble() * DotSize;

            var streak = new Line
            {
                Stroke = DotBrush,
                //Fill = new SolidColorBrush(Colors.SpringGreen),
                StrokeThickness = _starSize[i] / 4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = (double)i / DotCount // opacity will be changed later during render
            };

            PART_Canvas.Children.Add(streak);
        }
    }

    public double LineBaseSpeed { get; set; } = 100;
    public bool LineLowOpacity { get; set; } = false; // for subtle backgrounds
    void OnLineRendering(object? sender, EventArgs e)
    {
        if (_starX == null || _starY == null) { return; }

        double dt = GetDeltaSeconds();
        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < DotCount; i++)
        {
            var streak = (Line)PART_Canvas.Children[i];

            // Move outward
            _starX[i] += _starDirX[i] * _starSpeed[i] * dt;
            _starY[i] += _starDirY[i] * _starSpeed[i] * dt;

            // Distance from center
            double dist = Math.Sqrt(Math.Pow(_starX[i] - centerX, 2) + Math.Pow(_starY[i] - centerY, 2));

            // Streak length scales with distance
            double length = dist * 0.2; // tweak multiplier for effect

            // End point is current position
            streak.X2 = _starX[i];
            streak.Y2 = _starY[i];

            // Start point is behind along velocity vector
            streak.X1 = _starX[i] - _starDirX[i] * length;
            streak.Y1 = _starY[i] - _starDirY[i] * length;

            // Opacity increases with distance
            streak.Opacity = Math.Min(LineLowOpacity ? 0.2 : 0.9, 0.01 + dist / (ActualWidth / 2));

            // Re-spawn when out of bounds
            if (_starX[i] < -DotSize || _starX[i] > ActualWidth + DotSize ||
                _starY[i] < -DotSize || _starY[i] > ActualHeight + DotSize)
            {
                double angle = Random.Shared.NextDouble() * Tau;
                _starX[i] = centerX;
                _starY[i] = centerY;
                _starDirX[i] = Math.Cos(angle);
                _starDirY[i] = Math.Sin(angle);
                _starSpeed[i] = LineBaseSpeed + Random.Shared.NextDouble() * 50;
                _starSize[i] = 1 + Random.Shared.NextDouble() * DotSize;
                streak.StrokeThickness = _starSize[i] / 4;
            }
        }
    }

    /// <summary>
    /// Creates an array of lines to apply horizontal movement on.
    /// </summary>
    void CreateStripe()
    {
        if (PART_Canvas == null)
            return;

        _starX = new double[DotCount];
        _starY = new double[DotCount];
        _starSpeed = new double[DotCount];
        _starSize = new double[DotCount];
        _starDirX = new double[DotCount];
        _starDirY = new double[DotCount];

        PART_Canvas.Children.Clear();

        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < DotCount; i++)
        {
            double angle = Random.Shared.NextDouble() * Tau;
            _starX[i] = Random.Shared.NextDouble() * (ActualWidth - DotSize);
            _starY[i] = Random.Shared.NextDouble() * (ActualHeight - DotSize);  // start at random vertical position

            _starDirX[i] = Math.Cos(angle); // not needed
            _starDirY[i] = Math.Sin(angle); // not needed
            _starSpeed[i] = StripeBaseSpeed + Random.Shared.NextDouble() * 100;
            _starSize[i] = 1 + Random.Shared.NextDouble() * DotSize;

            var streak = new Line
            {
                Stroke = DotBrush,
                X1 = _starX[i] * -0.05, // start outside left-most
                X2 = _starX[i] * 0.05,  // end outside right-most
                Y1 = _starY[i] / (DotSize),
                Y2 = _starY[i] / (DotSize),
                StrokeThickness = _starSize[i] / 2.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = StripeLowOpacity ? RandomLowOpacity() : Random.Shared.NextDouble() + 0.09,
                //Fill = new SolidColorBrush(Colors.SpringGreen),
            };

            PART_Canvas.Children.Add(streak);
        }
    }

    public double StripeBaseSpeed { get; set; } = 50;
    public bool StripeLowOpacity { get; set; } = false; // for subtle backgrounds

    void OnStripeRendering(object? sender, EventArgs e)
    {
        if (_starX == null || _starY == null)
        {
            CreateStripe();
            return;
        }

        double dt = GetDeltaSeconds();

        for (int i = 0; i < DotCount; i++)
        {
            // move from left to right
            _starX[i] += _starSpeed[i] * dt;

            if (_starX[i] > (ActualWidth + 2))
            {
                // Re-spawn at side
                _starY[i] = Random.Shared.NextDouble() * (ActualHeight - DotSize);
                _starX[i] = -DotSize;
                _starSpeed[i] = StripeBaseSpeed + Random.Shared.NextDouble() * 100;
            }

            var line = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(line, _starX[i]);
            Canvas.SetTop(line, _starY[i]);
        }
    }

    public bool BounceLowOpacity { get; set; } = false; // for subtle backgrounds

    double[] _dotX;
    double[] _dotY;
    double[] _dotVX;
    double[] _dotVY;
    double[] _dotSize;
    /// <summary>
    /// Creates an array of dots to apply wind/gravity pressure on.
    /// </summary>
    void CreateBounce()
    {
        if (PART_Canvas == null)
            return;

        _dotX = new double[DotCount];
        _dotY = new double[DotCount];
        _dotVX = new double[DotCount];
        _dotVY = new double[DotCount];
        _dotSize = new double[DotCount];

        PART_Canvas.Children.Clear();

        for (int i = 0; i < DotCount; i++)
        {
            _dotX[i] = Random.Shared.NextDouble() * (ActualWidth - DotSize);
            _dotY[i] = Random.Shared.NextDouble() * (ActualHeight - DotSize);
            if (BounceSizeRandom)
                _dotSize[i] = 2 + Random.Shared.NextDouble() * DotSize;
            else
                _dotSize[i] = DotSize;

            // Random velocity between -BounceSpeed and +BounceSpeed in px/sec
            _dotVX[i] = RandomSwing(BounceSpeed); // (Random.Shared.NextDouble() * 200 - 100);
            _dotVY[i] = RandomSwing(BounceSpeed); // (Random.Shared.NextDouble() * 200 - 100);

            var dot = new Ellipse
            {
                Width = _dotSize[i],
                Height = _dotSize[i],
                Fill = DotBrush,
                Opacity = BounceLowOpacity ? RandomLowOpacity() : Random.Shared.NextDouble() + 0.09,
            };

            Canvas.SetLeft(dot, _dotX[i]);
            Canvas.SetTop(dot, _dotY[i]);
            PART_Canvas.Children.Add(dot);
        }
    }

    public bool BounceSizeRandom { get; set; } = false;
    public bool BounceCollisions { get; set; } = true;
    public double BounceSpeed { get; set; } = 80;
    void OnBounceRendering(object? sender, EventArgs e)
    {
        if (_dotX == null || _dotY == null)
        {
            CreateBounce();
            return;
        }

        // Restitution coefficient (0 to 1) makes collisions less bouncy.
        // Any values less than 1 will slowly absorb energy from the system
        // events over time, so the dots will eventually just slowly drift.
        double restitution = 1.0;

        double dt = GetDeltaSeconds();

        // Move dots
        for (int i = 0; i < DotCount; i++)
        {
            _dotX[i] += _dotVX[i] * dt;
            _dotY[i] += _dotVY[i] * dt;

            // Bounce off left/right walls
            if (_dotX[i] <= 0)
            {
                _dotX[i] = 0;
                _dotVX[i] = Math.Abs(_dotVX[i]) * restitution; // force rightward and apply restitution/friction
            }
            else if (_dotX[i] >= ActualWidth - DotSize)
            {
                _dotX[i] = ActualWidth - DotSize;
                _dotVX[i] = -Math.Abs(_dotVX[i]) * restitution; // force leftward and apply restitution/friction
            }

            // Bounce off top/bottom walls
            if (_dotY[i] <= 0)
            {
                _dotY[i] = 0;
                _dotVY[i] = Math.Abs(_dotVY[i]) * restitution; // force downward and apply restitution/friction
            }
            else if (_dotY[i] >= ActualHeight - DotSize)
            {
                _dotY[i] = ActualHeight - DotSize;
                _dotVY[i] = -Math.Abs(_dotVY[i]) * restitution; // force upward and apply restitution/friction
            }
        }

        // Handle collisions between dots
        if (BounceCollisions)
        {
            // If the time between frames is large, relative to the dot's speed, two dots
            // can "tunnel" through each other, they overlap deeply before we detect the
            // collision, or even skip past each other entirely. This can cause sticking,
            // jitter, or unnatural pushes. If sub-stepping is preferred then instead of
            // doing one big update per frame, we could break the frame's dt into smaller
            // slices and run multiple collision checks/updates.

            #region [Standard collision technique]
            for (int i = 0; i < DotCount; i++)
            {
                for (int j = i + 1; j < DotCount; j++)
                {
                    double dx = _dotX[j] - _dotX[i];
                    double dy = _dotY[j] - _dotY[i];
                    double distSq = dx * dx + dy * dy;
                    double minDist = DotSize;

                    if (distSq < minDist * minDist && distSq > Epsilon)
                    {
                        double dist = Math.Sqrt(distSq);

                        // Normal vector
                        double nx = dx / dist;
                        double ny = dy / dist;

                        // Tangent vector
                        double tx = -ny;
                        double ty = nx;

                        // Project velocities onto normal and tangent
                        double v1n = _dotVX[i] * nx + _dotVY[i] * ny;
                        double v1t = _dotVX[i] * tx + _dotVY[i] * ty;
                        double v2n = _dotVX[j] * nx + _dotVY[j] * ny;
                        double v2t = _dotVX[j] * tx + _dotVY[j] * ty;

                        // Swap normal components (equal mass, elastic)
                        double v1nAfter = v2n * restitution;
                        double v2nAfter = v1n * restitution;

                        // Recombine
                        _dotVX[i] = v1nAfter * nx + v1t * tx;
                        _dotVY[i] = v1nAfter * ny + v1t * ty;
                        _dotVX[j] = v2nAfter * nx + v2t * tx;
                        _dotVY[j] = v2nAfter * ny + v2t * ty;

                        // Minimum Translation Vector to separate them
                        double overlap = 0.5 * (minDist - dist);
                        _dotX[i] -= overlap * nx;
                        _dotY[i] -= overlap * ny;
                        _dotX[j] += overlap * nx;
                        _dotY[j] += overlap * ny;
                    }
                }
            }
            #endregion

            #region [Collision resolution with friction & restitution]
            /** This creates a "push each other out of the way" effect **/
            //double grip = 0.9; // tangential friction
            //for (int i = 0; i < DotCount; i++)
            //{
            //    for (int j = i + 1; j < DotCount; j++)
            //    {
            //        double dx = _dotX[j] - _dotX[i];
            //        double dy = _dotY[j] - _dotY[i];
            //        double minDist = DotSize;
            //        double distSq = dx * dx + dy * dy;
            //
            //        if (distSq < minDist * minDist && distSq > Epsilon)
            //        {
            //            double dist = Math.Sqrt(distSq);
            //
            //            // Normal and tangent
            //            double nx = dx / dist;
            //            double ny = dy / dist;
            //            double tx = -ny;
            //            double ty = nx;
            //
            //            // Overlap separation (MTV)
            //            double overlap = 0.5 * (minDist - dist);
            //            _dotX[i] -= overlap * nx;
            //            _dotY[i] -= overlap * ny;
            //            _dotX[j] += overlap * nx;
            //            _dotY[j] += overlap * ny;
            //
            //            // Project velocities
            //            double v1n = _dotVX[i] * nx + _dotVY[i] * ny;
            //            double v1t = _dotVX[i] * tx + _dotVY[i] * ty;
            //            double v2n = _dotVX[j] * nx + _dotVY[j] * ny;
            //            double v2t = _dotVX[j] * tx + _dotVY[j] * ty;
            //
            //            // Only resolve if approaching along the normal
            //            double relApproach = (v1n - v2n);
            //            if (relApproach < 0)
            //            {
            //                // Equal mass elastic exchange with restitution
            //                double v1nAfter = v2n * restitution;
            //                double v2nAfter = v1n * restitution;
            //
            //                // Apply tangential friction
            //                double v1tAfter = v1t * grip;
            //                double v2tAfter = v2t * grip;
            //
            //                // Recombine
            //                _dotVX[i] = v1nAfter * nx + v1tAfter * tx;
            //                _dotVY[i] = v1nAfter * ny + v1tAfter * ty;
            //                _dotVX[j] = v2nAfter * nx + v2tAfter * tx;
            //                _dotVY[j] = v2nAfter * ny + v2tAfter * ty;
            //            }
            //        }
            //    }
            //}
            #endregion
        }

        // Update visuals
        for (int i = 0; i < DotCount; i++)
        {
            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, _dotX[i]);
            Canvas.SetTop(dot, _dotY[i]);
        }
    }

    void CreateSquare()
    {
        if (PART_Canvas == null)
            return;

        if (SpiralArmCount <= 0)
            SpiralArmCount = 1;

        PART_Canvas.Children.Clear();

        // Create dots once
        _dots = new Ellipse[DotCount];
        for (int i = 0; i < DotCount; i++)
        {
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = (double)i / (double)DotCount + 0.01 // fade each consecutive dot
            };
            _dots[i] = dot;
            PART_Canvas.Children.Add(dot);
        }
    }

    public double SquareSize { get; set; } = 0; // fill available perimeter if zero
    public double SquareStep { get; set; } = 2;
    public bool SquareClockwise { get; set; } = true;
    void OnSquareRendering(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateSquare();
            return;
        }

        int count = PART_Canvas.Children.Count;

        double w = 0;
        double h = 0;

        // If SquareSize is zero, use the control's full available area
        if (SquareSize == 0)
        {
            w = ActualWidth;
            h = ActualHeight;
        }
        else
        {
            w = SquareSize;
            h = SquareSize;
        }

        double left = (ActualWidth - w) / 2;
        double top = (ActualHeight - h) / 2;

        // Leave if the control lacks any area
        if (w <= 0 || h <= 0)
            return;

        double cx = w / 2;
        double cy = h / 2;

        // Pixels along perimeter (per frame)
        if (SquareClockwise)
            _angle += SquareStep;
        else
            _angle -= SquareStep;

        double cornerRadius = 12; // radius of rounded corners
        double straightTop = w - 2 * cornerRadius;
        double straightRight = h - 2 * cornerRadius;
        double straightBottom = straightTop;
        double straightLeft = straightRight;
        double arcLen = Math.PI / 4 * cornerRadius;
        double perimeter = perimeter = 2 * (w + h);

        for (int i = 0; i < _dots.Length; i++)
        {
            // Each dot is spaced evenly along the perimeter
            double offset = (_angle + i * (perimeter / count)) % perimeter;

            // Ensure no negative values sneak in when walking counter‑clockwise
            offset = (offset + perimeter) % perimeter;

            double x = 0;
            double y = 0;

            #region [Original method without the new SquareSize]
            // Walk along top edge ⇨ right edge ⇨ bottom ⇨ left
            //if (offset < w)
            //{
            //    // Top edge (left ⇨ right)
            //    x = offset;
            //    y = 0;
            //}
            //else if (offset < w + h)
            //{
            //    // Right edge (top ⇨ bottom)
            //    x = w;
            //    y = offset - w;
            //}
            //else if (offset < w + h + w)
            //{
            //    // Bottom edge (right ⇨ left)
            //    x = w - (offset - (w + h));
            //    y = h;
            //}
            //else
            //{
            //    // Left edge (bottom ⇨ top)
            //    x = 0;
            //    y = h - (offset - (w + h + w));
            //}
            #endregion

            #region [Using the new SquareSize, center it in the control]
            if (offset < w)
            {
                // Top edge (left ⇨ right)
                x = left + offset;
                y = top;
            }
            else if (offset < w + h)
            {
                // Right edge (top ⇨ bottom)
                x = left + w;
                y = top + (offset - w);
            }
            else if (offset < w + h + w)
            {
                // Bottom edge (right ⇨ left)
                x = left + w - (offset - (w + h));
                y = top + h;
            }
            else
            {
                // Left edge (bottom ⇨ top)
                x = left;
                y = top + h - (offset - (w + h + w));
            }
            #endregion

            // Center dots on coordinates
            var dot = _dots[i];
            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);

            // Fade with index
            if (SquareClockwise)
                dot.Opacity = GetOpacityEaseInOut(count - i, count);
            else
                dot.Opacity = GetOpacityEaseInOut(i, count);
        }
    }

    double _bounceOffset = 0d;
    bool _bounceForward = true;
    /// <summary>
    /// Shuffle from left to right and then back from right to left.
    /// </summary>
    void OnShuffleRendering(object sender, EventArgs e)
    {
        double speed = ActualWidth / WaveDuration; // px per second (RotationDuration)
        double delta = speed * GetDeltaSeconds();

        if (_bounceForward)
        {
            _bounceOffset += delta;
            if (_bounceOffset >= ActualWidth - DotSize)
                _bounceForward = false;
        }
        else
        {
            _bounceOffset -= delta;
            if (_bounceOffset <= 0)
                _bounceForward = true;
        }

        // Position dots in a line, staggered
        int count = PART_Canvas.Children.Count;
        double spacing = DotSize * 0.65;

        for (int i = 0; i < count; i++)
        {
            double x = _bounceOffset + i * spacing;
            double y = (ActualHeight - DotSize) / 2;

            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x % (ActualWidth - DotSize)); // wrap if needed
            Canvas.SetTop(dot, y);
        }
    }

    public int RingsCount { get; set; } = 4;
    public double RingsAngleSpeed { get; set; } = 2;
    public bool RingsOutward { get; set; } = true;
    public bool RingsAlternateOpacity { get; set; } = false;
    public bool RingsLowOpacity { get; set; } = false; // for subtle backgrounds
    void OnRingsRendering(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateSpiral();
            return;
        }

        int count = PART_Canvas.Children.Count;

        double cx = ActualWidth / 2;
        double cy = ActualHeight / 2;

        // How far the rings can expand (to the smaller half dimension)
        double maxRadius = Math.Min(ActualWidth, ActualHeight) / 2;


        // Advance global phase
        if (RingsOutward)
            _angle += RingsAngleSpeed; // pixels per frame outward
        else
            _angle -= RingsAngleSpeed; // pixels per frame inward

        if (_angle > maxRadius)
            _angle -= maxRadius;
        if (_angle < 0)
            _angle += maxRadius;

        // Determine number of concentric rings
        int dotsPerRing = count / RingsCount;

        for (int i = 0; i < _dots.Length; i++)
        {
            int ringIndex = i / dotsPerRing;
            int dotIndex = i % dotsPerRing;

            // Each ring expands outward with a phase offset
            double ringOffset = (ringIndex * (maxRadius / RingsCount));
            double radius = (_angle + ringOffset) % maxRadius;

            // Evenly distribute dots around the circle
            double theta = (Tau / dotsPerRing) * dotIndex;

            double x = cx + radius * Math.Cos(theta);
            double y = cy + radius * Math.Sin(theta);

            var dot = _dots[i];
            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);

            if (RingsLowOpacity)
            {
                dot.Opacity = RandomLowOpacity();
            }
            else
            {
                if (RingsAlternateOpacity) // fade in cycles
                    dot.Opacity = ((double)i / count) + 0.01;
                else // Fade as radius grows (fades near edge)
                    dot.Opacity = 1.0 - (radius / maxRadius);
            }
        }
    }


    public int PulseCount { get; set; } = 4;
    public double PulseSpeed { get; set; } = 3; // radians per frame
    public double PulseRadiusFactor { get; set; } = 2; // larger means smaller
    public bool PulseAlternateOpacity { get; set; } = false;
    public bool PulseLowOpacity { get; set; } = false; // for subtle backgrounds
    void OnPulseRendering(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateSpiral();
            return;
        }

        int count = PART_Canvas.Children.Count;

        // Determine center of control
        double cx = ActualWidth / 2;
        double cy = ActualHeight / 2;

        // How far the rings can expand (to the smaller half dimension)
        double maxRadius = Math.Min(ActualWidth, ActualHeight) / PulseRadiusFactor;

        // Advance global phase
        _angle += PulseSpeed / 100d;
        if (_angle > Tau)
            _angle -= Tau;
        if (_angle < 0)
            _angle += Tau;

        int dotsPerRing = count / PulseCount;

        for (int i = 0; i < _dots.Length; i++)
        {
            int ringIndex = i / dotsPerRing;
            int dotIndex = i % dotsPerRing;

            // Each ring has a phase offset so they don't all pulse in sync
            double phase = _angle + (ringIndex * (Math.PI / PulseCount));

            // Radius oscillates between 0 and maxRadius
            double radius = (Math.Sin(phase) * 0.5 + 0.5) * maxRadius;

            // Evenly distribute dots around the circle
            double theta = (Tau / dotsPerRing) * dotIndex;

            double x = cx + radius * Math.Cos(theta);
            double y = cy + radius * Math.Sin(theta);

            var dot = _dots[i];
            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);

            if (PulseLowOpacity)
            {
                dot.Opacity = RandomLowOpacity();
            }
            else
            {
                if (PulseAlternateOpacity) // fade in cycles
                    dot.Opacity = ((double)i / count) + 0.01;
                else // Fade as radius grows (fades near edge)
                    dot.Opacity = 1.0 - (radius / maxRadius);
            }
        }
    }


    double[] _phases; // per-dot phase offsets
    void CreateTwinkle1()
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();

        _dots = new Ellipse[DotCount];
        _phases = new double[DotCount];

        for (int i = 0; i < DotCount; i++)
        {
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
            };

            // Random position across control
            double x = Random.Shared.NextDouble() * ActualWidth;
            double y = Random.Shared.NextDouble() * ActualHeight;

            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);

            // Random phase so stars twinkle out of sync
            _phases[i] = Random.Shared.NextDouble() * Tau;

            _dots[i] = dot;
            PART_Canvas.Children.Add(dot);
        }
    }

    public double TwinkleSpeed { get; set; } = 6;
    void OnTwinkleRendering1(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateTwinkle1();
            return;
        }

        double speed = TwinkleSpeed / 100d;
        _angle += speed;

        for (int i = 0; i < _dots.Length; i++)
        {
            // Each star's opacity oscillates between 0.2 and 1.0
            double phase = _angle + _phases[i];
            double pulse = (Math.Sin(phase) * 0.5 + 0.5); // 0 ⇨ 1
            double opacity = 0.1 + 0.5 * pulse;
            _dots[i].Opacity = opacity;
        }
    }

    Star[] _twinks;
    void CreateTwinkle2()
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();

        _dots = new Ellipse[DotCount];
        _twinks = new Star[DotCount];
        _phases = new double[DotCount];

        for (int i = 0; i < DotCount; i++)
        {
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
            };

            // Random position across control
            double x = Random.Shared.NextDouble() * ActualWidth;
            double y = Random.Shared.NextDouble() * ActualHeight;
            var phase = Random.Shared.NextDouble() * Tau;
            _phases[i] = phase;

            // 📝 NOTE: This extension method can be replaced by your own color logic.
            //    It's the only spinner code that is tied to an external helper file.
            // 70% Blue — 30% Red, bright
            var tiltedLight = Extensions.CreateRandomLightBrush(new Dictionary<Extensions.ColorTilt, double>
            {
                { Extensions.ColorTilt.Blue, 0.7 },
                { Extensions.ColorTilt.Red, 0.3 }
            });
            //var tiltedLight = Extensions.CreateRandomLightBrush(ColorTilt.Blue);
            //var randColor = Extensions.GenerateRandomColor();
            var edgeColor = Color.FromRgb(tiltedLight.Color.R, tiltedLight.Color.G, tiltedLight.Color.B);
            var coreColor = Random.Shared.NextDouble() > 0.49 ? Colors.White : Colors.LightGray;

            // Random phase so stars twinkle out of sync
            var rgb = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.75, 0.25), // center
                Center = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(coreColor, 0.0), // bright core
                    new GradientStop(LerpColor(coreColor, Colors.Black, 0.65), 0.7), // middle
                    new GradientStop(LerpColor(edgeColor, Colors.Black, 0.35), 1.0), // dark outer
                }
            };

            var ts = new Star
            {
                X = x,
                Y = y,
                Phase = phase,
                Brush = rgb,
                EdgeColor = edgeColor,
                CoreColor = coreColor,
                SpeedFactor = 0.5 + Random.Shared.NextDouble() * 1.5 // range: 0.5x – 2.0x speed
            };

            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);

            _twinks[i] = ts;
            _dots[i] = dot;

            PART_Canvas.Children.Add(dot);
        }
    }

    void OnTwinkleRendering2(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateTwinkle2();
            return;
        }

        for (int i = 0; i < _dots.Length; i++)
        {
            _angle += TwinkleSpeed / 1500d;

            // Animate breathing using a sine wave
            double phase = (_angle * _twinks[i].SpeedFactor) + _twinks[i].Phase;
            double pulse = (Math.Sin(phase) * 0.5 + 0.5); // 0 ⇨ 1

            // Modify the star's RadialGradientBrush
            var brush = _twinks[i].Brush;
            if (brush != null && brush.GradientStops.Count == 2)
            {
                #region [Two Stops]
                GradientStop inner = brush.GradientStops[0];
                GradientStop outer = brush.GradientStops[1];

                // Animate the offsets to make the glow "breathe"
                inner.Offset = 0.0 + pulse * 0.08;   // core expands/contracts slightly
                outer.Offset = 0.8 + pulse * 0.2;    // halo breathes between 0.8–1.0

                // Optionally animate alpha for extra shimmer
                //byte alpha = (byte)(180 + 75 * pulse); // 180–255
                //inner.Color = Color.FromArgb(alpha, inner.Color.R, inner.Color.G, inner.Color.B);

                // Animate colors
                // Base palette (could be per-star randomized at creation)
                Color baseCore = _twinks[i].CoreColor;   // e.g. White, LightBlue, Gold, etc.
                Color baseEdge = _twinks[i].EdgeColor;   // e.g. Blue, Orange, Red, etc.

                // Brighten/dim core with pulse
                Color brightCore = BrightenGamma(baseCore, 1.7);
                inner.Color = LerpColor(baseCore, brightCore, pulse);

                // Edge fades more strongly
                Color dimEdge = DarkenGamma(baseEdge, 0.2);
                outer.Color = LerpColor(dimEdge, baseEdge, pulse);
                #endregion
            }
            else if (brush != null && brush.GradientStops.Count == 3)
            {
                #region [Three Stops]
                //GradientStop inner = brush.GradientStops[0]; // core
                //GradientStop mid = brush.GradientStops[1];   // mid glow
                //GradientStop outer = brush.GradientStops[2]; // halo (edge)
                //
                //// Animate offsets
                //inner.Offset = 0.0 + pulse * 0.05;   // core expands slightly
                //mid.Offset = 0.4 + pulse * 0.1;      // mid glow shifts outward
                //outer.Offset = 0.9 + pulse * 0.1;    // halo breathes between 0.9–1.0
                //
                //// Core brightens/dims
                //Color brightCore = BrightenGamma(_twinks[i].CoreColor, 1.7);
                //inner.Color = LerpColor(_twinks[i].CoreColor, brightCore, pulse);
                //
                //// Mid glow oscillates between dimmer and base edge color
                //Color dimMid = DarkenGamma(_twinks[i].EdgeColor, 0.5);
                //mid.Color = LerpColor(dimMid, _twinks[i].EdgeColor, pulse);
                //
                //// Outer halo fades more strongly
                //byte haloAlpha = (byte)(40 + 100 * (1 - pulse)); // 40–140 alpha
                //outer.Color = Color.FromArgb(haloAlpha, _twinks[i].EdgeColor.R, _twinks[i].EdgeColor.G, _twinks[i].EdgeColor.B);
                #endregion

                #region [Mid-Lag Shimmer]
                GradientStop inner = brush.GradientStops[0]; // core
                GradientStop mid = brush.GradientStops[1];   // mid glow
                GradientStop outer = brush.GradientStops[2]; // halo (edge)

                // Animate offsets
                inner.Offset = 0.0 + pulse * 0.05;
                outer.Offset = 0.9 + pulse * 0.1;

                // Mid stop lags behind by shifting its phase
                double midPhase = phase + Math.PI / 4; // 45° lag
                double midPulse = (Math.Sin(midPhase) * 0.5 + 0.5);
                // Use a phase offset (+π/4) so it pulses slightly later than the core/edge.
                mid.Offset = 0.4 + midPulse * 0.1;

                // Core brightens/dims
                Color brightCore = BrightenGamma(_twinks[i].CoreColor, 1.6);
                inner.Color = LerpColor(_twinks[i].CoreColor, brightCore, pulse);

                // Mid glow lags in brightness too
                Color dimMid = DarkenGamma(_twinks[i].EdgeColor, 0.5);
                mid.Color = LerpColor(dimMid, _twinks[i].EdgeColor, midPulse);

                // Outer halo fades softly
                byte haloAlpha = (byte)(40 + 100 * (1 - pulse)); // 40–140 alpha
                outer.Color = Color.FromArgb(haloAlpha, _twinks[i].EdgeColor.R, _twinks[i].EdgeColor.G, _twinks[i].EdgeColor.B);
                #endregion
            }

            //double opacity = 0.1 + 0.5 * _twinks[i].Phase;
            //_dots[i].Opacity = opacity;

            _dots[i].Fill = brush;
        }
    }

    StarState[] _stars;
    void CreateMeteors1()
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();

        _dots = new Ellipse[DotCount];
        _stars = new StarState[DotCount];

        int trailLength = MeteorTrailLength; // number of trail dots per shooting star

        for (int i = 0; i < DotCount; i++)
        {
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
            };

            // Random position across control
            double x = Random.Shared.NextDouble() * ActualWidth;
            double y = Random.Shared.NextDouble() * ActualHeight;

            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);

            _dots[i] = dot;
            PART_Canvas.Children.Add(dot);

            // Pre-allocate meteor trail
            var trail = new Ellipse[trailLength];
            for (int j = 0; j < trailLength; j++)
            {
                var td = new Ellipse
                {
                    Width = DotSize / MeteorTrailFactor,
                    Height = DotSize / MeteorTrailFactor,
                    Fill = new SolidColorBrush(MeteorTrailColor),
                    Opacity = 0
                };
                trail[j] = td;
                PART_Canvas.Children.Add(td);
            }

            // Add the star to the array
            _stars[i] = new StarState
            {
                X = x,
                Y = y,
                Phase = Random.Shared.NextDouble() * Tau,
                IsShooting = false,
                TrailDots = trail
            };

        }
    }

    double lastFadePercent = 0.2; // start fading when life is below this percentage (20% by default)
    public int MeteorTrailLength { get; set; } = 16;
    public Color MeteorTrailColor { get; set; } = Colors.LightGray;
    public double MeteorTrailFactor { get; set; } = 2; // size reduction factor for trail dots
    public double MeteorSpeed { get; set; } = 5;
    public double MeteorSpreadAngle { get; set; } = 30;
    public bool MeteorSpread360 { get; set; } = false;
    public double MeteorShootChance { get; set; } = 1; // 1% chance per frame
    public bool MeteorReduceLoad { get; set; } = true;
    void OnMeteorRendering1(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateMeteors1();
            return;
        }

        // With the reduced load option we'll double the speed.
        double speed = MeteorReduceLoad ? (MeteorSpeed * 2) / 100d : MeteorSpeed / 100d;
        _angle += speed;

        for (int i = 0; i < _dots.Length; i++)
        {
            if (MeteorReduceLoad && i % 2 == 0)
                Thread.Sleep(1); // reduce CPU usage

            var star = _stars[i];
            var dot = _dots[i];

            if (star.IsShooting)
            {
                // Update shooting position
                star.ShootX += star.VX;
                star.ShootY += star.VY;
                star.Life--;

                // Position head
                Canvas.SetLeft(dot, star.ShootX - dot.Width / 2);
                Canvas.SetTop(dot, star.ShootY - dot.Height / 2);

                // Adjust opacity (fade out as life decreases)
                double lifeRatio = (double)star.Life / star.InitialLife;
                if (lifeRatio > lastFadePercent)
                {
                    dot.Opacity = 1.0; // full brightness
                }
                else
                {
                    double t = lifeRatio / lastFadePercent; // normalize last 20%
                    dot.Opacity = t * t; // quadratic fade
                }

                // Update trail dots
                for (int j = 0; j < star.TrailDots.Length; j++)
                {
                    double t = (double)j / star.TrailDots.Length; // 0 ⇨ 1 along trail
                    double tx = star.ShootX - star.VX * j * 0.5;
                    double ty = star.ShootY - star.VY * j * 0.5;

                    var td = star.TrailDots[j];
                    // fade along trail
                    //td.Opacity = 0.8 * (1 - t);
                    // fade along life span
                    double fade = (lifeRatio > lastFadePercent ? 1.0 : (lifeRatio / lastFadePercent) * (lifeRatio / lastFadePercent));
                    td.Opacity = (1 - t) * 0.8 * fade;

                    double tTrail = (double)j / star.TrailDots.Length;
                    // Shift to reddish-orange along the trail
                    Color trailColor = LerpColor(MeteorTrailColor, Colors.OrangeRed, tTrail);
                    td.Fill = new SolidColorBrush(trailColor);

                    Canvas.SetLeft(td, tx - td.Width / 2);
                    Canvas.SetTop(td, ty - td.Height / 2);
                }

                if (star.Life <= 0)
                {
                    // Reset to static star
                    star.IsShooting = false;
                    star.X = Random.Shared.NextDouble() * ActualWidth;
                    star.Y = Random.Shared.NextDouble() * ActualHeight;
                    Canvas.SetLeft(dot, star.X - dot.Width / 2);
                    Canvas.SetTop(dot, star.Y - dot.Height / 2);
                    // Hide trail
                    foreach (var td in star.TrailDots)
                        td.Opacity = 0;
                }
            }
            else
            {
                // Normal pulsing star
                double phase = _angle + star.Phase;
                double pulse = (Math.Sin(phase) * 0.5 + 0.5);
                dot.Opacity = 0.1 + 0.7 * pulse;

                Canvas.SetLeft(dot, star.X - dot.Width / 2);
                Canvas.SetTop(dot, star.Y - dot.Height / 2);

                // Random chance to trigger shooting star
                if (Random.Shared.NextDouble() < (MeteorShootChance / 1000d)) // % chance per frame
                {
                    star.IsShooting = true;
                    star.ShootX = star.X;
                    star.ShootY = star.Y;

                    #region [Determine shooting direction and speed]
                    double angle = 0;
                    if (MeteorSpread360)
                    {
                        angle = Random.Shared.NextDouble() * Tau; // Full 360° random angle
                    }
                    else
                    {
                        // Preferred direction (in radians)
                        //double radiant = Math.PI / 4; // Example: right = 45°
                        double radiant = Math.PI / 2; // Example: downward = 90°
                        //double spread = Math.PI / 6; // Spread around radiant (e.g. ±30° around the downward angle)
                        double spread = SpreadFromDegrees(MeteorSpreadAngle); // convert degrees to radians
                        angle = radiant + (Random.Shared.NextDouble() * 2 - 1) * spread; // Pick a random angle within that cone
                    }

                    double shootSpeed = MeteorSpeed + Random.Shared.NextDouble() * (MeteorSpeed * 2);
                    star.VX = Math.Cos(angle) * shootSpeed;
                    star.VY = Math.Sin(angle) * shootSpeed;
                    // Static life span
                    //star.Life = 60;
                    // Compute life based on distance to edge
                    star.Life = ComputeLife(star.ShootX, star.ShootY, star.VX, star.VY, ActualWidth, ActualHeight, margin: -20);
                    star.InitialLife = star.Life;
                    #endregion
                }
            }
        }
    }

    StarStateWithColor[] _stars2;
    void CreateMeteors2()
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();

        _dots = new Ellipse[DotCount];
        _stars2 = new StarStateWithColor[DotCount];

        int trailLength = MeteorTrailLength; // number of trail dots per shooting star

        for (int i = 0; i < DotCount; i++)
        {
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
            };

            // Random position across control
            double x = Random.Shared.NextDouble() * ActualWidth;
            double y = Random.Shared.NextDouble() * ActualHeight;

            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);

            _dots[i] = dot;
            PART_Canvas.Children.Add(dot);

            // Pre-allocate meteor trail
            var trail = new Ellipse[trailLength];
            for (int j = 0; j < trailLength; j++)
            {
                var td = new Ellipse
                {
                    Width = DotSize / MeteorTrailFactor,
                    Height = DotSize / MeteorTrailFactor,
                    Fill = new SolidColorBrush(MeteorTrailColor),
                    Opacity = 0
                };
                trail[j] = td;
                PART_Canvas.Children.Add(td);
            }

            // Add the star to the array
            _stars2[i] = new StarStateWithColor
            {
                X = x,
                Y = y,
                Phase = Random.Shared.NextDouble() * Tau,
                IsShooting = false,
                TrailDots = trail
            };

        }
    }

    void OnMeteorRendering2(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateMeteors2();
            return;
        }

        // With the reduced load option we'll double the speed.
        double speed = MeteorReduceLoad ? (MeteorSpeed * 2) / 100d : MeteorSpeed / 100d;
        _angle += speed;

        for (int i = 0; i < _dots.Length; i++)
        {
            if (MeteorReduceLoad && i % 2 == 0)
                Thread.Sleep(1); // reduce CPU usage

            var star = _stars2[i];
            var dot = _dots[i];

            if (star.IsShooting)
            {
                // Update shooting position
                star.ShootX += star.VX;
                star.ShootY += star.VY;
                star.Life--;

                // Position head
                Canvas.SetLeft(dot, star.ShootX - dot.Width / 2);
                Canvas.SetTop(dot, star.ShootY - dot.Height / 2);

                // Adjust opacity (fade out as life decreases)
                double lifeRatio = (double)star.Life / star.InitialLife;
                if (lifeRatio > lastFadePercent)
                {
                    dot.Opacity = 1.0; // full brightness
                }
                else
                {
                    double t = lifeRatio / lastFadePercent; // normalize last 20%
                    dot.Opacity = t * t; // quadratic fade
                }

                // Color interpolation
                Color currentColor;
                if (lifeRatio > 0.5)
                {
                    double t = (lifeRatio - 0.5) / 0.5; // 1 ⇨ 0
                    currentColor = LerpColor(star.MidColor, star.StartColor, t);
                }
                else
                {
                    double t = lifeRatio / 0.5; // 1 ⇨ 0
                    currentColor = LerpColor(star.EndColor, star.MidColor, t);
                }
                //dot.Fill = new SolidColorBrush(currentColor);
                dot.Fill = new RadialGradientBrush
                {
                    GradientOrigin = new Point(0.75, 0.25), // center
                    Center = new Point(0.5, 0.5),
                    RadiusX = 0.5,
                    RadiusY = 0.5,
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(LerpColor(currentColor, Colors.White, 0.75), 0.0), // bright core
                        new GradientStop(currentColor, 0.7),
                        new GradientStop(LerpColor(currentColor, Colors.Black, 0.3), 0.92), // dark outer
                        new GradientStop(Color.FromArgb(90, currentColor.R, currentColor.G, currentColor.B), 1.0) // transparent edge
                    }
                };

                // Update trail dots
                for (int j = 0; j < star.TrailDots.Length; j++)
                {
                    double t = (double)j / star.TrailDots.Length; // 0 ⇨ 1 along trail
                    double tx = star.ShootX - star.VX * j * 0.5;
                    double ty = star.ShootY - star.VY * j * 0.5;

                    var td = star.TrailDots[j];
                    // fade along trail
                    //td.Opacity = 0.8 * (1 - t);
                    // fade along life span
                    double fade = (lifeRatio > lastFadePercent ? 1.0 : (lifeRatio / lastFadePercent) * (lifeRatio / lastFadePercent));
                    td.Opacity = (1 - t) * 0.8 * fade;

                    double tTrail = (double)j / star.TrailDots.Length;
                    Color trailColor = LerpColor(currentColor, star.EndColor, tTrail);
                    //td.Fill = CreateGradientBrush(currentColor, trailColor);
                    td.Fill = new SolidColorBrush(trailColor);

                    Canvas.SetLeft(td, tx - td.Width / 2);
                    Canvas.SetTop(td, ty - td.Height / 2);
                }

                if (star.Life <= 0)
                {
                    // Reset to static star
                    star.IsShooting = false;
                    star.X = Random.Shared.NextDouble() * ActualWidth;
                    star.Y = Random.Shared.NextDouble() * ActualHeight;
                    Canvas.SetLeft(dot, star.X - dot.Width / 2);
                    Canvas.SetTop(dot, star.Y - dot.Height / 2);
                    // Hide trail
                    foreach (var td in star.TrailDots)
                        td.Opacity = 0;
                }
            }
            else
            {
                // Normal pulsing star
                double phase = _angle + star.Phase;
                double pulse = (Math.Sin(phase) * 0.5 + 0.5);
                dot.Opacity = 0.1 + 0.7 * pulse;

                Canvas.SetLeft(dot, star.X - dot.Width / 2);
                Canvas.SetTop(dot, star.Y - dot.Height / 2);

                // Random chance to trigger shooting star
                if (Random.Shared.NextDouble() < (MeteorShootChance / 1000d)) // % chance per frame
                {
                    star.IsShooting = true;
                    star.ShootX = star.X;
                    star.ShootY = star.Y;

                    #region [Determine shooting direction and speed]
                    double angle = 0;
                    if (MeteorSpread360)
                    {
                        angle = Random.Shared.NextDouble() * Tau; // Full 360° random angle
                    }
                    else
                    {
                        // Preferred direction (in radians)
                        //double radiant = Math.PI / 4; // Example: right = 45°
                        double radiant = Math.PI / 2; // Example: downward = 90°
                        //double spread = Math.PI / 6; // Spread around radiant (e.g. ±30° around the downward angle)
                        double spread = SpreadFromDegrees(MeteorSpreadAngle); // convert degrees to radians
                        angle = radiant + (Random.Shared.NextDouble() * 2 - 1) * spread; // Pick a random angle within that cone
                    }

                    double shootSpeed = MeteorSpeed + Random.Shared.NextDouble() * (MeteorSpeed * 2);
                    star.VX = Math.Cos(angle) * shootSpeed;
                    star.VY = Math.Sin(angle) * shootSpeed;
                    // Static life span
                    //star.Life = 60;
                    // Compute life based on distance to edge
                    star.Life = ComputeLife(star.ShootX, star.ShootY, star.VX, star.VY, ActualWidth, ActualHeight, margin: -20);
                    star.InitialLife = star.Life;
                    #endregion

                    // Random palette
                    int palette = Random.Shared.Next(3);
                    switch (palette)
                    {
                        case 0: // Bluish-white
                            star.StartColor = Colors.White;
                            star.MidColor = Colors.LightBlue;
                            star.EndColor = Colors.DeepSkyBlue;
                            break;
                        case 1: // Golden
                            star.StartColor = Colors.White;
                            star.MidColor = Colors.Gold;
                            star.EndColor = Colors.Orange;
                            break;
                        case 2: // Reddish
                            star.StartColor = Colors.White;
                            star.MidColor = Colors.Orange;
                            star.EndColor = Colors.Red;
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Converts a spread angle in degrees into a radian value of the form Math.PI/N.<br/>
    /// For example: 30° ⇨ Math.PI / 6.<br/>
    /// - SpreadFromDegrees(30) ⇨ 0.523 ⇨ Math.PI/6<br/>
    /// - SpreadFromDegrees(45) ⇨ 0.785 ⇨ Math.PI/4<br/>
    /// - SpreadFromDegrees(60) ⇨ 1.047 ⇨ Math.PI/3<br/>
    /// </summary>
    /// <remarks>range is 0 to 180 (straight up to straight down)</remarks>
    static double SpreadFromDegrees(double degrees)
    {
        if (degrees <= 0 || degrees >= 180)
            degrees = 90; // default to 90° if out of range

        // Convert degrees to radians
        double radians = degrees * Math.PI / 180.0;

        // Equivalent divisor (Math.PI / N)
        double divisor = Math.PI / radians;

        // Return just the radians
        return radians;
    }

    /// <summary>
    /// Converts a spread angle in degrees into radians and also<br/>
    /// returns the divisor N such that spread ≈ Math.PI/N.<br/>
    /// - SpreadFromDegreesToRadians(30) ⇨ (0.523, 6) ⇨ Math.PI/6<br/>
    /// - SpreadFromDegreesToRadians(45) ⇨ (0.785, 4) ⇨ Math.PI/4<br/>
    /// - SpreadFromDegreesToRadians(60) ⇨ (1.047, 3) ⇨ Math.PI/3<br/>
    /// </summary>
    static (double Radians, double Divisor) SpreadFromDegreesToRadians(double degrees)
    {
        if (degrees <= 0 || degrees >= 180)
            degrees = 90; // default to 90° if out of range

        // Convert degrees to radians
        double radians = degrees * Math.PI / 180.0;

        // Equivalent divisor (Math.PI / N)
        double divisor = Math.PI / radians;

        // Return radians and divisor
        return (radians, divisor);
    }

    /// <summary>
    /// Computes star life based on distance to control's edge.<br/>
    /// The lifetime of a shooting star should be proportional to how far it has to travel before leaving the control bounds.<br/>
    /// </summary>
    /// <param name="startX"><see cref="StarState.ShootX"/></param>
    /// <param name="startY"><see cref="StarState.ShootY"/></param>
    /// <param name="vx"><see cref="StarState.VX"/></param>
    /// <param name="vy"><see cref="StarState.VY"/></param>
    /// <param name="width">ActualWidth</param>
    /// <param name="height">ActualHeight</param>
    /// <returns>life remaining as <see cref="int"/></returns>
    static int ComputeLife(double startX, double startY, double vx, double vy, double width, double height)
    {
        double maxT = double.MaxValue;

        // Right edge
        if (vx > 0)
            maxT = Math.Min(maxT, (width - startX) / vx);
        else if (vx < 0)
            maxT = Math.Min(maxT, (0 - startX) / vx);

        // Bottom edge
        if (vy > 0)
            maxT = Math.Min(maxT, (height - startY) / vy);
        else if (vy < 0)
            maxT = Math.Min(maxT, (0 - startY) / vy);

        // Distance to edge
        double distance = maxT * Math.Sqrt(vx * vx + vy * vy);

        // Convert to frames (assuming 60fps, 1 unit per pixel per frame)
        return (int)(distance / Math.Sqrt(vx * vx + vy * vy));
    }

    /// <summary>
    /// Add a margin buffer so shooting stars fade out gracefully before they hit the control’s edge.<br/>
    /// This way, they won’t just pop off‑screen, but instead taper away naturally.<br/>
    /// </summary>
    /// <param name="startX"><see cref="StarState.ShootX"/></param>
    /// <param name="startY"><see cref="StarState.ShootY"/></param>
    /// <param name="vx"><see cref="StarState.VX"/></param>
    /// <param name="vy"><see cref="StarState.VY"/></param>
    /// <param name="width">ActualWidth</param>
    /// <param name="height">ActualHeight</param>
    /// <param name="margin">negative values will allow extension outside control bounds, positive values for inside control bounds</param>
    /// <returns>life remaining as <see cref="int"/></returns>
    static int ComputeLife(double startX, double startY, double vx, double vy, double width, double height, double margin = 20)
    {
        double maxT = double.MaxValue;

        // Right edge
        if (vx > 0)
            maxT = Math.Min(maxT, (width - margin - startX) / vx);
        else if (vx < 0)
            maxT = Math.Min(maxT, (margin - startX) / vx);

        // Bottom edge
        if (vy > 0)
            maxT = Math.Min(maxT, (height - margin - startY) / vy);
        else if (vy < 0)
            maxT = Math.Min(maxT, (margin - startY) / vy);

        // Distance to edge (minus margin)
        double distance = maxT * Math.Sqrt(vx * vx + vy * vy);

        // Convert to frames (1 unit per pixel per frame)
        return Math.Max(1, (int)(distance / Math.Sqrt(vx * vx + vy * vy)));
    }


    public double FallingBaseSpeed { get; set; } = 4;
    public bool FallingAcceleration { get; set; } = false;
    public int FallingFinishedPause { get; set; } = 90; // frames to pause when all dots reach bottom

    void CreateFalling()
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();

        _dots = new Ellipse[DotCount];
        _twinks = new Star[DotCount];
        for (int i = 0; i < DotCount; i++)
        {
            double x = Random.Shared.NextDouble() * ActualWidth;
            double y = Random.Shared.NextDouble() * ActualHeight / 10; // start near top

            Ellipse dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = (double)i / (double)DotCount + 0.01 // fade each consecutive dot
            };

            var baseAccel = ((FallingBaseSpeed * 2.5) / 100);
            var ts = new Star
            {
                X = x,
                Y = y,
                SpeedFactor = FallingBaseSpeed + Random.Shared.NextDouble() * 4.0, // px per frame
                Velocity = 0, // start at rest
                Acceleration = baseAccel + Random.Shared.NextDouble() * 0.1 // vary gravity a bit
            };

            _dots[i] = dot;
            _twinks[i] = ts;

            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
            PART_Canvas.Children.Add(dot);
        }
    }

    int _pauseCounter = 0;

    void OnFallingRendering(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateFalling();
            return;
        }

        bool allAtBottom = true;


        for (int i = 0; i < _dots.Length; i++)
        {
            // If this star hasn't reached the bottom yet, move it down
            if (_twinks[i].Y < (ActualHeight - DotSize))
            {
                // Apply acceleration to velocity
                _twinks[i].Velocity += _twinks[i].Acceleration;

                // Move down by its speed factor, but clamp to bottom
                if (FallingAcceleration)
                    _twinks[i].Y = Math.Min(_twinks[i].Y + _twinks[i].Velocity, ActualHeight - DotSize);
                else
                    _twinks[i].Y = Math.Min(_twinks[i].Y + _twinks[i].SpeedFactor, ActualHeight - DotSize);
                allAtBottom = false; // at least one is still falling
            }
            else
            {
                // Clamp to bottom
                _twinks[i].Y = ActualHeight - DotSize;
            }

            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, _twinks[i].X - DotSize / 2);
            Canvas.SetTop(dot, _twinks[i].Y);
        }

        // If all stars are at the bottom, reset them together
        if (allAtBottom)
        {
            // Increment and check counter
            if (++_pauseCounter >= FallingFinishedPause)
            {
                // Reset all dots together
                foreach (var twink in _twinks)
                {
                    //star.Y = -DotSize; // reset above the top
                    twink.Y = Random.Shared.NextDouble() * ActualHeight / 10;
                    twink.X = Random.Shared.NextDouble() * ActualWidth; // randomize X
                    twink.Velocity = 0; // reset velocity
                }
                _pauseCounter = 0; // reset pause
            }
        }
        else  // Reset pause counter if not all at bottom yet
            _pauseCounter = 0;
    }

    public double ExplosionBaseSpeed { get; set; } = 2;
    public bool ExplosionFadeGradually { get; set; } = false;
    public int ExplosionFinishedPause { get; set; } = 30; // frames to pause when all dots reach bottom
    void CreateExplosion()
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();

        _dots = new Ellipse[DotCount];
        _stars = new StarState[DotCount];
        for (int i = 0; i < DotCount; i++)
        {
            // start at bottom middle
            double x = ActualWidth / 2;
            double y = ActualHeight / 2;

            Ellipse dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = (double)i / (double)DotCount + 0.01 // fade each consecutive dot
            };

            // Random direction
            double angle = Random.Shared.NextDouble() * 2 * Math.PI;
            double speed = ExplosionBaseSpeed + Random.Shared.NextDouble() * 4; // vary speed

            var es = new StarState
            {
                X = x,
                Y = y,
                VX = Math.Cos(angle) * speed,  // start at rest
                VY = -Math.Sin(angle) * speed, // negative Y is up
                Opacity = 0.9                  // start visible
            };

            _dots[i] = dot;
            _stars[i] = es;

            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
            PART_Canvas.Children.Add(dot);
        }
    }

    void OnExplosionRendering(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateExplosion();
            return;
        }

        bool allOutside = true;

        for (int i = 0; i < _dots.Length; i++)
        {
            // Update position
            _stars[i].X += _stars[i].VX;
            _stars[i].Y += _stars[i].VY;

            if (ExplosionFadeGradually) // Fade out gradually
            {
                _stars[i].Opacity -= 0.03; // fade speed
                if (_stars[i].Opacity < 0) { _stars[i].Opacity = 0; }
            }
            else // Compute fade based on bounds
            {
                _stars[i].Opacity = ComputeBoundsAwareOpacity(_stars[i].X, _stars[i].Y, ActualWidth, ActualHeight);
            }

            // Check if dot is still inside bounds
            if (_stars[i].X >= 0 && _stars[i].X <= (ActualWidth - DotSize) &&
                _stars[i].Y >= 0 && _stars[i].Y <= (ActualHeight- DotSize))
            {
                allOutside = false;
            }

            var dot = (UIElement)PART_Canvas.Children[i];
            dot.Opacity = _stars[i].Opacity;
            Canvas.SetLeft(dot, _stars[i].X - DotSize / 2);
            Canvas.SetTop(dot, _stars[i].Y - DotSize / 2);
        }

        if (allOutside)
        {
            if (++_pauseCounter >= ExplosionFinishedPause)
            {
                // Instead of recreating the arrays, we'll just reset them in place.
                //CreateExplosion();
                foreach (var star in _stars)
                {
                    star.Y = ActualHeight / 2;
                    star.X = ActualWidth / 2;
                    double angle = Random.Shared.NextDouble() * 2 * Math.PI;
                    double speed = ExplosionBaseSpeed + Random.Shared.NextDouble() * 4; // vary speed
                    star.VX = Math.Cos(angle) * speed; // start at rest
                    star.VY = -Math.Sin(angle) * speed; // negative Y is up
                    star.Opacity = 1.0; // reset opacity
                }
                _pauseCounter = 0; // reset pause
            }
        }
        else  // Reset pause counter if not all at bottom yet
            _pauseCounter = 0;
    }


    public double FountainSpreadDegrees { get; set; } = 30.0;
    public double FountainFadeRate { get; set; } = 2;
    public double FountainBaseSpeed { get; set; } = 2.5;
    void CreateFountain()
    {
        double originX = ActualWidth / 2.0;
        double originY = ActualHeight; // bottom center
        
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();

        _dots = new Ellipse[DotCount];
        _stars = new StarState[DotCount];

        for (int i = 0; i < DotCount; i++)
        {
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush
            };

            _stars[i] = new StarState
            {
                Opacity = 0.9
            };

            Canvas.SetLeft(dot, originX);
            Canvas.SetTop(dot, originY);
            PART_Canvas.Children.Add(dot);

            ResetFountainParticle(_stars[i], originX, originY);
        }
    }
    
    void ResetFountainParticle(StarState star, double originX, double originY)
    {
        // Launch spread around straight up (-π/2), e.g. ±30°
        double spreadDeg = FountainSpreadDegrees; // total spread; adjust for tighter/wider spray
        double spreadRad = spreadDeg * Math.PI / 180.0;

        #region [Simple angle calc with spread cone]
        //double angle = -Math.PI / 2.0 + ((Random.Shared.NextDouble() - 0.5) * spreadRad);
        #endregion

        #region [Modulating spread angle over time]
        //double baseSpreadDeg = FountainSpreadDegrees;
        //double spreadAmplitude = 15.0;
        //double currentSpreadDeg = baseSpreadDeg + Math.Sin(_spreadTime) * spreadAmplitude;
        //spreadRad = currentSpreadDeg * Math.PI / 180.0;
        //double angle = -Math.PI / 2.0 + ((Random.Shared.NextDouble() - 0.5) * spreadRad); // Angle around vertical
        #endregion

        #region [Gaussian angle distribution]
        // Box–Muller transform
        double u1 = 1.0 - Random.Shared.NextDouble();
        double u2 = 1.0 - Random.Shared.NextDouble();
        double gaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        // Scale to spread
        double offset = gaussian * (spreadRad / 4.0); // 95% within ±spread/2
        double angle = -Math.PI / 2.0 + offset;
        #endregion

        // Launch speed (px/frame)
        double speed = FountainBaseSpeed + Random.Shared.NextDouble() * 5.0; // 3–8

        star.X = originX;
        star.Y = originY;
        star.VX = Math.Cos(angle) * speed;
        star.VY = Math.Sin(angle) * speed; // negative (upwards), will arc with gravity
        star.Opacity = 1.0;
        star.FadeRate = (FountainFadeRate / 100) + Random.Shared.NextDouble() * 0.01; // vary fade slightly

        // slight color variation
        // star.Brush = new SolidColorBrush(Color.FromRgb(240, 240, (byte)(220 + Random.Shared.Next(35))));
    }


    double _spreadTime = 0;           // if using modulating spread angle
    double fountainGravity = 0.15;    // px/frame^2 pulling downward
    double fountainDrag = 0.995;      // mild damping for smooth arcs
    double fountainBoundsMargin = 5;  // tolerance outside screen before recycle
    void OnFountainRendering(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateFountain();
            return;
        }

        double originX = ActualWidth / 2.0;
        double originY = ActualHeight;
        
        _spreadTime += 0.05; // adjust speed of oscillation

        for (int i = 0; i < _dots.Length; i++)
        {
            // Physics integration
            _stars[i].VY += fountainGravity;  // gravity
            _stars[i].VX *= fountainDrag;     // mild horizontal damping
            _stars[i].VY *= fountainDrag;     // mild vertical damping

            _stars[i].X += _stars[i].VX;
            _stars[i].Y += _stars[i].VY;

            // Fade: altitude-based plus soft edge fade near bounds
            double fade = _stars[i].FadeRate;
            double topFadeStart = ActualHeight * 0.25; // start fading after rising ~25% of height
            double altitude = originY - _stars[i].Y; // how high above the origin
            if (altitude > topFadeStart)
            {
                // Fade faster once high
                fade *= 1.6;
            }

            // Soft bounds-aware fade near edges (outer 10%)
            double marginX = ActualWidth * 0.1;
            double marginY = ActualHeight * 0.1;
            double opacityX = 1.0;
            double opacityY = 1.0;

            if (_stars[i].X < marginX) 
                opacityX = Math.Max(0, _stars[i].X / marginX);
            else if (_stars[i].X > ActualWidth - marginX) 
                opacityX = Math.Max(0, (ActualWidth - _stars[i].X) / marginX);

            if (_stars[i].Y < marginY) 
                opacityY = Math.Max(0, _stars[i].Y / marginY);

            // Combine fades
            double boundsOpacity = Math.Min(opacityX, opacityY);
            if (boundsOpacity < 1.0)
            {
                // accelerate fade near edges
                fade *= 1.3;
            }

            _stars[i].Opacity = Math.Max(0, Math.Min(1.0, _stars[i].Opacity - fade));

            var dot = (UIElement)PART_Canvas.Children[i];
            dot.Opacity = _stars[i].Opacity;

            // Apply position
            Canvas.SetLeft(dot, _stars[i].X - DotSize / 2.0);
            Canvas.SetTop(dot, _stars[i].Y - DotSize / 2.0);

            // Recycle when done:
            bool outOfBounds =
                _stars[i].X < -fountainBoundsMargin || _stars[i].X > ActualWidth + fountainBoundsMargin ||
                _stars[i].Y > ActualHeight + fountainBoundsMargin; // fell below floor

            bool invisible = _stars[i].Opacity <= 0.0;

            if (outOfBounds || invisible)
            {
                ResetFountainParticle(_stars[i], originX, originY);
            }
        }
    }
    #endregion

    #region [Color Helpers]
    static Color LerpColor(Color from, Color to, double t)
    {
        return Color.FromArgb(
            (byte)(from.A + (to.A - from.A) * t),
            (byte)(from.R + (to.R - from.R) * t),
            (byte)(from.G + (to.G - from.G) * t),
            (byte)(from.B + (to.B - from.B) * t));
    }

    /// <summary><code>
    ///   var brighter = Brighten(baseColor, 1.5); // 50% brighter
    /// </code></summary>
    static Color Brighten(Color color, double factor)
    {
        // Ensure factor is >= 1.0 for brightening
        if (factor < 1.0) factor = 1.0;

        byte r = (byte)Math.Min(255, color.R * factor);
        byte g = (byte)Math.Min(255, color.G * factor);
        byte b = (byte)Math.Min(255, color.B * factor);

        return Color.FromArgb(color.A, r, g, b);
    }

    /// <summary>
    /// Gamma‑corrected brighten (perceptually smoother)
    /// <code>
    ///   var brighter = BrightenGamma(baseColor, 1.5); // 50% brighter
    /// </code>
    /// </summary>
    static Color BrightenGamma(Color color, double factor = 1.5, double gamma = 2.2)
    {
        // Convert sRGB ⇨ linear
        double r = Math.Pow(color.R / 255.0, gamma);
        double g = Math.Pow(color.G / 255.0, gamma);
        double b = Math.Pow(color.B / 255.0, gamma);

        // Apply brighten factor in linear space
        r = Math.Min(1.0, r * factor);
        g = Math.Min(1.0, g * factor);
        b = Math.Min(1.0, b * factor);

        // Convert back linear ⇨ sRGB
        byte R = (byte)(Math.Pow(r, 1.0 / gamma) * 255);
        byte G = (byte)(Math.Pow(g, 1.0 / gamma) * 255);
        byte B = (byte)(Math.Pow(b, 1.0 / gamma) * 255);

        return Color.FromArgb(color.A, R, G, B);
    }

    /// <summary>
    /// Gamma‑corrected darken (perceptually smoother)
    /// <code>
    ///   var darker = DarkenGamma(baseColor, 0.7); // Darken to 70% brightness
    /// </code>
    /// </summary>
    static Color DarkenGamma(Color color, double factor = 0.7, double gamma = 2.2)
    {
        // factor < 1.0 will darken, factor = 1.0 no change
        if (factor > 1.0) factor = 1.0;
        if (factor < 0.0) factor = 0.0;

        // Convert sRGB ⇨ linear
        double r = Math.Pow(color.R / 255.0, gamma);
        double g = Math.Pow(color.G / 255.0, gamma);
        double b = Math.Pow(color.B / 255.0, gamma);

        // Apply darken factor in linear space
        r *= factor;
        g *= factor;
        b *= factor;

        // Convert back linear ⇨ sRGB
        byte R = (byte)(Math.Pow(r, 1.0 / gamma) * 255);
        byte G = (byte)(Math.Pow(g, 1.0 / gamma) * 255);
        byte B = (byte)(Math.Pow(b, 1.0 / gamma) * 255);

        return Color.FromArgb(color.A, R, G, B);
    }

    /// <summary>
    /// Generates a random <see cref="LinearGradientBrush"/> using two <see cref="System.Windows.Media.Color"/>s.
    /// </summary>
    /// <returns><see cref="LinearGradientBrush"/></returns>
    static LinearGradientBrush CreateGradientBrush(Color c1, Color c2)
    {
        var gs1 = new GradientStop(c1, 0);
        var gs3 = new GradientStop(c2, 1);
        var gsc = new GradientStopCollection { gs1, gs3 };
        var lgb = new LinearGradientBrush
        {
            ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(0, 1),
            GradientStops = gsc
        };
        return lgb;
    }

    /// <summary>
    /// Generates a random <see cref="LinearGradientBrush"/> using three <see cref="System.Windows.Media.Color"/>s.
    /// </summary>
    /// <returns><see cref="LinearGradientBrush"/></returns>
    static LinearGradientBrush CreateGradientBrush(Color c1, Color c2, Color c3)
    {
        var gs1 = new GradientStop(c1, 0);
        var gs2 = new GradientStop(c2, 0.5);
        var gs3 = new GradientStop(c3, 1);
        var gsc = new GradientStopCollection { gs1, gs2, gs3 };
        var lgb = new LinearGradientBrush
        {
            ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(0, 1),
            GradientStops = gsc
        };
        return lgb;
    }
    #endregion

    #region [Delta Calc]
    DateTime _last = DateTime.MinValue;
    /// <summary>
    /// A simple delta-time tracker.
    /// </summary>
    /// <returns>
    /// How much time has elapsed since the last check.
    /// </returns>
    double GetDeltaSeconds()
    {
        var now = DateTime.UtcNow;
        if (_last == DateTime.MinValue || _last == DateTime.MaxValue)
            _last = now;
        var dt = (now - _last).TotalSeconds;
        _last = now;
        return dt;
    }
    #endregion

    #region [Opacity Helpers]
    double ComputeBoundsAwareOpacity(double x, double y, double width, double height, double initial = 0.9, double percentage = 0.15)
    {
        // Set initial opacity
        double opacityX = initial;
        double opacityY = initial;

        double marginX = width * percentage;
        double marginY = height * percentage;

        // Left fade
        if (x < marginX)
            opacityX = x / marginX;

        // Right fade
        else if (x > width - marginX)
            opacityX = (width - x) / marginX;

        // Top fade
        if (y < marginY)
            opacityY = y / marginY;

        // Bottom fade
        else if (y > height - marginY)
            opacityY = (height - y) / marginY;

        // Final opacity is the minimum of both axes
        return Math.Max(0, Math.Min(opacityX, opacityY));
    }

    static double GetOpacityForIndex(int index, int totalCount)
    {
        if (totalCount <= 1)
            return 1d;

        // Linear fade: 1.0 at index 0 ⇨ 0.1 at the last dot
        double t = (double)index / (totalCount - 1);
        double opacity = 1.0 - 0.9 * t;

        return Math.Max(0.0, Math.Min(1.0, opacity));
    }

    static double GetOpacityExponetial(int index, int totalCount)
    {
        if (totalCount <= 1)
            return 1d;

        // Linear fade: 1.0 at index 0 ⇨ 0.1 at the last dot
        double t = (double)index / (totalCount - 1);
        double opacity = 1.0 - t * t;

        return Math.Max(0.0, Math.Min(1.0, opacity));
    }

    static double GetOpacityLinear(int index, int totalCount)
    {
        double t = Normalize(index, totalCount);
        return 1.0 - t; // straight line fade
    }

    static double GetOpacityEaseIn(int index, int totalCount)
    {
        double t = Normalize(index, totalCount);
        return 1.0 - (t * t); // quadratic ease-in
    }

    static double GetOpacityEaseOut(int index, int totalCount)
    {
        double t = Normalize(index, totalCount);
        return 1.0 - Math.Sqrt(t); // square root ease-out
    }

    static double GetOpacityEaseInOut(int index, int totalCount)
    {
        double t = Normalize(index, totalCount);
        return 1.0 - (3 * t * t - 2 * t * t * t); // cubic smooth-step
    }

    static double Normalize(int index, int totalCount)
    {
        if (totalCount <= 1) { return 0; }
        return (double)index / (totalCount - 1); // t in [0,1]
    }
    #endregion

    #region [Random Helpers]
    /// <summary>
    /// <see cref="Random.Shared"/>.NextDouble() gives [0.000 to 0.999], so scale to [-value to +value]
    /// </summary>
    /// <returns>negative <paramref name="value"/> to positive <paramref name="value"/></returns>
    static double RandomSwing(double value)
    {
        double factor = Random.Shared.NextDouble() * 2.0 - 1.0;
        return value * factor;
    }

    /// <summary>
    /// <see cref="Random.Shared"/>.Next() gives [min to max], so scale to [-value to +value]
    /// </summary>
    /// <returns>negative <paramref name="value"/> to positive <paramref name="value"/></returns>
    static int RandomSwing(int value)
    {
        // Returns a random int in [-value, +value]
        return Random.Shared.Next(-value, value + 1);
    }

    /// <summary>
    /// Returns a random opacity value between 0.1 and 0.4 (inclusive of 0.1, exclusive of 0.4).
    /// </summary>
    static double RandomLowOpacity()
    {
        return 0.1 + Random.Shared.NextDouble() * (0.4 - 0.1);
    }

    /// <summary>
    /// Returns a random opacity value between 0.5 and 0.99 (inclusive of 0.5, exclusive of 0.99).
    /// </summary>
    static double RandomHighOpacity()
    {
        return 0.1 + Random.Shared.NextDouble() * (0.99 - 0.5);
    }


    /// <summary>
    /// Returns a normally distributed random number using Box-Muller.
    /// mean = 0, stdDev = 1 by default.
    /// <code>
    ///   var noise = RandomGaussian(0, 10); // e.g. -6.2
    /// </code>
    /// </summary>
    static double RandomGaussian(double mean = 0, double stdDev = 1)
    {
        double u1 = 1.0 - Random.Shared.NextDouble(); // avoid 0
        double u2 = 1.0 - Random.Shared.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // Box-Muller transform
        return mean + stdDev * randStdNormal;
    }

    /// <summary>
    /// Returns a Gaussian random clamped to [-maxAbs, +maxAbs].
    /// <code>
    ///   var clamped = RandomGaussianClamped(0, 20, 100); // e.g. +87.5
    /// </code>
    /// </summary>
    static double RandomGaussianClamped(double mean, double stdDev, double maxAbs)
    {
        double value = RandomGaussian(mean, stdDev);
        // Hard clamp if outside
        if (value > maxAbs) { return maxAbs; }
        if (value < -maxAbs) { return -maxAbs; }
        return value;
    }

    /// <summary>
    /// Returns a Gaussian random number, retrying until it falls within [-maxAbs, +maxAbs].
    /// Preserves the bell-curve distribution without flattening at the edges.
    /// <code>
    ///   var clamped = RandomGaussianBounded(0, 10, 50); // e.g. +24.1
    /// </code>
    /// </summary>
    static double RandomGaussianBounded(double mean, double stdDev, double maxAbs)
    {
        double value;
        // Retry until inside (no hard clamping)
        do { value = RandomGaussian(mean, stdDev); }
        while (value < -maxAbs || value > maxAbs);
        return value;
    }

    /// <summary>
    /// Returns a Gaussian random number with directional bias.
    /// Bias > 0 skews right (positive), Bias < 0 skews left (negative).
    /// Bias magnitude ~0.0–1.0 (0 = no bias, 1 = strong bias).
    /// <code>
    ///   var biased = RandomGaussianBiased(0, 10, -0.3);
    /// </code>
    /// </summary>
    static double RandomGaussianBiased(double mean, double stdDev, double bias)
    {
        // Base Gaussian
        double g = RandomGaussian(mean, stdDev);

        // Apply bias: shift distribution toward one side
        // Bias is scaled by stdDev so it feels proportional
        double shift = bias * stdDev;

        return g + shift;
    }
    #endregion
}

#region [Support Classes]
/// <summary>
/// For new twinkle simulation.
/// </summary>
class Star
{
    public double X, Y;               // static position
    public double Phase;              // twinkle phase
    public double SpeedFactor;        // breathing speed multiplier
    public double Velocity;           // current falling speed
    public double Acceleration;       // constant acceleration (gravity)
    public Color CoreColor;
    public Color EdgeColor;
    public RadialGradientBrush Brush; // gradient brush
}

/// <summary>
/// For "shooting stars" simulation.
/// </summary>
class StarState
{
    public double X, Y;           // static position
    public double Phase;          // twinkle phase
    public bool IsShooting;       // shooting star flag
    public double ShootX, ShootY; // current shooting position
    public double VX, VY;         // velocity vector
    public int Life;              // frames remaining in shooting
    public int InitialLife;       // for fade curve
    public double Opacity;        // current alpha
    public double FadeRate;       // opacity decrement per frame
    public Ellipse[] TrailDots;   // pre-allocated trail ellipses
}

/// <summary>
/// For "shooting stars" simulation.
/// </summary>
class StarStateWithColor
{
    public double X, Y;
    public double Phase;
    public bool IsShooting;
    public double ShootX, ShootY;
    public double VX, VY;
    public int Life;
    public int InitialLife;
    public Ellipse[] TrailDots;  // pre-allocated trail ellipses

    // Color palette
    public Color StartColor;
    public Color MidColor;
    public Color EndColor;
}
#endregion