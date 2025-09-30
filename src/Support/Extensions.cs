using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SpinnerDemo;

public static class Extensions
{
    #region [Logger with automatic duplicate checking]
    static HashSet<string> _logCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static DateTime _logCacheUpdated = DateTime.Now;
    static int _repeatAllowedSeconds = 15;
    public static void WriteToLog(this string message, string fileName = "AppLog.txt")
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_logCache.Add(message))
        {
            _logCacheUpdated = DateTime.Now;
            try { System.IO.File.AppendAllText(fileName, $"[{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt")}] {message}{Environment.NewLine}"); }
            catch (Exception) { }
            
        }
        else
        {
            var diff = DateTime.Now - _logCacheUpdated;
            if (diff.Seconds > _repeatAllowedSeconds)
                _logCache.Clear();
            else
                Debug.WriteLine($"[WARNING] Duplicate not allowed: {diff.Seconds}secs < {_repeatAllowedSeconds}secs");
        }
    }
    #endregion

    /// <summary>
    /// Returns a <see cref="System.Windows.Media.Imaging.BitmapImage"/> from the provided <paramref name="uriPath"/>.
    /// </summary>
    /// <param name="uriPath">the pack uri path to the image</param>
    /// <returns><see cref="System.Windows.Media.Imaging.BitmapImage"/></returns>
    /// <remarks>
    /// URI Packing can assume the following formats:
    /// 1) Content File
    ///    "pack://application:,,,/Assets/logo.png"
    ///    https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf?view=netframeworkdesktop-4.8#content-file-pack-uris
    /// 2) Referenced Assembly Resource
    ///    "pack://application:,,,/AssemblyNameHere;component/Resources/logo.png"
    ///    https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf?view=netframeworkdesktop-4.8#referenced-assembly-resource-file
    /// 3) Site Of Origin
    ///    "pack://siteoforigin:,,,/Assets/SiteOfOriginFile.xaml"
    ///    https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf?view=netframeworkdesktop-4.8#site-of-origin-pack-uris
    /// </remarks>
    public static System.Windows.Media.Imaging.BitmapImage ReturnImageSource(this string uriPath)
    {
        try
        {
            System.Windows.Media.Imaging.BitmapImage holder = new System.Windows.Media.Imaging.BitmapImage();
            holder.BeginInit();
            holder.UriSource = new Uri(uriPath); //new Uri("pack://application:,,,/AssemblyName;component/Resources/logo.png");
            holder.EndInit();
            return holder;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] ReturnImageSource: {ex.Message}");
            return new System.Windows.Media.Imaging.BitmapImage();
        }
    }

    /// <summary>
    /// Scales all numeric values in a path geometry string by the given factor.
    /// <code>
    ///   string original = "M 1,1 L 4,4 L 7,1"; /* chevron */
    ///   string scaled = PathScaler.ScalePathData(original, 2.0); /* M 2,2 L 8,8 L 14,2 */
    /// </code>
    /// </summary>
    /// <param name="pathData">Original path data string (e.g. "M 1,1 L 4,4")</param>
    /// <param name="scale">Scaling factor (e.g. 2.0 = double size)</param>
    /// <returns>Scaled path data string</returns>
    public static string ScalePathData(string pathData, double scale = 2.0)
    {
        if (string.IsNullOrWhiteSpace(pathData))
            return string.Empty;

        // Works for M, L, C, Q, A, etc. (blindly scales all numbers)
        // Culture-invariant parsing ("." is always the decimal separator)

        // Match all numbers (handles decimals, negatives, scientific notation)
        var regex = new System.Text.RegularExpressions.Regex(@"-?\d+(\.\d+)?([eE][-+]?\d+)?", System.Text.RegularExpressions.RegexOptions.Compiled);

        string scaled = regex.Replace(pathData, match =>
        {
            double value = double.Parse(match.Value, CultureInfo.InvariantCulture);
            double newValue = value * scale;
            return newValue.ToString(CultureInfo.InvariantCulture);
        });

        return scaled;
    }

    /// <summary>
    /// Scales all coordinates in a path geometry string by the given factor around an origin point.
    /// </summary>
    /// <param name="pathData">Original path data string (e.g. "M 1,1 L 4,4")</param>
    /// <param name="scale">Scaling factor (e.g. 2.0 = double size)</param>
    /// <param name="originX">X coordinate of scaling origin</param>
    /// <param name="originY">Y coordinate of scaling origin</param>
    /// <returns>Scaled path data string</returns>
    public static string ScalePathData(string pathData, double scale = 2.0, double originX = 0, double originY = 0)
    {
        if (string.IsNullOrWhiteSpace(pathData))
            return string.Empty;

        // Match all numbers
        var regex = new Regex(@"-?\d+(\.\d+)?([eE][-+]?\d+)?", RegexOptions.Compiled);

        // We'll track whether we're reading an X or Y coordinate
        bool isX = true;
        double lastX = 0;

        string scaled = regex.Replace(pathData, match =>
        {
            double value = double.Parse(match.Value, CultureInfo.InvariantCulture);
            if (isX)
            {   // scale X
                lastX = originX + (value - originX) * scale;
                isX = false;
                return lastX.ToString(CultureInfo.InvariantCulture);
            }
            else
            {   // scale Y
                double newY = originY + (value - originY) * scale;
                isX = true;
                return newY.ToString(CultureInfo.InvariantCulture);
            }
        });

        return scaled;
    }

    /// <summary>
    /// Helper method for returning a collection of visual control types.
    /// </summary>
    public static IEnumerable<T> FindChildrenOfType<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T hit)
            {
                yield return hit;
            }
            foreach (T? grandChild in FindChildrenOfType<T>(child))
            {
                yield return grandChild;
            }
        }
    }

    /// <summary>
    /// An updated string truncation helper.
    /// </summary>
    /// <remarks>
    /// This can be helpful when the CharacterEllipsis TextTrimming Property is not available.
    /// </remarks>
    public static string Truncate(this string text, int maxLength, string mesial = "…")
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (maxLength > 0 && text.Length > maxLength)
        {
            var limit = maxLength / 2;
            if (limit > 1)
            {
                return String.Format("{0}{1}{2}", text.Substring(0, limit).Trim(), mesial, text.Substring(text.Length - limit).Trim());
            }
            else
            {
                var tmp = text.Length <= maxLength ? text : text.Substring(0, maxLength).Trim();
                return String.Format("{0}{1}", tmp, mesial);
            }
        }
        return text;
    }

    /// <summary>
    /// Reads all lines from file <paramref name="path"/> and joins them into a single string with the given <paramref name="separator"/>.
    /// </summary>
    public static string ReadIntoOneString(string path, string separator = ",")
    {
        if (!System.IO.File.Exists(path))
            return string.Empty;

        var items = System.IO.File.ReadAllLines(path).Distinct(StringComparer.OrdinalIgnoreCase);
        //return items.Aggregate((a, b) => a + separator + b);
        return string.Join(separator, items.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s));

        #region [Using StringBuilder]
        var alt = System.IO.File.ReadAllLines(path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var sb = new StringBuilder();
        for (int i = 0; i < alt.Count; i++)
        {
            sb.Append(alt[i]);
            // Don't append separator after last item
            if (i < alt.Count - 1) { sb.Append(separator); }
        }
        return $"{sb}";
        #endregion
    }

    /// <summary>
    /// De-dupe file reader using a <see cref="HashSet{string}"/>.
    /// </summary>
    public static HashSet<string> ReadLines(string path)
    {
        if (!System.IO.File.Exists(path))
            return new HashSet<string>();
        return new HashSet<string>(System.IO.File.ReadAllLines(path), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// De-dupe file writer using a <see cref="HashSet{string}"/>.
    /// </summary>
    public static bool WriteLines(string path, IEnumerable<string> lines)
    {
        var output = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        using (System.IO.TextWriter writer = System.IO.File.CreateText(path))
        {
            foreach (var line in output)
                writer.WriteLine(line);
        }
        return true;
    }

    /// <summary>
    /// Converts long file size into typical browser file size.
    /// </summary>
    public static string ToFileSize(this long size)
    {
        if (size < 1024) { return (size).ToString("F0") + " Bytes"; }
        if (size < Math.Pow(1024, 2)) { return (size / 1024).ToString("F0") + " KB"; }
        if (size < Math.Pow(1024, 3)) { return (size / Math.Pow(1024, 2)).ToString("F1") + " MB"; }
        if (size < Math.Pow(1024, 4)) { return (size / Math.Pow(1024, 3)).ToString("F1") + " GB"; }
        if (size < Math.Pow(1024, 5)) { return (size / Math.Pow(1024, 4)).ToString("F1") + " TB"; }
        if (size < Math.Pow(1024, 6)) { return (size / Math.Pow(1024, 5)).ToString("F1") + " PB"; }
        return (size / Math.Pow(1024, 6)).ToString("F1") + " EB";
    }

    /// <summary>
    /// Display a readable sentence as to when the time will happen.
    /// e.g. "in one second" or "in 2 days"
    /// </summary>
    /// <param name="value"><see cref="TimeSpan"/>the future time to compare from now</param>
    /// <returns>human friendly format</returns>
    public static string ToReadableTime(this TimeSpan value, bool reportMilliseconds = false)
    {
        double delta = value.TotalSeconds;
        if (delta < 1 && !reportMilliseconds) { return "less than one second"; }
        if (delta < 1 && reportMilliseconds) { return $"{value.TotalMilliseconds:N1} milliseconds"; }
        if (delta < 60) { return value.Seconds == 1 ? "one second" : value.Seconds + " seconds"; }
        if (delta < 120) { return "a minute"; }                  // 2 * 60
        if (delta < 3000) { return value.Minutes + " minutes"; } // 50 * 60
        if (delta < 5400) { return "an hour"; }                  // 90 * 60
        if (delta < 86400) { return value.Hours + " hours"; }    // 24 * 60 * 60
        if (delta < 172800) { return "one day"; }                // 48 * 60 * 60
        if (delta < 2592000) { return value.Days + " days"; }    // 30 * 24 * 60 * 60
        if (delta < 31104000)                                    // 12 * 30 * 24 * 60 * 60
        {
            int months = Convert.ToInt32(Math.Floor((double)value.Days / 30));
            return months <= 1 ? "one month" : months + " months";
        }
        int years = Convert.ToInt32(Math.Floor((double)value.Days / 365));
        return years <= 1 ? "one year" : years + " years";
    }

    /// <summary>
    /// Returns a description like: "today at 3:45 PM (in 1h 20m)" or "on Fri at 9:00 AM (in 2d 4h)"
    /// </summary>
    public static string DescribeFutureTime(this TimeSpan delta, DateTimeOffset? reference = null, CultureInfo? culture = null)
    {
        if (delta < TimeSpan.Zero)
            delta = TimeSpan.Zero; // clamp

        var now = reference ?? DateTimeOffset.Now;
        var target = now + delta;

        culture ??= CultureInfo.CurrentCulture;

        string when = DescribeDayAndTime(now, target, culture);
        string rel = DescribeRelative(delta);

        return $"{when} ({rel})";
    }

    /// <summary>
    /// Optional version: Returns both the target and the description.
    /// </summary>
    public static (DateTimeOffset Target, string Description) DescribeFutureTimeWithTarget(TimeSpan delta, DateTimeOffset? reference = null, CultureInfo? culture = null)
    {
        if (delta < TimeSpan.Zero)
            delta = TimeSpan.Zero;

        var now = reference ?? DateTimeOffset.Now;
        var target = now + delta;
        return (target, DescribeFutureTime(delta, now, culture));
    }

    /// <summary>
    ///   <code>
    ///     Extensions.DescribeDayAndTime(new DateTimeOffset(DateTime.Now.AddHours(1)), CultureInfo.CurrentCulture);
    ///   </code>
    /// </summary>
    /// <returns>"today at 9:00 AM"</returns>
    public static string DescribeDayAndTime(DateTimeOffset target, CultureInfo culture)
    {
        return DescribeDayAndTime(DateTimeOffset.Now, target, culture ?? System.Globalization.CultureInfo.CurrentCulture);
    }
    static string DescribeDayAndTime(DateTimeOffset now, DateTimeOffset target, CultureInfo culture)
    {
        DateTime today = now.Date;
        DateTime tDate = target.Date;
        string dayPart = string.Empty;

        if (tDate == today)
            dayPart = "today";
        else if (tDate == today.AddDays(1))
            dayPart = "tomorrow";
        else if (tDate <= today.AddDays(7)) // e.g., "on Tue"
            dayPart = "on " + culture.DateTimeFormat.AbbreviatedDayNames[(int)tDate.DayOfWeek];
        else // e.g., "on Aug 25, 2025"
            dayPart = "on " + target.ToString(culture.DateTimeFormat.ShortDatePattern, culture);

        string timePart = target.ToLocalTime().ToString("t", culture); // short time

        return $"{dayPart} at {timePart}";
    }

    public static string DescribeRelative(TimeSpan delta)
    {
        if (delta < TimeSpan.FromSeconds(1))
            return "now";

        int components = 0;
        var sb = new StringBuilder();

        void add(string label, long value)
        {
            if (value <= 0 || components >= 2) 
                return;
            if (sb.Length > 0) 
                sb.Append(' ');
            sb.Append(value).Append(label);
            components++;
        }

        add("day", (long)delta.TotalDays);
        delta -= TimeSpan.FromDays((long)delta.TotalDays);

        add("hr", (long)delta.TotalHours);
        delta -= TimeSpan.FromHours((long)delta.TotalHours);

        add("min", (long)delta.TotalMinutes);
        delta -= TimeSpan.FromMinutes((long)delta.TotalMinutes);

        if (components < 2)
            add("sec", (long)Math.Round(delta.TotalSeconds));

        return $"in {sb}";
    }

    /// <summary>
    /// Converts a DateTime to a DateTimeOffset with the specified offset
    /// </summary>
    /// <param name="date">The DateTime to convert</param>
    /// <param name="offset">The offset to apply to the date field</param>
    /// <returns>The corresponding DateTimeOffset</returns>
    public static DateTimeOffset ToOffset(this DateTime date, TimeSpan offset) => new DateTimeOffset(date).ToOffset(offset);

    #region [Color Brush Methods]
    /// <summary>
    /// Generates a random <see cref="System.Windows.Media.Color"/>.
    /// </summary>
    /// <returns><see cref="System.Windows.Media.Color"/> with 255 alpha</returns>
    public static System.Windows.Media.Color GenerateRandomColor()
    {
        return System.Windows.Media.Color.FromRgb((byte)Random.Shared.Next(0, 256), (byte)Random.Shared.Next(0, 256), (byte)Random.Shared.Next(0, 256));
    }

    /// <summary>
    /// Generates a random <see cref="LinearGradientBrush"/> using two <see cref="System.Windows.Media.Color"/>s.
    /// </summary>
    /// <returns><see cref="LinearGradientBrush"/></returns>
    public static LinearGradientBrush CreateGradientBrush(Color c1, Color c2)
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
    public static LinearGradientBrush CreateGradientBrush(Color c1, Color c2, Color c3)
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

    /// <summary>
    /// Generates a random <see cref="SolidColorBrush"/>.
    /// </summary>
    /// <returns><see cref="SolidColorBrush"/> with 255 alpha</returns>
    public static SolidColorBrush CreateRandomBrush()
    {
        byte r = (byte)Random.Shared.Next(0, 256);
        byte g = (byte)Random.Shared.Next(0, 256);
        byte b = (byte)Random.Shared.Next(0, 256);
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
    }

    /// <summary>
    /// Avoids near-white values by using high saturation ranges prevent desaturation.
    /// </summary>
    public static SolidColorBrush CreateRandomLightBrush(byte alpha = 255)
    { 
        return CreateRandomHsvBrush(
            hue: Random.Shared.NextDouble() * 360.0,
            saturation: Lerp(0.65, 1.0, Random.Shared.NextDouble()), // high saturation to avoid gray
            value: Lerp(0.85, 1.0, Random.Shared.NextDouble()),      // bright
            alpha: alpha);
    }

    /// <summary>
    /// Avoids near-black values by using high saturation ranges prevent desaturation.
    /// </summary>
    public static SolidColorBrush CreateRandomDarkBrush(byte alpha = 255)
    {
        return CreateRandomHsvBrush(
            hue: Random.Shared.NextDouble() * 360.0,
            saturation: Lerp(0.65, 1.0, Random.Shared.NextDouble()), // high saturation to avoid gray
            value: Lerp(0.2, 0.45, Random.Shared.NextDouble()),      // dark
            alpha: alpha);
    }

    public static SolidColorBrush CreateRandomHsvBrush(double hue, double saturation, double value, byte alpha)
    {
        var (r, g, b) = HsvToRgb(hue, saturation, value);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, r, g, b));
        if (brush.CanFreeze) 
            brush.Freeze(); // freeze for performance (if animation is not needed)
        return brush;
    }

    static (byte r, byte g, byte b) HsvToRgb(double h, double s, double v)
    {
        // h: [0,360), s,v: [0,1]
        if (s <= 0.00001)
        {
            // If saturation is approx zero then return achromatic (grey)
            byte grey = (byte)Math.Round(v * 255.0);
            return (grey, grey, grey);
        }

        h = (h % 360 + 360) % 360; // normalize
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = v - c;

        //double (r1, g1, b1) = h switch
        //{
        //    < 60 => (c, x, 0),
        //    < 120 => (x, c, 0),
        //    < 180 => (0, c, x),
        //    < 240 => (0, x, c),
        //    < 300 => (x, 0, c),
        //    _ => (c, 0, x)
        //};
        double r1, g1, b1;
        if (h < 60)       { r1 = c; g1 = x; b1 = 0; }
        else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
        else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
        else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
        else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
        else              { r1 = c; g1 = 0; b1 = x; }
        byte r = (byte)Math.Round((r1 + m) * 255.0);
        byte g = (byte)Math.Round((g1 + m) * 255.0);
        byte b = (byte)Math.Round((b1 + m) * 255.0);
        return (r, g, b);
    }

    static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        // Hue
        if (delta < 0.00001) { h = 0; }
        else if (max == rd) { h = 60 * (((gd - bd) / delta) % 6); }
        else if (max == gd) { h = 60 * (((bd - rd) / delta) + 2); }
        else { h = 60 * (((rd - gd) / delta) + 4); }
        if (h < 0) { h += 360; }

        // Saturation
        s = (max <= 0) ? 0 : delta / max;

        // Value
        v = max;
    }
    
    static double Lerp(double a, double b, double t) => a + (b - a) * t;

    public enum ColorTilt
    {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Purple
    }

    /// <summary>
    /// Generates a random <see cref="SolidColorBrush"/> based on a given <see cref="ColorTilt"/>.
    /// </summary>
    public static SolidColorBrush CreateRandomLightBrush(ColorTilt tilt, double tiltStrength = 30, byte alpha = 255)
    {
        double hue = GetTiltedHue(tilt, tiltStrength);
        double saturation = Lerp(0.65, 1.0, Random.Shared.NextDouble()); // high saturation to avoid gray
        double value = Lerp(0.85, 1.0, Random.Shared.NextDouble());      // bright
        return CreateBrushFromHsv(hue, saturation, value, alpha);
    }

    /// <summary>
    /// Generates a random <see cref="SolidColorBrush"/> based on a given <see cref="ColorTilt"/>.
    /// </summary>
    public static SolidColorBrush CreateRandomDarkBrush(ColorTilt tilt, double tiltStrength = 30, byte alpha = 255)
    {
        double hue = GetTiltedHue(tilt, tiltStrength);
        double saturation = Lerp(0.65, 1.0, Random.Shared.NextDouble()); // high saturation to avoid gray
        double value = Lerp(0.2, 0.45, Random.Shared.NextDouble());      // dark
        return CreateBrushFromHsv(hue, saturation, value, alpha);
    }

    /// <summary>
    /// Generates a random <see cref="SolidColorBrush"/> based on a given dictionary of <see cref="ColorTilt"/>s.
    /// </summary>
    public static SolidColorBrush CreateRandomLightBrush(Dictionary<ColorTilt, double> tiltWeights, double tiltStrength = 30, byte alpha = 255)
    {
        double hue = GetBlendedTiltedHue(tiltWeights, tiltStrength);
        double saturation = Lerp(0.65, 1.0, Random.Shared.NextDouble()); // high saturation to avoid gray
        double value = Lerp(0.85, 1.0, Random.Shared.NextDouble());      // bright
        return CreateBrushFromHsv(hue, saturation, value, alpha);
    }

    /// <summary>
    /// Generates a random <see cref="SolidColorBrush"/> based on a given dictionary of <see cref="ColorTilt"/>s.
    /// </summary>
    public static SolidColorBrush CreateRandomDarkBrush(Dictionary<ColorTilt, double> tiltWeights, double tiltStrength = 30, byte alpha = 255)
    {
        double hue = GetBlendedTiltedHue(tiltWeights, tiltStrength);
        double saturation = Lerp(0.65, 1.0, Random.Shared.NextDouble()); // high saturation to avoid gray
        double value = Lerp(0.2, 0.45, Random.Shared.NextDouble());      // dark
        return CreateBrushFromHsv(hue, saturation, value, alpha);
    }

    static SolidColorBrush CreateBrushFromHsv(double hue, double saturation, double value, byte alpha)
    {
        var (r, g, b) = HsvToRgb(hue, saturation, value);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, r, g, b));
        if (brush.CanFreeze) { brush.Freeze(); }
        return brush;
    }

    static double GetTiltedHue(ColorTilt tilt, double variance = 30)
    {
        // Hue centers in degrees for basic colors
        double centerHue;
        switch (tilt)
        {
            case ColorTilt.Red: centerHue = 0.0;      // also wraps near 360
                break;
            case ColorTilt.Orange: centerHue = 30.0;
                break;
            case ColorTilt.Yellow: centerHue = 60.0;
                break;
            case ColorTilt.Green: centerHue = 120.0;
                break;
            case ColorTilt.Blue: centerHue = 240.0;
                break;
            case ColorTilt.Purple: centerHue = 280.0; // between magenta (300) and blue
                break;
            default: centerHue = 0.0;
                break;
        }

        // Clamp variance to [0,180]
        variance = Math.Max(0, Math.Min(variance, 180));

        // Allow ±30° variation for variety
        double minHue = centerHue - variance;
        double maxHue = centerHue + variance;

        double hue = minHue + Random.Shared.NextDouble() * (maxHue - minHue);
        // Wrap around 0–360
        if (hue < 0) { hue += 360; }
        if (hue >= 360) {hue -= 360; }

        return hue;
    }

    static double GetBlendedTiltedHue(Dictionary<ColorTilt, double> tiltWeights, double tiltStrength)
    {
        if (tiltWeights == null || tiltWeights.Count == 0)
            return Random.Shared.NextDouble() * 360.0;

        // Normalize weights
        double total = tiltWeights.Values.Sum();
        if (total <= 0) return Random.Shared.NextDouble() * 360.0;

        // Pick a tilt based on weighted random
        double roll = Random.Shared.NextDouble() * total;
        double cumulative = 0;
        ColorTilt chosenTilt = tiltWeights.First().Key;

        foreach (var kvp in tiltWeights)
        {
            cumulative += kvp.Value;
            if (roll <= cumulative)
            {
                chosenTilt = kvp.Key;
                break;
            }
        }

        // Get center hue for chosen tilt
        double centerHue = GetCenterHue(chosenTilt);

        // Clamp tiltStrength
        tiltStrength = Math.Max(0, Math.Min(tiltStrength, 180));

        // ± tiltStrength variation
        double minHue = centerHue - tiltStrength;
        double maxHue = centerHue + tiltStrength;

        double hue = minHue + Random.Shared.NextDouble() * (maxHue - minHue);
        if (hue < 0) hue += 360;
        if (hue >= 360) hue -= 360;

        return hue;
    }

    static double GetCenterHue(ColorTilt tilt)
    {
        switch (tilt)
        {
            case ColorTilt.Red:    return 0.0;
            case ColorTilt.Orange: return 30.0;
            case ColorTilt.Yellow: return 60.0;
            case ColorTilt.Green:  return 120.0;
            case ColorTilt.Blue:   return 240.0;
            case ColorTilt.Purple: return 280.0;
            default:               return 0.0;
        }
    }

    public static SolidColorBrush BrightenBrush(SolidColorBrush brush, double amount)
    {
        if (brush == null)
            throw new ArgumentNullException(nameof(brush));

        // Clamp amount to [0, 1]
        amount = Math.Max(0, Math.Min(amount, 1));

        Color color = brush.Color;

        // Convert to HSV
        double h, s, v;
        RgbToHsv(color.R, color.G, color.B, out h, out s, out v);

        // Increase brightness
        v = Math.Min(1.0, v + amount);

        // Convert back to RGB
        var (r, g, b) = HsvToRgb(h, s, v);

        var newBrush = new SolidColorBrush(Color.FromArgb(color.A, r, g, b));
        if (newBrush.CanFreeze) { newBrush.Freeze(); }
        return newBrush;
    }

    public static SolidColorBrush DarkenBrush(SolidColorBrush brush, double amount)
    {
        if (brush == null)
            throw new ArgumentNullException(nameof(brush));

        // Clamp amount to [0, 1]
        amount = Math.Max(0, Math.Min(amount, 1));

        Color color = brush.Color;

        // Convert to HSV
        double h, s, v;
        RgbToHsv(color.R, color.G, color.B, out h, out s, out v);

        // Decrease brightness
        v = Math.Max(0.0, v - amount);

        // Convert back to RGB
        var (r, g, b) = HsvToRgb(h, s, v);

        var newBrush = new SolidColorBrush(Color.FromArgb(color.A, r, g, b));
        if (newBrush.CanFreeze) newBrush.Freeze();
        return newBrush;
    }

    /// <summary><code>
    ///  /* Brighten by 20%, no saturation change */
    ///  var brighter = Extensions.AdjustBrush(baseBrush, brightnessDelta: 0.2);
    ///  /* Darken by 30%, mute by 20% */
    ///  var darkerMuted = Extensions.AdjustBrush(baseBrush, brightnessDelta: -0.3, saturationDelta: -0.2);
    ///  /* Keep brightness, boost saturation */
    ///  var vivid = Extensions.AdjustBrush(baseBrush, saturationDelta: 0.3);
    /// </code></summary>
    public static SolidColorBrush AdjustBrush(SolidColorBrush brush, double brightnessDelta = 0.0, double saturationDelta = 0.0)
    {
        if (brush == null)
            throw new ArgumentNullException(nameof(brush));

        Color color = brush.Color;

        // Convert to HSV
        double h, s, v;
        RgbToHsv(color.R, color.G, color.B, out h, out s, out v);

        // Apply deltas
        v = Math.Max(0.0, Math.Min(1.0, v + brightnessDelta));
        s = Math.Max(0.0, Math.Min(1.0, s + saturationDelta));

        // Convert back to RGB
        var (r, g, b) = HsvToRgb(h, s, v);

        var adjusted = new SolidColorBrush(Color.FromArgb(color.A, r, g, b));
        if (adjusted.CanFreeze) { adjusted.Freeze(); }
        return adjusted;
    }

    public static SolidColorBrush ShiftSaturation(SolidColorBrush brush, double amount)
    {
        if (brush == null)
            throw new ArgumentNullException(nameof(brush));

        // amount can be positive (more vivid) or negative (more muted)
        // Clamp to [-1, 1] so we don't overshoot
        amount = Math.Max(-1, Math.Min(amount, 1));

        Color color = brush.Color;

        // Convert to HSV
        double h, s, v;
        RgbToHsv(color.R, color.G, color.B, out h, out s, out v);

        // Adjust saturation
        s = Math.Max(0.0, Math.Min(1.0, s + amount));

        // Convert back to RGB
        var (r, g, b) = HsvToRgb(h, s, v);

        var newBrush = new SolidColorBrush(Color.FromArgb(color.A, r, g, b));
        if (newBrush.CanFreeze) 
            newBrush.Freeze();

        return newBrush;
    }

    /// <summary>
    /// Returns the Euclidean distance between two <see cref="System.Windows.Media.Color"/>s.
    /// </summary>
    /// <param name="color1">1st <see cref="System.Windows.Media.Color"/></param>
    /// <param name="color2">2nd <see cref="System.Windows.Media.Color"/></param>
    public static double ColorDistance(System.Windows.Media.Color color1, System.Windows.Media.Color color2)
    {
        return Math.Sqrt(Math.Pow(color1.R - color2.R, 2) + Math.Pow(color1.G - color2.G, 2) + Math.Pow(color1.B - color2.B, 2));
    }
    #endregion

    /// <summary>
    /// Fetch all <see cref="System.Windows.Media.Brushes"/>.
    /// </summary>
    /// <returns><see cref="List{T}"/></returns>
    public static List<Brush> GetAllMediaBrushes()
    {
        List<Brush> brushes = new List<Brush>();
        Type brushesType = typeof(Brushes);

        //TypeAttributes ta = typeof(Brushes).Attributes;
        //Debug.WriteLine($"[INFO] TypeAttributes: {ta}");

        // Iterate through the static properties of the Brushes class type.
        foreach (PropertyInfo pi in brushesType.GetProperties(BindingFlags.Static | BindingFlags.Public))
        {
            // Check if the property type is Brush/SolidColorBrush
            if (pi != null && (pi.PropertyType == typeof(Brush) || pi.PropertyType == typeof(SolidColorBrush)))
            {
                if (pi.Name.Contains("Transparent"))
                    continue;

                Debug.WriteLine($"[INFO] Adding brush '{pi.Name}'");

                // Get the brush value from the static property
                var br = (Brush?)pi?.GetValue(null, null);
                if (br != null)
                    brushes.Add(br);
            }
        }
        return brushes;
    }

    /// <summary>
    /// 'BitmapCacheBrush','DrawingBrush','GradientBrush','ImageBrush',
    /// 'LinearGradientBrush','RadialGradientBrush','SolidColorBrush',
    /// 'TileBrush','VisualBrush','ImplicitInputBrush'
    /// </summary>
    /// <returns><see cref="List{T}"/></returns>
    public static List<Type> GetAllDerivedBrushClasses()
    {
        List<Type> derivedBrushes = new List<Type>();
        // Get the assembly containing the Brush class
        Assembly assembly = typeof(Brush).Assembly;
        try
        {   // Iterate through all types in the assembly
            foreach (Type type in assembly.GetTypes())
            {
                // Check if the type is a subclass of Brush
                if (type.IsSubclassOf(typeof(Brush)))
                {
                    //Debug.WriteLine($"[INFO] Adding type '{type.Name}'");
                    derivedBrushes.Add(type);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetAllDerivedBrushClasses: {ex.Message}");
        }
        return derivedBrushes;
    }

    /// <summary>
    /// Fetch all derived types from a super class.
    /// </summary>
    /// <returns><see cref="List{T}"/></returns>
    public static List<Type> GetDerivedSubClasses<T>(T objectClass) where T : class
    {
        List<Type> derivedClasses = new List<Type>();
        // Get the assembly containing the base class
        Assembly assembly = typeof(T).Assembly;
        try
        {   // Iterate through all types in the assembly
            foreach (Type type in assembly.GetTypes())
            {
                // Check if the type is a subclass of T
                if (type.IsSubclassOf(typeof(T)))
                {
                    //Debug.WriteLine($"[INFO] Adding subclass type '{type.Name}'");
                    derivedClasses.Add(type);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetDerivedClasses: {ex.Message}");
        }
        return derivedClasses;
    }

    /// <summary>
    /// Example of <see cref="UIElement"/> traversal.
    /// </summary>
    public static void IterateAllUIElements(DockPanel dock)
    {
        UIElementCollection uic = dock.Children;

        foreach (Grid uie in uic)
            uie.Background = new SolidColorBrush(Colors.Green);

        foreach (Border uie in uic)
            uie.Background = new SolidColorBrush(Colors.Orange);

        foreach (StackPanel uie in uic)
            uie.Background = new SolidColorBrush(Colors.Blue);

        foreach (Button uie in uic)
        {
            uie.Background = new SolidColorBrush(Colors.Yellow);

            // Example of restoring default properties
            var locallySetProperties = uie.GetLocalValueEnumerator();
            while (locallySetProperties.MoveNext())
            {
                DependencyProperty propertyToClear = locallySetProperties.Current.Property;
                if (!propertyToClear.ReadOnly)
                    uie.ClearValue(propertyToClear);
            }
        }
    }

    /// <summary>
    /// FindVisualChild element in a control group.
    /// <code>
    ///   /* Getting the ContentPresenter of myListBoxItem */
    ///   var myContentPresenter = FindVisualChild<ContentPresenter>(myListBoxItem);
    ///   
    ///   /* Getting the currently selected ListBoxItem. Note that the ListBox must have IsSynchronizedWithCurrentItem set to True for this to work */
    ///   var myListBoxItem = (ListBoxItem)(myListBox.ItemContainerGenerator.ContainerFromItem(myListBox.Items.CurrentItem));
    ///   
    ///   /* Finding textBlock from the DataTemplate that is set on that ContentPresenter */
    ///   var myDataTemplate = myContentPresenter.ContentTemplate;
    ///   var myTextBlock = (TextBlock)myDataTemplate.FindName("textBlock", myContentPresenter);
    ///
    ///   /* Do something to the DataTemplate-generated TextBlock */
    ///   MessageBox.Show($"The text of the TextBlock of the selected list item: {myTextBlock.Text}");
    /// </code>
    /// </summary>
    public static TChildItem? FindVisualChild<TChildItem>(DependencyObject obj) where TChildItem : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is TChildItem)
                return (TChildItem)child;
            var childOfChild = FindVisualChild<TChildItem>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }

    /// <summary>
    /// Find & return a WPF control based on its resource key name.
    /// </summary>
    public static T? FindControl<T>(this FrameworkElement control, string resourceKey) where T : FrameworkElement
    {
        return (T?)control.FindResource(resourceKey);
    }

    /// <summary>
    /// <code>
    ///   IEnumerable<DependencyObject> cntrls = this.FindUIElements();
    /// </code>
    /// If you're struggling to get this working and finding that your Window (for instance)
    /// has zero visual children, try running this method in the "_Loaded" event handler. 
    /// If you call this from a constructor (even after InitializeComponent), the visual 
    /// children won't be added to the VisualTree yet and it won't work properly.
    /// </summary>
    /// <param name="parent">some parent control like <see cref="System.Windows.Window"/></param>
    /// <returns>list of <see cref="IEnumerable{DependencyObject}"/></returns>
    public static IEnumerable<DependencyObject> FindUIElements(this DependencyObject parent)
    {
        if (parent == null)
            yield break;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject o = VisualTreeHelper.GetChild(parent, i);
            foreach (DependencyObject obj in FindUIElements(o))
            {
                if (obj == null)
                    continue;
                if (obj is UIElement ret)
                    yield return ret;
            }
        }
        yield return parent;
    }

    /// <summary>
    /// Should be called on UI thread only.
    /// </summary>
    public static void HideAllVisualChildren<T>(this UIElementCollection coll) where T : UIElementCollection
    {
        // Casting the UIElementCollection into List
        List<FrameworkElement> lstElement = coll.Cast<FrameworkElement>().ToList();
        var lstControl = lstElement.OfType<Control>();
        foreach (Control control in lstControl)
        {
            if (control == null)
                continue;
            control.Visibility = System.Windows.Visibility.Hidden;
        }
    }

    public static IEnumerable<Control> GetAllControls<T>(this UIElementCollection coll) where T : UIElementCollection
    {
        // Casting the UIElementCollection into List
        List<FrameworkElement> lstElement = coll.Cast<FrameworkElement>().ToList();
        var lstControl = lstElement.OfType<Control>();
        foreach (Control control in lstControl)
        {
            if (control == null)
                continue;
            yield return control;
        }
    }

    /// <summary>
    /// Image helper method
    /// </summary>
    /// <param name="UriPath"></param>
    /// <returns><see cref="BitmapFrame"/></returns>
    public static BitmapFrame? GetBitmapFrame(string UriPath)
    {
        try
        {
            IconBitmapDecoder ibd = new IconBitmapDecoder(new Uri(UriPath, UriKind.RelativeOrAbsolute), BitmapCreateOptions.None, BitmapCacheOption.Default);
            return ibd.Frames[0];
        }
        catch (System.IO.FileFormatException fex)
        {
            Debug.WriteLine($"[ERROR] GetBitmapFrame: {fex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Example image source setting method.
    /// </summary>
    /// <param name="imgCtrl"><see cref="Image"/> control to update</param>
    /// <param name="ImageUrl">URL path to the image source</param>
    public static async Task UpdateImageFromRemoteSource(this Image imgCtrl, string imgUrl)
    {
        var client = new System.Net.Http.HttpClient();
        byte[] bytes = await client.GetByteArrayAsync(imgUrl);
        var image = new BitmapImage();
        using (var mem = new MemoryStream(bytes))
        {
            mem.Position = 0;
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = mem;
            image.EndInit();
        }
        image.Freeze();
        client.Dispose();
        imgCtrl.Source = image;
    }

    public static void RestorePosition(this Window window, double left, double top)
    {
        try
        {
            // Desktop bounds from WPF
            double screenWidth = SystemParameters.VirtualScreenWidth;
            double screenHeight = SystemParameters.VirtualScreenHeight;
            double screenLeft = SystemParameters.VirtualScreenLeft;
            double screenTop = SystemParameters.VirtualScreenTop;

            // Validate that window fits inside current bounds
            if (left >= screenLeft && top >= screenTop &&
                left + window.Width <= screenLeft + screenWidth &&
                top + window.Height <= screenTop + screenHeight)
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = left;
                window.Top = top;
            }
        }
        catch { /* ignore */ }
    }

    #region [Easing Functions]

    // Quadratic Easing (t²): EaseInQuadratic → Starts slow, speeds up.
    public static double EaseInQuadratic(double t) => t * t;
    // EaseOutQuadratic → Starts fast, slows down.
    public static double EaseOutQuadratic(double t) => 1.0 - (1.0 - t) * (1.0 - t);
    // EaseInOutQuadratic → Symmetric acceleration-deceleration.
    public static double EaseInOutQuadratic(double t) => t < 0.5 ? 2.0 * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 2.0) / 2.0;

    // Cubic Easing (t³): EaseInCubic → Stronger acceleration.
    public static double EaseInCubic(double t) => Math.Pow(t, 3.0);
    // EaseOutCubic → Slower deceleration.
    public static double EaseOutCubic(double t) => 1.0 - Math.Pow(1.0 - t, 3.0);
    // EaseInOutCubic → Balanced smooth curve.
    public static double EaseInOutCubic(double t) => t < 0.5 ? 4.0 * Math.Pow(t, 3.0) : 1.0 - Math.Pow(-2.0 * t + 2.0, 3.0) / 2.0;

    // Quartic Easing (t⁴): Sharper transition than cubic easing.
    public static double EaseInQuartic(double t) => Math.Pow(t, 4.0);
    public static double EaseOutQuartic(double t) => 1.0 - Math.Pow(1.0 - t, 4.0);
    public static double EaseInOutQuartic(double t) => t < 0.5 ? 8.0 * Math.Pow(t, 4.0) : 1.0 - Math.Pow(-2.0 * t + 2.0, 4.0) / 2.0;

    // Quintic Easing (t⁵): Even steeper curve for dramatic transitions.
    public static double EaseInQuintic(double t) => Math.Pow(t, 5.0);
    public static double EaseOutQuintic(double t) => 1.0 - Math.Pow(1.0 - t, 5.0);
    public static double EaseInOutQuintic(double t) => t < 0.5 ? 16.0 * Math.Pow(t, 5.0) : 1.0 - Math.Pow(-2.0 * t + 2.0, 5.0) / 2.0;

    // Elastic Easing (Bouncing Effect)
    public static double EaseInElastic(double t) => t == 0 ? 0 : t == 1 ? 1 : -Math.Pow(2.0, 10.0 * t - 10.0) * Math.Sin((t * 10.0 - 10.75) * (2.0 * Math.PI) / 3.0);
    public static double EaseOutElastic(double t) => t == 0 ? 0 : t == 1 ? 1 : Math.Pow(2.0, -10.0 * t) * Math.Sin((t * 10.0 - 0.75) * (2.0 * Math.PI) / 3.0) + 1.0;
    public static double EaseInOutElastic(double t) => t == 0 ? 0 : t == 1 ? 1 : t < 0.5 ? -(Math.Pow(2.0, 20.0 * t - 10.0) * Math.Sin((20.0 * t - 11.125) * (2.0 * Math.PI) / 4.5)) / 2.0 : (Math.Pow(2.0, -20.0 * t + 10.0) * Math.Sin((20.0 * t - 11.125) * (2.0 * Math.PI) / 4.5)) / 2.0 + 1.0;

    //Bounce Easing(Ball Bouncing Effect)
    public static double EaseInBounce(double t) => 1.0 - EaseOutBounce(1.0 - t);
    public static double EaseOutBounce(double t)
    {
        double n1 = 7.5625, d1 = 2.75;
        if (t < 1.0 / d1)
            return n1 * t * t;
        else if (t < 2.0 / d1)
            return n1 * (t -= 1.5 / d1) * t + 0.75;
        else if (t < 2.5 / d1)
            return n1 * (t -= 2.25 / d1) * t + 0.9375;
        else
            return n1 * (t -= 2.625 / d1) * t + 0.984375;
    }
    public static double EaseInOutBounce(double t) => t < 0.5 ? (1.0 - EaseOutBounce(1.0 - 2.0 * t)) / 2.0 : (1.0 + EaseOutBounce(2.0 * t - 1.0)) / 2.0;

    // Exponential Easing(Fast Growth/Decay)
    public static double EaseInExpo(double t) => t == 0 ? 0 : Math.Pow(2.0, 10.0 * t - 10.0);
    public static double EaseOutExpo(double t) => t == 1 ? 1 : 1.0 - Math.Pow(2.0, -10.0 * t);
    public static double EaseInOutExpo(double t) => t == 0 ? 0 : t == 1 ? 1 : t < 0.5 ? Math.Pow(2.0, 20.0 * t - 10.0) / 2.0 : (2.0 - Math.Pow(2.0, -20.0 * t + 10.0)) / 2.0;

    // Circular Easing(Smooth Circular Motion)
    public static double EaseInCircular(double t) => 1.0 - Math.Sqrt(1.0 - Math.Pow(t, 2.0));
    public static double EaseOutCircular(double t) => Math.Sqrt(1.0 - Math.Pow(t - 1.0, 2.0));
    public static double EaseInOutCircular(double t) => t < 0.5 ? (1.0 - Math.Sqrt(1.0 - Math.Pow(2.0 * t, 2.0))) / 2.0 : (Math.Sqrt(1.0 - Math.Pow(-2.0 * t + 2.0, 2.0)) + 1.0) / 2.0;

    // Back Easing(Overshoots Before Settling)
    public static double EaseInBack(double t) => 2.70158 * t * t * t - 1.70158 * t * t;
    public static double EaseOutBack(double t) => 1.0 + 2.70158 * Math.Pow(t - 1.0, 3.0) + 1.70158 * Math.Pow(t - 1.0, 2.0);
    public static double EaseInOutBack(double t) => t < 0.5 ? (Math.Pow(2.0 * t, 2.0) * ((2.59491 + 1.0) * 2.0 * t - 2.59491)) / 2.0 : (Math.Pow(2.0 * t - 2.0, 2.0) * ((2.59491 + 1.0) * (t * 2.0 - 2.0) + 2.59491) + 2.0) / 2.0;

    #endregion

#if SYSTEM_DRAWING
    public static ImageSource GetFileIcon(string path)
    {
        try
        {
            if (System.IO.File.Exists(path))
            {
                using (Icon sysIcon = Icon.ExtractAssociatedIcon(path))
                {
                    if (sysIcon != null)
                        return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(sysIcon.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
            }
        }
        catch { /* Ignore */ }
        return null;
    }
#endif

    /// <summary>
    /// Attempts to retrieve a resource of type T from Application resources.
    /// Returns null if not found or of wrong type.
    /// </summary>
    public static T? TryGetResource<T>(object key) where T : class
    {
        /* [EXAMPLE USAGE]
         Style? myStyle = Extensions.TryGetResource<Style>("ExampleButtonStyle");
         if (myStyle != null)
         {
             button.Style = myStyle;
         }
        */
        if (Application.Current == null || key == null)
            return null;

        var resource = Application.Current.TryFindResource(key);
        return resource as T;
    }

    /// <summary>
    /// Attempts to retrieve a resource of type T from a given ResourceDictionary.
    /// Returns null if not found or of wrong type.
    /// </summary>
    public static T? TryGetFromDictionary<T>(ResourceDictionary dictionary, object key) where T : class
    {
        /* [EXAMPLE USAGE]
         var dict = new ResourceDictionary { Source = new Uri("pack://application:,,,/ExtraTheme.xaml") };
         Brush? bkgrndBrush = Extensions.TryGetFromDictionary<Brush>(dict, "SettingsBackground");
         if (bkgrndBrush != null)
         {
             panel.Background = backgroundBrush;
         }
        */
        if (dictionary == null || key == null)
            return null;

        if (dictionary.Contains(key))
            return dictionary[key] as T;

        // Search merged dictionaries recursively
        foreach (var merged in dictionary.MergedDictionaries)
        {
            var found = TryGetFromDictionary<T>(merged, key);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// Convert a <see cref="DateTime"/> object into an ISO 8601 formatted string.
    /// </summary>
    /// <param name="dateTime"><see cref="DateTime"/></param>
    /// <returns>ISO 8601 formatted string</returns>
    /// <remarks>You can also use <c>DateTime.UtcNow.ToString("o")</c></remarks>
    public static string ToJsonFormat(this DateTime dateTime) => dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

    /// <summary>
    /// Converts a JSON date time format string "yyyy-MM-ddTHH:mm:ssZ" into a DateTime object.
    /// </summary>
    /// <param name="jsonDateTimeString">The JSON date time string to convert (e.g., "2023-10-27T10:30:00Z").</param>
    /// <returns>A DateTime object representing the parsed date and time, or null if the string is invalid.</returns>
    public static DateTime ParseJsonDateTime(this string jsonDateTimeString)
    {
        if (string.IsNullOrWhiteSpace(jsonDateTimeString))
            return DateTime.MinValue;

        try
        {
            return DateTime.ParseExact(jsonDateTimeString, "yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal);
        }
        catch (FormatException)
        {
            return DateTime.MinValue;
        }
    }

    public static IEnumerable<T> JoinLists<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
    {
        var joined = new[] { list1, list2 }.Where(x => x != null).SelectMany(x => x);
        return joined ?? Enumerable.Empty<T>();
    }

    public static IEnumerable<T> JoinMany<T>(params IEnumerable<T>[] array)
    {
        var final = array.Where(x => x != null).SelectMany(x => x);
        return final ?? Enumerable.Empty<T>();
    }

    public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action, Action<Exception>? onError = null)
    {
        foreach (var i in ie)
        {
            try { action(i); }
            catch (Exception ex) { onError?.Invoke(ex); }
        }
    }

    public static string NameOf(this object o)
    {
        if (o == null)
            return "null";

        // Similar: System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name
        return $"{o?.GetType()?.Name} ⇒ {o?.GetType()?.BaseType?.Name}";
    }

    public static bool IsDisposable(this Type type)
    {
        if (!typeof(IDisposable).IsAssignableFrom(type))
            return false;

        return true;
    }

    public static bool IsClonable(this Type type)
    {
        if (!typeof(ICloneable).IsAssignableFrom(type))
            return false;
        return true;
    }

    public static bool IsComparable(this Type type)
    {
        if (!typeof(IComparable).IsAssignableFrom(type))
            return false;
        return true;
    }

    public static bool IsConvertible(this Type type)
    {
        if (!typeof(IConvertible).IsAssignableFrom(type))
            return false;
        return true;
    }

    public static bool IsFormattable(this Type type)
    {
        if (!typeof(IFormattable).IsAssignableFrom(type))
            return false;
        return true;
    }

    public static bool IsEnumerable<T>(this Type type)
    {
        if (!typeof(IEnumerable<T>).IsAssignableFrom(type))
            return false;
        return true;
    }

    /// <summary>
    ///   Generic retry mechanism with 2-second retry until <paramref name="attempts"/>.
    /// </summary>
    public static T Retry<T>(this Func<T> operation, int attempts)
    {
        while (true)
        {
            try
            {
                attempts--;
                return operation();
            }
            catch (Exception ex) when (attempts > 0)
            {
                Debug.WriteLine($"[ERROR] Failed: {ex.Message}");
                Debug.WriteLine($"[INFO] Attempts left: {attempts}");
                Thread.Sleep(2000);
            }
        }
    }

    /// <summary>
    ///   Generic retry mechanism with exponential back-off
    /// <example><code>
    ///   Retry(() => MethodThatHasNoReturnValue());
    /// </code></example>
    /// </summary>
    public static void Retry(this Action action, int maxRetry = 3, int retryDelay = 1000)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                action();
                break;
            }
            catch (Exception ex)
            {
                retries++;
                if (retries > maxRetry)
                {
                    throw new TimeoutException($"Operation failed after {maxRetry} retries: {ex.Message}", ex);
                }
                Debug.WriteLine($"[ERROR] Retry {retries}/{maxRetry} after failure: {ex.Message}. Retrying in {retryDelay} ms...");
                Thread.Sleep(retryDelay);
                retryDelay *= 2; // Double the delay after each attempt.
            }
        }
    }

    /// <summary>
    ///   Modified retry mechanism for return value with exponential back-off.
    /// <example><code>
    ///   int result = Retry(() => MethodThatReturnsAnInteger());
    /// </code></example>
    /// </summary>
    public static T Retry<T>(this Func<T> func, int maxRetry = 3, int retryDelay = 1000)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                retries++;
                if (retries > maxRetry)
                {
                    throw new TimeoutException($"Operation failed after {maxRetry} retries: {ex.Message}", ex);
                }
                Debug.WriteLine($"[ERROR] Retry {retries}/{maxRetry} after failure: {ex.Message}. Retrying in {retryDelay} ms...");
                Thread.Sleep(retryDelay);
                retryDelay *= 2; // Double the delay after each attempt.
            }
        }
    }

    /// <summary>
    ///   Generic retry mechanism with exponential back-off
    /// <example><code>
    ///   await RetryAsync(() => AsyncMethodThatHasNoReturnValue());
    /// </code></example>
    /// </summary>
    public static async Task RetryAsync(this Func<Task> action, int maxRetry = 3, int retryDelay = 1000)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                await action();
                break;
            }
            catch (Exception ex)
            {
                retries++;
                if (retries > maxRetry)
                {
                    throw new InvalidOperationException($"Operation failed after {maxRetry} retries: {ex.Message}", ex);
                }
                Debug.WriteLine($"[ERROR] Retry {retries}/{maxRetry} after failure: {ex.Message}. Retrying in {retryDelay} ms...");
                await Task.Delay(retryDelay);
                retryDelay *= 2; // Double the delay after each attempt.
            }
        }
    }

    /// <summary>
    ///   Modified retry mechanism for return value with exponential back-off.
    /// <example><code>
    ///   int result = await RetryAsync(() => AsyncMethodThatReturnsAnInteger());
    /// </code></example>
    /// </summary>
    public static async Task<T> RetryAsync<T>(this Func<Task<T>> func, int maxRetry = 3, int retryDelay = 1000)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                retries++;
                if (retries > maxRetry)
                {
                    throw new InvalidOperationException($"Operation failed after {maxRetry} retries: {ex.Message}", ex);
                }
                Debug.WriteLine($"[ERROR] Retry {retries}/{maxRetry} after failure: {ex.Message}. Retrying in {retryDelay} ms...");
                await Task.Delay(retryDelay);
                retryDelay *= 2; // Double the delay after each attempt.
            }
        }
    }

    public const double Epsilon = 0.000000000001;
    public static bool IsZeroOrLess(this double value) => value < Epsilon;
    public static bool IsZeroOrLess(this float value) => value < (float)Epsilon;
    public static bool IsZero(this double value) => Math.Abs(value) < Epsilon;
    public static bool IsZero(this float value) => Math.Abs(value) < (float)Epsilon;
    public static bool IsInvalid(this double value)
    {
        if (value == double.NaN || value == double.NegativeInfinity || value == double.PositiveInfinity)
            return true;

        return false;
    }
    public static bool IsInvalidOrZero(this double value)
    {
        if (value == double.NaN || value == double.NegativeInfinity || value == double.PositiveInfinity || value <= 0)
            return true;

        return false;
    }
    public static bool IsOne(this double value)
    {
        return Math.Abs(value) >= 1d - Epsilon && Math.Abs(value) <= 1d + Epsilon;
    }
    public static bool AreClose(this double left, double right)
    {
        if (left == right)
            return true;

        double a = (Math.Abs(left) + Math.Abs(right) + 10.0d) * Epsilon;
        double b = left - right;
        return (-a < b) && (a > b);
    }
    public static bool AreClose(this float left, float right)
    {
        if (left == right)
            return true;

        float a = (Math.Abs(left) + Math.Abs(right) + 10.0f) * (float)Epsilon;
        float b = left - right;
        return (-a < b) && (a > b);
    }

    /// <summary>
    /// Home-brew parallel invoke that will not block while actions run.
    /// </summary>
    /// <param name="actions">array of <see cref="Action"/>s</param>
    public static void ParallelInvokeAndForget(params Action[] action)
    {
        action.ForEach(a =>
        {
            try
            {
                ThreadPool.QueueUserWorkItem((obj) => { a.Invoke(); });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] ParallelInvokeAndForget: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// An un-optimized, home-brew parallel for each implementation.
    /// </summary>
    public static void ParallelForEach<T>(IEnumerable<T> source, Action<T> action)
    {
        var tasks = from item in source select Task.Run(() => action(item));
        Task.WaitAll(tasks.ToArray());
    }

    /// <summary>
    /// An optimized, home-brew parallel ForEach implementation.
    /// Creates branched execution based on available processors.
    /// </summary>
    public static void ParallelForEachUsingEnumerator<T>(IEnumerable<T> source, Action<T> action, Action<Exception> onError)
    {
        IEnumerator<T> e = source.GetEnumerator();
        IEnumerable<Task> tasks = from i
             in Enumerable.Range(0, Environment.ProcessorCount)
                                  select Task.Run(() =>
                                  {
                                      while (true)
                                      {
                                          T item;
                                          lock (e)
                                          {
                                              if (!e.MoveNext()) { return; }
                                              item = e.Current;
                                          }
                                          #region [Must stay outside locking scope, or defeats the purpose of parallelism]
                                          try
                                          {
                                              action(item);
                                          }
                                          catch (Exception ex)
                                          {
                                              onError?.Invoke(ex);
                                          }
                                          #endregion
                                      }
                                  });
        Task.WaitAll(tasks.ToArray());
    }

    /// <summary>
    /// An optimized, home-brew parallel ForEach implementation.
    /// Creates branched execution based on available processors.
    /// </summary>
    public static void ParallelForEachUsingPartitioner<T>(IEnumerable<T> source, Action<T> action, Action<Exception> onError, EnumerablePartitionerOptions options = EnumerablePartitionerOptions.NoBuffering)
    {
        //IList<IEnumerator<T>> partitions = Partitioner.Create(source, options).GetPartitions(Environment.ProcessorCount);
        IEnumerable<Task> tasks = from partition
            in Partitioner.Create(source, options).GetPartitions(Environment.ProcessorCount)
                                  select Task.Run(() =>
                                  {
                                      using (partition) // partitions are disposable
                                      {
                                          while (partition.MoveNext())
                                          {
                                              try
                                              {
                                                  action(partition.Current);
                                              }
                                              catch (Exception ex)
                                              {
                                                  onError?.Invoke(ex);
                                              }
                                          }
                                      }
                                  });
        Task.WaitAll(tasks.ToArray());
    }

    /// <summary>
    /// An optimized, home-brew parallel ForEach implementation.
    /// </summary>
    public static void ParallelForEachUsingPartitioner<T>(IList<T> list, Action<T> action, Action<Exception> onError, EnumerablePartitionerOptions options = EnumerablePartitionerOptions.NoBuffering)
    {
        //IList<IEnumerator<T>> partitions = Partitioner.Create(list, options).GetPartitions(Environment.ProcessorCount);
        IEnumerable<Task> tasks = from partition
            in Partitioner.Create(list, options).GetPartitions(Environment.ProcessorCount)
                                  select Task.Run(() =>
                                  {
                                      using (partition) // partitions are disposable
                                      {
                                          while (partition.MoveNext())
                                          {
                                              try
                                              {
                                                  action(partition.Current);
                                              }
                                              catch (Exception ex)
                                              {
                                                  onError?.Invoke(ex);
                                              }
                                          }
                                      }
                                  });
        Task.WaitAll(tasks.ToArray());
    }

    #region [Task Helpers]
    /// <summary>
    /// Semaphore extension method for disposable tasks.
    /// </summary>
    /// <param name="ss"><see cref="SemaphoreSlim"/></param>
    /// <returns>a disposable task</returns>
    public static async Task<IDisposable> EnterAsync(this SemaphoreSlim ss)
    {
        await ss.WaitAsync().ConfigureAwait(false);
        return Disposable.Create(() => ss.Release());
    }

    /// <summary>
    /// Task.Factory.StartNew (() => { throw null; }).IgnoreExceptions();
    /// </summary>
    public static void IgnoreExceptions(this Task task, Action<Exception>? errorHandler = null)
    {
        task.ContinueWith(t =>
        {
            AggregateException? ignore = t.Exception;
            ignore?.Flatten().Handle(ex =>
            {
                if (errorHandler != null)
                    errorHandler(ex);

                return true; // don't re-throw
            });

        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// Chainable task helper.
    /// var result = await SomeLongAsyncFunction().WithTimeout(TimeSpan.FromSeconds(2));
    /// </summary>
    /// <typeparam name="TResult">the type of task result</typeparam>
    /// <returns><see cref="Task"/>TResult</returns>
    public async static Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
    {
        Task winner = await (Task.WhenAny(task, Task.Delay(timeout)));

        if (winner != task)
            throw new TimeoutException();

        return await task; // Unwrap result/re-throw
    }

    /// <summary>
    /// Task extension to add a timeout.
    /// </summary>
    /// <returns>The task with timeout.</returns>
    /// <param name="task">Task.</param>
    /// <param name="timeoutInMilliseconds">Timeout duration in Milliseconds.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    public async static Task<T> WithTimeout<T>(this Task<T> task, int timeoutInMilliseconds)
    {
        var retTask = await Task.WhenAny(task, Task.Delay(timeoutInMilliseconds)).ConfigureAwait(false);

#pragma warning disable CS8603 // Possible null reference return.
        return retTask is Task<T> ? task.Result : default;
#pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary>
    /// Chainable task helper.
    /// var result = await SomeLongAsyncFunction().WithCancellation(cts.Token);
    /// </summary>
    /// <typeparam name="TResult">the type of task result</typeparam>
    /// <returns><see cref="Task"/>TResult</returns>
    public static Task<TResult> WithCancellation<TResult>(this Task<TResult> task, CancellationToken cancelToken)
    {
        TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
        CancellationTokenRegistration reg = cancelToken.Register(() => tcs.TrySetCanceled());
        task.ContinueWith(ant =>
        {
            reg.Dispose(); // NOTE: it's important to dispose of CancellationTokenRegistrations or they will hand around in memory until the application closes
            if (ant.IsCanceled)
                tcs.TrySetCanceled();
            else if (ant.IsFaulted)
                tcs.TrySetException(ant.Exception?.InnerException ?? ant.Exception ?? new Exception("No exception information available."));
            else
                tcs.TrySetResult(ant.Result);
        });
        return tcs.Task; // Return the TaskCompletionSource result
    }

    public static Task<T> WithAllExceptions<T>(this Task<T> task)
    {
        TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

        task.ContinueWith(ignored =>
        {
            switch (task.Status)
            {
                case TaskStatus.Canceled:
                    Debug.WriteLine($"[WARNING] TaskStatus.Canceled");
                    tcs.SetCanceled();
                    break;
                case TaskStatus.RanToCompletion:
                    tcs.SetResult(task.Result);
                    //Debug.WriteLine($"[INFO] TaskStatus.RanToCompletion");
                    break;
                case TaskStatus.Faulted:
                    // SetException will automatically wrap the original AggregateException
                    // in another one. The new wrapper will be removed in TaskAwaiter, leaving
                    // the original intact.
                    Debug.WriteLine($"[ERROR] TaskStatus.Faulted: {task.Exception?.Message}");
                    tcs.SetException(task.Exception ?? new Exception("No exception information available."));
                    break;
                default:
                    Debug.WriteLine($"[ERROR] TaskStatus: Continuation called illegally.");
                    tcs.SetException(new InvalidOperationException("Continuation called illegally."));
                    break;
            }
        });

        return tcs.Task;
    }

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
    /// <summary>
    /// Attempts to await on the task and catches exception
    /// </summary>
    /// <param name="task">Task to execute</param>
    /// <param name="onException">What to do when method has an exception</param>
    /// <param name="continueOnCapturedContext">If the context should be captured.</param>
    public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null, bool continueOnCapturedContext = false)
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex) when (onException != null)
        {
            onException.Invoke(ex);
        }
        catch (Exception ex) when (onException == null)
        {
            Debug.WriteLine($"[WARNING] SafeFireAndForget: {ex.Message}");
        }
    }
    #endregion

    /// <summary>
    /// A  WinForms-like "DoEvents" UI repaint.
    /// </summary>
    /// <param name="useNestedFrame">if true, employ <see cref="Dispatcher.PushFrame"/></param>
    public static void DoEvents(bool useNestedFrame = false)
    {
        if (!useNestedFrame)
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new System.Threading.ThreadStart(() => System.Threading.Thread.Sleep(0)));
        else
        {
            // Create new nested message pump.
            DispatcherFrame nested = new DispatcherFrame(true);

            // Dispatch a callback to the current message queue, when getting called,
            // this callback will end the nested message loop. The priority of this
            // callback should always be lower than that of the UI event messages.
            #pragma warning disable CS8622 // Nullability of reference types
            var exitFrameOp = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, (SendOrPostCallback)delegate (object arg)
            {
                var f = arg as DispatcherFrame;
                if (f != null) { f.Continue = false; }
            }, nested);
            #pragma warning restore CS8622

            // Pump the nested message loop, the nested message loop will immediately
            // process the messages left inside the message queue.
            Dispatcher.PushFrame(nested);

            // If the exit frame callback doesn't get completed, abort it.
            if (exitFrameOp.Status != DispatcherOperationStatus.Completed)
                exitFrameOp.Abort();
        }
    }

    /// <summary>
    /// A handy method to determine if a call to the Application Dispatcher is necessary.
    /// </summary>
    /// <param name="action">The <see cref="Action"/> to perform.</param>
    public static void InvokeIf(Action execute)
    {
        if (System.Threading.Thread.CurrentThread == Application.Current.Dispatcher.Thread)
            execute();
        else
            Application.Current.Dispatcher.Invoke(execute);

        /** Other Techniques **/
        /*
        System.Windows.Threading.Dispatcher dispatcher = System.Windows.Threading.Dispatcher.FromThread(Thread.CurrentThread);
        if (dispatcher != null)
        {
            // We know the thread have a dispatcher that we can use.
            CustomMessageBox.Show($" Thread test at {DateTime.Now.ToLongTimeString()} ", $"{App.SystemTitle}", 2000); // show for 2 seconds
        }

        // -- Also --
        if (dispatcher != null && dispatcher.CheckAccess())
        {
            try
            {
                dispatcher.VerifyAccess();
                // Calling thread is associated with the Dispatcher, so...
                // do whatever you want here and know it's on the GUI thread.
            }
            catch (InvalidOperationException)
            {
                // Thread can't use dispatcher
            }
        }
        */
    }

    /// <summary>
    /// Runs a command if the updating flag is not set.
    /// If the flag is true (indicating the function is already running) then the action is not run.
    /// If the flag is false (indicating no running function) then the action is run.
    /// Once the action is finished if it was run, then the flag is reset to false
    /// </summary>
    /// <param name="updatingFlag">The boolean property flag defining if the command is already running. This variable must be a property.</param>
    /// <param name="action">The action to run if the command is not already running.</param>
    /// <remakes>
    /// The flag must be a property, not a field, so that the expression tree can get/set the value.
    /// </remarks>
    public static async Task RunCommandAsync(System.Linq.Expressions.Expression<Func<bool>> updatingFlag, Func<Task> action)
    {
        // Check if the flag property is true (meaning the function is already running)
        if (updatingFlag.GetPropertyValue())
            return;

        // Set the property flag to true to indicate we are running
        updatingFlag.SetPropertyValue(true);

        try
        {
            // Run the passed in action
            await action();
        }
        finally
        {
            // Set the property flag back to false now it's finished
            updatingFlag.SetPropertyValue(false);
        }
    }

    /// <summary>
    /// Compiles an expression and gets the functions return value
    /// </summary>
    /// <typeparam name="T">The type of return value</typeparam>
    /// <param name="lambda">The expression to compile</param>
    /// <returns></returns>
    public static T GetPropertyValue<T>(this System.Linq.Expressions.Expression<Func<T>> lambda)
    {
        // Compile & invoke the expression and return the target
        return lambda.Compile().Invoke();
    }

    /// <summary>
    /// Sets the underlying properties value to the given value
    /// from an expression that contains the property
    /// </summary>
    /// <typeparam name="T">The type of value to set</typeparam>
    /// <param name="lambda">The expression</param>
    /// <param name="value">The value to set the property to</param>
    public static void SetPropertyValue<T>(this System.Linq.Expressions.Expression<Func<T>> lambda, T value)
    {
        // Converts a lambda () => some.Property, to some.Property
        var expression = (lambda as System.Linq.Expressions.LambdaExpression).Body as System.Linq.Expressions.MemberExpression;
        if (expression == null)
            return;

        // Get the property information so we can set it
        var propertyInfo = (System.Reflection.PropertyInfo?)expression?.Member;

        // Compile & invoke the expression to get the target object instance
        var target = System.Linq.Expressions.Expression.Lambda(expression?.Expression)?.Compile()?.DynamicInvoke();
        if (target == null)
            return;

        // Set the property value
        propertyInfo?.SetValue(target, value);

    }

    /// <summary>
    /// Fetch all referenced <see cref="System.Reflection.AssemblyName"/> used by the current process.
    /// </summary>
    /// <returns><see cref="List{T}"/></returns>
    public static List<string> ListAllAssemblies()
    {
        List<string> results = new List<string>();
        try
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Reflection.AssemblyName main = assembly.GetName();
            results.Add($"Main Assembly: {main.Name}, Version: {main.Version}");
            IOrderedEnumerable<System.Reflection.AssemblyName> names = assembly.GetReferencedAssemblies().OrderBy(o => o.Name);
            foreach (var sas in names)
                results.Add($"Sub Assembly: {sas.Name}, Version: {sas.Version}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] ListAllAssemblies: {ex.Message}");
        }
        return results;
    }

    /// <summary>
    /// Reflects the AssemblyInfo attributes
    /// </summary>
    public static string ReflectAssemblyFramework(this Type type, bool extraDetails = false)
    {
        try
        {
            System.Reflection.Assembly assembly = type.Assembly;
            if (assembly != null)
            {
                // Versioning
                var frameAttr = (TargetFrameworkAttribute)assembly.GetCustomAttributes(typeof(TargetFrameworkAttribute), false)[0];
                var targetAttr = (TargetPlatformAttribute)assembly.GetCustomAttributes(typeof(TargetPlatformAttribute), false)[0];
                var supportedAttr = (SupportedOSPlatformAttribute)assembly.GetCustomAttributes(typeof(SupportedOSPlatformAttribute), false)[0];
                // Reflection
                var compAttr = (AssemblyCompanyAttribute)assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0];
                var confAttr = (AssemblyConfigurationAttribute)assembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false)[0];
                var fileVerAttr = (AssemblyFileVersionAttribute)assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)[0];
                var infoAttr = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0];
                var nameAttr = (AssemblyProductAttribute)assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
                var titleAttr = (AssemblyTitleAttribute)assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0];
                if (extraDetails)
                {
                    return string.Format("{0}  v{1}  ©  {4}  –  {3}  [{2}]  ({5})",
                        nameAttr.Product,
                        fileVerAttr.Version,
                        string.IsNullOrEmpty(confAttr.Configuration) ? "N/A" : confAttr.Configuration,
                        string.IsNullOrEmpty(frameAttr.FrameworkDisplayName) ? frameAttr.FrameworkName : frameAttr.FrameworkDisplayName,
                        !string.IsNullOrEmpty(compAttr.Company) ? compAttr.Company : Environment.UserName,
                        System.Runtime.InteropServices.RuntimeInformation.OSDescription);
                }
                else
                {
                    return string.Format("{0}  –  v{1}", nameAttr.Product, fileVerAttr.Version);
                }
            }
        }
        catch (Exception) { }
        return string.Empty;
    }

    /// <summary>
    /// Reflects the assembly code base attribute (uses Location since CodeBase was deprecated).
    /// </summary>
    public static string ReflectAssemblyCodeBase(this Type type)
    {
        try
        {
            return type.Assembly?.Location ?? string.Empty;
        }
        catch (Exception) { return string.Empty; }
    }

    /// <summary>
    /// Fetches the custom attribute <see cref="AssemblyConfigurationAttribute"/> as a string.
    /// </summary>
    public static string GetBuildConfig(this Type type)
    {
        try
        {
            AssemblyConfigurationAttribute? confAttr = type?.Assembly?.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false)[0] as AssemblyConfigurationAttribute;
            if (confAttr != null)
                return $"{confAttr.Configuration}";
        }
        catch (Exception) { }
        return string.Empty;
    }

    /// <summary>
    /// Fetches the <see cref="System.Runtime.InteropServices.RuntimeInformation"/> properties as a string.
    /// </summary>
    public static string GetRuntimeInfo()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} ({System.Runtime.InteropServices.RuntimeInformation.OSArchitecture})");
        sb.Append("  –  ");
        sb.Append($"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} ({System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier})");
        return $"{sb}";
    }

    /// <summary>
    /// Helpful when dealing with ".lnk" UTF16LE shortcut files.
    /// </summary>
    public static List<string> ExtractAllStrings(string filePath, int minAsciiChars = 4, int minUtf16Chars = 4)
    {
        var results = new List<string>();
        try
        {
            byte[] bytes = System.IO.File.ReadAllBytes(filePath);

            #region [ASCII scan]
            var asciiBuilder = new StringBuilder();
            foreach (byte b in bytes)
            {
                if (b >= 0x20 && b <= 0x7E) // printable ASCII
                {
                    asciiBuilder.Append((char)b);
                }
                else
                {
                    if (asciiBuilder.Length >= minAsciiChars)
                        results.Add(asciiBuilder.ToString());
                    asciiBuilder.Clear();
                }
            }
            if (asciiBuilder.Length >= minAsciiChars)
                results.Add(asciiBuilder.ToString());
            #endregion

            #region [UTF-16 Little Endian scan]
            int i = 0;
            while (i < bytes.Length - 1)
            {
                int start = i;
                int charCount = 0;

                while (i < bytes.Length - 1 && bytes[i] >= 0x20 && bytes[i] <= 0x7E && bytes[i + 1] == 0x00)
                {
                    charCount++;
                    i += 2;
                }

                // Do we have enough?
                if (charCount >= minUtf16Chars)
                {
                    // Decode the slice as UTF-16LE
                    int lengthBytes = charCount * 2;
                    string s = Encoding.Unicode.GetString(bytes, start, lengthBytes);
                    results.Add(s);
                }

                i += 2;
            }
            #endregion

            return results.Distinct().ToList();
        }
        catch (Exception) { return results; }
    }

    /// <summary>
    /// Removes an element from the middle of a queue without disrupting the other elements.
    /// </summary>
    /// <typeparam name="T">The element to remove.</typeparam>
    /// <param name="queue">The queue to modify.</param>
    /// <param name="valueToRemove">The value to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the value was found and removed, <see langword="false"/> if no match was found.
    /// </returns>
    /// <remarks>
    /// If a value appears multiple times in the queue, only its first entry is removed.<br/>
    /// This method could be costly if the queue is extremely large.
    /// </remarks>
    public static bool RemoveFromQueue<T>(this Queue<T> queue, T valueToRemove) where T : class
    {
        if (valueToRemove == null)
            throw new ArgumentNullException(nameof(valueToRemove));
        if (queue == null)
            throw new ArgumentNullException(nameof(queue));
        if (queue.Count == 0)
            return false;

        bool found = false;
        int originalCount = queue.Count;
        int dequeueCounter = 0;
        while (dequeueCounter < originalCount)
        {
            dequeueCounter++;
            T dequeued = queue.Dequeue();
            if (!found && dequeued == valueToRemove)
            { 
                // don't enqueue since the goal is to remove
                found = true;
            }
            else
            {
                queue.Enqueue(dequeued);
            }
        }
        return found;
    }
    /// <summary>
    /// Removes an element from the middle of a queue without disrupting the other elements.
    /// </summary>
    /// <typeparam name="T">The element to remove.</typeparam>
    /// <param name="queue">The queue to modify.</param>
    /// <param name="valueToRemove">The value to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the value was found and removed, <see langword="false"/> if no match was found.
    /// </returns>
    /// <remarks>
    /// If a value appears multiple times in the queue, only its first entry is removed.<br/>
    /// This method could be costly if the queue is extremely large.
    /// </remarks>
    public static bool RemoveFromQueue<T>(this ConcurrentQueue<T> queue, T valueToRemove) where T : class
    {
        if (valueToRemove == null)
            throw new ArgumentNullException(nameof(valueToRemove));
        if (queue == null)
            throw new ArgumentNullException(nameof(queue));
        if (queue.IsEmpty)
            return false;

        bool found = false;
        int originalCount = queue.Count;
        int dequeueCounter = 0;
        int failCounter = 0;
        while (dequeueCounter < originalCount && failCounter < 50)
        {
            if (queue.TryDequeue(out T? dequeued))
            {
                dequeueCounter++;
                if (!found && dequeued != null && dequeued == valueToRemove)
                {
                    // don't enqueue since the goal is to remove
                    found = true;
                }
                else if (dequeued != null)
                {
                    queue.Enqueue(dequeued);
                }
            }
            else
            {
                failCounter++;
            }
        }
        return found;
    }
}

