using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
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
    Chase,     // for spinner chase animation (grow & shrink)
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
    Radar,     // for radar arm animation
    Progress,  // for "progress bar" animation
    Splash,    // for raining with splash animation
    Fireworks, // for launch and explode animation
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
///   phase, but this could be moved to the creation phase if desired.<br/>
///   If you need some of the parameters to update real-time, e.g. <see cref="Spinner.ProgressValue"/>,<br/>
///   then is may be conducive to convert it into a dependency property.<br/>
/// </remarks>
public partial class Spinner : UserControl
{
    bool hasAppliedTemplate = false;
    bool _renderHooked = false;
    double _angle = 0.0;
    const double Tau = 2.0 * Math.PI;
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
        //Debug.WriteLine($"[INFO] {nameof(Spinner)} template has been applied.");
    }

    protected override Size MeasureOverride(Size constraint)
    {
        // The width/height is used in render object calculations, so we must have some value.
        if (constraint.Width.IsInvalidOrZero()) { Width = 50; }
        if (constraint.Height.IsInvalidOrZero()) { Height = 50; }

        //Debug.WriteLine($"[INFO] {nameof(Spinner)} is measured to be {constraint}");
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
        else if (RenderShape == SpinnerRenderShape.Splash)
            CreateSplashDots();
        else if (RenderShape == SpinnerRenderShape.Fireworks)
            CreateFireworkDots();
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
        else if (RenderShape == SpinnerRenderShape.Chase)
            CompositionTarget.Rendering += OnChaseRendering;
        else if (RenderShape == SpinnerRenderShape.Radar)
            CompositionTarget.Rendering += OnRadarRendering;
        else if (RenderShape == SpinnerRenderShape.Progress)
            CompositionTarget.Rendering += OnProgressRendering;
        else if (RenderShape == SpinnerRenderShape.Splash)
            CompositionTarget.Rendering += OnSplashRendering;
        else if (RenderShape == SpinnerRenderShape.Fireworks)
            CompositionTarget.Rendering += OnFireworkRendering;
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
        else if (RenderShape == SpinnerRenderShape.Chase)
            CompositionTarget.Rendering -= OnChaseRendering;
        else if (RenderShape == SpinnerRenderShape.Radar)
            CompositionTarget.Rendering -= OnRadarRendering;
        else if (RenderShape == SpinnerRenderShape.Progress)
            CompositionTarget.Rendering -= OnProgressRendering;
        else if (RenderShape == SpinnerRenderShape.Splash)
            CompositionTarget.Rendering -= OnSplashRendering;
        else if (RenderShape == SpinnerRenderShape.Fireworks)
            CompositionTarget.Rendering -= OnFireworkRendering;
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

    double _lastFadePercent = 0.2; // start fading when life is below this percentage (20% by default)
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
                if (lifeRatio > _lastFadePercent)
                {
                    dot.Opacity = 1.0; // full brightness
                }
                else
                {
                    double t = lifeRatio / _lastFadePercent; // normalize last 20%
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
                    double fade = (lifeRatio > _lastFadePercent ? 1.0 : (lifeRatio / _lastFadePercent) * (lifeRatio / _lastFadePercent));
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
                if (lifeRatio > _lastFadePercent)
                {
                    dot.Opacity = 1.0; // full brightness
                }
                else
                {
                    double t = lifeRatio / _lastFadePercent; // normalize last 20%
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
                    double fade = (lifeRatio > _lastFadePercent ? 1.0 : (lifeRatio / _lastFadePercent) * (lifeRatio / _lastFadePercent));
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
    public bool FallingUp { get; set; } = false;
    public bool FallingAutoReverse { get; set; } = false;
    public bool FallingScrambleX { get; set; } = true;  // re-mix X position when resetting
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
            double x = Random.Shared.NextDouble() * (ActualWidth - (DotSize / 2.0));
            double y = FallingUp ? ActualHeight - (Random.Shared.NextDouble() * DotSize) : 0;

            Ellipse dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = (double)i / (double)DotCount + 0.01 // fade each consecutive dot
            };

            var baseAccel = ((FallingBaseSpeed * 2.5) / 100.0);
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
            if (FallingUp)
            {
                // If this star hasn't reached the top yet, move it
                if (_twinks[i].Y > DotSize / 2)
                {
                    // Apply acceleration to velocity
                    _twinks[i].Velocity += _twinks[i].Acceleration;

                    // Move up by its speed factor, but clamp to top
                    if (FallingAcceleration)
                        _twinks[i].Y = Math.Min(_twinks[i].Y - _twinks[i].Velocity, ActualHeight - DotSize);
                    else
                        _twinks[i].Y = Math.Min(_twinks[i].Y - _twinks[i].SpeedFactor, ActualHeight - DotSize);

                    allAtBottom = false; // at least one is still falling
                }
                else // Clamp to top
                {
                    _twinks[i].Y = 0;
                }
            }
            else
            {
                // If this star hasn't reached the bottom yet, move it
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
                else // Clamp to bottom
                {
                    _twinks[i].Y = ActualHeight - DotSize;
                }
            }

            if (_twinks[i].X < 0)
                Debugger.Break(); // sanity check

            var dot = (UIElement)PART_Canvas.Children[i];
            var adjustedX = _twinks[i].X - (DotSize / 2.0); // don't overstep the right side boundary
            Canvas.SetLeft(dot, adjustedX);
            Canvas.SetTop(dot, _twinks[i].Y);
        }

        // If all stars are at the bottom, reset them together
        if (allAtBottom)
        {
            // Increment and check counter
            if (++_pauseCounter >= FallingFinishedPause)
            {
                if (FallingAutoReverse)
                    FallingUp = !FallingUp; // auto-reverse direction

                // Reset all dots together
                foreach (var twink in _twinks)
                {
                    twink.Y = FallingUp ? ActualHeight - (Random.Shared.NextDouble() * DotSize) : 0;
                    if (FallingScrambleX)
                        twink.X = Random.Shared.NextDouble() * (ActualWidth - (DotSize / 2.0)); // randomize X on next turn
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
                _stars[i].Y >= 0 && _stars[i].Y <= (ActualHeight - DotSize))
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
                // Instead of recreating arrays, just reset them in place.
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
    public bool FountainColorTransition { get; set; } = false;
    public bool FountainColorPaletteFire { get; set; } = false;
    public bool FountainColorTransitionEaseOut { get; set; } = true;
    public bool FountainFromBottom { get; set; } = true;
    public double FountainGravity { get; set; } = 18; // px/frame^2 pulling downward
    public int FountainBaseLife { get; set; } = 30;   // initial life in frames (will be randomized a bit)
    public Color FountainTransitionStartColor { get; set; } = Colors.White;
    public Color FountainTransitionMidColor { get; set; } = Colors.Yellow;
    public Color FountainTransitionEndColor { get; set; } = Colors.Firebrick;

    // Fiery palette
    List<Color> CoreColors = new() { Colors.White, Colors.Yellow, Colors.Orange, Colors.Red };
    List<Color> MidColors = new() { Colors.LightYellow, Colors.Orange, Colors.OrangeRed, Colors.DarkRed };
    List<Color> EdgeColors = new() { Colors.Gold, Colors.OrangeRed, Colors.DarkOrange, Colors.Black };

    void CreateFountain()
    {
        double originX = 0;
        double originY = 0;
        if (FountainFromBottom) // bottom center
        {
            originX = ActualWidth / 2.0;
            originY = ActualHeight;
        }
        else // top center
        {
            //fountainGravity = -0.18; // reverse gravity for top-down fountain
            originX = ActualWidth / 2.0;
            originY = 0;
        }

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
                X = originX,
                Y = originY,
                Opacity = 0.9,
                StartColor = FountainTransitionStartColor,
                MidColor = FountainTransitionMidColor,
                EndColor = FountainTransitionEndColor,
                InitialBrush = DotBrush,
                Life = 0,
                InitialLife = FountainBaseLife + Random.Shared.Next(60), // 1–2 seconds at 60fps
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
        if (!FountainFromBottom)
            angle = Math.PI / 2.0 + offset;
        #endregion

        // Launch speed (px/frame)
        double speed = FountainBaseSpeed + Random.Shared.NextDouble() * 5.0;

        star.X = originX;
        star.Y = originY;
        star.VX = Math.Cos(angle) * speed;
        star.VY = Math.Sin(angle) * speed; // negative (upwards), will arc with gravity
        star.Opacity = 1.0;
        star.InitialLife = FountainBaseLife + Random.Shared.Next(60); // 1–2 seconds at 60fps
        star.Life = 0;
        star.FadeRate = (FountainFadeRate / 100d) + Random.Shared.NextDouble() * 0.01; // vary fade slightly
        // NOTE: The initial color/brush is determined by DotBrush.
        star.StartColor = FountainTransitionStartColor;
        star.MidColor = FountainTransitionMidColor;
        star.EndColor = FountainTransitionEndColor;
    }


    double _spreadTime = 0;           // if using modulating spread angle
    double fountainDrag = 0.995;      // mild damping for smooth arcs
    double fountainBoundsMargin = 5;  // tolerance outside screen before recycle
    void OnFountainRendering(object? sender, EventArgs e)
    {
        if (_dots == null || _dots.Length == 0)
        {
            CreateFountain();
            return;
        }

        double originX = 0;
        double originY = 0;
        if (FountainFromBottom) // bottom center
        {
            originX = ActualWidth / 2.0;
            originY = ActualHeight;
        }
        else // top center
        {
            originX = ActualWidth / 2.0;
            originY = 0;
        }

        _spreadTime += 0.05; // adjust speed of oscillation

        for (int i = 0; i < _dots.Length; i++)
        {
            // Physics integration
            _stars[i].VY += FountainFromBottom ? (FountainGravity / 100d) : (FountainGravity / 100d) * -1d;  // gravity
            _stars[i].VX *= fountainDrag; // mild horizontal damping
            _stars[i].VY *= fountainDrag; // mild vertical damping
            _stars[i].X += _stars[i].VX;  // apply horizontal velocity
            _stars[i].Y += _stars[i].VY;  // apply vertical velocity

            // Fade: altitude-based plus soft edge fade near bounds
            double fade = _stars[i].FadeRate;
            double topFadeStart = ActualHeight * 0.25; // start fading after rising ~25% of height
            double altitude = originY - _stars[i].Y;   // how high above the origin
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

            //var dot = (UIElement)PART_Canvas.Children[i];
            var dot = (Ellipse)PART_Canvas.Children[i];

            _stars[i].Opacity = Math.Max(0, Math.Min(1.0, _stars[i].Opacity - fade));

            if (FountainColorTransition) // Transition color based on age
            {
                if (_stars[i].Life == 0)
                {
                    dot.Fill = _stars[i].InitialBrush; // reset
                }
                else // Color transition
                {
                    double t = 0;
                    double rawT = (double)_stars[i].Life / (double)_stars[i].InitialLife;
                    if (FountainColorTransitionEaseOut)
                    {
                        rawT = Math.Max(0, Math.Min(1, rawT)); // Clamp to [0,1] just in case
                        t = 1 - (1 - rawT) * (1 - rawT); // EaseOut quadratic
                    }
                    else
                    {
                        t = Math.Min(1.0, rawT * rawT); // EaseIn quadratic
                    }

                    // Determine the brush type and LERP colors accordingly
                    if (dot.Fill is RadialGradientBrush rgb)
                    {
                        //var t1 = LerpColor(rgb.GradientStops[0].Color, _stars[i].StartColor, t);
                        //var t2 = LerpColor(rgb.GradientStops[1].Color, _stars[i].MidColor, t);
                        //var t3 = LerpColor(rgb.GradientStops[2].Color, _stars[i].EndColor, t);
                        var t1 = LerpColor(_stars[i].StartColor, _stars[i].StartColor, t);
                        var t2 = LerpColor(_stars[i].MidColor, _stars[i].EndColor, t);
                        var t3 = LerpColor(_stars[i].EndColor, _stars[i].EndColor, t);
                        if (FountainColorPaletteFire) // Use a predefined palette
                        {
                            t1 = UpdateGradient(CoreColors, t);
                            t2 = UpdateGradient(MidColors, t);
                            t3 = UpdateGradient(EdgeColors, t);
                        }
                        //if (t < 0.29)
                        //{
                        //    t1 = LerpColor(_stars[i].StartColor, Colors.White, t);
                        //    t2 = LerpColor(_stars[i].StartColor, Colors.Gray, t);
                        //    t3 = LerpColor(_stars[i].StartColor, Colors.Black, t);
                        //}
                        //else if (t < 0.61)
                        //{
                        //    t1 = LerpColor(_stars[i].MidColor, Colors.White, t);
                        //    t2 = LerpColor(_stars[i].MidColor, Colors.Gray, t);
                        //    t3 = LerpColor(_stars[i].MidColor, Colors.Black, t);
                        //}
                        //else if (t < 1.01)
                        //{
                        //    t1 = LerpColor(_stars[i].EndColor, Colors.White, t);
                        //    t2 = LerpColor(_stars[i].EndColor, Colors.Gray, t);
                        //    t3 = LerpColor(_stars[i].EndColor, Colors.Black, t);
                        //}

                        dot.Fill = new RadialGradientBrush
                        {
                            ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
                            GradientOrigin = new Point(0.75, 0.25), // center
                            Center = new Point(0.5, 0.5),
                            RadiusX = 0.5,
                            RadiusY = 0.5,
                            GradientStops = new GradientStopCollection
                            {
                                new GradientStop(t1, 0.0), // inner
                                new GradientStop(t2, 0.36),
                                new GradientStop(t3, 1.0), // outer
                            }
                        };
                    }
                    else if (dot.Fill is LinearGradientBrush lgb)
                    {
                        //var t1 = LerpColor(lgb.GradientStops[0].Color, _stars[i].StartColor, t);
                        //var t2 = LerpColor(lgb.GradientStops[1].Color, _stars[i].MidColor, t);
                        //var t3 = LerpColor(lgb.GradientStops[2].Color, _stars[i].EndColor, t);
                        var t1 = LerpColor(_stars[i].StartColor, _stars[i].StartColor, t);
                        var t2 = LerpColor(_stars[i].MidColor, _stars[i].EndColor, t);
                        var t3 = LerpColor(_stars[i].EndColor, _stars[i].EndColor, t);
                        if (FountainColorPaletteFire) // Use a predefined palette
                        {
                            t1 = UpdateGradient(CoreColors, t);
                            t2 = UpdateGradient(MidColors, t);
                            t3 = UpdateGradient(EdgeColors, t);
                        }
                        dot.Fill = new LinearGradientBrush
                        {
                            ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
                            StartPoint = new System.Windows.Point(0, 0),
                            EndPoint = new System.Windows.Point(0, 1),
                            GradientStops = new GradientStopCollection
                            {
                                new GradientStop(t1, 0.0), // inner
                                new GradientStop(t2, 0.36),
                                new GradientStop(t3, 1.0), // outer
                            }
                        };
                    }
                    else if (dot.Fill is SolidColorBrush scb)
                    {
                        //var t1 = LerpColor(scb.Color, _stars[i].EndColor, t);
                        var t1 = LerpColor(_stars[i].StartColor, _stars[i].EndColor, t);
                        if (FountainColorPaletteFire) // Use a predefined palette
                        {
                            t1 = UpdateGradient(CoreColors, t);
                        }
                        dot.Fill = new SolidColorBrush(t1);
                    }
                }
                _stars[i].Life++;
            }

            dot.Opacity = _stars[i].Opacity;

            // Apply position
            Canvas.SetLeft(dot, _stars[i].X - DotSize / 2.0);
            Canvas.SetTop(dot, _stars[i].Y - DotSize / 2.0);

            // Recycle when done
            bool outOfBounds = _stars[i].X < -fountainBoundsMargin ||
                _stars[i].X > ActualWidth + fountainBoundsMargin ||
                _stars[i].Y > ActualHeight + fountainBoundsMargin; // fell below floor

            bool invisible = _stars[i].Opacity <= 0.0;

            if (outOfBounds || invisible)
            {
                ResetFountainParticle(_stars[i], originX, originY);
            }
        }
    }

    ChaseDot[] _dots2;
    void CreateChase(double radius, double centerX, double centerY)
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();

        _dots2 = new ChaseDot[DotCount];
        for (int i = 0; i < DotCount; i++)
        {
            double angle = i * Tau / DotCount;
            double x = centerX + radius * Math.Cos(angle);
            double y = centerY + radius * Math.Sin(angle);

            // Create one object for the visual element (Canvas)
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                //RenderTransformOrigin = new Point(0.5, 0.5)
            };

            // And create another object for state (calculations)
            _dots2[i] = new ChaseDot
            {
                BaseSize = dot.Width,
                Scale = 1.0,
                Index = i
            };

            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);
            PART_Canvas.Children.Add(dot);
        }
    }

    double _inflateTime = 0;  // time accumulator for animation
    public double ChasePulseSpeed { get; set; } = 3;  // animation speed
    public double ChasePulseSize { get; set; } = 9;   // grow factor
    public double ChaseRingRadius { get; set; } = 80; // ring size
    public int ChaseMode { get; set; } = 1;           // Sin/Cos/Asin/Acos
    public int ChaseTailLength { get; set; } = 0;     // zero ⇨ disabled (no tail)
    public bool ChaseClockwise { get; set; } = true;
    void OnChaseRendering(object? sender, EventArgs e)
    {
        if (_dots2 == null || _dots2.Length == 0)
        {
            CreateChase(DotSize / 2, ActualWidth / 2, ActualHeight / 2);
            return;
        }

        double radius = ChaseRingRadius;
        if (radius == 0)
        {   // Use a percentage of the control's size
            if (ActualHeight > ActualWidth)
                radius = (ActualWidth / 4);
            else if (ActualWidth > ActualHeight)
                radius = (ActualHeight / 4);
            else
                radius = (ActualHeight / 4);
        }

        _inflateTime += ChasePulseSpeed / 10.0;
        int dotCount = _dots2.Length;
        for (int i = 0; i < dotCount; i++)
        {
            var obj = _dots2[i];
            // Compute phase offset for round-robin
            double phase = ChaseClockwise ? (_inflateTime - i) % dotCount : (_inflateTime + i) % dotCount;
            // Normalize to [0,1]
            double t = (phase < 0 ? phase + dotCount : phase) / dotCount;

            // Pulse strength: closer to the leader ═ stronger
            double strength = Math.Max(0, Math.Cos(t * Math.PI * ChaseTailLength)); // Use a cosine falloff for smooth trailing

            #region [Experimental]
            //double anglePerDot = Tau / dotCount;
            //double leaderAngle = (_inflateTime * anglePerDot) % (Tau);
            //double dotAngle = i * anglePerDot;
            //double delta = (dotAngle - leaderAngle + Tau) % (Tau);
            //double normalized = delta / Tau;
            //double tailFraction = 0.3; // 30% of circle
            //if (normalized < tailFraction)
            //{
            //    double t2 = normalized / tailFraction;
            //    strength = 1.0 - t2; // linear falloff
            //}
            #endregion

            double pulse = 0;
            switch (ChaseMode)
            {
                case 2: pulse = Math.Cos(t * Math.PI); break;
                case 3: pulse = Math.Cos(t * Tau) + 1.0; break;
                case 4: pulse = Math.Acos(t * Math.PI); break;
                case 5: pulse = Math.Acosh(t * Math.PI); break;
                case 6: pulse = Math.Asin(t * Math.PI); break;
                case 7: pulse = Math.Asinh(t * Math.PI); break;
                // standard mode (use sine wave pulse, EaseInOut)
                default: pulse = Math.Sin(t * Math.PI); break;
            }

            if (ChaseTailLength > 0)
                obj.Scale = 1.0 + strength * (ChasePulseSize / 10.0);
            else // Scale between 1.0 and InflatePulseSize
                obj.Scale = 1.0 + pulse * (ChasePulseSize / 10.0);

            //var dot = (UIElement)PART_Canvas.Children[i];
            var dot = (Ellipse)PART_Canvas.Children[i];

            double size = obj.BaseSize * obj.Scale; // Apply scale to dot size
            double opacity = 0.3 + strength * 0.7; // Keep faint at tail end

            dot.Width = size;
            dot.Height = size;
            dot.Opacity = opacity;

            // Because size changed, re-center the dot
            double angle = (double)i * Tau / (double)dotCount;
            double x = (ActualWidth / 2) + radius * Math.Cos(angle);
            double y = (ActualHeight / 2) + radius * Math.Sin(angle);

            Canvas.SetLeft(dot, x - size / 2.0);
            Canvas.SetTop(dot, y - size / 2.0);
        }
    }


    double _sweepAngle = 0.0;                         // in radians
    const double _decay = 0.75;                       // multiplicative decay each frame
    List<RadarBlip> _blips = new List<RadarBlip>();   // radar contacts (blips)
    public double RadarExciteWidth { get; set; } = 2; // (0.2 ⇨ radians) angular width of excitation band
    public double RadarSweepSpeed { get; set; } = 6;  // 0.06 ⇨ radians per frame
    public bool RadarClockwise { get; set; } = true;
    public bool RadarUseCone { get; set; } = true;
    public bool RadarRingBlips { get; set; } = false;
    public bool RadarBlipsTravel { get; set; } = false;
    public double RadarBeamWidth { get; set; } = 6;  // 0.6 ⇨ radians
    public double RadarRadius { get; set; } = 80;    // ring size (zero ⇨ auto calculate)
    public Brush RadarBeamBrush { get; set; } = new SolidColorBrush(Color.FromRgb(30, 255, 120)); // green
    public Brush RadarBlipBrush { get; set; } = new SolidColorBrush(Color.FromRgb(30, 255, 120)); // green

    void TrySpawnMovingBlip(double centerX, double centerY, double radius)
    {
        if (PART_Canvas == null)
            return;

        if (Random.Shared.NextDouble() <= 0.02) // ~2% chance per frame
        {
            // Pick polar coordinate inside circle (radius * 0.7 ⇨ keep inside outer edge)
            double r = (radius * 0.7) * Math.Sqrt(Random.Shared.NextDouble());

            // Random position inside circle
            //double angle = Random.Shared.NextDouble() * Tau;

            // Cone-adjacent blip placement
            double spread = 0.15; // radians (~9° cone around sweep)
            double angle = _sweepAngle + ((Random.Shared.NextDouble() - 0.5) * 2 * spread);

            double x = centerX + r * Math.Cos(angle);
            double y = centerY + r * Math.Sin(angle);

            // Small random drift velocity
            double vx = (Random.Shared.NextDouble() - 0.5) * 0.5;
            double vy = (Random.Shared.NextDouble() - 0.5) * 0.5;

            var dot = new Ellipse
            {
                Width = DotSize * 1.2,
                Height = DotSize * 1.2,
                Fill = RadarBlipBrush,
                Opacity = 0.0
            };

            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);
            PART_Canvas.Children.Add(dot);

            _blips.Add(new RadarBlip
            {
                Dot = dot,
                X = x,
                Y = y,
                VX = vx,
                VY = vy,
                Lifetime = 100 + Random.Shared.Next(100), // 2-5 seconds
                Age = 0
            });
        }
    }

    void UpdateMovingBlips(double centerX, double centerY, double radius)
    {
        if (PART_Canvas == null || _blips == null || _blips.Count == 0)
            return;

        for (int i = _blips.Count - 1; i >= 0; i--)
        {
            var blip = _blips[i];
            blip.Age++;

            // Move
            blip.X += blip.VX;
            blip.Y += blip.VY;

            // Keep inside radar circle
            double dx = blip.X - centerX;
            double dy = blip.Y - centerY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // Bounce back inward
            //if (dist > radius)
            //{
            //    double factor = radius / dist;
            //    blip.X = centerX + dx * factor;
            //    blip.Y = centerY + dy * factor;
            //    blip.VX *= -0.5; blip.VY *= -0.5;
            //}

            // How close to the border (0 = center, 1 = edge)
            double normalized = dist / radius;

            // Fade factor: full visible until 80% of radius, then fade to 0 at edge
            double borderFade = 1.0;
            if (normalized > 0.8)
            {
                double tf = (normalized - 0.8) / 0.2; // 0 at 80%, 1 at 100%
                borderFade = 1.0 - Math.Min(1.0, tf);
            }

            // Fade in/out based on age
            double t = blip.Age / blip.Lifetime;
            double opacity = (t < 0.1) ? t * 10 : (t > 0.9 ? (1 - t) * 10 : 1.0);
            //blip.Dot.Opacity = opacity;
            blip.Dot.Opacity = opacity * borderFade;

            // Position
            Canvas.SetLeft(blip.Dot, blip.X - blip.Dot.Width / 2);
            Canvas.SetTop(blip.Dot, blip.Y - blip.Dot.Height / 2);

            // Remove if expired
            if (blip.Age >= blip.Lifetime)
            {
                PART_Canvas.Children.Remove(blip.Dot);
                _blips.RemoveAt(i);
            }
        }
    }

    void TrySpawnStaticBlip(double centerX, double centerY, double radius)
    {
        if (PART_Canvas == null)
            return;

        if (Random.Shared.NextDouble() <= 0.02) // ~2% chance per frame
        {
            // Pick polar coordinate inside circle (radius * 0.75 ⇨ keep inside outer edge)
            double r = (radius * 0.75) * Math.Sqrt(Random.Shared.NextDouble()); // sqrt for uniform distribution

            // Random blip placement
            //double angle = Random.Shared.NextDouble() * Tau;

            // Cone-adjacent blip placement
            double spread = 0.15; // radians (~9° cone around sweep)
            double angle = _sweepAngle + ((Random.Shared.NextDouble() - 0.5) * 2 * spread);

            double x = centerX + r * Math.Cos(angle);
            double y = centerY + r * Math.Sin(angle);

            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = RadarBlipBrush,
                Opacity = 0.8
            };

            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);
            PART_Canvas.Children.Add(dot);

            _blips.Add(new RadarBlip
            {
                Dot = dot,
                X = x,
                Y = y,
                Lifetime = 30 + Random.Shared.Next(60), // 1–2 seconds (frame based)
                Age = 0
            });
        }
    }

    void UpdateStaticBlips()
    {
        if (PART_Canvas == null || _blips == null || _blips.Count == 0)
            return;

        for (int i = _blips.Count - 1; i >= 0; i--)
        {
            var blip = _blips[i];
            blip.Age++; // age each blip per frame
            double t = blip.Age / blip.Lifetime;
            blip.Dot.Opacity = 1.0 - t; // fade out with age
            double size = (DotSize * 1.5) * (1.0 - 0.5 * t); // shrink as it fades
            blip.Dot.Width = size;
            blip.Dot.Height = size;
            Canvas.SetLeft(blip.Dot, blip.X - size / 2);
            Canvas.SetTop(blip.Dot, blip.Y - size / 2);
            if (blip.Age >= blip.Lifetime)
            {
                PART_Canvas.Children.Remove(blip.Dot);
                _blips.RemoveAt(i);
            }
        }
    }

    void CreateRadar(double radius, double centerX, double centerY)
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();

        _dots2 = new ChaseDot[DotCount];

        for (int i = 0; i < DotCount; i++)
        {
            double angle = i * (Tau / DotCount);

            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = 0.8
            };

            double x = centerX + radius * Math.Cos(angle);
            double y = centerY + radius * Math.Sin(angle);
            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);
            PART_Canvas.Children.Add(dot);

            _dots2[i] = new ChaseDot
            {
                Angle = angle,
                BaseSize = dot.Width,
                Intensity = 0.0
            };
        }
    }

    void OnRadarRendering(object? sender, EventArgs e)
    {
        if (_dots2 == null || _dots2.Length == 0)
        {
            CreateRadar(DotSize / 2, ActualWidth / 2, ActualHeight / 2);
            return;
        }

        // Advance sweep angle
        double dir = RadarClockwise ? -1.0 : 1.0;
        _sweepAngle = (_sweepAngle + dir * (RadarSweepSpeed / 100)) % Tau;
        if (_sweepAngle < 0)
            _sweepAngle += Tau;

        double radius = RadarRadius;
        if (radius == 0)
        {   // Use a percentage of the control's size
            if (ActualHeight > ActualWidth)
                radius = (ActualWidth / 4);
            else if (ActualWidth > ActualHeight)
                radius = (ActualHeight / 4);
            else
                radius = (ActualHeight / 4);
        }

        // Iterate through element visuals
        for (int i = 0; i < _dots2.Length; i++)
        {
            var obj = _dots2[i];

            // Angular distance from sweep
            double delta = Math.Abs(WrapAngle(obj.Angle - _sweepAngle));

            // Excitation: strong near the arm, falls off by angle
            double excite = 0.0;
            if (delta <= (RadarExciteWidth / 10d))
            {
                double t = delta / (RadarExciteWidth / 10d);    // 0 at arm, 1 at edge
                excite = 1.0 - t;                  // linear falloff
                // Optional: use a sharper curve
                // excite = 1.0 - t * t;           // quadratic
                // excite = Math.Exp(-3.0 * t);    // exponential
            }

            // Apply decay, then add excitation
            obj.Intensity = obj.Intensity * _decay + excite * (1.0 - _decay);

            double scale = 0;
            double opacity = 0;
            if (RadarRingBlips)
            {
                // For blip effect, combine intensity with blip intensity
                double combined = Math.Max(obj.Intensity, obj.BlipIntensity);
                // Map intensity to visuals
                scale = 1.2 + combined * 0.9;    // size boost
                opacity = 0.5 + combined * 0.9;  // glow boost
            }
            else
            {
                // Map intensity to visuals
                scale = 1.2 + obj.Intensity * 0.9;    // size boost
                opacity = 0.5 + obj.Intensity * 0.9;  // glow boost
            }
            double size = obj.BaseSize * scale;

            //var dot = (UIElement)PART_Canvas.Children[i];
            // More versatile if working with color/brush modification
            var dot = (Ellipse)PART_Canvas.Children[i];

            dot.Width = size;
            dot.Height = size;
            dot.Opacity = opacity;

            // tail/dim ⇨ lead/bright
            //Color clr = LerpColor(Color.FromRgb(0, 60, 0), Color.FromRgb(30, 255, 120), obj.Intensity);
            //var brush = dot.Fill as SolidColorBrush;
            //if (brush != null) { brush.Color = clr; }

            if (RadarRingBlips)
            {
                if (Random.Shared.NextDouble() < 0.002) // ~0.2% chance per frame
                    obj.BlipIntensity = 1.0; // full flare

                // Decay blip over time
                obj.BlipIntensity *= 0.91;
            }

            // Keep centered (size changes)
            double x = (ActualWidth / 2) + radius * Math.Cos(obj.Angle);
            double y = (ActualHeight / 2) + radius * Math.Sin(obj.Angle);
            Canvas.SetLeft(dot, x - size / 2);
            Canvas.SetTop(dot, y - size / 2);
        }

        // Draw the sweep arm
        DrawSweepArm(ActualWidth / 2, ActualHeight / 2, radius, _sweepAngle);

        // Possible blip spawn inside radar diameter
        if (!RadarRingBlips)
        {
            if (RadarBlipsTravel)
            {
                TrySpawnMovingBlip(ActualWidth / 2, ActualHeight / 2, radius);
                UpdateMovingBlips(ActualWidth / 2, ActualHeight / 2, radius);
            }
            else
            {
                TrySpawnStaticBlip(ActualWidth / 2, ActualHeight / 2, radius);
                UpdateStaticBlips();
            }
        }
    }

    void DrawSweepArm(double centerX, double centerY, double radius, double angle)
    {
        if (RadarUseCone)
        {
            if (_armBeam == null)
                CreateSweepBeam(centerX, centerY, radius);

            UpdateSweepBeam(centerX, centerY, radius, angle);
        }
        else
        {
            if (_armLine == null)
                CreateSweepArm(centerX, centerY, radius);

            double x2 = centerX + radius * Math.Cos(angle);
            double y2 = centerY + radius * Math.Sin(angle);
            _armLine.X1 = centerX; _armLine.X2 = x2;
            _armLine.Y1 = centerY; _armLine.Y2 = y2;
        }
    }

    Line _armLine;
    void CreateSweepArm(double centerX, double centerY, double radius)
    {
        if (PART_Canvas == null)
            return;

        _armLine = new Line
        {
            Stroke = RadarBeamBrush,
            Opacity = 0.8,
            StrokeThickness = DotSize,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };
        PART_Canvas.Children.Remove(_armLine);
        PART_Canvas.Children.Add(_armLine);
        UpdateSweepArm(centerX, centerY, radius, 0);
    }

    void UpdateSweepArm(double centerX, double centerY, double radius, double angle)
    {
        if (_armLine == null)
            return;

        double x2 = centerX + radius * Math.Cos(angle);
        double y2 = centerY + radius * Math.Sin(angle);
        _armLine.X1 = centerX; _armLine.X2 = x2;
        _armLine.Y1 = centerY; _armLine.Y2 = y2;
    }

    System.Windows.Shapes.Path _armBeam;
    void CreateSweepBeam(double centerX, double centerY, double radius)
    {
        if (PART_Canvas == null)
            return;

        _armBeam = new System.Windows.Shapes.Path
        {
            Fill = RadarBeamBrush,
            StrokeThickness = 0,
            Stroke = DotBrush,
            Opacity = 0.8,
            IsHitTestVisible = false
        };
        PART_Canvas.Children.Remove(_armBeam);
        PART_Canvas.Children.Add(_armBeam);
        UpdateSweepBeam(centerX, centerY, radius, 0.0);
    }

    void UpdateSweepBeam(double cx, double cy, double r, double angle)
    {
        if (_armBeam == null)
            return;

        double a1 = angle - (RadarBeamWidth / 10d) * 0.5;
        double a2 = angle + (RadarBeamWidth / 10d) * 0.5;

        // Create a pie-slice geometry
        Point p1 = new Point(cx + r * Math.Cos(a1), cy + r * Math.Sin(a1));
        Point p2 = new Point(cx + r * Math.Cos(a2), cy + r * Math.Sin(a2));
        var fig = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };
        fig.Segments.Add(new LineSegment(p1, true));
        fig.Segments.Add(new ArcSegment(p2, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(new Point(cx, cy), true));

        _armBeam.Data = new PathGeometry(new[] { fig });
    }


    bool _progressEaseEdge = true;
    double _progressMinOpacity = 0.2;
    double _sweepTime = 0.0;
    public int ProgressValue { get; set; } = 0;
    public double ProgressSpacing { get; set; } = 10;
    public double ProgressSweepSpeed { get; set; } = 16;    // controls how fast the highlight moves
    public double ProgressHighlightWidth { get; set; } = 3; // how many dots wide the highlight is
    public bool ProgressIndeterminate { get; set; } = false; // if false, uses _progress value (0..100)

    ProgressDot[] _dots3;
    void CreateProgressDots(double spacing)
    {
        if (PART_Canvas == null)
            return;

        PART_Canvas.Children.Clear();

        _dots3 = new ProgressDot[DotCount];

        // total width of all dots + spacing between them
        double totalWidth = (DotCount - 1) * spacing;

        // left-most starting X so the row is centered
        double startX = (ActualWidth / 2.0) - totalWidth / 2.0;
        double startY = ActualHeight / 2.0;

        for (int i = 0; i < DotCount; i++)
        {
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = _progressMinOpacity
            };

            double x = startX + i * spacing;
            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, startY - dot.Height / 2);
            PART_Canvas.Children.Add(dot);

            _dots3[i] = new ProgressDot
            {
                X = x - dot.Width / 2,
                Y = startY - dot.Height / 2,
                Dot = dot,
                BaseSize = dot.Width,
                Index = i
            };
        }
    }

    void OnProgressRendering(object? sender, EventArgs e)
    {
        if (_dots3 == null || _dots3.Length == 0)
        {
            CreateProgressDots(ProgressSpacing);
            return;
        }

        _sweepTime += (ProgressSweepSpeed / 100.0);

        // Position of the highlight center (wraps around)
        double highlightCenter = (_sweepTime % _dots3.Length);

        #region [Classic fill mode]
        if (ProgressValue > 100)
            ProgressValue = 0;
        // Normalize: if progress > 1, assume percent (0..100)
        double p = (double)ProgressValue > 1.0 ? (double)ProgressValue / 100.0 : (double)ProgressValue;
        // Clamp to [0,1]
        p = Math.Min(1.0, Math.Max(0.0, p));
        // Map to dot index domain
        double scaled = p * DotCount;
        // Fully filled dots count and fractional fill on the next dot
        int filledCount = (int)Math.Floor(scaled);
        double frac = scaled - filledCount; // 0..1
        #endregion

        for (int i = 0; i < _dots3.Length; i++)
        {
            var pd = _dots3[i];

            if (ProgressIndeterminate)
            {
                // Distance from highlight center
                double dist = Math.Abs(i - highlightCenter);
                // Wrap around edges
                if (dist > _dots3.Length / 2.0)
                    dist = _dots3.Length - dist;
                // Strength is one at center, fades with distance
                double strength = Math.Max(0, 1.0 - dist / ProgressHighlightWidth);
                // Map to opacity
                pd.Dot.Opacity = 0.2 + strength * 0.9;
            }
            else // classic fill mode
            {
                if (p <= 0.0)
                {   // All baseline at start
                    pd.Dot.Opacity = _progressMinOpacity;
                }
                else if (p >= 1.0)
                {   // All full at completion
                    pd.Dot.Opacity = 1.0;
                }
                else if (i < filledCount)
                {   // Fully filled dots to the left of the edge
                    pd.Dot.Opacity = 1.0;
                }
                else if (i == filledCount)
                {   // Edge dot: partial fill
                    double t = frac;
                    // Optional easing for a smoother ramp
                    if (_progressEaseEdge)
                    {   // Smoothstep: t^2 * (3 - 2t)
                        t = t * t * (3.0 - 2.0 * t);
                    }
                    pd.Dot.Opacity = _progressMinOpacity + t * (1.0 - _progressMinOpacity);
                }
                else
                {   // Remaining dots: baseline
                    pd.Dot.Opacity = _progressMinOpacity;
                }
            }

            //var dot = (UIElement)PART_Canvas.Children[i];
            var dot = (Ellipse)PART_Canvas.Children[i];
            dot.Opacity = pd.Dot.Opacity;
            Canvas.SetLeft(dot, pd.X);
            Canvas.SetTop(dot, pd.Y);
        }
    }



    bool _addSplashWind = false;
    double _rainAngleDegrees = 81; // 95 = slight right-leaning down
    // Optional gentle wind (small horizontal acceleration)
    double _windAX = 0.03;
    double _gravityAY = 0.01; // keep 0 if you want constant speed rain; set e.g. 0.05 to accelerate

    List<FallingDot> _falling;
    List<SplashParticle> _splash;
    public int SplashBaseCount { get; set; } = 4;
    public double SplashConeDegrees { get; set; } = 0; // zero ⇨ use natural cone
    public double SplashBaseSpeed { get; set; } = 4.0;
    public double SplashGravity { get; set; } = 24; // 0.24
    public double SplashBaseLife { get; set; } = 6.0; // base lifetime in frames for splash particles
    public bool SplashExplode { get; set; } = false;
    public Brush SplashBrush { get; set; } = new SolidColorBrush(Color.FromRgb(30, 100, 250)); // blue
    void CreateSplashDots(int count = 0)
    {
        if (PART_Canvas == null)
            return;

        if (_falling == null)
            _falling = new List<FallingDot>();
        if (_splash == null)
            _splash = new List<SplashParticle>();
        if (count <= 0)
            count = DotCount;

        // Constrain to middle 80% of control's width for spawning
        double margin = ActualWidth * 0.1;
        double usableWidth = ActualWidth * 0.8;

        // Wind parameters
        double speedMin = 2.0;
        double speedMax = 4.0;
        double angleJitterDeg = 3.0;

        for (int i = 0; i < count; i++)
        {
            // Slight randomization around _rainAngleDegrees to avoid perfect uniformity
            double jitter = (Random.Shared.NextDouble() * 2.0 - 1.0) * angleJitterDeg;
            double speed = speedMin + Random.Shared.NextDouble() * (speedMax - speedMin);
            double angle = _rainAngleDegrees + jitter;
            var (vx, vy) = VelocityFromAngle(speed, angle);

            double x = margin + Random.Shared.NextDouble() * usableWidth;
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = 1.0
            };
            Canvas.SetLeft(dot, x - dot.Width / 2.0);
            Canvas.SetTop(dot, 0.0); //Canvas.SetTop(dot, -dot.Height);
            PART_Canvas.Children.Add(dot);
            _falling.Add(new FallingDot
            {
                Dot = dot,
                X = x, Y = 0.0, //Y = -dot.Height,
                VX = vx, VY = vy,
                VelocityY = SplashBaseSpeed + Random.Shared.NextDouble() * 3.0
            });
        }
    }

    void OnSplashRendering(object? sender, EventArgs e)
    {
        if (_falling == null || _splash == null)
        {
            CreateSplashDots(DotCount);
            return;
        }

        // We spawn new dots based on how many crossed the threshold (85% down the control)
        int crossedCount = 0;
        double thresholdY = ActualHeight * 5.0 / 6.0;

        // Update falling dots
        for (int i = _falling.Count - 1; i >= 0; i--)
        {
            var fd = _falling[i];
            double prevY = fd.Y;

            if (_addSplashWind)
            {
                // Slight wind effect
                fd.VX += _windAX;
                fd.VY += _gravityAY;
                fd.X += fd.VX;
                fd.Y += fd.VY;
            }
            else // no wind effect
            {
                fd.Y += fd.VelocityY;
            }

            Canvas.SetLeft(fd.Dot, fd.X - fd.Dot.Width / 2.0);
            Canvas.SetTop(fd.Dot, fd.Y);

            // Cull if dot goes off-screen horizontally (if wind enabled)
            if (fd.X < -fd.Dot.Width || fd.X > ActualWidth + fd.Dot.Width)
            {
                PART_Canvas.Children.Remove(fd.Dot);
                _falling.RemoveAt(i);
                if (crossedCount < DotCount) 
                    crossedCount++;
                continue;
            }

            // Count if just crossed 85% height
            if (prevY < thresholdY && fd.Y >= thresholdY && _falling.Count < (DotCount + 1))
                crossedCount++;

            // Cull if dot goes off-screen vertically
            if (fd.Y >= ActualHeight - fd.Dot.Height)
            {
                PART_Canvas.Children.Remove(fd.Dot);
                _falling.RemoveAt(i);
                SpawnSplash(fd.X, ActualHeight - (DotSize/2.0));
            }
        }

        // Update splash particles
        for (int i = _splash.Count - 1; i >= 0; i--)
        {
            var sp = _splash[i];
            sp.Age++;
            sp.X += sp.VX;
            sp.Y += sp.VY;
            sp.VY += SplashGravity / 100.0; // add gravity each frame
            double t = sp.Age / sp.Lifetime;
            double size = SplashExplode ? sp.Dot.Width * (1.0 - t) : (DotSize * 1.25) * (1.0 - t);
            sp.Dot.Opacity = 1.0 - t;
            sp.Dot.Width = size;
            sp.Dot.Height = size;
            Canvas.SetLeft(sp.Dot, sp.X - size / 2.0);
            Canvas.SetTop(sp.Dot, sp.Y - size / 2.0);
            if (sp.Age >= sp.Lifetime)
            {
                PART_Canvas.Children.Remove(sp.Dot);
                _splash.RemoveAt(i);
            }
        }

        // Randomly spawn new falling dots
        //if (Random.Shared.NextDouble() < 0.02)
        //    CreateSplashDots(Random.Shared.Next(1, DotCount));

        // Spawn new dots equal to how many crossed threshold
        if (_falling.Count == 0 || (crossedCount > 0 && crossedCount < DotCount))
            CreateSplashDots(crossedCount);
    }

    void SpawnSplash(double x, double y)
    {
        double upwardBoost = 1.05; // upward boost in velocity Y
        int count = SplashBaseCount + Random.Shared.Next(SplashBaseCount);

        for (int i = 0; i < count; i++)
        {
            #region [angle/speed experiments]
            double angle = 0;
            double speed = 1.25 + Random.Shared.NextDouble() * 3.0;

            // 180° random
            //angle = Random.Shared.NextDouble() * Tau;

            // 90° random
            //angle = (-Math.PI / 2.0) + (Random.Shared.NextDouble() - 0.5) * Math.PI / 2.0;

            // Bias toward upward by squaring a random
            //double t = Random.Shared.NextDouble();
            //angle = -Math.PI / 2 + (t * t - 0.5) * Math.PI;

            if (IsZero(SplashConeDegrees)) // natural splash cone
            {
                angle = Random.Shared.NextDouble() * Tau;
            }
            else // user defined splash cone
            {
                double coneCenter = -Math.PI / 2;
                double coneWidth = DegreesToRadians(SplashConeDegrees);

                // Pick a random angle within the cone
                double halfWidth = coneWidth / 2;
                angle = coneCenter + (Random.Shared.NextDouble() * coneWidth - halfWidth);
            }

            double vx = Math.Cos(angle) * speed;
            double vy = -Math.Abs(Math.Sin(angle) * speed * upwardBoost);
            #endregion

            var dot = new Ellipse
            {
                Width = SplashExplode ? DotSize * 8.0 : DotSize,
                Height = SplashExplode ? DotSize * 8.0 : DotSize,
                Fill = SplashBrush,
                Opacity = 1.0
            };
            Canvas.SetLeft(dot, x - dot.Width / 2.0);
            Canvas.SetTop(dot, y - dot.Height / 2.0);
            PART_Canvas.Children.Add(dot);
            _splash.Add(new SplashParticle
            {
                Dot = dot,
                X = x, Y = y,
                VX = vx, // VX = Math.Cos(angle) * speed,
                VY = vy, // VY = -Math.Abs(Math.Sin(angle) * speed),
                Age = 0,
                Lifetime = SplashBaseLife + Random.Shared.Next(30) // frames
            });
        }
    }



    List<FireworkDot> _fireworks;
    public int FireworkParticleCount { get; set; } = 5;
    public double FireworkBaseSpeed { get; set; } = 3.0;
    public double FireworkExplosionSpeed { get; set; } = 1.0;
    public double FireworkGravity { get; set; } = 1.9;
    public int FireworkLifetime { get; set; } = 30;
    public Brush FireworkBrush1 { get; set; } = new SolidColorBrush(Color.FromRgb(250, 200, 10));
    public Brush FireworkBrush2 { get; set; } = new SolidColorBrush(Color.FromRgb(10, 250, 160));
    public Brush FireworkBrush3 { get; set; } = new SolidColorBrush(Color.FromRgb(10, 160, 250));
    void CreateFireworkDots()
    {
        if (_fireworks == null)
            _fireworks = new List<FireworkDot>();
        if (_splash == null)
            _splash = new List<SplashParticle>();

        // Constrain to middle 60% of control's width for spawning
        double margin = ActualWidth * 0.2;
        double usableWidth = ActualWidth * 0.6;

        for (int i = 0; i < DotCount; i++)
        {
            double x = margin + Random.Shared.NextDouble() * usableWidth;
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = 1.0
            };
            Canvas.SetLeft(dot, x - dot.Width / 2.0);
            Canvas.SetTop(dot, ActualHeight - dot.Height); // start at bottom
            PART_Canvas.Children.Add(dot);

            // Launch upward with some velocity
            double vy = -(FireworkBaseSpeed + Random.Shared.NextDouble() * FireworkBaseSpeed); // upward
            double vx = (Random.Shared.NextDouble() - 0.5) * 2.0; // sideways drift

            // Random apex height (somewhere between 30% and 60% of canvas height)
            //double apexY = ActualHeight * (0.3 + Random.Shared.NextDouble() * 0.3);

            // General pattern
            //double minRatio = 0.2; // lower bound (20% of height)
            //double maxRatio = 0.4; // upper bound (40% of height)
            //// Apex between 20% and 40% of height
            //double apexY = ActualHeight * (minRatio + Random.Shared.NextDouble() * (maxRatio - minRatio));

            if (FireworkGravity <= 0)
                FireworkGravity = 1;

            // Gravity strength (should match update loop)
            double g = (FireworkGravity / 10.0);

            /*  Launch velocity determines apex height
             *   ⮞ Faster launches ═ higher arcs
             *   ⮞ Slower launches ═ lower arcs
             */
            // Physics-based apex offset
            double apexOffset = (vy * vy) / (2.0 * g);
            // Apex Y = starting Y - offset
            double apexY = (ActualHeight - (dot.Height + 1.0)) - apexOffset;

            // Clamp into a configurable band
            double minY = ActualHeight * 0.1; // 10% from top
            double maxY = ActualHeight * 0.4; // 40% from top
            apexY = Math.Min(Math.Max(apexY, minY), maxY);

            // Add a little randomness so not all arcs are identical
            apexY *= (0.9 + Random.Shared.NextDouble() * 0.2);

            _fireworks.Add(new FireworkDot
            {
                Dot = dot,
                X = x, Y = ActualHeight - dot.Height,
                VX = vx, VY = vy,
                ApexY = apexY
            });
        }
    }

    void OnFireworkRendering(object? sender, EventArgs e)
    {
        if (_fireworks == null || _fireworks.Count == 0)
        {
            CreateFireworkDots();
            return;
        }

        // Update every dot's position and velocity
        for (int i = _fireworks.Count - 1; i >= 0; i--)
        {
            var ld = _fireworks[i];
            // Apply velocity
            ld.X += ld.VX; ld.Y += ld.VY;
            // Gravity pulls down
            ld.VY += (FireworkGravity / 10.0);
            Canvas.SetLeft(ld.Dot, ld.X - ld.Dot.Width / 2);
            Canvas.SetTop(ld.Dot, ld.Y);
            // Check if reached apex (velocity downward, or passed random apexY)
            if (ld.VY >= 0 || ld.Y <= ld.ApexY)
            {
                PART_Canvas.Children.Remove(ld.Dot);
                _fireworks.RemoveAt(i);
                SpawnExplosion(ld.X, ld.Y);
            }
        }

        UpdateExplosionParticles();
    }

    Brush? selected;
    void SpawnExplosion(double x, double y)
    {
        int count = FireworkParticleCount + Random.Shared.Next(FireworkParticleCount);
        selected = Random.Shared.NextDouble() > 0.65 ? FireworkBrush1 : Random.Shared.NextDouble() > 0.49 ? FireworkBrush2 : FireworkBrush3;
        for (int i = 0; i < count; i++)
        {
            double angle = Random.Shared.NextDouble() * Tau;
            double speed = FireworkExplosionSpeed + Random.Shared.NextDouble() * 4.0;
            var dot = new Ellipse
            {
                Width = (DotSize / 2.0) + Random.Shared.NextDouble() * DotSize,
                Height = (DotSize / 2.0) + Random.Shared.NextDouble() * DotSize,
                Fill = selected,
                Opacity = 1.0
            };
            Canvas.SetLeft(dot, x - dot.Width / 2.0);
            Canvas.SetTop(dot, y - dot.Height / 2.0);
            PART_Canvas.Children.Add(dot);
            _splash.Add(new SplashParticle
            {
                Dot = dot,
                X = x, Y = y,
                VX = Math.Cos(angle) * speed, VY = Math.Sin(angle) * speed,
                Age = 0, Lifetime = FireworkLifetime + Random.Shared.Next(FireworkLifetime)
            });
        }
    }

    void UpdateExplosionParticles()
    {
        double marginX = ActualWidth * 0.1;
        double marginY = ActualHeight * 0.1;

        double leftBound = marginX;
        double rightBound = ActualWidth - marginX;
        double topBound = marginY;
        double bottomBound = ActualHeight - marginY;

        for (int i = _splash.Count - 1; i >= 0; i--)
        {
            var sp = _splash[i];
            sp.Age++;

            // Apply velocity
            sp.X += sp.VX;
            sp.Y += sp.VY;

            // Apply gravity
            sp.VY += (FireworkGravity / 10.0);

            // Normalize lifetime progression
            double t = sp.Age / sp.Lifetime;

            // Check if particle is outside boundary
            //bool outOfBounds = sp.X < leftBound || sp.X > rightBound || sp.Y < topBound || sp.Y > bottomBound;
            //if (outOfBounds) { t = 1.0; } // Force fade faster
            //sp.Dot.Opacity = 1.0 - t;

            #region [Gradual edge fade]
            double edgeFade = 1.0; // multiplier
            if (sp.X < leftBound)
                edgeFade = (sp.X / leftBound); // 1 ⇨ boundary, 0 ⇨ edge
            else if (sp.X > rightBound)
                edgeFade = ((ActualWidth - sp.X) / marginX);
            if (sp.Y < topBound)
                edgeFade = Math.Min(edgeFade, (sp.Y / topBound));
            else if (sp.Y > bottomBound)
                edgeFade = Math.Min(edgeFade, ((ActualHeight - sp.Y) / marginY));

            edgeFade = Math.Max(0, Math.Min(1, edgeFade));
            #endregion

            // Combine lifetime fade and edge fade
            double opacity = (1.0 - t) * edgeFade;
            sp.Dot.Opacity = opacity;

            double size = Math.Max(1, DotSize * (1.0 - t));
            sp.Dot.Width = size;
            sp.Dot.Height = size;

            Canvas.SetLeft(sp.Dot, sp.X - size / 2.0);
            Canvas.SetTop(sp.Dot, sp.Y - size / 2.0);

            //if (sp.Age >= sp.Lifetime || t >= 1.0)
            //{
            //    PART_Canvas.Children.Remove(sp.Dot);
            //    _splash.RemoveAt(i);
            //}

            // Remove particle if fully faded or lifetime exceeded
            if (sp.Age >= sp.Lifetime || opacity <= 0.01)
            {
                PART_Canvas.Children.Remove(sp.Dot);
                _splash.RemoveAt(i);
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
    /// Picks the next color in a palette based on progress [0,1].<br/>
    /// </summary>
    static Color UpdateGradient(List<Color> palette, double progress)
    {
        int segments = palette.Count - 1;
        double segLength = 1.0 / segments;

        int segIndex = Math.Min(segments - 1, (int)(progress / segLength));
        double localT = (progress - segIndex * segLength) / segLength;

        Color c1 = palette[segIndex];
        Color c2 = palette[segIndex + 1];
        return LerpColor(c1, c2, localT);
    }

    /// <summary>
    /// Picks the next color in a palette based on progress [0,1].<br/>
    /// </summary>
    void UpdateGradient(GradientStop stop, List<Color> palette, double progress)
    {
        int segments = palette.Count - 1;
        double segLength = 1.0 / segments;

        int segIndex = Math.Min(segments - 1, (int)(progress / segLength));
        double localT = (progress - segIndex * segLength) / segLength;

        Color c1 = palette[segIndex];
        Color c2 = palette[segIndex + 1];
        stop.Color = LerpColor(c1, c2, localT);
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

    #region [Angle Helpers]
    /// <summary>
    /// Converts a total angle (in <paramref name="degrees"/>) into radians for use as a cone width.<br/>
    /// Example: 60 ⇨ Math.PI ÷ 3<br/>
    /// </summary>
    static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;

        #region [Examples]
        // Wide, 120° cone
        //double coneCenter = -Math.PI / 2;
        //double coneWidth = Tau / 3;

        // Straight up, 60° cone
        //double coneCenter = -Math.PI / 2;
        //double coneWidth = Math.PI / 3;

        // Straight up, 30° cone
        //double coneCenter = -Math.PI / 2;
        //double coneWidth = Math.PI / 6;
        #endregion
    }

    /// <summary>
    /// Returns (vx, vy) for a given speed and angle in degrees.
    /// In WPF: 0° is right, 90° is down, 180° is left, 270° is up.
    /// </summary>
    static (double vx, double vy) VelocityFromAngle(double speed, double angleDegrees)
    {
        double a = DegreesToRadians(angleDegrees);
        return (Math.Cos(a) * speed, Math.Sin(a) * speed);
    }

    /// <summary>
    /// Wrap to [-π, π]
    /// </summary>
    static double WrapAngle(double ang)
    {
        ang = (ang + Math.PI) % Tau;
        if (ang < 0) { ang += Tau; }
        return ang - Math.PI;
    }

    static double Clamp(double value, double min, double max) => Math.Min(Math.Max(value, min), max);
    #endregion

    #region [Compare Helpers]
    static bool IsZero(double value) => Math.Abs(value) < Epsilon;
    static bool IsZeroOrLess(double value) => value < Epsilon;
    static bool IsInvalid(double value)
    {
        if (value == double.NaN || value == double.NegativeInfinity || value == double.PositiveInfinity) { return true; }
        return false;
    }
    static bool IsInvalidOrZero(double value)
    {
        if (value == double.NaN || value == double.NegativeInfinity || value == double.PositiveInfinity || value <= 0) { return true; }
        return false;
    }
    static bool IsOne(double value)
    {
        return Math.Abs(value) >= 1d - Epsilon && Math.Abs(value) <= 1d + Epsilon;
    }
    static bool AreClose(double left, double right)
    {
        if (left == right) { return true; }
        double a = (Math.Abs(left) + Math.Abs(right) + 10.0d) * Epsilon;
        double b = left - right;
        return (-a < b) && (a > b);
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

    #region [Frame Capture]
    void SaveSpinnerAsPng(Canvas canvas, string filePath)
    {
        if (canvas == null) { return; }
        Size size = new Size(canvas.ActualWidth, canvas.ActualHeight);
        canvas.Measure(size);
        //canvas.Arrange(new Rect(size));
        var rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(canvas);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        try
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(fs);
            }
        }
        catch (Exception) { }
    }
    #endregion
}

#region [Support Classes]
public class FireworkDot
{
    public Ellipse Dot;
    public double X, Y;
    public double VX, VY;
    public double ApexY; // random apex trigger
}

public class FallingDot
{
    public Ellipse Dot;
    public double X, Y;
    public double VX, VY; // optional horizontal velocity
    public double VelocityY;
}

public class SplashParticle
{
    public Ellipse Dot;
    public double X, Y;
    public double VX, VY;
    public double Age, Lifetime;
}

public class ProgressDot
{
    public Ellipse Dot;
    public double X, Y;
    public double BaseSize;
    public int Index;
}

/// <summary>
/// For <c>Inflate</c> mode spinner animation.
/// </summary>
public class ChaseDot
{
    //public Ellipse Dot;        // FrameworkElement
    public double BaseSize;      // original size
    public double Scale;         // current scale factor
    public int Index;            // position in the circle
    public double Angle;         // radians: 0..2π
    public double Intensity;     // 0-1 current glow
    public double BlipIntensity; // random detection blip
}

/// <summary>
/// For <c>Radar</c> mode spinner animations.
/// </summary>
public class RadarBlip
{
    public Ellipse Dot;
    public double X, Y;
    public double VX, VY;     // velocity (for moving blips)
    public double Lifetime;   // total frames
    public double Age;        // current age
}

/// <summary>
/// For new twinkle simulation.<br/>
/// Some properties are unused in other effects.<br/>
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
/// For "shooting stars" simulation and others.<br/>
/// Some properties are unused in other effects.<br/>
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
    public Color StartColor;      // initial color
    public Color MidColor;        // middle color
    public Color EndColor;        // final color
    public Brush InitialBrush;    // user-defined brush
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