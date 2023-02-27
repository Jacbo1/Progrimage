# Progrimage
**A User-end scripting focused image editor**
**NOTE: This is a very unfinished early build. On the rare occassion it may crash but I am working on its stability and have been fixing crashes.**  
Some (or a lot) of the code also needs refactoring to make it look better.  
  
The main feature of this image editor is the ability for users to easily create their own Lua scripts. These currently are in the form of user-created tools and and composites. Composites are procedural, non-destructive image filters/effects.  

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
  * Style editor
  * Easily pan and zoom without switching tools or clearing steps
* Other basic minor things not worth mentioning
  
**Planned Features**
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

# Building
**Requirements**
* [My vector library](https://github.com/Jacbo1/Vector-Library)
* [My ImageSharp Extensions](https://github.com/Jacbo1/ImageSharpExtensions/tree/master)
* [ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp/2.1.3)
* [ImGui.NET](https://www.nuget.org/packages/ImGui.NET/1.89.1?_src=template)
* [DesktopGL](https://www.nuget.org/packages/MonoGame.Framework.DesktopGL/3.8.1.303?_src=template)
* [NLua](https://www.nuget.org/packages/NLua/1.6.0?_src=template)
* [ImageSharp.Drawing](https://www.nuget.org/packages/SixLabors.ImageSharp.Drawing/1.0.0-beta15?_src=template)

## Ownership
I do not own or take credit for the brush, eraser, fill, or move tool icons. They are placeholders I found on Google images.