/// <summary>
/// A chainable brush helper class.
/// </summary>
public class BrushAdjustmentChain
{
    readonly Color _originalColor;
    double _h, _s, _v;

    public BrushAdjustmentChain(Color color)
    {
        _originalColor = color;
        RgbToHsv(color.R, color.G, color.B, out _h, out _s, out _v);
    }

    public static BrushAdjustmentChain From(SolidColorBrush brush)
    {
        if (brush == null)
            throw new ArgumentNullException(nameof(brush));

        return new BrushAdjustmentChain(brush.Color);
    }

    public BrushAdjustmentChain Brighten(double amount)
    {
        _v = ClampLocal(_v + amount);
        return this;
    }

    public BrushAdjustmentChain Darken(double amount)
    {
        _v = ClampLocal(_v - amount);
        return this;
    }

    public BrushAdjustmentChain Saturate(double amount)
    {
        _s = ClampLocal(_s + amount);
        return this;
    }

    public BrushAdjustmentChain Desaturate(double amount)
    {
        _s = ClampLocal(_s - amount);
        return this;
    }

    public BrushAdjustmentChain ShiftHue(double degrees)
    {
        _h = (_h + degrees) % 360;
        if (_h < 0) _h += 360;
        return this;
    }

    public SolidColorBrush Apply()
    {
        var (r, g, b) = HsvToRgb(_h, _s, _v);
        var brush = new SolidColorBrush(Color.FromArgb(_originalColor.A, r, g, b));
        if (brush.CanFreeze) { brush.Freeze(); }
        return brush;
    }

