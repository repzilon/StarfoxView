﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.DAT;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Screens;
using static StarFox.Interop.GFX.CAD;

namespace StarFoxMapVisualizer.Misc
{
    /// <summary>
    /// Common helper functions for interacting with GFX files in the editor environment
    /// </summary>
    internal static class GFXStandard
    {
        /// <summary>
        /// Extracts a CCR file, shows the BitDepth dialog, and finally returns the file path of the created object.
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        internal static async Task<string> ExtractCCR(FileInfo File) {
            var menu = new BPPDepthMenu()
            {
                Owner = Application.Current.MainWindow
            };
            if (!menu.ShowDialog() ?? true) return default;
            var ccr = await SFGFXInterface.TranslateCompressedCCR(File.FullName, menu.FileType);
            await EditScreen.Current.ImportCodeProject(true); // UPDATE PROJECT FILES
            return ccr;
        }
        /// <summary>
        /// Extracts a PCR file and returns the file path of the created object.
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        internal static async Task<string> ExtractPCR(FileInfo File)
        {
            var pcr = await SFGFXInterface.TranslateCompressedPCR(File.FullName);
            await EditScreen.Current.ImportCodeProject(true); // UPDATE PROJECT FILES
            return pcr;
        }
        /// <summary>
        /// Translates the BGS.ASM naming scheme of palettes to be the actual name of the palette and returns the palette
        /// <para>Also returns the system file path of the palette so you can find it.</para>
        /// </summary>
        /// <param name="MAPContextColorPaletteName"></param>
        /// <param name="PaletteFullPath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static COL MAPContext_GetPaletteByName(string MAPContextColorPaletteName, out string PaletteFullPath)
        {
            var ColorPaletteName = MAPContextColorPaletteName;
            if (ColorPaletteName == default)
                throw new ArgumentNullException(nameof(ColorPaletteName) + " was not set on this context." +
                    " Can't render this without a palette.");
            if (ColorPaletteName.ToLower() == "2a") {
                ColorPaletteName = "BG2-A";
            } else if (ColorPaletteName.ToLower() == "2b") {
                ColorPaletteName = "BG2-B";
            } else if (ColorPaletteName.ToLower() == "2c") {
                ColorPaletteName = "BG2-C";
            } else if (ColorPaletteName.ToLower() == "2d") {
                ColorPaletteName = "BG2-D";
            } else if (ColorPaletteName.ToLower() == "2e") {
                ColorPaletteName = "BG2-E";
            } else if (ColorPaletteName.ToLower() == "2f") {
                ColorPaletteName = "BG2-F";
            } else if (ColorPaletteName.ToLower() == "2g") {
                ColorPaletteName = "BG2-G";
            } else if (ColorPaletteName.ToLower() == "tm") {
                ColorPaletteName = "T-M";
            } else if (ColorPaletteName.ToLower() == "tm2") {
                ColorPaletteName = "T-M-2";
            } else if (ColorPaletteName.ToLower() == "tm3") {
                ColorPaletteName = "T-M-3";
            } else if (ColorPaletteName.ToLower() == "tm4") {
                ColorPaletteName = "T-M-4";
            }

            PaletteFullPath = default;
            //CHECK IF THE PALETTE IS INCLUDED FIRST
            var results = AppResources.ImportedProject.Palettes.FirstOrDefault(
                x => Path.GetFileNameWithoutExtension(x.Key).Replace("-", "").ToUpper() == ColorPaletteName.Replace("-", "").ToUpper());
            if (results.Value == default) return default;
            PaletteFullPath = results.Key;
            return results.Value;
        }
        /// <summary>
        /// Will search through project files searching for the CGX with the provided name.
        /// <para>If the CGX is extracted, and <paramref name="ForceExtractCCR"/> is false, it will return the extracted one.</para>
        /// <para>If it isn't extracted, it will extract the *.CCR, if found. Returns the CGX that was extracted.</para>
        /// </summary>
        /// <param name="CHRName"></param>
        /// <param name="ForceExtractCCR"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        internal static async Task<FileInfo> FindProjectCGXByName(string CHRName, bool ForceExtractCCR = false)
        {
            if (!FILEStandard.SearchProjectForFile($"{CHRName}.CGX", out var CGXFileInfo, true) || ForceExtractCCR) // CGX File Search
            {
                if (ForceExtractCCR && CGXFileInfo != default)
                    AppResources.ImportedProject.CloseFile(CGXFileInfo.FullName);
                if (!FILEStandard.SearchProjectForFile($"{CHRName}.CCR", out var CCRFileInfo, true)) // CCR File Search
                    throw new FileNotFoundException($"The CGX file(s) (or CCR files) requested were not found.\n" +
                        $"{CHRName}");
                //EXTRACT CCR
                var cgxPath = await ExtractCCR(CCRFileInfo);
                if (string.IsNullOrWhiteSpace(cgxPath))
                    throw new Exception($"{cgxPath} could not be found, or {CHRName} could not be extracted.");
                CGXFileInfo = new FileInfo(cgxPath);
            }
            return CGXFileInfo;
        }
        /// <summary>
        /// Will search through project files searching for the SCR with the provided name.
        /// <para>If the SCR is extracted, and <paramref name="ForceExtractPCR"/> is false, it will return the extracted one.</para>
        /// <para>If it isn't extracted, it will extract the *.PCR, if found. Returns the SCR that was extracted.</para>
        /// </summary>
        /// <param name="SCRName"></param>
        /// <param name="ForceExtractPCR"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        internal static async Task<FileInfo> FindProjectSCRByName(string SCRName, bool ForceExtractPCR = false)
        {
            if (!FILEStandard.SearchProjectForFile($"{SCRName}.SCR", out var SCRFileInfo, true) || ForceExtractPCR) // SCR File Search
            {
                if (ForceExtractPCR && SCRFileInfo != default)
                    AppResources.ImportedProject.CloseFile(SCRFileInfo.FullName);
                if (!FILEStandard.SearchProjectForFile($"{SCRName}.PCR", out var PCRFileInfo, true)) // PCR File Search
                    throw new FileNotFoundException($"The SCR file(s) (or PCR files) requested were not found.\n" +
                        $"{SCRName}");
                //EXTRACT PCR
                var scrPath = await ExtractPCR(PCRFileInfo);
                if (string.IsNullOrWhiteSpace(scrPath))
                    throw new Exception($"{scrPath} could not be found, or {SCRName} could not be extracted.");
                SCRFileInfo = new FileInfo(scrPath);
            }
            return SCRFileInfo;
        }
        /// <summary>
        /// Will create a <see cref="BitmapSource"/> containing the rendered *.SCR file using only the Color Palette's
        /// name, the SCR's name, and optionally the *.CGX file's name.
        /// <para>In reference to <see cref="FindProjectCGXByName(string, bool)"/> and <see cref="FindProjectSCRByName(string, bool)"/>:</para>
        /// <para>Will search through project files searching for the Graphics Resource with the provided name.
        /// <para>If the Graphics Resource is extracted, and <paramref name="ForceExtractPCR"/> is false, it will return the extracted one.</para>
        /// <para>If it isn't extracted, it will extract the *.Compressed Graphics Resource, if found. Returns the Graphics Resource that was extracted.</para>
        /// </para>
        /// </summary>
        /// <param name="ColorPaletteName">The name of the color palette (not a file path)</param>
        /// <param name="SCRName">The name of the SCR file (not a file path)</param>
        /// <param name="CHRName">The name of the CGX file (not a file path). If default will just use the SCR name.</param>
        /// <param name="Screen">Optionally can specify which quadrant of the SCR file to draw</param>
        /// <returns></returns>
        internal static async Task<Bitmap> RenderSCR(string ColorPaletteName, string SCRName, string CHRName = default,
            int Screen = -1, bool ForceExtractCCR = false, bool ForceExtractPCR = false)
        {
            var palette = MAPContext_GetPaletteByName(ColorPaletteName, out _);
            if (palette == default) throw new FileNotFoundException($"{ColorPaletteName} was not found as" +
                $" an included Palette in this project."); // NOPE IT WASN'T
            //SET THE CHRName TO BE THE SCR NAME IF DEFAULT
            if (CHRName == null) CHRName = SCRName;
            //MAKE SURE BOTH OF THESE FILES ARE EXTRACTED AND EXIST
            //SEARCH AND EXTRACT CGX FIRST
            var CGXFileInfo = await FindProjectCGXByName(CHRName, ForceExtractCCR);
            //THEN SCR
            var SCRFileInfo = await FindProjectSCRByName(SCRName, ForceExtractPCR);
            return await RenderSCR(palette, CGXFileInfo, SCRFileInfo, Screen);
        }
        /// <summary>
        /// Will render the provided <paramref name="SCR"/> file, using the given <paramref name="CGX"/> file as a base,
        /// and colorized with the provided <paramref name="Palette"/>.
        /// <para>All GFX resources must be extracted. Need help extracting? Use: <see cref="RenderSCR(string, string, string?, bool, bool)"/></para>
        /// </summary>
        /// <param name="Palette">The palette to use.</param>
        /// <param name="CGX">The file name of the CGX file to use. Has to be extracted.</param>
        /// <param name="SCR">The file name of the SCR file to use. Has to be extracted.</param>
        /// <param name="Screen">Optionally can specify which quadrant of the SCR file to draw</param>
        /// <returns></returns>
        internal static async Task<Bitmap> RenderSCR(COL Palette, FileInfo CGX, FileInfo SCR, int Screen = -1)
        {
            //LOAD THE CGX
            var fxCGX = await OpenCGX(CGX);
            if (fxCGX == null) throw new InvalidDataException("The CGX file is not available.");
            //LOAD THE SCR
            var fxSCR = OpenSCR(SCR);
            //RENDER OUT
            return fxSCR.Render(fxCGX, Palette, false, Screen);
        }

