using ImGuiNET;
using NewMath;
using Progrimage.Selectors;
using Progrimage.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = SixLabors.ImageSharp.Color;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Progrimage.DrawingShapes
{
    public struct DrawingBoundingBoxDots : IShape
    {
        public int2 Min, Max;
        public Color Color;

        public bool Hidden { get; set; }

        public double2 Pos
        {
            get => Min;
            set
            {
                Max += (int2)value - Min;
                Min = (int2)value;
            }
        }

        public DrawingBoundingBoxDots(int2 min, int2 max, Color color)
        {
            Min = min;
            Max = max;
            Color = color;
            Hidden = false;
        }

        public void Draw(IImageProcessingContext context) { }

        public void DrawToRender(IImageProcessingContext context)
        {
            int2 min = Util.CanvasToRender(Min);
            int2 max = Util.CanvasToRender(Max + 1) - 1;
            int2 center = (max + min) / 2;

            // Draw box
            new DrawingRect(Color, min, max - min + 1, 1, true).Draw(context);

            // Draw dots
            for (int j = 0; j < 8; j++)
            {
                int2 point = int2.Zero;
                switch (j)
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

                // Move dots
                new DrawingOval(Color.Gray, point - Defs.DotOuterSize / 2, Defs.DotOuterSize).Draw(context);
                new DrawingOval(Color.White, point - Defs.DotInnerSize / 2, Defs.DotInnerSize).Draw(context);
            }
        }

        public Rectangle GetBounds()
        {
            int2 size = Max - Min + 1;
            return new Rectangle(Min.x, Min.y, size.x, size.y);
        }

        public int2 GetSize() => Max - Min + 1;

		public ResizeDir? GetResizeDir(bool grabEdge = true, bool edgeMoves = false, DrawingShapeCollection? collectionMember = null, ImGuiMouseCursor defaultCursor = ImGuiMouseCursor.Arrow)
		{
			var instance = Program.ActiveInstance;
			double2 origin = int2.Zero;
			if (collectionMember is not null)
			{
				origin = collectionMember.Pos;
				if (collectionMember.AttachedToLayer)
					origin += collectionMember.Layer.Pos;
			}
			int2 min = Math2.FloorToInt((Min + origin) * instance.Zoom) + MainWindow.CanvasOrigin;
			int2 max = Math2.FloorToInt((Max + 1 + origin) * instance.Zoom) - 1 + MainWindow.CanvasOrigin;
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

			if (!grabEdge) goto DIR_FOUND;
			if (closestDir is null)
			{
				// Check edges
				// Check top and bottom
				if (MainWindow.MousePosScreen.x >= min.x && MainWindow.MousePosScreen.x <= max.x)
				{
					// Top
					if (Math.Abs(MainWindow.MousePosScreen.y - min.y) <= Defs.CURSOR_CHANGE_RADIUS)
					{
						closestDir = edgeMoves ? ResizeDir.Move : ResizeDir.Up;
						goto DIR_FOUND;
					}

					// Bottom
					if (Math.Abs(MainWindow.MousePosScreen.y - max.y) <= Defs.CURSOR_CHANGE_RADIUS)
					{
						closestDir = edgeMoves ? ResizeDir.Move : ResizeDir.Down;
						goto DIR_FOUND;
					}
				}

				// Check left and right
				if (MainWindow.MousePosScreen.y >= min.y && MainWindow.MousePosScreen.y <= max.y)
				{
					// Left
					if (Math.Abs(MainWindow.MousePosScreen.x - min.x) <= Defs.CURSOR_CHANGE_RADIUS)
					{
						closestDir = edgeMoves ? ResizeDir.Move : ResizeDir.Left;
						goto DIR_FOUND;
					}

					// Right
					if (Math.Abs(MainWindow.MousePosScreen.x - max.x) <= Defs.CURSOR_CHANGE_RADIUS)
					{
						closestDir = edgeMoves ? ResizeDir.Move : ResizeDir.Right;
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
				case ResizeDir.Move: Util.SetMouseCursor(ImGuiMouseCursor.ResizeAll); break;
				default: Util.SetMouseCursor(defaultCursor); break;
			}

			return closestDir;
		}

		public void Resize(ResizeDir resizeDir, int2 resizeStartMin, int2 resizeStartMax, int2 resizeStartOffset, DrawingShapeCollection? collectionMember = null)
		{
			int2 origin = int2.Zero;
			if (collectionMember is not null)
			{
				origin += Math2.RoundToInt(collectionMember.Pos);
				if (collectionMember.AttachedToLayer)
					origin += collectionMember.Layer.Pos;
			}
			resizeStartMin -= origin;
			resizeStartMax -= origin;
			int2 mousePos = MainWindow.MousePosCanvas - origin;
			switch (resizeDir)
			{
				case ResizeDir.Up:
					// Drag top middle
					Min = new int2(Min.x, Math.Min(resizeStartMax.y, mousePos.y));
					Max = new int2(Max.x, Math.Max(resizeStartMax.y, mousePos.y));
					break;
				case ResizeDir.Down:
					// Drag bottom middle
					Min = new int2(Min.x, Math.Min(resizeStartMin.y, mousePos.y));
					Max = new int2(Max.x, Math.Max(resizeStartMin.y, mousePos.y));
					break;
				case ResizeDir.Left:
					// Drag left middle
					Min = new int2(Math.Min(resizeStartMax.x, mousePos.x), Min.y);
					Max = new int2(Math.Max(resizeStartMax.x, mousePos.x), Max.y);
					break;
				case ResizeDir.Right:
					// Drag right middle
					Min = new int2(Math.Min(resizeStartMin.x, mousePos.x), Min.y);
					Max = new int2(Math.Max(resizeStartMin.x, mousePos.x), Max.y);
					break;
				case ResizeDir.UpLeft:
					// Drag top left
					Min = Math2.Min(resizeStartMax, mousePos);
					Max = Math2.Max(resizeStartMax, mousePos);
					break;
				case ResizeDir.UpRight:
					// Drag top right
					Min = new int2(Math.Min(resizeStartMin.x, mousePos.x), Math.Min(resizeStartMax.y, mousePos.y));
					Max = new int2(Math.Max(resizeStartMin.x, mousePos.x), Math.Max(resizeStartMax.y, mousePos.y));
					break;
				case ResizeDir.DownLeft:
					// Drag bottom left
					Min = new int2(Math.Min(resizeStartMax.x, mousePos.x), Math.Min(resizeStartMin.y, mousePos.y));
					Max = new int2(Math.Max(resizeStartMax.x, mousePos.x), Math.Max(resizeStartMin.y, mousePos.y));
					break;
				case ResizeDir.DownRight:
					// Drag bottom right
					Min = Math2.Min(resizeStartMin, mousePos);
					Max = Math2.Max(resizeStartMin, mousePos);
					break;
				case ResizeDir.Move:
					// Move
					Pos = mousePos - resizeStartOffset;
					break;
			}
		}
	}
}
