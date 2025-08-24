using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Starfox.Editor;
using StarFox.Interop.ASM;
using StarFox.Interop.MAP.EVT;
using StarwingMapVisualizer;
using StarwingMapVisualizer.Dialogs;
using StarwingMapVisualizer.Screens;
//using StarwingMapVisualizer.Controls;
//using StarwingMapVisualizer.Controls.Subcontrols;

namespace StarwingMapVisualizer.Misc
{
	/// <summary>
	/// Common helper functions for creating an interactive editor environment
	/// </summary>
	internal static class EDITORStandard
	{
		// FIXME : Welcome wagon is skipped on Avalonia to prevent an non-CPU bound hang
		private static bool WelcomeWagonShownOnce = true;

		/// <summary>
		/// There should only ever be one instance of the Edit screen at any given time
		/// </summary>
		internal static EditScreen CurrentEditorScreen { get; set; }

		private static LoadingWindow _loadingWindow;

		/// <summary>
		/// Shows a Loading... Window in the middle of the Window.
		/// <para/> If a EditScreen is created, it will be hidden using the <see cref="DimEditorScreen"/> method
		/// </summary>
		public static void ShowLoadingWindow()
		{
			DimEditorScreen();
			if (_loadingWindow == null) {
				_loadingWindow = new LoadingWindow();
			}

			_loadingWindow.Show(Application.Current.MainWindow());
		}

		/// <summary>
		/// Hides the <see cref="ShowLoadingWindow"/> window
		/// </summary>
		public static void HideLoadingWindow()
		{
			UndimEditorScreen();

			_loadingWindow?.Hide();
		}

		/// <summary>
		/// If an <see cref="EditScreen"/> is added to this program, this will dim it.
		/// </summary>
		public static void DimEditorScreen()
		{
			if (CurrentEditorScreen != null)
				CurrentEditorScreen.LoadingSpan.IsVisible = true;
		}

		/// <summary>
		/// See: <see cref="DimEditorScreen"/>
		/// </summary>
		public static void UndimEditorScreen()
		{
			if (CurrentEditorScreen != null)
				CurrentEditorScreen.LoadingSpan.IsVisible = false;
		}

		/// <summary>
		/// Changes the interface to be on the Editor passed as an argument
		/// </summary>
		public static async Task SwitchEditorView(EditScreen.ViewMode view) =>
			await CurrentEditorScreen.SwitchView(view);

		/* TODO : Import OBJViewer, MAPViewer and ASMViewer
		/// <summary>
		/// See: <see cref="SHAPEControl.ShowShape(string, int)"/>
		/// </summary>
		/// <param name="ShapeName"></param>
		/// <param name="Frame"></param>
		/// <returns></returns>
		public static async Task<bool> ShapeEditor_ShowShapeByName(string ShapeName, int Frame = -1)
		{
			if (!await CurrentEditorScreen.OBJViewer.ShowShape(ShapeName, Frame))
				return false;
			await SwitchEditorView(EditScreen.ViewMode.OBJ);
			return true;
		}

		/// <summary>
		/// This function is used when the user selects a MapNode in the MAPControl.
		/// <para/>Map Nodes can represent many types of information, using the <paramref name="Component"/>
		/// can narrow down what the user actually meant to select to get more info on.
		/// <para/><paramref name="Component"/> being null indicates it's unclear what they meant to select
		/// and the most generic action should be taken
		/// </summary>
		/// <param name="MapEvent"></param>
		/// <param name="Component"></param>
		/// <returns></returns>
		public static Task<bool> MapEditor_MapNodeSelected(MAPEvent MapEvent, Type Component) =>
			CurrentEditorScreen.MAPViewer.MapNodeSelected(MapEvent, Component);

		/// <summary>
		/// Opens the ASMViewer in the editor to the passed <see cref="ASMChunk"/>
		/// </summary>
		/// <param name="Symbol"></param>
		/// <returns></returns>
		public static async Task AsmEditor_OpenSymbol(ASMChunk Symbol)
		{
			await SwitchEditorView(EditScreen.ViewMode.ASM);
			await CurrentEditorScreen.ASMViewer.OpenSymbol(Symbol);
		}//*/