    public string ToHex(bool includeAlpha = false)
    {
        var (r, g, b) = HsvToRgb(_h, _s, _v);
        byte a = _originalColor.A;
        return includeAlpha ? $"#{a:X2}{r:X2}{g:X2}{b:X2}" : $"#{r:X2}{g:X2}{b:X2}";
    }

    static double ClampLocal(double value) => Math.Max(0.0, Math.Min(1.0, value));

    static (byte r, byte g, byte b) HsvToRgb(double h, double s, double v)
    {
        // h: [0,360), s,v: [0,1]
        if (s <= 0.00001)
        {
            // If saturation is approx zero then return achromatic (grey)
            byte grey = (byte)Math.Round(v * 255.0);
            return (grey, grey, grey);
        }

        h = (h % 360 + 360) % 360; // normalize
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = v - c;

        //double (r1, g1, b1) = h switch
        //{
        //    < 60 => (c, x, 0),
        //    < 120 => (x, c, 0),
        //    < 180 => (0, c, x),
        //    < 240 => (0, x, c),
        //    < 300 => (x, 0, c),
        //    _ => (c, 0, x)
        //};
        double r1, g1, b1;
        if (h < 60) { r1 = c; g1 = x; b1 = 0; }
        else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
        else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
        else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
        else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
        else { r1 = c; g1 = 0; b1 = x; }
        byte r = (byte)Math.Round((r1 + m) * 255.0);
        byte g = (byte)Math.Round((g1 + m) * 255.0);
        byte b = (byte)Math.Round((b1 + m) * 255.0);
        return (r, g, b);
    }

