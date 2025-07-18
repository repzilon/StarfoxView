﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Starfox.Editor;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.BRR;
using StarFox.Interop.BSP;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX.DAT.MSPRITES;
using StarFox.Interop.MAP;
using StarFox.Interop.MSG;
using StarFox.Interop.SPC;
using StarFoxMapVisualizer.Controls;
using StarFoxMapVisualizer.Controls.Subcontrols;

namespace StarFoxMapVisualizer.Misc
{
	/// <summary>
	/// Common helper functions for interacting with files in the editor environment
	/// </summary>
	internal static class FILEStandard
	{
		internal static readonly ASMImporter ASMImport = new ASMImporter();
		internal static readonly MAPImporter MAPImport = new MAPImporter();
		internal static readonly BSPImporter BSPImport = new BSPImporter();
		internal static readonly MSGImporter MSGImport = new MSGImporter();
		internal static readonly COLTABImporter COLTImport = new COLTABImporter();
		internal static readonly BRRImporter BRRImport = new BRRImporter();
		internal static readonly SPCImporter SPCImport = new SPCImporter();
		internal static readonly MSpritesImporter DEFSPRImport = new MSpritesImporter();
		private static readonly TRNImporter TRNImport = new TRNImporter();

		/// <summary>
		/// Includes a <see cref="SFCodeProjectFileTypes.Assembly"/>, <see cref="SFCodeProjectFileTypes.Include"/> or
		/// <see cref="SFCodeProjectFileTypes.Palette"/>.
		/// <para>Note that passing generic type T is optional, since it will return default if it is not a matching type.</para>
		/// <para>Note that unless ContextualFileType is passed and not default, a dialog is displayed asking the user what kind of file this is.</para>
		/// </summary>
		/// <param name="File"></param>
		/// <returns></returns>
		public static async Task<T> IncludeFile<T>(FileInfo File, SFFileType.ASMFileTypes? ContextualFileType = default) where T : class
		{
			if (!AppResources.IsFileIncluded(File)) {
				switch (File.GetSFFileType()) {
					case SFCodeProjectFileTypes.Include:
					case SFCodeProjectFileTypes.Assembly:
						var asmFile = await ParseFile(File, ContextualFileType);
						if (asmFile == default) return default; // USER CANCEL
						AppResources.Includes.Add(asmFile); // INCLUDE FILE FOR SYMBOL LINKING
						return asmFile as T;
					case SFCodeProjectFileTypes.Palette:
						using (var file = File.OpenRead()) {
							var palette = CAD.COL.Load(file);
							if (palette == default) return default;
							AppResources.ImportedProject.Palettes.Add(File.FullName, palette);
							return palette as T;
						}
				}
			} else return AppResources.Includes.First(x => x.OriginalFilePath == File.FullName) as T;
			return default;
		}
		public static void IncludeFile(ASMFile asmFile)
		{
			if (!AppResources.IsFileIncluded(new FileInfo(asmFile.OriginalFilePath))) {
				//INCLUDE FILE FOR SYMBOL LINKING
				AppResources.Includes.Add(asmFile);
			}
		}
		public static bool SearchProjectForFile(string FileName, out FileInfo File, bool IgnoreHyphens = false)
		{
			File = null;
			var results = AppResources.ImportedProject.SearchFile(FileName, IgnoreHyphens);
			if (!results.Any()) return false;
			if (results.Count() > 1) // ambiguous
				return false;
			File = new FileInfo(results.First().FilePath);
			return true;
		}
		/// <summary>
		/// Common helper function that will set imports on all <see cref="CodeImporter{T}"/>s
		/// to be <see cref="AppResources.Includes"/>
		/// <code>COLTImport, ASMImport, MAPImport, BSPImport and MSGImport</code>
		/// </summary>
		public static void ReadyImporters()
		{
			COLTImport.SetImports(AppResources.Includes.ToArray());
			ASMImport.SetImports(AppResources.Includes.ToArray());
			MAPImport.SetImports(AppResources.Includes.ToArray());
			BSPImport.SetImports(AppResources.Includes.ToArray());
			MSGImport.SetImports(AppResources.Includes.ToArray());
			DEFSPRImport.SetImports(AppResources.Includes.ToArray());
			TRNImport.SetImports(AppResources.Includes.ToArray());
		}
		/// <summary>
		/// Performs the Auto-Include function, which will ask the importer what includes it needs
		/// and automatically includes them to this project file
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="File"></param>
		/// <param name="importer"></param>
		/// <returns></returns>
		private static async Task<bool> HandleImportMessages<T>(FileInfo File, CodeImporter<T> importer) where T : IImporterObject
		{
			async Task AutoIncludeNow(string message, IEnumerable<string> ExpectedIncludes)
			{
				//**AUTO INCLUDE
				List<string> autoIncluded = new List<string>();
				if (!string.IsNullOrWhiteSpace(message)) { // attempt to silence the warning
					var includes = ExpectedIncludes;
					foreach (var include in includes) {
						if (!SearchProjectForFile(include, out var file)) continue;
						await IncludeFile<object>(file, SFFileType.ASMFileTypes.ASM);
						autoIncluded.Add(file.Name);
					}
				}

				if (autoIncluded.Any()) {
					await EDITORStandard.ShowNotification($"Auto-Include included these files to the project:\n" +
								$" {string.Join(", ", autoIncluded)}");
				}
				//** END AUTO INCLUDE
			}
			var message2 = importer.CheckWarningMessage(File.FullName);
			await AutoIncludeNow(message2, importer.ExpectedIncludes);
			ReadyImporters();
			message2 = importer.CheckWarningMessage(File.FullName);
			if (!string.IsNullOrWhiteSpace(message2)) {
				if (MessageBox.Show(message2, "Continue?", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
					return false;
			}
			return true;
		}

		/// <summary>
		/// When called on <c>COLTABS.ASM</c> will parse out the color table and add it to the project
		/// <para/>If no other palettes are added to the project, will import <paramref name="DefaultPalette"/>
		/// automatically
		/// </summary>
		/// <param name="File"></param>
		/// <returns></returns>
		public static async Task<bool> TryIncludeColorTable(FileInfo File, string DefaultPalette = "NIGHT.COL")
		{
			if (!AppResources.IsFileIncluded(File)) {
				ReadyImporters();
				if (!await HandleImportMessages(File, COLTImport)) return false;
				var result = await COLTImport.ImportAsync(File.FullName);
				if (result == default) return false;
				AppResources.Includes.Add(result); // INCLUDE FILE FOR SYMBOL LINKING
				var msg = string.Join(Environment.NewLine, result.Groups);
				MessageBox.Show(msg, "Success!", MessageBoxButton.OKCancel);
				if (!AppResources.ImportedProject.Palettes.Any()) {
					if (!SearchProjectForFile(DefaultPalette, out var file)) return false;
					await IncludeFile<ASMFile>(file);
				}
			}
			return true;
		}
		/// <summary>
		/// Returns a palette from the file reference provided
		/// </summary>
		/// <param name="File"></param>
		/// <returns></returns>
		public static async Task<CAD.COL> GetPalette(FileInfo File)
		{
			if (!AppResources.IsFileIncluded(File)) {
				var success = await IncludeFile<object>(File) != default;
				if (!success) return null;
			}
			return AppResources.ImportedProject.Palettes[File.FullName];
		}
		/// <summary>
		/// Opens the <see cref="PaletteView"/> window with the palette file name provided
		/// </summary>
		/// <param name="File"></param>
		/// <returns></returns>
		public static async Task OpenPalette(FileInfo File)
		{
			if (!AppResources.IsFileIncluded(File)) {
				var success = await IncludeFile<object>(File) != default;
				if (!success) return;
			}
			var col = AppResources.ImportedProject.Palettes[File.FullName];
			var view = new PaletteView()
			{
				Owner = Application.Current.MainWindow
			};
			view.SetupControl(col);
			view.Show();
		}
		/// <summary>
		/// If the file is in <see cref="AppResources.OpenFiles"/>, the file will be closed
		/// </summary>
		/// <param name="File"></param>
		/// <returns></returns>
		public static bool CloseFileIfOpen(FileInfo File)
		{
			if (AppResources.OpenFiles.ContainsKey(File.FullName)) {
				AppResources.ImportedProject.CloseFile(File.FullName);
				return true;
			}
			return false;
		}
		/// <summary>
		/// Opens the specified BSP File, if it's already open, will return the file.
		/// </summary>
		/// <param name="File"></param>
		/// <param name="ForceReload">Forces the library to reload the file from disk.</param>
		/// <returns></returns>
		public static async Task<BSPFile> OpenBSPFile(FileInfo File, bool ForceReload = false)
		{
			if (AppResources.ImportedProject.OpenFiles.TryGetValue(File.FullName, out var fileInstance)) { // FILE IS OPEN
				if (fileInstance is BSPFile && !ForceReload) return fileInstance as BSPFile; // RETURN IT
																							 // WE HAVE TO RELOAD IT, SO CLOSE IT
				CloseFileIfOpen(File);
			}
			//3D IMPORT LOGIC
			if (!await HandleImportMessages(File, BSPImport)) return default;
			//**AUTO-INCLUDE COLTABS.ASM
			if (SearchProjectForFile("coltabs.asm", out var projFile))
				await TryIncludeColorTable(projFile);
			//**
			var file = await BSPImport.ImportAsync(File.FullName);
			return file;
		}
		/// <summary>
		/// Opens the specified <see cref="MAPFile"/> and returns a reference to it
		/// </summary>
		/// <param name="File"></param>
		/// <returns></returns>
		public static async Task<MAPFile> OpenMAPFile(FileInfo File)
		{
			//MAP IMPORT LOGIC
			if (!await HandleImportMessages(File, MAPImport)) return default;
			if (!MAPImport.MapContextsSet)
				await MAPImport.ProcessLevelContexts();
			var rObj = await MAPImport.ImportAsync(File.FullName);
			ShowPossibleErrors(MAPImport, File);
			return rObj;
		}

		private static void ShowPossibleErrors<T>(CodeImporter<T> importer, FileInfo file) where T : IImporterObject
		{
			var errors = importer.ErrorOut.ToString();
			if (!string.IsNullOrWhiteSpace(errors)) {
				MessageBox.Show(errors + "\nThe file was still imported -- use caution when viewing for inaccuracies.",
									"Errors Occured when Importing " + file.Name);
			}
		}

		/// <summary>
		/// Opens a <see cref="MSGFile"/> and returns a reference to it
		/// </summary>
		/// <param name="File"></param>
		/// <returns></returns>
		public static async Task<ASMFile> OpenMSGFile(FileInfo File)
		{
			// Try to auto-include MOJI_0.TRN
			var trn = AppResources.OpenFiles.Values.FirstOrDefault(x => x is TRNFile) as TRNFile;
			if (trn == null) {
				var hit = AppResources.ImportedProject.SearchFile("MOJI_0.TRN").FirstOrDefault();
				if (hit != null) {
					trn = await OpenTRNFile(new FileInfo(hit.FilePath));
				}
			}

			//MSG IMPORT LOGIC
			if (!await HandleImportMessages(File, MSGImport)) return default;
			MSGImport.TranslationTable = trn ?? AppResources.Includes.OfType<TRNFile>().LastOrDefault();
			var rObj = await MSGImport.ImportAsync(File.FullName);
			ShowPossibleErrors(MSGImport, File);
			return rObj;
		}

		public static async Task<TRNFile> OpenTRNFile(FileInfo File)
		{
			if (!await HandleImportMessages(File, TRNImport)) return default;
			var rObj = await TRNImport.ImportAsync(File.FullName);
			ShowPossibleErrors(TRNImport, File);
			return rObj;
		}

		/// <summary>
		/// Will import an *.ASM file into the project's OpenFiles collection and return the parsed result.
		/// <para>NOTE: This function WILL call a dialog to have the user select which kind of file this is.
		/// This can cause a softlock if this logic is nested with other Parse logic.</para>
		/// <para>To avoid this, please make diligent use of the <paramref name="ContextualFileType"/> parameter.</para>
		/// </summary>
		/// <param name="File">The file to parse.</param>
		/// <param name="ContextualFileType">Will skip the Dialog asking what kind of file this is parsing by using this value
		/// <para>If <see langword="default"/>, a dialog is displayed.</para></param>
		/// <returns></returns>
		private static async Task<ASMFile> ParseFile(FileInfo File, SFFileType.ASMFileTypes? ContextualFileType = default)
		{
			//GET IMPORTS SET
			ReadyImporters();
			//DO FILE PARSE NOW
			ASMFile asmfile = default;
			var fileType = File.GetSFFileType();
			if ((fileType == SFCodeProjectFileTypes.Assembly) ||
			(fileType == SFCodeProjectFileTypes.Include)) // assembly file
			{ // DOUBT AS TO FILE TYPE
			  //CREATE THE MENU WINDOW
				SFFileType.ASMFileTypes selectFileType = SFFileType.ASMFileTypes.ASM;
				if (!ContextualFileType.HasValue) {
					var importMenu = new FileImportMenu()
					{
						Owner = Application.Current.MainWindow
					};
					if (!importMenu.ShowDialog() ?? true) return default; // USER CANCEL
					selectFileType = importMenu.FileType;
				} else selectFileType = ContextualFileType.Value;

				if (selectFileType == SFFileType.ASMFileTypes.ASM) {
					asmfile = await ASMImport.ImportAsync(File.FullName);
				} else if (selectFileType == SFFileType.ASMFileTypes.MAP) {
					asmfile = await OpenMAPFile(File);
				} else if (selectFileType == SFFileType.ASMFileTypes.BSP) {
					asmfile = await OpenBSPFile(File);
				} else if (selectFileType == SFFileType.ASMFileTypes.MSG) {
					asmfile = await OpenMSGFile(File);
				} else if (selectFileType == SFFileType.ASMFileTypes.DEFSPR) {
					asmfile = await OpenDEFSPRFile(File);
				} else if (selectFileType == SFFileType.ASMFileTypes.TRN) {
					asmfile = await OpenTRNFile(File);
				} else {
					return default;
				}
			}
			if (asmfile == default) return default;
			if (!AppResources.OpenFiles.ContainsKey(File.FullName))
				AppResources.OpenFiles.Add(File.FullName, asmfile);
			return asmfile;
		}

		/// <summary>
		/// Ensures that a <see cref="MSpritesDefinitionFile"/> is in <see cref="AppResources.OpenFiles"/>
		/// </summary>
		/// <param name="DefinitionASMName"></param>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException"></exception>
		public static async Task<MSpritesDefinitionFile> EnsureMSpritesDefinitionOpen(string DefinitionASMName = "DEFSPR.ASM")
		{
			var mSpriteDef = AppResources.OpenFiles.Values.OfType<MSpritesDefinitionFile>().FirstOrDefault();
			if (mSpriteDef == null) { // there isn't one, try adding it now
				var hit = AppResources.ImportedProject.SearchFile(DefinitionASMName).FirstOrDefault();
				if (hit != default) // Can't find DEFSPR.ASM
					mSpriteDef = await OpenDEFSPRFile // found it, trying to add it
						(new FileInfo(hit.FilePath));
			}
			if (mSpriteDef == null) // could't add it
				throw new FileNotFoundException($"{DefinitionASMName} could not be found or is otherwise unreadable.");
			return mSpriteDef;
		}

		/// <summary>
		/// Opens a <see cref="MSpritesDefinitionFile"/> and returns a reference to it
		/// <para/>This function will NOT add the file to <see cref="AppResources.OpenFiles"/> --
		/// use <see cref="ParseFile(FileInfo, SFFileType.ASMFileTypes?)"/>
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static async Task<MSpritesDefinitionFile> OpenDEFSPRFile(FileInfo file, bool IgnoreSFOptimMissing = false)
		{
			if (AppResources.OpenFiles.TryGetValue(file.FullName, out var DefSpr)) return DefSpr as MSpritesDefinitionFile;
			//DEFSPR IMPORT LOGIC
			if (!IgnoreSFOptimMissing) {
				if (AppResources.ImportedProject?.GetOptimizerByTypeOrDefault(SFOptimizerTypeSpecifiers.MSprites) == null) {
					MessageBox.Show("You have chosen to open an MSprites definition file, yet you haven't created the MSpritesMap.\n\n" +
						"Please do that first and then try this function again.");
					return default;
				}
			}
			if (!await HandleImportMessages(file, DEFSPRImport)) return default;
			var rObj = await DEFSPRImport.ImportAsync(file.FullName);
			ShowPossibleErrors(DEFSPRImport, file);
			if (!AppResources.OpenFiles.ContainsKey(file.FullName))
				AppResources.OpenFiles.Add(file.FullName, rObj);
			return rObj;
		}

		internal static async Task<ASMFile> OpenASMFile(FileInfo File, bool IgnoreDialogs = false)
		{
			//DO FILE PARSE NOW
			return await ParseFile(File, IgnoreDialogs ? (SFFileType.ASMFileTypes?)SFFileType.ASMFileTypes.ASM : null);
		}

		/// <summary>
		/// Spawns a new <see cref="CommonFileDialog"/> with the desired options
		/// </summary>
		/// <param name="Title"></param>
		/// <param name="FolderBrowser"></param>
		/// <param name="InitialDirectory"></param>
		/// <param name="Multiselect"></param>
		/// <returns></returns>
		internal static string[] ShowGenericFileBrowser(string Title, bool FolderBrowser = false, string InitialDirectory = null, bool Multiselect = false)
		{
			if (InitialDirectory == null) InitialDirectory = AppResources.ImportedProject.WorkspaceDirectory.FullName;
			var d = new OpenFileDialog()
			{
				Title = Title,
				Multiselect = Multiselect,
				InitialDirectory = InitialDirectory
			}; // CREATE THE FOLDER PICKER
			if (d.ShowDialog() == true) {
				if (!FolderBrowser) {
					return d.FileNames;
				} else if (Multiselect) {
					return d.FileNames.Where(Directory.Exists).ToArray();
				} else if (Directory.Exists(d.FileName)) {
					return d.FileNames;
				} else if (File.Exists(d.FileName)) {
					return new string[] { Path.GetDirectoryName(d.FileName) };
				} else {
					return new string[0];
				}
			} else {
				return new string[0];
			}
		}

		/// <summary>
		/// Attempts to load the given <see cref="SFOptimizerNode"/> for the given type
		/// <para/>Upon failure, will throw an exception
		/// </summary>
		/// <param name="Type"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		internal static SFOptimizerNode GetOptimizerByType(SFOptimizerTypeSpecifiers Type)
		{
			var project = AppResources.ImportedProject;
			//Load the SFOptimizer
			if (!project.Optimizers.Any())
				throw new Exception("There aren't any optimizers added to this project yet.\n" +
					"Use the Refresh <Type>Map button to create this.");
			//Find the one that is a STAGE MAP
			var sfOptim = project.GetOptimizerByTypeOrDefault(Type);
			if (sfOptim == default)
				throw new Exception($"This project has Optimizers, but none of them are for {Type}.\n" +
					"Use the Refresh <Type>Map button to create this.");
			return sfOptim;
		}

		/// <summary>
		/// This uses an SFOptimizer in the node that stores Levels to map Level Macro Name to the File it appears in.
		/// <para/>This will look up the level by it's name as it appears in it's header.
		/// </summary>
		/// <returns></returns>
		internal static async Task<MAPScript> GetMapScriptByMacroName(string LevelMacroName)
		{
			var HeaderName = LevelMacroName;
			var stageOptim = GetOptimizerByType(SFOptimizerTypeSpecifiers.Maps);
			var stageMap = stageOptim.OptimizerData.ObjectMap;
			//Try to find the file that contains the stage we want
			if (!stageMap.TryGetValue(HeaderName, out var FileName)) return default;
			var path = Path.Combine(Path.GetDirectoryName(stageOptim.FilePath), FileName);
			//Open the file
			var file = await OpenMAPFile(new FileInfo(path));
			if (file.Scripts.TryGetValue(LevelMacroName, out var script))
				return script;
			return default;
		}

		internal static SaveFileDialog InitSaveFileDialog(string title, string defaultFileName)
		{
			return new SaveFileDialog()
			{
				AddExtension = false,
				CreatePrompt = false,
				CheckFileExists = false,
				CheckPathExists = true,
				InitialDirectory = AppResources.ImportedProject.WorkspaceDirectory.FullName,
				Title = title,
				FileName = defaultFileName,
				OverwritePrompt = true
			};
		}
	}
}
