// SPDX-License-Identifier: GPL-3.0-or-later


using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SnapX.Core.Media;
using SnapX.Core.SharpCapture.Helpers;

namespace SnapX.Core.SharpCapture;

public static class RegionCaptureTasks
{
    public static Image GetRegionImage(RegionCaptureOptions options = null)
    {
        RegionCaptureOptions newOptions = GetRegionCaptureOptions(options);

        // using (RegionCaptureForm form = new RegionCaptureForm(RegionCaptureMode.Default, newOptions))
        // {
        //     form.ShowDialog();
        //
        //     return form.GetResultImage();
        // }
        return new Image<Rgba32>(400, 400);
    }

    public static Image GetRegionImage(out Rectangle rect, RegionCaptureOptions options = null)
    {
        RegionCaptureOptions newOptions = GetRegionCaptureOptions(options);
        //
        // using (RegionCaptureForm form = new RegionCaptureForm(RegionCaptureMode.Default, newOptions))
        // {
        //     form.ShowDialog();
        //
        //     rect = form.GetSelectedRectangle();
        //     return form.GetResultImage();
        // }
        // TODO: Implement GetRegionImage in UI
        rect = new Rectangle();
        return new Image<Rgba32>(400, 400);
    }

    public static bool GetRectangleRegion(out Rectangle rect, RegionCaptureOptions options = null)
    {
        RegionCaptureOptions newOptions = GetRegionCaptureOptions(options);

        // using (RegionCaptureForm form = new RegionCaptureForm(RegionCaptureMode.Default, newOptions))
        // {
        //     form.ShowDialog();
        //
        //     rect = form.GetSelectedRectangle();
        // }
        rect = new Rectangle();

        return !rect.IsEmpty;
    }

    public static bool GetRectangleRegion(out Rectangle rect, out WindowInfo windowInfo, RegionCaptureOptions options = null)
    {
        RegionCaptureOptions newOptions = GetRegionCaptureOptions(options);

        // using (RegionCaptureForm form = new RegionCaptureForm(RegionCaptureMode.Default, newOptions))
        // {
        //     form.ShowDialog();
        //
        //     rect = form.GetSelectedRectangle();
        //     windowInfo = form.GetWindowInfo();
        // }
        rect = new Rectangle();
        windowInfo = new WindowInfo();

        return !rect.IsEmpty;
    }

    public static bool GetRectangleRegionTransparent(out Rectangle rect)
    {
        // using (RegionCaptureTransparentForm regionCaptureTransparentForm = new RegionCaptureTransparentForm())
        // {
        //     if (regionCaptureTransparentForm.ShowDialog() == DialogResult.OK)
        //     {
        //         rect = regionCaptureTransparentForm.SelectionRectangle;
        //         return true;
        //     }
        // }

        rect = Rectangle.Empty;
        return false;
    }

    // public static PointInfo GetPointInfo(RegionCaptureOptions options, Image canvas = null)
    // {
    // RegionCaptureOptions newOptions = GetRegionCaptureOptions(options);
    // newOptions.DetectWindows = false;
    // newOptions.BackgroundDimStrength = 0;

    // using (RegionCaptureForm form = new RegionCaptureForm(RegionCaptureMode.ScreenColorPicker, newOptions, canvas))
    // {
    //     form.ShowDialog();
    //
    //     if (form.Result == RegionResult.Region)
    //     {
    //         PointInfo pointInfo = new PointInfo();
    //         pointInfo.Position = form.CurrentPosition;
    //         pointInfo.Color = form.ShapeManager.GetCurrentColor();
    //         return pointInfo;
    //     }
    // }
    //
    //     return null;
    // }

    public static SimpleWindowInfo GetWindowInfo(RegionCaptureOptions options)
    {
        RegionCaptureOptions newOptions = GetRegionCaptureOptions(options);
        newOptions.BackgroundDimStrength = 0;
        newOptions.ShowMagnifier = false;

        // using (RegionCaptureForm form = new RegionCaptureForm(RegionCaptureMode.OneClick, newOptions))
        // {
        //     form.ShowDialog();
        //
        //     if (form.Result == RegionResult.Region)
        //     {
        //         return form.SelectedWindow;
        //     }
        // }

        return null;
    }

    // public static void ShowScreenColorPickerDialog(RegionCaptureOptions options)
    // {
    //     Color color = Color.Red;
    //     ColorPickerForm colorPickerForm = new ColorPickerForm(color, true, true, options.ColorPickerOptions);
    //     colorPickerForm.EnableScreenColorPickerButton(() => GetPointInfo(options));
    //     colorPickerForm.Show();
    // }

    public static void ShowScreenRuler(RegionCaptureOptions options)
    {
        RegionCaptureOptions newOptions = GetRegionCaptureOptions(options);
        newOptions.QuickCrop = false;
        newOptions.UseLightResizeNodes = true;

        // using (RegionCaptureForm form = new RegionCaptureForm(RegionCaptureMode.Ruler, newOptions))
        // {
        //     form.ShowDialog();
        // }
    }

    // public static Image ApplyRegionPathToImage(Image img, GraphicsPath gp, out Rectangle resultArea)
    // {
    //     if (img != null && gp != null)
    //     {
    //         Rectangle regionArea = Rectangle.Round(gp.GetBounds());
    //         Rectangle screenRectangle = CaptureHelpers.GetScreenBounds();
    //         resultArea = Rectangle.Intersect(regionArea, new Rectangle(0, 0, screenRectangle.Width, screenRectangle.Height));
    //
    //         if (resultArea.IsValid())
    //         {
    //             using (Bitmap bmpResult = img.CreateEmptyBitmap())
    //             using (Graphics g = Graphics.FromImage(bmpResult))
    //             using (TextureBrush brush = new TextureBrush(img))
    //             {
    //                 g.PixelOffsetMode = PixelOffsetMode.Half;
    //                 g.SmoothingMode = SmoothingMode.HighQuality;
    //
    //                 g.FillPath(brush, gp);
    //
    //                 return ImageHelpers.CropBitmap(bmpResult, resultArea);
    //             }
    //         }
    //     }
    //
    //     resultArea = Rectangle.Empty;
    //     return null;
    // }

    private static RegionCaptureOptions GetRegionCaptureOptions(RegionCaptureOptions options)
    {
        if (options == null)
        {
            return new RegionCaptureOptions();
        }
        else
        {
            return new RegionCaptureOptions()
            {
                DetectControls = options.DetectControls,
                SnapSizes = options.SnapSizes,
                ShowMagnifier = options.ShowMagnifier,
                UseSquareMagnifier = options.UseSquareMagnifier,
                MagnifierPixelCount = options.MagnifierPixelCount,
                MagnifierPixelSize = options.MagnifierPixelSize,
                ShowCrosshair = options.ShowCrosshair,
                ScreenColorPickerInfoText = options.ScreenColorPickerInfoText
            };
        }
    }
}
