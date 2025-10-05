# ImageViewerX
A new cross-platform Image Viewer. This is a port of [Image-viewer](https://github.com/torum/Image-viewer) developed with ObjectPascal/Lazarus back in 2018. This time, the app is built with C#/.NET using [Avalonia UI](https://github.com/AvaloniaUI/Avalonia).

**Work In Progress**

![ImageViewerX](https://github.com/torum/ImageViewerX/blob/main/Images/ImageViewerX.png?raw=true) 

## Current progress

[x] Open single or multiple image files via command line parameters. (this includes launching from File Mangager or Explorer and shell:sendto)  
[x] Open single or multiple image files via drag and drop of file or folder onto the app window. (Except on Linux due to Avalonia's limitation)  
[x] Multiple images viewing with transitional effects.  
[x] Showing "Queued" images as a list of thumbnail previews and display original image by selecting it.  
[x] Fullscreen viewing.  
[x] Slideshow viewing.  
[x] Basic keyboard command including,  
```
Space/Pause/P => Play or Pause slideshow
Right => Next  
Left => Previous  
F => Fullscreen on/off 
Escape => Fullscreen off 
Alt+F4 => App quit 
```
[x] Basic mouse control including,  
```
Wheel up/down => Next/Previous   
Double click => Fullscreen on/off
```

 ## TODO:
[x] Right click popup menu rework (work in progress)  
[ ] Detailed image property dialog.   
[ ] More and more options.   
[ ] Commandline options.  
