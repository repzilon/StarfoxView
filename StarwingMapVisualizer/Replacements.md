# XAML
RenderOptions.BitmapScalineMode="NearestNeighbor"
RenderOptions.BitmapInterpolationMode="None"

ContentPresenter ContentSource="Header"
ContentPresenter Content="{TemplateBinding Header}"

# C Sharp
## GDI+ to SkiaSharp
Graphics.FillRectangle(Brush, x, y, w, h)
SKCanvas.DrawRect(x, y, w, h, SKPaint)

Graphics.DrawImage(Bitmap, x, y, w, h)
SKCanvas.DrawBitmap(SKBitmap, x, y, SKPaint)

## Avalonia
.Visibility = Visibility.Visible;
.IsVisible  = true;

.Visibility = Visibility.Collapsed;
.IsVisible  = false;
