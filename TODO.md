# Repzilon's TODO list
Large plans are in order of priority, while everything else is in no particular order.

## Large plans
- [ ] Replace System.Drawing with SkiaSharp (multi OS and used by Avalonia)
- [ ] Port to Avalonia
- [ ] Have System.Threading.Tasks available for .NET Framework 3.5 and 4.0 ==WIP==
- [ ] Back port to .NET Framework 4.0 ==WIP==
- [ ] Correct code analyzers warnings
- [ ] Find and refactor duplicate code

## New features
- [ ] Display a game text translation grid extracted from gametext.asm
- [ ] Display a grid for the enemy specs shown during the game ending sequence (UltraStarFox source code needs rework to add other languages)
- [ ] Experiment with fastJSON (fast, stable, polymorphic, available on older frameworks, but handles dictionaries poorly)
- [x] Interpret Japanese communication messages and its proprietary character set
- [ ] Have the menu bar and context menu items do something ==WIP==
- [x] Stop asking for bit depth of CGX files when it can be guessed by file contents
- [ ] Rewrite export to .obj without the geometry4Sharp dependency
- [ ] Replace RichTextBox-based assembly code display with AvalonEdit
- [ ] Export to JSON and import JSON .scr files ==WIP==

## Smaller tasks
- [ ] Test JSON serialization under .NET Framework 4.6 that uses Newtonsoft.Json
- [ ] Replace the optimizers' message box wagon with a single dialog
- [x] Handle English<->foreign language mapping with the newest message format
- [x] Replace auto-import message box with the notification banner
- [x] Correct the dialog and menu background color that is too light, making text unreadable
- [x] Add a Collect garbage button and memory stats to the About box
- [x] In Messages view, have taller lists so we scroll less vertically
- [x] In Messages view, put correct message number (the one from the end of the line inside the message source file)
- [x] In Messages view, fit a bit more text in each entry of the lists
- [x] In Shapes view, format the coordinates numbers to align them
- [x] Include Pepper and Andross to communication mugshots
- [x] Stop listing duplicate .pcr and .ccr files in workspace (generated files)
- [x] In Graphics view, export pictures
