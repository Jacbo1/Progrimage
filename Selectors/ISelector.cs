using ImGuiNET;
using NewMath;
using Progrimage.DrawingShapes;
using Progrimage.Utils;

namespace Progrimage.Selectors
{
    public abstract class ISelector : IDisposable
    {
        protected Layer _layer;
        public abstract int2 Pos { get; set; }
        public abstract int2 Min { get; set; }
        public abstract int2 Max { get; set; }
        public abstract bool GrabSource { get; set; }
        public abstract bool DrawImageInOverlay { get; set; }
        public abstract bool DrawBoundaryDots { get; set; }
        public abstract Image<Argb32> Image { get; set; }
        public virtual DrawingShapeCollection Overlay { get; protected set; }
        public virtual void ClearSelection() => ClearSelection(_layer);
        public abstract void ClearSelection(Layer layer);
        public void DrawImage() => DrawImage(_layer);
        public abstract void DrawImage(Layer layer);
        public abstract void DrawOutline(IImageProcessingContext image);
        public abstract void Dispose();
        public virtual ResizeDir? GetResizeDir(ImGuiMouseCursor defaultCursor = ImGuiMouseCursor.Arrow)
        {
            var instance = Program.ActiveInstance;
            int2 min = Math2.FloorToInt(Min * instance.Zoom) + MainWindow.CanvasOrigin;
            int2 max = Math2.FloorToInt((Max + 1) * instance.Zoom) - 1 + MainWindow.CanvasOrigin;
            
            if (instance.Selection != this || instance.ActiveLayer is null) return null;
            DrawBoundaryDots = true;
            instance.OverlayChanged();

            int2 center = (max + min) / 2;
            int minDist = int.MaxValue;
            ResizeDir? closestDir = null;

            // Find closest point to mouse
            for (int i = 0; i < 8; i++)
            {
                int2 point = int2.Zero;
                switch (i)
                {
                    case 0: point = new(center.x, min.y); break;
                    case 1: point = new(center.x, max.y); break;
                    case 2: point = new(min.x, center.y); break;
                    case 3: point = new(max.x, center.y); break;
                    case 4: point = min; break;
                    case 5: point = new(max.x, min.y); break;
                    case 6: point = new(min.x, max.y); break;
                    case 7: point = max; break;
                }

                int dist = MainWindow.MousePosScreen.DistanceSqr(point);
                if (dist < minDist && dist <= Defs.CURSOR_CHANGE_RADIUS_SQR)
                {
                    // Mouse closest and in range to this point
                    minDist = dist;
                    closestDir = (ResizeDir)i;
                }
            }

            if (closestDir is null)
            {
                // Check entire sides
                // Check top and bottom
                if (MainWindow.MousePosScreen.x >= min.x && MainWindow.MousePosScreen.x <= max.x)
                {
                    // Top
                    if (Math.Abs(MainWindow.MousePosScreen.y - min.y) <= Defs.CURSOR_CHANGE_RADIUS)
                    {
                        closestDir = ResizeDir.Up;
                        goto DIR_FOUND;
                    }

                    // Bottom
                    if (Math.Abs(MainWindow.MousePosScreen.y - max.y) <= Defs.CURSOR_CHANGE_RADIUS)
                    {
                        closestDir = ResizeDir.Down;
                        goto DIR_FOUND;
                    }
                }

                // Check left and right
                if (MainWindow.MousePosScreen.y >= min.y && MainWindow.MousePosScreen.y <= max.y)
                {
                    // Left
                    if (Math.Abs(MainWindow.MousePosScreen.x - min.x) <= Defs.CURSOR_CHANGE_RADIUS)
                    {
                        closestDir = ResizeDir.Left;
                        goto DIR_FOUND;
                    }

                    // Right
                    if (Math.Abs(MainWindow.MousePosScreen.x - max.x) <= Defs.CURSOR_CHANGE_RADIUS)
                    {
                        closestDir = ResizeDir.Right;
                        goto DIR_FOUND;
                    }
                }
            }

        DIR_FOUND:

            switch (closestDir)
            {
                case ResizeDir.UpLeft:
                case ResizeDir.DownRight: Util.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE); break;
                case ResizeDir.UpRight:
                case ResizeDir.DownLeft: Util.SetMouseCursor(ImGuiMouseCursor.ResizeNESW); break;
                case ResizeDir.Up:
                case ResizeDir.Down: Util.SetMouseCursor(ImGuiMouseCursor.ResizeNS); break;
                case ResizeDir.Left:
                case ResizeDir.Right: Util.SetMouseCursor(ImGuiMouseCursor.ResizeEW); break;
                default: Util.SetMouseCursor(defaultCursor); break;
            }

            return closestDir;
        }
        public abstract Image<Argb32> GetImageFromRender();
    }
}