		/// <summary>
		/// Includes a *.CGX file into the project.
		/// <para>This function will NOT extract a *.CGR file.</para>
		/// <para>This function spawns dialog modals.</para>
		/// </summary>
		/// <param name="fileMeta"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		internal static async Task<FXCGXFile> OpenCGX(FileInfo fileMeta)
		{
			var fullPath = fileMeta.FullName;
            if (!AppResources.OpenFiles.ContainsKey(fullPath))
            {
                //ATTEMPT TO OPEN THE FILE AS WELL-FORMED
                var fxGFX = SFGFXInterface.OpenCGX(fullPath);

				if (fxGFX == null) {
					// Attempt to open StarFox truncated CGX without prompting a bit depth
					fxGFX = await SFGFXInterface.TryImportFoxCGX(fullPath);
                }

                if (fxGFX == null) { // NOPE CAN'T DO THAT
                    var menu = new BPPDepthMenu()
                    {
                        Owner = Application.Current.MainWindow
                    };
                    if (!menu.ShowDialog() ?? true) return null; // USER CANCELLED!
                    //OKAY, TRY TO IMPORT IT WITH THE SPECIFIED BIT DEPTH
                    fxGFX = await SFGFXInterface.ImportCGX(fullPath, menu.FileType);
                }
                if (fxGFX == null) throw new Exception("That file cannot be opened or imported."); // GIVE UP
                //ADD IT AS AN OPEN FILE
                AppResources.OpenFiles.Add(fullPath, fxGFX);
                return fxGFX;
            }
            else return AppResources.OpenFiles[fullPath] as FXCGXFile;
        }