    static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        // Hue
        if (delta < 0.00001) { h = 0; }
        else if (max == rd) { h = 60 * (((gd - bd) / delta) % 6); }
        else if (max == gd) { h = 60 * (((bd - rd) / delta) + 2); }
        else { h = 60 * (((rd - gd) / delta) + 4); }
        if (h < 0) { h += 360; }

        // Saturation
        s = (max <= 0) ? 0 : delta / max;

        // Value
        v = max;
    }
}

/// <summary>
/// Provides a set of static methods for creating Disposables.
/// This is based off of
/// https://docs.microsoft.com/en-us/previous-versions/dotnet/reactive-extensions/hh229792(v=vs.103)
/// </summary>
public static class Disposable
{
    /// <summary>
    /// Creates the disposable that invokes the specified action when disposed.
    /// </summary>
    /// <example>
    /// using var scope = Disposable.Create(() => Console.WriteLine("Done!"))
    /// {
    ///    [Do stuff here]
    /// } // scope is disposed and Working on it... is printed to console 
    /// </example>
    /// <param name="onDispose">The action to run during IDisposable.Dispose.</param>
    /// <returns>The disposable object that runs the given action upon disposal.</returns>
    public static IDisposable Create(Action onDispose) => new ActionDisposable(onDispose);

    class ActionDisposable : IDisposable
    {
        volatile Action? _onDispose;