		private static async Task<SFOptimizerDataStruct> Editor_BaseDoRefreshMap(SFOptimizerTypeSpecifiers type,
		Func<FileInfo, Dictionary<string, string>, Task<bool>> processFunction, string initialDirectory = null,
		string keyFile = null)
		{
		retry:
			var filesSelected =
				await FILEStandard.ShowGenericFileBrowser($"Select ALL of your {type.ToString().ToUpper()} Files",
					false, initialDirectory, true);
			if (filesSelected == null) {
				return null; // User Cancelled
			}

			var errorBuilder = new StringBuilder(); // ERRORS
			if (!filesSelected.Any()) {
				return null;
			}

			var dirInfo = Path.GetDirectoryName(filesSelected.First());
			if (dirInfo == null || !Directory.Exists(dirInfo)) {
				return null;
			}

			//TEST SOMETHING OUT
			if (!filesSelected.Select(x => Path.GetFileName(x).ToLower()).Contains(keyFile.ToLower())) {
				if (MessageBox.Show("It looks like the directory you selected doesn't have at least " +
									$"a {keyFile.ToUpper()} file in it. Have you selected the {type.ToString().ToUpper()} directory in your workspace?\n\n" +
									"Would you like to continue anyway? No will go back to file selection.",
						"Directory Selection Message",
						MessageBoxButton.YesNo) != MessageBoxResult.Yes) {
					goto retry;
				}
			}

			//GET IMPORTS SET
			FILEStandard.ReadyImporters();
			var shapesMap = new Dictionary<string, string>();
			foreach (var file in filesSelected.Select(x => new FileInfo(x))) // ITERATE OVER DIR FILES
			{
				try {
					bool result = await processFunction(file, shapesMap);
					if (!result) {
						break;
					}
				} catch (Exception ex) {
					errorBuilder.AppendLine(
						$"The file: {file.FullName} could not be exported.\n***\n{ex}\n***"); // ERROR INFO
				}
			}

			return new SFOptimizerDataStruct(type, dirInfo, shapesMap) {
				ErrorOut = errorBuilder
			};
		}