        /// <summary>
        /// Includes a *.SCR file into the project.
        /// <para>This function will NOT extract a *.PCR file.</para>
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static FXSCRFile OpenSCR(FileInfo File)
        {
            if (!AppResources.OpenFiles.ContainsKey(File.FullName))
            {
                //ATTEMPT TO OPEN THE FILE AS WELL-FORMED
                var fxGFX = SFGFXInterface.OpenSCR(File.FullName);
                if (fxGFX == null)
                { // NOPE CAN'T DO THAT
                  //OKAY, TRY TO IMPORT IT
                    fxGFX = SFGFXInterface.ImportSCR(File.FullName);
                }
                if (fxGFX == null) throw new Exception("That file cannot be opened or imported."); // GIVE UP
                //ADD IT AS AN OPEN FILE
                AppResources.OpenFiles.Add(File.FullName, fxGFX);
                return fxGFX;
            }
            else return (FXSCRFile)AppResources.OpenFiles[File.FullName];
        }

        internal static async Task ConvertFromSfscreen()
        {
	        var ofd = new OpenFileDialog
	        {
		        Multiselect = false,
		        CheckFileExists = true,
		        CheckPathExists = true,
		        Title = "Select file to convert",
		        ValidateNames = true,
		        InitialDirectory = AppResources.ImportedProject.WorkspaceDirectory.FullName
	        };
	        var filter = new FileDialogFilterBuilder(false);
	        filter.Add("Super Famicom screen JSON representation", ".sfscreen");
	        filter.IncludeAllFiles = true;
	        ofd.Filter = filter.ToString();
	        if (ofd.ShowDialog() == true) {
		        var sfd = FILEStandard.InitSaveFileDialog("Export as SCR to",
			        Path.ChangeExtension(ofd.SafeFileName, ".SCR"));
		        filter = new FileDialogFilterBuilder(false);
		        filter.Add("SG-CAD Super Famicom/Super NES SCR format", ".SCR");
		        sfd.Filter = filter.ToString();
		        if (sfd.ShowDialog() == true) {
			        SFGFXInterface.ConvertSfscreenToSCR(ofd.FileName, sfd.FileName);
			        await EDITORStandard.ShowNotification(
				        $"Converted {ofd.SafeFileName} to {sfd.SafeFileName} successfully.");
		        }
	        }
		}
    }
}
