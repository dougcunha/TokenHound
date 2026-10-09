Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
$root = [Windows.Controls.Grid]::new()
$root.Background = [Windows.Media.Brushes]::White
$panel = [Windows.Controls.Canvas]::new()
$root.Children.Add($panel) > $null
$envelope = [Windows.Media.RectangleGeometry]::new([Windows.Rect]::new(0,0,167.5,55),22,22)
$transform = [Windows.Media.MatrixTransform]::new(0.8,0,0,0.8,12,8)
$transformed = $envelope.Clone()
$transformed.Transform = $transform
$panel.Clip = [Windows.Media.Geometry]::Combine([Windows.Media.RectangleGeometry]::new([Windows.Rect]::new(0,0,135,46)), $transformed, 'Exclude', $null)
$path = [Windows.Shapes.Path]::new()
$path.Fill = [Windows.Media.Brushes]::Black
$path.Data = $envelope
$path.RenderTransform = $transform
$effect = [Windows.Media.Effects.DropShadowEffect]::new()
$effect.BlurRadius = 20
$effect.ShadowDepth = 4
$effect.Direction = 270
$effect.Opacity = 0.65
$path.Effect = $effect
$panel.Children.Add($path) > $null
$root.Measure([Windows.Size]::new(135,46))
$root.Arrange([Windows.Rect]::new(0,0,135,46))
$root.UpdateLayout()
$bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(270,92,192,192,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($root)
$encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$file = [IO.File]::Create('C:/Users/Admin/AppData/Local/Temp/TokenHound-shadow-render.png')
$encoder.Save($file)
$file.Dispose()
'RENDER_DONE'
