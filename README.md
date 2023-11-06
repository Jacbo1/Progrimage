# Progrimage
A user-end scripting focused image editor  
Early unfinished build

**Requires [.NET 6.0](https://dotnet.microsoft.com/en-us/download) and [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) to run**
Windows only

You can download Progrimage in the [releases](https://github.com/Jacbo1/Progrimage/releases/latest) section.
Documentation and examples can be found on the [wiki](https://github.com/Jacbo1/Progrimage/wiki/Lua-Examples).

The main feature of this image editor is the ability for users to easily create their own Lua scripts. These currently are in the form of user-created tools and and composites. Composites are procedural, non-destructive image filters/effects.
  
**IMPORTANT: Either ImGui.NET or MonoGame DesktopGL does not seem to work if you do not have a dedicated GPU i.e. if you only have integrated graphics.** I'm not 100% sure about this but it seems to be the case.

**IMPORTANT: If on a laptop, you must launch while plugged in or else the interface will be black except for outlines and text.** I have no idea why this happens and I can only assume it's ImGui.NET or DesktopGL as I have checked the colors I am passing to ImGui.NET and they are still correct.

[Demo video](https://www.youtube.com/watch?v=uSaQBk6_q1U)
![Demo video](https://github.com/Jacbo1/Progrimage/assets/86734639/570aea88-a4ef-4090-a17c-11b125baf8f5)

# Controls
* Ctrl + C - Copy selection
* Ctrl + V - Paste
  * Files can also be pasted in
* Ctrl + Z - Undo (currently not supported by everything)
* Ctrl + Y - Redo
* Ctrl + A - Marquee select entire canvas
* A - Marquee select current layer
* H - Flip current layer horizontally
* V - Flip current layer vertically
* Escape - Closes some popups/menus and clears the selection
* Shift/Control - Modifier keys for some tools
  * Rectangle tool - Holding shift draws squares
  * Oval tool - Holding shift draws circles
  * Line tool - Holding shift draws vertical or horizontal lines
  * Move tool
    * When moving, holding shift makes it only move on one axis (vertically or horizontally but not both)
    * When resizing a selected area, holding shift maintains aspect ratio

**Non-key controls**
* Mouse scroll - Zoom in and out of the canvas centered on the cursor.
* Marquee selections can be resized by dragging at any point on the edge, not just the dots while the marquee selection tool is active.
  * When cropping, the crop tool must be active.

# Supported file types
* Import
  * Png
  * Jpeg
  * Svg
  * WebP (still image, first frame)
  * Bmp
  * Pbm
  * Tiff
  * Tga
  * Dds
  * Gif (still image, first frame)
* Export
  * Png
  * Jpeg
  * Bmp
  * Tga

# Features
* Tools
  * Brush - has a pencil mode
  * Eraser - has a pencil mode
  * Fill - has an eraser mode and option for sampling all layers and filling contiguously
  * Pipette/Color picker
  * Move - can move layers and selections or resize selections
  * Marquee Selection
  * Rectangle
  * Oval
  * Line
  * Quadratic Bézier curve
  * Cubic Bézier curve
  * Text tool
  * Crop tool - automatically selects invisible edges to crop off when tool is selected
  * User-created Lua tools
* Composites (procedural, non-destructive image filters)
  * Glow - Uses a bloom-like algorithm. Meant for transparent images.
  * HSV
  * HSL
  * Multiply Color - Multiplies the colors in the image by the assigned color.
  * Color Mask - Meant for transparent images. Sets every pixel in the image to the assigned color while preserving alpha.
  * Contrast
  * Invert
  * Grayscale
  * Remove Alpha - Make opaque
  * Multiply Alpha
  * Crustify - Joke composite. Saves the image as a JPEG in memory with different quality levels and replaces the image with the final result.
  * User-created Lua composites
* Other
  * Lua tools and composites automatically rerun when the file is edited so you can edit them in real-time
  * Copy images directly out of the program to paste elsewhere without needing to save to a file.
  
**Planned Features**
* Changeable brush textures
* Allow multiple theme files
* Save and load projects
* ~~Resizing entire layers~~ - Added
* ~~Flipping layers~~ - Added
* Various snapping-related features
* Output file specifications (e.g. jpeg quality)
* Full undo and redo functionality (only certain tools currently have it)
* Allow dragging anywhere on layer and composite tabs for reordering instead of only the thumbnail or name
* Multiple project tabs
* Create my own icons for the tools that currently have placeholders from Google images
  
# Building
**Requirements**
* The dlls included in the release.
* [DesktopGL](https://www.nuget.org/packages/MonoGame.Framework.DesktopGL/3.8.1.303)
* [ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp/2.1.3)
* [ImageSharp.Drawing](https://www.nuget.org/packages/SixLabors.ImageSharp.Drawing/1.0.0-beta15)
* [ImGui.NET](https://www.nuget.org/packages/ImGui.NET/1.89.1)
* [NLua](https://www.nuget.org/packages/NLua/1.6.0)
* [Pfim](https://www.nuget.org/packages/Pfim/0.11.2)

## Ownership
I do not own or take credit for the brush, eraser, fill, pipette, or move tool icons. They are placeholders I found on Google Images.