        public ActionDisposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _onDispose, null)?.Invoke();
        }
    }
}

/// <summary>
/// Helper class for creating an asynchronous scope.
/// A scope is simply a using block that calls an async method
/// at the end of the block by returning an <see cref="IAsyncDisposable"/>.
/// This is the same concept as
/// the <see cref="Disposable.Create"/> method.
/// </summary>
/// <remarks>ValueTask.CompletedTask is only available in .NET 5.0 and up.</remarks>
/// http://haacked.com/archive/2021/12/10/async-disposables/
public static class AsyncDisposable
{
    /// <summary>
    /// Creates an <see cref="IAsyncDisposable"/> that calls
    /// the specified method asynchronously at the end
    /// of the scope upon disposal.
    /// </summary>
    /// <param name="onDispose">The method to call at the end of the scope.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that represents the scope.</returns>
    public static IAsyncDisposable Create(Func<ValueTask> onDispose)
    {
        return new AsyncScope(onDispose);
    }

    class AsyncScope : IAsyncDisposable
    {
        Func<ValueTask>? _onDispose;

        public AsyncScope(Func<ValueTask> onDispose)
        {
            _onDispose = onDispose;
        }

        public ValueTask DisposeAsync()
        {
            return Interlocked.Exchange(ref _onDispose, null)?.Invoke() ?? ValueTask.CompletedTask;
        }
    }
}
