﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Starfox.Editor.Settings;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.BRR;
using StarFox.Interop.MAP;
using StarFox.Interop.MISC;
using static StarFox.Interop.GFX.CAD;

namespace Starfox.Editor
{

	/// <summary>
	/// Represents a StarFox Editor Code Project
	/// </summary>
	public class SFCodeProject
	{
		/// <summary>
		/// Tracks long-term storage like Settings and Paths without runtime information
		/// </summary>
		protected SFCodeProjectState BaseState { get; set; } = new SFCodeProjectState();

		public IDictionary<SFCodeProjectSettingsTypes, SFEditorSettings> Settings => BaseState.Settings;
		/// <summary>
		/// Gets the specified type of settings and casts to the provided type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Type">The type of settings</param>
		/// <returns></returns>
		public T GetSettings<T>(SFCodeProjectSettingsTypes Type) where T : SFEditorSettings => BaseState.GetSettings<T>(Type);
		/// <summary>
		/// Palettes that have been included in this project
		/// <para>FilePath, COL</para>
		/// </summary>
		public Dictionary<string, COL> Palettes { get; } = new Dictionary<string, COL>();
		/// <summary>
		/// SampleFiles that have been included in this project
		/// <para>FilePath, BRR</para>
		/// </summary>
		public Dictionary<string, BRRFile> Samples { get; } = new Dictionary<string, BRRFile>();
		/// <summary>
		/// Files that are marked as *include files, as in containing symbol information
		/// </summary>
		public HashSet<ASMFile> Includes { get; } = new HashSet<ASMFile>();
		/// <summary>
		/// All files that have been imported by a <see cref="CodeImporter"/>
		/// </summary>
		public Dictionary<string, IImporterObject> OpenFiles { get; } = new Dictionary<string, IImporterObject>();
		public IEnumerable<MAPFile> OpenMAPFiles => OpenFiles.Values.OfType<MAPFile>();
		/// <summary>
		/// A map of all directory nodes by their name
		/// </summary>
		public Dictionary<string, SFCodeProjectNode> DirectoryNodes = new Dictionary<string, SFCodeProjectNode>();
		/// <summary>
		/// A map of all file nodes by their file name
		/// </summary>
		public Dictionary<string, SFCodeProjectNode> FileNodes = new Dictionary<string, SFCodeProjectNode>();
		/// <summary>
		/// Looks for the specified file (or palette) to see if it's included
		/// </summary>
		/// <param name="File"></param>
		/// <returns></returns>
		public bool IsFileIncluded(FileInfo File)
		{
			var value = Includes.Any(x => x.OriginalFilePath == File.FullName);
			if (value) return value;
			value = IsPaletteIncluded(File.FullName);
			return value;
		}
		public ASMFile GetInclude(FileInfo File) => Includes.FirstOrDefault(x => x.OriginalFilePath == File.FullName);
		/// <summary>
		/// Looks to see if that palette has been referenced already
		/// </summary>
		/// <param name="FilePath"></param>
		/// <returns></returns>
		public bool IsPaletteIncluded(string FilePath) => Palettes.Any(x => x.Key.ToLower() == FilePath.ToLower());
		/// <summary>
		/// The path to the base directory of this <see cref="SFCodeProject"/> instance
		/// </summary>
		public DirectoryInfo WorkspaceDirectory { get; }
		/// <summary>
		/// The root node of this project file
		/// </summary>
		public SFCodeProjectNode ParentNode { get; private set; }
		public IEnumerable<SFOptimizerNode> Optimizers => FileNodes.Values.OfType<SFOptimizerNode>();
		/// <summary>
		/// Creates a new <see cref="SFCodeProject"/> with the given path to the base folder of the starfox project file
		/// </summary>
		/// <param name="workspaceDirectory"></param>
		public SFCodeProject(string workspaceDirectory)
		{
			var dirInfo = new DirectoryInfo(workspaceDirectory);
			if (!dirInfo.Exists) throw new ArgumentException("The given directory doesn't actually exist.");
			WorkspaceDirectory = dirInfo;
			ParentNode = new SFCodeProjectNode(SFCodeProjectNodeTypes.Project, workspaceDirectory);
		}
		/// <summary>
		/// Searches for all files with matching filename (not fullpath)
		/// </summary>
		/// <param name="FileName"></param>
		/// <returns></returns>
		public IEnumerable<SFCodeProjectNode> SearchFile(string FileName, bool IgnoreHyphens = false) =>
			FileNodes.Where(x => Path.GetFileName(IgnoreHyphens ? x.Key.Replace("-", "") : x.Key).ToLower()
			== FileName.ToLower()).Select(y => y.Value);
		/// <summary>
		/// Searches for all directories with matching name (not fullpath)
		/// </summary>
		/// <param name="FileName"></param>
		/// <returns></returns>
		public IEnumerable<SFCodeProjectNode> SearchDirectory(string Name) =>
			DirectoryNodes.Where(x => x.Key.ToLower().EndsWith(Name.ToLower())).Select(y => y.Value);
		public bool CloseFile(string FilePath) => OpenFiles.Remove(FilePath);
		public bool CloseFile(IImporterObject File) => CloseFile(File.OriginalFilePath);
		/// <summary>
		/// When called will populate the project file with all directories and files from the given project directory.
		/// <para>Dictated by the <see cref="WorkspaceDirectory"/></para>
		/// </summary>
		/// <returns></returns>
		public Task EnumerateAsync()
		{
			SFCodeProjectNode processFile(FileInfo File, SFCodeProjectNode ParentNode)
			{// make a new node that represents this file in the parent node
				if (File.Name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase)) {
					return null;
				} else {
					var node = ParentNode.AddFile(File);
					FileNodes.Add(File.FullName, node);
					return node;
				}
			}

