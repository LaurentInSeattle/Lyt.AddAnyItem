# Lyt.AddAnyItem
Visual Studio 2022 and 2026 Extension allowing users to add any kind of custom items to a project.

# Notes 
- Only for Visual Studio 2022 and 2026 as this extension uses the new Visual Studio Extensibilty API.
- Not available through Visual Studio "marketplace". 
- Still many bugs in the new Visual Studio Extensibility framework, some preventing adding some much needed functionality!

# Issues ~ Stuff barely works...
- Cannot add a Build Action
- Does not open new files in the Editor
- Launching a folder browser from a modal dialog crashes Visual Studio
- Samples are broken 
   
# How To
- Clone this repo, build the extension, locate the VSIX and install it into Visual Studio.
- In your "Documents" folder create subfolders, example: C:\Users\Someone\Documents\Lyt\AddAnyItem\Avalonia View ViewModel
- Place there your templates, see the SamplesTemplates in this repo for Avalonia. Edit as you see fit.
- When in Visual Studio, with a solution open, right-click on any file or folder to launch the "Add Any Item..." extension.
- New files will be created and added to the selected project.
