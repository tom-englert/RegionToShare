# ![Icon](./src/Assets/32.png) Region to Share 
[![Build Status](https://dev.azure.com/tom-englert/Open%20Source/_apis/build/status/tom-englert.RegionToShare?branchName=main)](https://dev.azure.com/tom-englert/Open%20Source/_build/latest?definitionId=48&branchName=main)  [![Sponsor](https://img.shields.io/badge/-Sponsor-fafbfc?logo=GitHub%20Sponsors)](https://github.com/sponsors/tom-englert)

A Windows helper app to share only a part of a screen via video conference apps that only support either full screen or single window like e.g. Teams, WebEx, etc.

## How it works

This tool simply mirrors the content of a screen region into a hidden window. In your meeting app you then can just share the content of this hidden window.

**Region to Share is not aware of your meeting app nor what the meeting app is doing with the content of the window.**
It's up to your meeting app whether it properly shares this hidden windows content or not - if it's not working as expected, there is nothing Region to Share can do about this.

## Prerequisites

- Windows 10 or 11
- DotNet 4.6.2 or newer

## Installation

- Download and install this app from the [Windows Store](https://www.microsoft.com/store/productId/9N4066W2R5Q4)
  or pick the latest binaries from the [release page](https://github.com/tom-englert/RegionToShare/releases)

## Usage

### Tutorial

Watch this great tutorial by James Montemagno

[![Watch the tutorial](https://img.youtube.com/vi/4WVY-mFPFNI/hqdefault.jpg)](https://www.youtube.com/embed/4WVY-mFPFNI)

### Quick Start

- Start the "RegionToShare" app.
- Move the window to the region you want to share.
- In your meeting app start sharing the window "Region to Share".

![StartSharing](./src/Assets/StartSharing.gif)

- Now click the "Region to Share" window to start sharing the selected region.
  The window will change to the region selection frame, and others are seeing what's inside this frame.
- Close the region frame to stop showing the region without stopping to share.

![ShowRegion](./src/Assets/ShowRegion.gif)

### Aspect ratio

Pick an aspect ratio (4:3, 5:4, 16:10, 16:9) in the toolbar, or while sharing from the menu button (▾) of the region frame.
The width is kept and the height is adjusted; while a ratio is selected, resizing keeps it. Choose "Free" to resize freely again.
The list of ratios can be changed in `%APPDATA%\RegionToShare\aspectratios.txt`, one ratio like `21:9` per line.

### Fit a window into the region

Press **Win+Ctrl+W** to move and resize the active window so it exactly fills the shared region.
The hotkey can be changed in the settings.

### Mouse highlighter

A double ring around the mouse cursor shows where you are pointing; the inner ring is more transparent than the outer one.
Left and right clicks briefly change the color of the ring and play a sound. The highlighter is off by default; once turned on
in the settings, it is active all the time, not only while sharing. Color, opacity, size, click colors, sounds and volume can be
adjusted in the settings. The ring can be shown on your own screen, in the shared region, or both.

### Settings

Open the settings with the gear button in the toolbar, or from the menu button of the region frame.
Changes apply immediately. The app is available in English and German, and follows the Windows display language by default.

## Feedback 😄

If you like this tool, don't forget to ⭐ it.
