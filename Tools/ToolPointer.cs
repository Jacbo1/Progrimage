using ImGuiNET;
using NewMath;
using Progrimage.CoroutineUtils;
using Progrimage.DrawingShapes;
using Progrimage.ImGuiComponents;
using Progrimage.Selectors;
using Progrimage.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = SixLabors.ImageSharp.Color;

namespace Progrimage.Tools
{
    public class ToolPointer : ITool
    {
        #region Fields
        // Public fields
        public const string CONST_NAME = "Pointer";

        // Private static
        private static int2 _dotOuterSize = 8, _dotInnerSize = 6;

        // Private
        private bool _dragging, _resizing;
        private DrawingShapeCollection? _shapes;
        private Image<Argb32>? _sourceImage;
        private ResizeDir _resizeDir;
        #endregion

        #region Properties
        public string Name => CONST_NAME;
        public TexPair Icon { get; private set; }
        #endregion

        #region Constructor
        public ToolPointer()
        {
			//Icon = new(@"Assets\Textures\Tools\brush.png", Defs.TOOL_ICON_SIZE);
		}
		#endregion

		#region ITool Methods

		#endregion
	}
}
