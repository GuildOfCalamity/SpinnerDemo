![Icon](./src/Assets/AppIcon.ico)

# SpinnerDemo - WPF

## v1.0.0.0 - October, 2025
**Dependencies**

| Assembly | Version |
| ---- | ---- |
| .NET | 8.0 |

- A [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf) application that demostrates my home-brew `Spinner` control.
- Visibility determines if animation runs, so this can be bound to view model properties, e.g. `IsBusy`/`IsNotBusy`.
- If mode is set to `SpinnerRenderMode.RotateCanvas` then some of the more advanced animations will not render correctly, it's
   recommended to keep the mode set to `SpinnerRenderMode.AnimatePositions` which employs the `CompositionTarget.Rendering` surface event.
- Most render methods have their own data elements, however some are shared, e.g. the **Snow/Wind/Space** modes all use the `_rain` arrays.
- Many of these modes/shapes could be used as a screen saver if you desire, e.g. the `Meteor1` and `Meteor2` mode are entertaining to watch in a maximized transparency window.

## Spinner Control

- The re-usable spiner control has many modes for rendering shapes:

```
public enum SpinnerRenderShape
{
    Dots,       // for standard/classic spinner
    Chase,      // for spinner chase animation
    Worm,       // for wiggle worm animation
    Spiral,     // for spiral rotation animation
    Polys,      // for spinner with more complex shapes
    Snow,       // for raining/snowing animation
    Wind,       // for horizontal animation
    Wave,       // for sine wave animation
    Space,      // for starfield animation
    Line,       // for line warp animation
    Stripe,     // for exaggerated line animation
    Bounce,     // for dot bouncing animation
    Square,     // for walking square animation
    Rings,      // for concentric ring animation
    Pulse,      // for ring pulse animation
    Twinkle1,   // for twinkling star animation
    Twinkle2,   // for twinkling star animation (with enhanced gradient brushes)
    Meteor1,    // for shooting star animation
    Meteor2,    // for shooting star animation (with enhanced color palette)
    Falling,    // for drop animation
    Explode,    // for explosion animation
    Fountain,   // for fountain animation
}
```

![Running](./src/Assets/Demo.gif)
