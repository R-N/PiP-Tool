# ![logo][] PiP-Tool (Picture In Picture tool)

[![Release](https://img.shields.io/github/release/R-N/PiP-Tool.svg)](https://github.com/R-N/PiP-Tool/releases)

[logo]: https://i.imgur.com/SMaB1GI.png

PiP tool is a software to use the Picture in Picture mode on Windows. This feature allows you to watch content (video for example) in thumbnail format on the screen while continuing to use any other software on Windows.

To use this tool, select a window and the region of the window, then validate. A new window (Always on the top) will appear with the selected region. The software does not work yet with minimized windows (If you're watching a video, and you minimize the window, the video in the Picture in Picture mode will stop)

### Machine Learning ([ML.NET](https://www.microsoft.com/net/learn/apps/machine-learning-and-ai/ml-dotnet) - [dotnet/machinelearning](https://github.com/dotnet/machinelearning))

Machine learning is used in this software to predict selected regions in order to simplify and speed up the usage of the PiP-Tool. 

There are no default data provided with this app, data are created and stored locally from your previous uses of this software. When you have validated a selected region to enter in Picture in Picture Mode, region' data are stored and the machine learning' model is improved.

### Changes since LionelJouin version

* Changed target to .NET 4.8
* Changed topbar to sidebar on the right, so you can place the PiP window at topmost of your screen, touching the top edge
* Added button & slider to adjust selected window's volume from PiP (slider only shows if the selected window is playing sound and detected by Windows audio mixer)
* Added button to switch to the selected window (this also minimizes PiP window)
* Added PiP opacity slider (only 50% to 100%, and sidebar gets semitransparent too)
* Added minimize button
* Added workaround to disable aero snap
* Added feature to forward mouse and keyboard inputs to the selected window (VERY BUGGY)

## Requirements

* Microsoft Windows Vista or greater (64 bit).
* Microsoft .NET Framework 4.8.

## Installation

Get the [latest version](https://github.com/R-N/PiP-Tool/releases) from the releases section as an MSI installer.

## Screenshot
  
![Pip-Tool](https://i.imgur.com/h53u77B.png)
![Pip-Tool](https://i.imgur.com/Uyth2KG.png)
![Pip-Tool](https://i.imgur.com/wReFIh7.png)
![Pip-Tool](https://i.imgur.com/X5Udm8L.gif)

## Authors

* **Lionel Jouin** - [LionelJouin](https://github.com/LionelJouin)  
* **R-N** - [R-N](https://github.com/R-N)  

See also the list of [contributors](https://github.com/R-N/PiP-Tool/graphs/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
