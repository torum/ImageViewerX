# ImageViewerX

<img src="https://github.com/torum/ImageViewerX/blob/main/Src/ImageViewer.Desktop/Assets/ImageViewerX-AppIcon.png?raw=true" width="48" height="48"/>

A new cross-platform Image Viewer. This is a port of [Image-viewer](https://github.com/torum/Image-viewer) developed with ObjectPascal/Lazarus back in 2018. This time, the app is built with C#/.NET using [Avalonia UI](https://github.com/AvaloniaUI/Avalonia).

![ImageViewerX](https://github.com/torum/ImageViewerX/blob/main/Images/ImageViewerX.png?raw=true) 

## Current Status
**Work in Progress**  

[x] Open single or multiple image files via command line parameters. (this includes launching from File Mangager or Explorer and shell:sendto)  
[x] Open single or multiple image files via drag and drop of file or folder onto the app window. (Except on Linux due to Avalonia's limitation)  
[x] Multiple images viewing with transitional effects.  
[x] Showing "Queued" images as a list of thumbnail previews and display original image by selecting it.  
[x] Fullscreen viewing.  
[x] Slideshow viewing with transitional effects.   
[x] Basic keyboard command including,  
```
Space/Pause/P => Play or Pause slideshow
Right => Next  
Left => Previous  
F => Fullscreen on/off 
Escape => Fullscreen off 
Alt+F4/Ctrl+Q => App quit 
```
[x] Basic mouse control including,  
```
Wheel up/down => Next/Previous   
Double click => Fullscreen on/off
```
[x] Right click popup menu  
[x] Localization (English, Japanese)  

 ## TODO:
[ ] Image property dialog.   
[ ] More and more options.   
[ ] Command-line options.  
