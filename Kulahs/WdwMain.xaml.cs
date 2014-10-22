namespace Kulahs
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    public partial class WdwMain
    {
        private readonly ObservableCollection<Color> colors;

        private bool isMouseCaptured;

        private bool isBorderShown;

        private int alphaInteger;

        private string alphaHex;

        private Color color;

        public WdwMain()
        {
            InitializeComponent();
            Mouse.AddLostMouseCaptureHandler(this, this.WdwMainLostMouseCapture);
            DataObject.AddPastingHandler(this.TbHtml, this.OnPaste);
            this.alphaInteger = 255;
            this.alphaHex = string.Format("{0:X2}", this.alphaInteger);
            this.color = Colors.Black;
            this.colors = new ObservableCollection<Color>();
            this.colors.CollectionChanged += this.ColorsCollectionChanged;
            this.colors.Add(this.color);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            try
            {
                if (this.isMouseCaptured)
                {
                    Pt mousePosition;
                    GetCursorPos(out mousePosition);
                    var setColor = this.GetPixelColor(mousePosition.X, mousePosition.Y);
                    this.TbHtml.Text = setColor.ToString();
                }
            }
            catch
            {
                this.Error("Mouse Move Err");
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            try
            {
                Mouse.Capture(null);
                this.isMouseCaptured = false;
                Cursor = Cursors.Arrow;
                this.ImgEyedrop.Visibility = Visibility.Visible;
                this.ResetColors();
            }
            catch
            {
                this.Error("Mouse Up Err");
            }

            base.OnMouseUp(e);
        }

        [DllImport("user32.dll")]
        private static extern int GetCursorPos(out Pt lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        private static Rgb HsvToRgb(Hsv hsv)
        {
            double r = 0;
            double g = 0;
            double b = 0;

            if (hsv.Saturation == 0)
            {
                r = g = b = hsv.Value;
            }
            else
            {
                var sectorPos = hsv.Hue / 60.0;
                var sectorNumber = (int)Math.Floor(sectorPos);
                var fractionalSector = sectorPos - sectorNumber;
                var p = hsv.Value * (1.0 - hsv.Saturation);
                var q = hsv.Value * (1.0 - (hsv.Saturation * fractionalSector));
                var t = hsv.Value * (1.0 - (hsv.Saturation * (1 - fractionalSector)));

                switch (sectorNumber)
                {
                    case 0:
                        r = hsv.Value;
                        g = t;
                        b = p;
                        break;
                    case 1:
                        r = q;
                        g = hsv.Value;
                        b = p;
                        break;
                    case 2:
                        r = p;
                        g = hsv.Value;
                        b = t;
                        break;
                    case 3:
                        r = p;
                        g = q;
                        b = hsv.Value;
                        break;
                    case 4:
                        r = t;
                        g = p;
                        b = hsv.Value;
                        break;
                    case 5:
                        r = hsv.Value;
                        g = p;
                        b = q;
                        break;
                }
            }

            return new Rgb
            {
                Red = Convert.ToInt32(double.Parse(string.Format("{0:0.00}", r * 255.0))),
                Green = Convert.ToInt32(double.Parse(string.Format("{0:0.00}", g * 255.0))),
                Blue = Convert.ToInt32(double.Parse(string.Format("{0:0.00}", b * 255.0)))
            };
        }

        private static Hsv RgbToHsv(Rgb rgb)
        {
            var r = rgb.Red / 255.0;
            var g = rgb.Green / 255.0;
            var b = rgb.Blue / 255.0;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var h = 0.0;
            if (max == r && g >= b)
            {
                h = 60 * (g - b) / (max - min);
            }
            else if (max == r && g < b)
            {
                h = (60 * (g - b) / (max - min)) + 360;
            }
            else if (max == g)
            {
                h = (60 * (b - r) / (max - min)) + 120;
            }
            else if (max == b)
            {
                h = (60 * (r - g) / (max - min)) + 240;
            }

            var s = (max == 0) ? 0.0 : (1.0 - (min / max));
            var v = max;
            var hsv = new Hsv { Hue = double.IsNaN(h) ? 0 : h, Saturation = double.IsNaN(s) ? 0 : s, Value = v };
            return hsv;
        }

        private static Rgb HtmlToRgb(string html)
        {
            var c = (Color)ColorConverter.ConvertFromString(html);
            var rgb = new Rgb { Red = c.R, Green = c.G, Blue = c.B };
            return rgb;
        }

        private static string RgbToHtml(Rgb rgb, int alpha = 255)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", alpha, rgb.Red, rgb.Green, rgb.Blue).ToUpper();
        }

        private static Rgb ColorToRgb(Color c)
        {
            return new Rgb { Red = c.R, Green = c.G, Blue = c.B };
        }

        private static Color RgbToColor(Rgb rgb)
        {
            return new Color { R = (byte)rgb.Red, G = (byte)rgb.Green, B = (byte)rgb.Blue };
        }

        private Color GetPixelColor(int x, int y)
        {
            try
            {
                var hdc = GetDC(IntPtr.Zero);
                this.SetColors(GetPixel(hdc, x - 1, y - 1), "leftTop");
                this.SetColors(GetPixel(hdc, x, y - 1), "top");
                this.SetColors(GetPixel(hdc, x + 1, y - 1), "rightTop");
                this.SetColors(GetPixel(hdc, x - 1, y), "left");
                this.SetColors(GetPixel(hdc, x, y), "primary");
                this.SetColors(GetPixel(hdc, x + 1, y), "right");
                this.SetColors(GetPixel(hdc, x - 1, y + 1), "leftBottom");
                this.SetColors(GetPixel(hdc, x, y + 1), "bottom");
                this.SetColors(GetPixel(hdc, x + 1, y + 1), "rightBottom");
                ReleaseDC(IntPtr.Zero, hdc);
            }
            catch
            {
                this.Error("Get Pixel Err");
            }

            return this.color;
        }

        private string ConvertColor(string argb)
        {
            try
            {
                var validChars = new List<string> { "a", "r", "g", "b" };
                for (var i = 0; i < 10; i++)
                {
                    validChars.Add(i.ToString());
                }

                var testString = argb.ToLower();
                testString = testString.Where(c => !validChars.Contains(c.ToString())).Aggregate(testString, (current, c) => current.Replace(c.ToString(), string.Empty));
                var hexValue = new StringBuilder();
                var parsedNumbers = new StringBuilder();
                int hexInt;
                if (testString.Contains("r") && testString.Contains("g") && testString.Contains("b"))
                {
                    for (var i = testString.IndexOf('r') + 1; i < testString.Length; i++)
                    {
                        int p;
                        var q = testString.Substring(i, 1);
                        if (int.TryParse(q, out p))
                        {
                            parsedNumbers.Append(p);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (int.TryParse(parsedNumbers.ToString(), out hexInt) && hexInt < 256)
                    {
                        hexValue.Append(string.Format("{0:X2}", hexInt));
                    }
                    else
                    {
                        return argb;
                    }

                    parsedNumbers.Clear();
                    for (var i = testString.IndexOf('g') + 1; i < testString.Length; i++)
                    {
                        int p;
                        var q = testString.Substring(i, 1);
                        if (int.TryParse(q, out p))
                        {
                            parsedNumbers.Append(p);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (int.TryParse(parsedNumbers.ToString(), out hexInt) && hexInt < 256)
                    {
                        hexValue.Append(string.Format("{0:X2}", hexInt));
                    }
                    else
                    {
                        return argb;
                    }

                    parsedNumbers.Clear();
                    for (var i = testString.IndexOf('b') + 1; i < testString.Length; i++)
                    {
                        int p;
                        var q = testString.Substring(i, 1);
                        if (int.TryParse(q, out p))
                        {
                            parsedNumbers.Append(p);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (int.TryParse(parsedNumbers.ToString(), out hexInt) && hexInt < 256)
                    {
                        hexValue.Append(string.Format("{0:X2}", hexInt));
                    }
                    else
                    {
                        return argb;
                    }
                }
                else
                {
                    return argb;
                }

                if (testString.Contains("a"))
                {
                    parsedNumbers.Clear();
                    for (var i = testString.IndexOf('a') + 1; i < testString.Length; i++)
                    {
                        int p;
                        var q = testString.Substring(i, 1);
                        if (int.TryParse(q, out p))
                        {
                            parsedNumbers.Append(p);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (int.TryParse(parsedNumbers.ToString(), out hexInt) && hexInt < 256)
                    {
                        hexValue.Insert(0, string.Format("#{0:X2}", hexInt));
                        this.alphaInteger = hexInt;
                        this.alphaHex = string.Format("{0:X2}", hexInt);
                    }
                    else
                    {
                        return argb;
                    }
                }
                else
                {
                    hexValue.Insert(0, "#FF");
                    this.alphaInteger = 255;
                    this.alphaHex = string.Format("{0:X2}", 255);
                }

                return hexValue.ToString();
            }
            catch
            {
                return argb;
            }
        }

        private void BuildPalette()
        {
            try
            {
                this.SpPalette.Children.Clear();
                foreach (var b in this.colors.Select(c => new Border { Background = new SolidColorBrush(c), Height = 16, Width = 16, BorderBrush = new SolidColorBrush(Colors.Black) }))
                {
                    b.MouseDown += this.BdrPaletteMouseDown;
                    if (this.isBorderShown)
                    {
                        b.BorderThickness = new Thickness(0, 1, 1, 0);
                    }
                    else
                    {
                        b.BorderThickness = new Thickness(0);
                    }

                    this.SpPalette.Children.Add(b);
                }

                this.SvPalette.ScrollToHorizontalOffset(0);
            }
            catch
            {
                this.Error("Collection Err");
            }
        }

        private void ExportPalette()
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                              {
                                  FileName = "KulahsExport",
                                  DefaultExt = ".text",
                                  Filter = "Text documents (.txt)|*.txt"
                              };

                var result = dlg.ShowDialog();

                if (result == true)
                {
                    var sbc = new StringBuilder();
                    foreach (var swatch in this.colors)
                    {
                        sbc.Append("HTML: " + RgbToHtml(ColorToRgb(swatch), swatch.A) + " ");
                        sbc.Append("Alpha: " + swatch.A + " ");
                        sbc.Append("Red: " + swatch.R + " ");
                        sbc.Append("Green: " + swatch.G + " ");
                        sbc.Append("Blue: " + swatch.B + " ");
                        sbc.Append("Hue: " + RgbToHsv(ColorToRgb(swatch)).Hue + " ");
                        sbc.Append("Saturation: " + RgbToHsv(ColorToRgb(swatch)).Saturation + " ");
                        sbc.Append("Value: " + RgbToHsv(ColorToRgb(swatch)).Value);
                        sbc.Append(Environment.NewLine);
                    }

                    System.IO.File.WriteAllText(dlg.FileName, sbc.ToString());
                }
            }
            catch
            {
                this.Error("Export Err");
            }
        }

        private void Error(string err)
        {
            this.BdrColor.Background = new SolidColorBrush(Colors.Black);
            this.TkBadColor.Visibility = Visibility.Visible;
            this.TkBadColor.Text = err;
        }
        
        private void ResetColors()
        {
            this.BdrColorLeftTop.Background = null;
            this.BdrColorRightTop.Background = null;
            this.BdrColorTop.Background = null;
            this.BdrColorLeft.Background = null;
            this.BdrColorCenter.Background = null;
            this.BdrColorRight.Background = null;
            this.BdrColorLeftBottom.Background = null;
            this.BdrColorBottom.Background = null;
            this.BdrColorRightBottom.Background = null;
        }

        private void ToggleCorners()
        {
            this.BdrColor.CornerRadius = this.BdrColor.CornerRadius.TopLeft > 0 ? new CornerRadius(0) : new CornerRadius(6,6,0,0);
        }

        private void CycleSwatch()
        {
            try
            {
                if (this.ImgPalette.Source == FindResource("swatch1"))
                {
                    this.ImgPalette.Source = (ImageSource)FindResource("swatch2");
                    return;
                }

                if (this.ImgPalette.Source == FindResource("swatch2"))
                {
                    this.ImgPalette.Source = (ImageSource)FindResource("swatch3");
                    return;
                }

                this.ImgPalette.Source = (ImageSource)FindResource("swatch1");
            }
            catch
            {
                this.ImgPalette.Source = (ImageSource)FindResource("swatch1");
            }
        }

        private void SyncZoomSwatch()
        {
            this.ImgPaletteZoom.Source = this.ImgPalette.Source;
        }

        private void Help()
        {
            string helpText = @"
*******************
Kulahs " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + @"
*******************
Usage:
*F1 opens this help menu.
*Once opened the window will stay on top of everything.
*You can either type in a color in HEX (including the alpha channel and the #) or you can click and hold the eyedropper and let it go over whatever you want to extract color from on your screen
*Left-click, drag the color swatch to move the window.
*Left-click the palette to cycle through palette choices.
*P to toggle a larger palette.
*Mouse-wheel (or up/down arrow keys) over the main swatch to change alpha.
*Mouse-wheel (or up/down arrow keys) + R to change the Red channel.
*Mouse-wheel (or up/down arrow keys) + G to change the Green channel.
*Mouse-wheel (or up/down arrow keys) + B to change the Blue channel.
*Mouse-wheel (or up/down arrow keys) + H to change the Hue.
*Mouse-wheel (or up/down arrow keys) + S to change the Saturation.
*Mouse-wheel (or up/down arrow keys) + V to change the Value (Brightness).
*Right-click on the color swatch to add it to the swatch collection below (it won't add duplicates.)
*Left-click on a swatch in the swatch collection to set the main swatch and text to that color.
*If you have a lot of swatches in the collection you can mouse-wheel over the collection to scroll back and forth.
*Double-click toggles the toolbar.
*Shift-click removes the current swatch from behind the swatch collection.
*Ctrl-click resets the alpha to full (FF).
*Ctrl-B will put borders around each of the colors added to the swatch collection.
*Ctrl-R will toggle the rounded corners of the main swatch.
*Ctrl-S will allow you to export and save your current swatch collection.
*Ctrl-E will exit the app (will not prompt to save your swatches!)
*You can paste properly formatted RGB values directly into the text box to convert them to HTML. Format: as long as there is an 'r' then a number and the same for 'g' and 'b' with an optional 'a', in any order, it will parse it. This includes 'A=34, R=2, G=90, B=189' or 'a34b34r235g34'. As long as there is the channel and a number that is less than 256 (in that order), it will work.

Tips:
You can open multiple instances to compare color or overlap to see the impact of alpha changes.
You can see how a color with a < 100% alpha will look by putting it in the swatch collection and then setting the main swatch to something else.
If you want to see the entire swatch collection over something else or you don't want the main swatch to influence the combined color you can use the shift click to remove it.
Combined colors based on alpha differences are different colors! So if you set a color @ FF then reduce the alpha you can sample that color again as a combination of whatever is underneath.

Bugs:
Report anything you find to joel.vandenberg@bestbuy.com

Caveats:
I haven't tested this on anything but Windows 7.";
                MessageBox.Show(helpText, "Help");
        }

        private void SetColors(uint pixel, string sector)
        {
            try
            {
                var a = this.alphaHex.ToLower();
                var r = string.Format("{0:X2}", (byte)(pixel & 0xffL));
                var g = string.Format("{0:X2}", (byte)((pixel >> 8) & 0xffL));
                var b = string.Format("{0:X2}", (byte)((pixel >> 0x10) & 0xffL));
                switch (sector)
                {
                    case "leftTop":
                        {
                            this.BdrColorLeftTop.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, r, g, b)));
                            break;
                        }

                    case "top":
                        {
                            this.BdrColorTop.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, r, g, b)));
                            break;
                        }

                    case "rightTop":
                        {
                            this.BdrColorRightTop.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, r, g, b)));
                            break;
                        }

                    case "left":
                        {
                            this.BdrColorLeft.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, r, g, b)));
                            break;
                        }

                    case "primary":
                        {
                            this.color = (Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, r, g, b));
                            var x = this.screenShot.GetPixel(10, 10).Name;
                            Color c = Color.FromRgb(this.screenShot.GetPixel(10, 10).R, this.screenShot.GetPixel(10, 10).G, this.screenShot.GetPixel(10, 10).B);
                            this.BdrColorCenter.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, this.screenShot.GetPixel(10, 10).R, this.screenShot.GetPixel(10, 10).G, this.screenShot.GetPixel(10, 10).B)));
                            //this.BdrColorCenter.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, r, g, b)));
                            break;
                        }

                    case "right":
                        {
                            this.BdrColorRight.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, r, g, b)));
                            break;
                        }

                    case "leftBottom":
                        {
                            this.BdrColorLeftBottom.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, r, g, b)));
                            break;
                        }

                    case "bottom":
                        {
                            this.BdrColorBottom.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, r, g, b)));
                            break;
                        }

                    case "rightBottom":
                        {
                            this.BdrColorRightBottom.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.Format("#{0}{1}{2}{3}", a, r, g, b)));
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
            }
            catch
            {
                this.Error("Set Color Err");
            }
        }

        private void ColorsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.BuildPalette();
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            try
            {
                if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true))
                {
                    return;
                }

                var pasteText = e.SourceDataObject.GetData(DataFormats.Text) as string;
                if (pasteText != null && pasteText.Contains('='))
                {
                    e.CancelCommand();
                    this.TbHtml.Text = this.ConvertColor(pasteText);
                }
            }
            catch
            {
                this.Error("Color Parse Err");
            }
        }

        private void BdrPaletteMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.TbHtml.Text = ((Border)sender).Background.ToString();
                this.alphaHex = ((Border)sender).Background.ToString().Substring(1, 2);
                this.alphaInteger = int.Parse(this.alphaHex, System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                this.Error("Palette Err");
            }
        }

        private void BdrColorMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Mouse.Capture(null);
                    this.isMouseCaptured = false;
                    Cursor = Cursors.SizeAll;
                    this.DragMove();
                    Cursor = Cursors.Arrow;

                    if (e.ClickCount > 1)
                    {
                        Storyboard sb;
                        if (this.ToolBar.Opacity > 0)
                        {
                            sb = (Storyboard)FindResource("HideToolBar");
                        }
                        else
                        {
                            sb = (Storyboard)FindResource("ShowToolBar");
                        }

                        sb.Begin(this);
                    }

                    if (((Keyboard.GetKeyStates(Key.LeftCtrl) | Keyboard.GetKeyStates(Key.RightCtrl)) & KeyStates.Down) > 0)
                    {
                        this.alphaInteger = 255;
                        this.TbHtml.Text = RgbToHtml(new Rgb{Red = this.color.R, Green = this.color.G, Blue = this.color.B}, this.alphaInteger);
                    }

                    if (((Keyboard.GetKeyStates(Key.LeftShift) | Keyboard.GetKeyStates(Key.RightShift)) & KeyStates.Down) > 0)
                    {
                        Grid.SetRowSpan(this.BdrColor, Grid.GetRowSpan(this.BdrColor) > 1 ? 1 : 2);
                    }
                }

                if (e.RightButton == MouseButtonState.Pressed && !this.colors.Contains(this.color))
                {
                    this.colors.Insert(0, this.color);
                }
            }
            catch
            {
                this.Error("Swatch Input Err");
            }
        }

        private void BdrColorMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var rgb = HtmlToRgb(this.TbHtml.Text);
            if ((Keyboard.GetKeyStates(Key.R) & KeyStates.Down) > 0)
            {
                if (e.Delta > 0)
                {
                    rgb.Red = (rgb.Red + 3) <= 255 ? rgb.Red + 3 : 255;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }

                if (e.Delta < 0)
                {
                    rgb.Red = (rgb.Red - 3) >= 0 ? rgb.Red - 3 : 0;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }
            }

            if ((Keyboard.GetKeyStates(Key.G) & KeyStates.Down) > 0)
            {
                if (e.Delta > 0)
                {
                    rgb.Green = (rgb.Green + 3) <= 255 ? rgb.Green + 3 : 255;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }

                if (e.Delta < 0)
                {
                    rgb.Green = (rgb.Green - 3) >= 0 ? rgb.Green - 3 : 0;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }
            }

            if ((Keyboard.GetKeyStates(Key.B) & KeyStates.Down) > 0)
            {
                if (e.Delta > 0)
                {
                    rgb.Blue = (rgb.Blue + 3) <= 255 ? rgb.Blue + 3 : 255;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }

                if (e.Delta < 0)
                {
                    rgb.Blue = (rgb.Blue - 3) >= 0 ? rgb.Blue - 3 : 0;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }
            }

            var hsv = RgbToHsv(HtmlToRgb(this.TbHtml.Text));
            if ((Keyboard.GetKeyStates(Key.H) & KeyStates.Down) > 0)
            {
                if (e.Delta > 0)
                {
                    hsv.Hue = (hsv.Hue + 3) <= 359 ? hsv.Hue + 3 : 359;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }

                if (e.Delta < 0)
                {
                    hsv.Hue = (hsv.Hue - 3) >= 0 ? hsv.Hue - 3 : 0;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }
            }

            if ((Keyboard.GetKeyStates(Key.S) & KeyStates.Down) > 0)
            {
                if (e.Delta > 0)
                {
                    hsv.Saturation = (hsv.Saturation + .03) <= 1 ? hsv.Saturation + .03 : 1;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }

                if (e.Delta < 0)
                {
                    hsv.Saturation = (hsv.Saturation - .03) >= 0 ? hsv.Saturation - .03 : .01;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }
            }

            if ((Keyboard.GetKeyStates(Key.V) & KeyStates.Down) > 0)
            {
                if (e.Delta > 0)
                {
                    hsv.Value = (hsv.Value + .03) <= 1 ? hsv.Value + .03 : 1;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }

                if (e.Delta < 0)
                {
                    hsv.Value = (hsv.Value - .03) >= 0 ? hsv.Value - .03 : .01;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }
            }

            if (e.Delta > 0)
            {
                this.alphaInteger = (this.alphaInteger + 3) <= 255 ? this.alphaInteger + 3 : 255;
                this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                return;
            }

            if (e.Delta < 0)
            {
                this.alphaInteger = (this.alphaInteger - 3) >= 1 ? this.alphaInteger - 3 : 1;
                this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                return;
            }
        }

        private void WdwMainKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.GetKeyStates(Key.F1) & KeyStates.Down) > 0)
            {
                this.Help();
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control & (Keyboard.GetKeyStates(Key.S) & KeyStates.Down) > 0)
            {
                this.ExportPalette();
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control & (Keyboard.GetKeyStates(Key.B) & KeyStates.Down) > 0)
            {
                this.isBorderShown = this.isBorderShown == true ? this.isBorderShown = false : this.isBorderShown = true;
                this.BuildPalette();
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control & (Keyboard.GetKeyStates(Key.R) & KeyStates.Down) > 0)
            {
                this.ToggleCorners();
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control & (Keyboard.GetKeyStates(Key.E) & KeyStates.Down) > 0)
            {
                this.Close();
            }

            if ((Keyboard.GetKeyStates(Key.P) & KeyStates.Down) > 0)
            {
                if (this.ImgPaletteZoom.Visibility == Visibility.Hidden)
                {
                    this.ImgPaletteZoom.Visibility = Visibility.Visible;
                }
                else
                {
                    this.ImgPaletteZoom.Visibility = Visibility.Hidden;
                }
            }

            if ((Keyboard.GetKeyStates(Key.Up) & KeyStates.Down) > 0)
            {
                var rgb = HtmlToRgb(this.TbHtml.Text);
                if ((Keyboard.GetKeyStates(Key.R) & KeyStates.Down) > 0)
                {
                    rgb.Red = (rgb.Red + 3) <= 255 ? rgb.Red + 3 : 255;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }

                if ((Keyboard.GetKeyStates(Key.G) & KeyStates.Down) > 0)
                {
                    rgb.Green = (rgb.Green + 3) <= 255 ? rgb.Green + 3 : 255;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }

                if ((Keyboard.GetKeyStates(Key.B) & KeyStates.Down) > 0)
                {
                    rgb.Blue = (rgb.Blue + 3) <= 255 ? rgb.Blue + 3 : 255;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }

                var hsv = RgbToHsv(HtmlToRgb(this.TbHtml.Text));
                if ((Keyboard.GetKeyStates(Key.H) & KeyStates.Down) > 0)
                {
                    hsv.Hue = (hsv.Hue + 3) <= 359 ? hsv.Hue + 3 : 359;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }

                if ((Keyboard.GetKeyStates(Key.S) & KeyStates.Down) > 0)
                {
                    hsv.Saturation = (hsv.Saturation + .03) <= 1 ? hsv.Saturation + .03 : 1;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }

                if ((Keyboard.GetKeyStates(Key.V) & KeyStates.Down) > 0)
                {
                    hsv.Value = (hsv.Value + .03) <= 1 ? hsv.Value + .03 : 1;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }

                this.alphaInteger = (this.alphaInteger + 3) <= 255 ? this.alphaInteger + 3 : 255;
                this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                return;
            }

            if ((Keyboard.GetKeyStates(Key.Down) & KeyStates.Down) > 0)
            {
                var rgb = HtmlToRgb(this.TbHtml.Text);
                if ((Keyboard.GetKeyStates(Key.R) & KeyStates.Down) > 0)
                {
                    rgb.Red = (rgb.Red - 3) >= 0 ? rgb.Red - 3 : 0;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }

                if ((Keyboard.GetKeyStates(Key.G) & KeyStates.Down) > 0)
                {
                    rgb.Green = (rgb.Green - 3) >= 0 ? rgb.Green - 3 : 0;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }

                if ((Keyboard.GetKeyStates(Key.B) & KeyStates.Down) > 0)
                {
                    rgb.Blue = (rgb.Blue - 3) >= 0 ? rgb.Blue - 3 : 0;
                    this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                    return;
                }

                var hsv = RgbToHsv(HtmlToRgb(this.TbHtml.Text));
                if ((Keyboard.GetKeyStates(Key.H) & KeyStates.Down) > 0)
                {
                    hsv.Hue = (hsv.Hue - 3) >= 0 ? hsv.Hue - 3 : 0;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }

                if ((Keyboard.GetKeyStates(Key.S) & KeyStates.Down) > 0)
                {
                    hsv.Saturation = (hsv.Saturation - .03) >= 0 ? hsv.Saturation - .03 : .01;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }

                if ((Keyboard.GetKeyStates(Key.V) & KeyStates.Down) > 0)
                {
                    hsv.Value = (hsv.Value - .03) >= 0 ? hsv.Value - .03 : .01;
                    this.TbHtml.Text = RgbToHtml(HsvToRgb(hsv), this.alphaInteger);
                    return;
                }

                this.alphaInteger = (this.alphaInteger - 3) >= 1 ? this.alphaInteger - 3 : 1;
                this.TbHtml.Text = RgbToHtml(rgb, this.alphaInteger);
                return;
            }
        }

        private void ImgEyedropMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Mouse.Capture(this);
                this.isMouseCaptured = true;
                Cursor = Cursors.Cross;
                this.ImgEyedrop.Visibility = Visibility.Hidden;
                this.GetScreenShot();
            }
            catch
            {
                this.Error("Dropper Err");
            }
        }

        private void ImgPaletteMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.CycleSwatch();
            this.SyncZoomSwatch();
        }

        private void WdwMainLostMouseCapture(object sender, MouseEventArgs e)
        {
            this.isMouseCaptured = false;
            this.ImgPaletteZoom.Visibility = System.Windows.Visibility.Hidden;
        }

        private void ScrollViewerMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (e.Delta > 0)
                {
                    this.SvPalette.ScrollToHorizontalOffset(this.SvPalette.HorizontalOffset + 10);
                }

                if (e.Delta < 0)
                {
                    this.SvPalette.ScrollToHorizontalOffset(this.SvPalette.HorizontalOffset - 10);
                }
            }
            catch
            {
                this.SvPalette.ScrollToHorizontalOffset(0);
            }
        }

        private void TbHtmlTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                this.color = (Color)ColorConverter.ConvertFromString(this.TbHtml.Text);
                this.TkBadColor.Visibility = Visibility.Hidden;
                this.BdrColor.Background = new SolidColorBrush(this.color);
            }
            catch
            {
                this.Error("Invalid Color");
            }
        }

        private void TbHtmlKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                try
                {
                    FrameworkElement parent = (FrameworkElement)this.TbHtml.Parent;
                    while (parent != null && parent is IInputElement && !((IInputElement)parent).Focusable)
                    {
                        parent = (FrameworkElement)parent.Parent;
                    }

                    DependencyObject scope = FocusManager.GetFocusScope(this.TbHtml);
                    FocusManager.SetFocusedElement(scope, parent as IInputElement);
                }
                catch
                {
                    return;
                }
            }
        }

        private struct Pt
        {
            public int X;

            public int Y;
        }

        private struct Hsv
        {
            public double Hue;

            public double Saturation;

            public double Value;
        }

        private struct Rgb
        {
            public int Red;

            public int Green;

            public int Blue;
        }

        // Screenshot method

        private System.Drawing.Bitmap screenShot;

        private void GetScreenShot()
        {
            System.Drawing.Rectangle bounds = System.Windows.Forms.Screen.GetBounds(System.Drawing.Point.Empty);
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(bounds.Width, bounds.Height))
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, bounds.Size);
                }
                this.screenShot = bitmap;
                var x = this.screenShot.GetPixel(10, 10).Name;
                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Bmp);
                System.Windows.Media.Imaging.BitmapImage bmpi = new System.Windows.Media.Imaging.BitmapImage(); 
                bmpi.BeginInit();
                bmpi.StreamSource = ms; 
                bmpi.EndInit();
                this.ImgScreenShot.Source = bmpi;
            }
        }

        static void DetectColorWithMarshal(System.Drawing.Bitmap image, byte searchedR, byte searchedG, byte searchedB, int tolerance)
        {

            BitmapData imageData = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            byte[] imageBytes = new byte[Math.Abs(imageData.Stride) * image.Height];
            IntPtr scan0 = imageData.Scan0;

            Marshal.Copy(scan0, imageBytes, 0, imageBytes.Length);

            byte unmatchingValue = 0;
            byte matchingValue = 255;

            for (int i = 0; i < imageBytes.Length; i += 3)
            {
                byte pixelB = imageBytes[i];
                byte pixelR = imageBytes[i + 2];
                byte pixelG = imageBytes[i + 1];

                int diffR = pixelR - searchedR;
                int diffG = pixelG - searchedG;
                int diffB = pixelB - searchedB;

                int distance = diffR * diffR + diffG * diffG + diffB * diffB;

                imageBytes[i] = imageBytes[i + 1] = imageBytes[i + 2] = distance > tolerance * tolerance ? unmatchingValue : matchingValue;
            }

            Marshal.Copy(imageBytes, 0, scan0, imageBytes.Length);

            image.UnlockBits(imageData);
        }

    }
}