			void TrashGeneratedFileNode(SFCodeProjectNode newNode, SFCodeProjectNode child, SFCodeProjectFileTypes originalType, List<SFCodeProjectNode> trashCan)
			{
				var strPath = Path.ChangeExtension(child.FilePath, "." + originalType);
				if (newNode.ChildNodes.Any(x =>
					    x.Type == SFCodeProjectNodeTypes.File &&
					    x.RecognizedFileType == originalType &&
					    x.FilePath.Equals(strPath, StringComparison.OrdinalIgnoreCase))) {
					trashCan.Add(child);
				}
			}

			SFCodeProjectNode processDirectory(DirectoryInfo Directory, SFCodeProjectNode ParentNode)
			{
				SFCodeProjectNode newNode = new SFCodeProjectNode(SFCodeProjectNodeTypes.Directory, Directory.FullName);
				var mDirName = Directory.FullName.Substring(WorkspaceDirectory.FullName.Length).TrimStart(Path.DirectorySeparatorChar);
				if (!string.IsNullOrWhiteSpace(mDirName))
					DirectoryNodes.Add(mDirName, newNode);
				if (ParentNode != null)
					newNode = ParentNode.AddDirectory(Directory); // make a new node that represents this folder in the parent node
				foreach (var folder in Directory.EnumerateDirectories()) // find every folder inside this one
					processDirectory(folder, newNode); // add it to this node
				foreach (var file in Directory.EnumerateFiles()) // find every file inside this one
					processFile(file, newNode); // add the file to this node

				// Keep CCR files in tree where there is no matching CGX file and 
				// keep PCR files where there is no matching SCR file
				var lstTrash = new List<SFCodeProjectNode>();
				foreach (var child in newNode.ChildNodes) {
					if (child.Type == SFCodeProjectNodeTypes.File) {
						if (child.RecognizedFileType == SFCodeProjectFileTypes.CCR) {
							TrashGeneratedFileNode(newNode, child, SFCodeProjectFileTypes.CGX, lstTrash);
						} else if (child.RecognizedFileType == SFCodeProjectFileTypes.PCR) {
							TrashGeneratedFileNode(newNode, child, SFCodeProjectFileTypes.SCR, lstTrash);
						}
					}
				}
				// Empty trash
				while (lstTrash.Count > 0) {
					newNode.ChildNodes.Remove(lstTrash[0]);
					lstTrash.RemoveAt(0);
				}

				return newNode;
			}
			FileNodes.Clear();
			DirectoryNodes.Clear();
			return Task.Run(delegate // do this work on a background thread
			{
				ParentNode = processDirectory(WorkspaceDirectory, null);
			});
		}

		/// <summary>
		/// Returns a reference to the <see cref="SFOptimizerNode"/> added matching the given <paramref name="type"/>
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public SFOptimizerNode GetOptimizerByTypeOrDefault(SFOptimizerTypeSpecifiers type) => Optimizers.FirstOrDefault(x =>
				x?.OptimizerData?.TypeSpecifier == type);

		/// <summary>
		/// Ensures necessary <see cref="SFOptimizerNode"/>s are included in the <see cref="SFCodeProject"/>
		/// <para/>Any missing optimizers are placed into the <paramref name="TypesMissing"/> array
		/// </summary>
		/// <returns></returns>
		public bool EnsureOptimizers(out SFOptimizerTypeSpecifiers[] TypesMissing)
		{
			var list = new List<SFOptimizerTypeSpecifiers>();
#if NETFRAMEWORK || NETSTANDARD
			foreach (var type in Utility.GetValues<SFOptimizerTypeSpecifiers>()) {
#else
			foreach (var type in Enum.GetValues<SFOptimizerTypeSpecifiers>()) {
#endif
				if (GetOptimizerByTypeOrDefault(type) == default)
					list.Add(type);
			}
			TypesMissing = list.ToArray();
			return !list.Any();
		}
	}
}
