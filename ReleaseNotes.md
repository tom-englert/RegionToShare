1.12
- Aspect ratios: lock the region to 4:3, 5:4, 16:10, 16:9 or custom ratios from %APPDATA%\RegionToShare\aspectratios.txt
- New settings dialog, all settings except resolution and aspect ratio moved there
- Hotkey (default Win+Ctrl+W) to move and resize the active window to exactly fit the shared region
- Mouse highlighter: double ring around the cursor on the screen and/or in the shared region, click colors and click sounds
- German translation, the language can be chosen in the settings

1.11
- Fix #80: Support native ARM64, contributed by Stefan Forstenlechner <stefan@forstenlechner.dev>
- Fix #79: Do not default to start activated
- Fix #72: automatically recover from corrupt settings file.

1.10
- Fix #60: Issues when restoring the window position on the secondary screen with different DPI settings

1.9
- Fix #69: restore windows position is not always correct, since glass frame thickness is not constant.
 
1.8
- Fix: Resolution not updated after restore from minimized state.

1.7
- Fix #62: adjust window by the glass frame size to work better with FancyZones

1.6
- Increase version to fix broken appstore upload

1.5
- Allow to start in activated mode

1.4
- Fix #44: Theme color is configurable, backgound image is less distracting
- Fix #46: Close button is now partially visible when maximized
- Fix #48: Optionally draw a shadow cursor
- FPS is now configurable
- Fix: Separation layer sometimes not visible

1.3
- Fix #41: Region of secondary monitor not shareable

1.2
- Fix #34: Incorrect pixel resolution 
- Fix #33: Window size can be selected from list
- Fix #21: Add app version in the title bar

1.1
- Fix #5: Flickering on Win11
- Fix #18: Double mouse cursors
- Fix #11: Remember size and position

1.0
- Initial version