		/// <summary>
		/// Prompts the user to export all 3D models and will export them
		/// </summary>
		/// <param name="format">.sfshape or .obj (case-sensitive)</param>
		public static async Task Editor_ExportAll3DShapes(string format)
		{
			// EXPORT 3D FUNCTION -- I MADE HISTORY HERE TODAY. 11:53PM 03/31/2023 JEREMY GLAZEBROOK.
			// I MADE A GUI PROGRAM THAT EXTRACTED STARFOX SHAPES SUCCESSFULLY AND DUMPED THEM ALL IN READABLE FORMAT.
			var r = MessageBox.Show($"Welcome to the Export 3D Assets Wizard!\n" +
									$"This tool will do the following: Export all 3D assets from the selected directory to *{format} files and palettes.\n" +
									$"It will dump them to the exports/models directory.\n" +
									$"You will get a manifest of all files dumped with their model names as well.\n" +
									$"Happy hacking! - Love, Bisquick <3", "Export 3D Assets Wizard",
				MessageBoxButton.OKCancel); // WELCOME MSG
			if (r == MessageBoxResult.Cancel) {
				return; // OOPSIES!
			}

			var filesSelected =
				await FILEStandard.ShowGenericFileBrowser($"Select ALL of your SHAPES Files", false, null, true);
			if ((filesSelected == null) || (filesSelected.Length < 1)) {
				return; // User Cancelled
			}

			ShowLoadingWindow();
			var errorBuilder  = new StringBuilder(); // ERRORS
			var exportedBSPs  = new StringBuilder(); // BSPS
			var exportedFiles = new StringBuilder(); // ALL FILES

			FILEStandard.ReadyImporters();                                     //GET IMPORTS SET
			foreach (var file in filesSelected.Select(x => new FileInfo(x))) { // ITERATE OVER DIR FILES
				var bspFile = await FILEStandard.OpenBSPFile(file);            // IMPORT THE BSP
				foreach (var shape in bspFile.Shapes) {                        // FIND ALL SHAPES
					try {
						var files = await SHAPEStandard.ExportShape(shape, format); // USE STANDARD EXPORT FUN
						var c     = files.Count;
						for (var i = 0; i < c; i++) {
							exportedFiles.AppendLine(files[i]); // EXPORTED FILES
						}

						if (c > 0) {
							exportedBSPs.AppendLine(files[0]); // BSP ONLY
						}
					} catch (Exception ex) {
						errorBuilder.AppendLine(
							$"The shape {shape.Header.Name} in file {file.FullName} could not be exported.\n***\n{ex}\n***"); // ERROR INFO
					}
				}
			}

			//CREATE THE MANIFEST FILE
			var strExportDir = SHAPEStandard.DefaultShapeExtractionDirectory;
			File.WriteAllText(Path.Combine(strExportDir, "manifest.txt"), exportedBSPs.ToString());
			File.WriteAllText(Path.Combine(strExportDir, "manifest-all.txt"), exportedFiles.ToString());
			var strExportEnded = $"Files exported to:\n{strExportDir} .\n";
			if (errorBuilder.Length > 0) {
				File.WriteAllText(Path.Combine(strExportDir, "errors.log"), errorBuilder.ToString());
				strExportEnded += "However, there have been errors.\n";
			}

			strExportEnded += "\nDo you want to open the directory and copy its location to the clipboard?";
			if (MessageBox.Show(strExportEnded, "Complete", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
				await TopLevel.GetTopLevel(Application.Current.MainWindow()).Clipboard
					.SetTextAsync(SHAPEStandard.DefaultShapeExtractionDirectory);
				OpenExternal.Folder(strExportDir);
			}

			HideLoadingWindow();
		}

		/// <summary>
		/// Refreshes the map that is provided using the <paramref name="type"/> parameter
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static async Task<SFOptimizerNode> Editor_RefreshMap(SFOptimizerTypeSpecifiers type,
		string initialDirectory = null)
		{
			async Task<bool> GetShapeMap(FileInfo file, Dictionary<string, string> map)
			{
				Dictionary<string, string> shapeMap = map;
				var                        bspFile  = await FILEStandard.OpenBSPFile(file); // IMPORT THE BSP
				foreach (var shape in bspFile.Shapes) {
					var sName    = shape.Header.Name.ToUpper();
					var fooSName = sName;
					int tries    = 1;
					while (shapeMap.ContainsKey(fooSName)) {
						fooSName = sName + "_" + tries;
						tries++;
					}

					sName = fooSName;
					shapeMap.Add(sName, file.Name);
				}

				int delta = bspFile.ShapeHeaderEntries.Count - bspFile.Shapes.Count;
				return true;
			}

			async Task<bool> GetLevelMap(FileInfo file, Dictionary<string, string> Map)
			{
				Dictionary<string, string> stageMap = Map;
				var                        mapFile  = await FILEStandard.OpenMAPFile(file); // IMPORT THE MAP
				foreach (var level in mapFile.Scripts) {
					var sName = level.Key;
					stageMap.TryAdd(sName, file.Name);
				}

				return true;
			}

			async Task<bool> GetMSpriteMap(FileInfo file, Dictionary<string, string> map)
			{
				string ext = file.Extension.ToUpper();
				if (ext == ".BIN" || ext == ".DAT") {
					map.Add(file.FullName, "");
				}

				return true;
				var defSpr = await FILEStandard.OpenDEFSPRFile(file, true);
				if (defSpr == null) {
					return false;
				}

				// IMPORT THE DEFSPR
				foreach (var bank in defSpr.Banks) {
					foreach (var sprite in bank.Value.Sprites)
						map.Add(sprite.Key, file.Name);
				}

				return true;
			}

			if (AppResources.ImportedProject == null) {
				return null;
			}

			SFOptimizerDataStruct dataStruct = null;
			try {
				switch (type) {
					case SFOptimizerTypeSpecifiers.Shapes:
						dataStruct = await Editor_BaseDoRefreshMap(type, GetShapeMap, initialDirectory, "shapes.asm");
						break;
					case SFOptimizerTypeSpecifiers.Maps:
						dataStruct = await Editor_BaseDoRefreshMap(type, GetLevelMap, initialDirectory, "level1_1.asm");
						break;
					case SFOptimizerTypeSpecifiers.MSprites:
						dataStruct = await Editor_BaseDoRefreshMap(type, GetMSpriteMap, initialDirectory, "tex_01.bin");
						break;
				}
			} catch (Exception ex) {
				AppResources.ShowCrash(ex, false, $"Refreshing the {type} optimizer");
			}

			if (dataStruct == null) {
				return null;
			}

			if (dataStruct.HasErrors) {
				MessageBox.Show(
					$"The following error(s) occured with optimizing that directory.\n{dataStruct.ErrorOut}");
			}

			//Attempt to add to project
			var dirNode = AppResources.ImportedProject.SearchDirectory(Path.GetFileName(dataStruct.DirectoryPath))
				.FirstOrDefault();
			if (dirNode == null) {
				AppResources.ShowCrash(
					new FileNotFoundException(
						"Couldn't find the node that matches this directory in the Code Project."),
					false,
					$"Could not refresh {type} because the directory it corresponds with isn't in this project.");
				return null;
			}

			var node = dirNode.AddOptimizer(type.ToString(), dataStruct);
			MessageBox.Show(
				$"The {type} Code Project Optimizer has been updated with {dataStruct.ObjectMap.Count} items.");
			return node;
		}

		/// <summary>
		/// Opens up the best editor to open an item mapped in the given optimizer type.
		/// </summary>
		/// <param name="optimizerType">The type of <see cref="SFOptimizerNode"/> this item appears in</param>
		/// <param name="objectName">The name of the object in the optimizer: Shapes, Levels, etc.</param>
		/// <exception cref="NotImplementedException"></exception>
		internal static Task<bool> InvokeOptimizerMapItem(SFOptimizerTypeSpecifiers optimizerType, string objectName)
		{
			switch (optimizerType) {
				case SFOptimizerTypeSpecifiers.Shapes:
				// TODO : Implement ShapeEditor_ShowShapeByName
				//return ShapeEditor_ShowShapeByName(ObjectName);
				case SFOptimizerTypeSpecifiers.Maps:
				default:
					throw new NotImplementedException("There is no way to handle that item yet.");
			}
		}

		/// <summary>
		/// Ensures all prerequisites are added to the project
		/// </summary>
		/// <returns>True if any changes were made to the project, false if there are no changes</returns>
		internal static async Task<bool> WelcomeWagon()
		{
			if (WelcomeWagonShownOnce) {
				return false;
			}

			if (AppResources.ImportedProject == null) {
				return false;
			}

			if (AppResources.ImportedProject.EnsureOptimizers(out SFOptimizerTypeSpecifiers[] missing)) {
				return false;
			}

			foreach (var missingType in missing) {
				if (MessageBox.Show($"Your project is missing the {missingType}Map optimizer.\n" +
									$"\nWould you like to add this now?", $"Missing {missingType}Map Optimizer",
						MessageBoxButton.YesNo)
					== MessageBoxResult.No) {
					continue;
				}

				await Editor_RefreshMap(missingType);
			}

			WelcomeWagonShownOnce = true;
			return true;
		}

		/// <summary>
		/// Shows a new <see cref="Notification"/> on the <see cref="MainWindow"/> and waits for the old one to expire
		/// </summary>
		/// <param name="text"></param>
		/// <param name="callback"></param>
		/// <param name="lifespan"></param>
		/// <returns></returns>
		internal static void ShowNotification(string text, Action callback, TimeSpan? lifespan = null)
		{
			// ShowNotification can be called from a task worker thread, but UI stuff must be done in the UI thread
			Dispatcher.UIThread.InvokeAsync(() => {
				var notif = new Notification("", text, NotificationType.Information,
					lifespan ?? TimeSpan.FromSeconds(2.5), null, callback);
				(Application.Current.MainWindow() as MainWindow)?.PushNotification(notif);
			});
		}

		internal static void ShowNotification(string text)
		{
			ShowNotification(text, Nop);
		}

		private static void Nop()
		{
			// do nothing
		}
	}
}
