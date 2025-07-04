﻿using System.Collections.Generic;
using System.Linq;

namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// Contains data on an audio BIN file and the addresses of the song data contained within
    /// </summary>
    public class AudioBINFile : IImporterObject
    {
        /// <summary>
        /// Contains the tables mapping where the song data lives in this file
        /// <para>Index, <see cref="AudioBINSongData"/></para>
        /// </summary>
        public HashSet<AudioBINTable> SongTables { get; } = new HashSet<AudioBINTable>();
        /// <summary>
        /// Contains the tables mapping where the sample data lives in this file
        /// <para>Index, <see cref="AudioBINSampleData"/></para>
        /// </summary>
        public HashSet<AudioBINTable> SampleTables { get; } = new HashSet<AudioBINTable>();
        /// <summary>
        /// A collection of songs that this file contains
        /// </summary>
        public List<AudioBINSongData> Songs { get; } = new List<AudioBINSongData>();
        /// <summary>
        /// A collection of samples that this file contains
        /// </summary>
        public List<AudioBINSampleData> Samples { get; } = new List<AudioBINSampleData>();
        /// <summary>
        /// The raw chunk data taken from the BIN file
        /// </summary>
        public List<AudioBINChunk> Chunks { get; private set; } = new List<AudioBINChunk>();

        public AudioBINFile(string originalFilePath)
        {
            OriginalFilePath = originalFilePath;
        }

        public string OriginalFilePath { get; }
        /// <summary>
        /// Combines <see cref="SampleTables"/> and <see cref="SongTables"/> and orders the result by <see cref="AudioBINTable.SPCAddress"/>
        /// <para/>By default, all Tables are split into <see cref="SampleTables"/> and <see cref="SongTables"/> and unordered
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AudioBINTable> GetAllTablesOrdered()
        {
            var allTables = new List<AudioBINTable>();
            allTables.AddRange(SongTables);
            allTables.AddRange(SampleTables);
            return allTables.OrderBy(t => t.SPCAddress);
        }
    }
}
