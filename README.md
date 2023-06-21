# Progrimage
**A User-end scripting focused image editor**  
**Requires [.NET 6.0](https://dotnet.microsoft.com/en-us/download) to run**  
You can download Progrimage in the [releases](https://github.com/Jacbo1/Progrimage/releases/latest).  
Documentation and examples can be found on the [wiki](https://github.com/Jacbo1/Progrimage/wiki/Lua-Examples).  

The main feature of this image editor is the ability for users to easily create their own Lua scripts. These currently are in the form of user-created tools and and composites. Composites are procedural, non-destructive image filters/effects.  

**NOTE: This is an early unfinished build. There is an issue where under some conditions or on some computers the entire interface be black except for text and icons. This is suspected to be an issue with ImGui.NET or DesktopGL. Tests were done and the proper colors are being sent to ImGui.NET but are displayed as black. If the canvas is not black on startup, the program is still usable and theme colors can seemingly be reselected and saved without issue.**  
Some (or a lot) of the code also needs refactoring to make it look better.  

![image](https://user-images.githubusercontent.com/86734639/220980725-df9c16d6-5d3d-4442-ac9e-c9c38844b8de.png)

**Features**
* Tools
  * Brush - has a pencil mode
  * Eraser - has a pencil mode
  * Fill - has an eraser mode and option for sampling all layers and filling contiguously
  * Move - can move layers and selections or resize selections
  * Marque Selection
  * Rectangle
  * Oval
  * Line
  * Quadratic Bézier curve
  * Cubic Bézier curve
  * Text tool (WIP)
  * Crop tool - automatically selects invisible edges to crop off when tool is selected
  * User-created Lua tools
* Composites (procedural, non-destructive image filters)
  * Glow - Uses a bloom-like algorithm. Meant for transparent images.
  * HSV
  * HSL
  * Contrast
  * Invert
  * Grayscale
  * Remove Alpha - Make opaque
  * Multiply Alpha
  * User-created Lua composites
* Other
  * Lua tools and composites are automatically rerun when the file is edited so you can edit them in real-time
  * Copy images directly out of the program to paste elsewhere without needing to save to a file. Note: Currently cannot preserve transparency
* Other basic minor things not worth mentioning
  
**Planned Features**
* Changeable brush textures
* Allow multiple theme files
* Save and load projects
* User-created Lua scripts to generate new layers
* Resizing entire layers
* Flipping layers
* Various snapping-related features
* Output file specifications (e.g. jpeg quality)
* Full undo and redo functionality (only certain tools currently have it)
* Allow dragging anywhere on layer and composite tabs for reordering instead of only the thumbnail or name
* Multiple project tabs
* Create my own icons for the tools that currently have placeholders from Google images
* Better Lua implementations
* More Lua functionality
* More that were not mentioned  
  
Known issue: sometimes everything will be black except for icons and text. I don't know what causes this but for me it happens when I start the program while on battery power. I think it's an issue with DesktopGL or ImGui.NET but I don't know if I can fix it.  
  
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
I do not own or take credit for the brush, eraser, fill, or move tool icons. They are placeholders I found on Google images.
