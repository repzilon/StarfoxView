namespace StarFox.Interop
{
    public static class SFFileType
    {
        /// <summary>
        /// Files that are interpreted as ASM
        /// </summary>
        public enum ASMFileTypes
        {
            /// <summary>
            /// General-purpose ASM Code File with no specialized behavior
            /// </summary>
            ASM,
            /// <summary>
            /// An ASM File that is written as a Map-Script.
            /// </summary>
            MAP,
            /// <summary>
            /// A source file representative of a 3D Model.
            /// </summary>
            BSP,
            /// <summary>
            /// Messages for commentary in the game
            /// </summary>
            MSG,
            /// <summary>
            /// 3D MSprites Definition
            /// </summary>
            DEFSPR,
        }

        /// <summary>
        /// Files that are stored and interpreted as Binary
        /// </summary>
        public enum BINFileTypes
        {
            /// <summary>
            /// Interlaced CGX files in High and Low Banks <see cref="GFX.DAT.FXGraphicsHiLowBanks"/>
            /// </summary>
            COMPRESSED_CGX,
            /// <summary>
            /// Sound Effects (Sampled Audio) using the Bit Rate Reduction technique
            /// </summary>
            BRR,
            /// <summary>
            /// Sequence data that dictates the structure of a Song
            /// </summary>
            SPC,
        }

        public static string GetSummary(ASMFileTypes Type)
        {
            var karSummaries = new string[] {
                "Just Assembly", "Map-Script File", "Compiled 3D Models", "Communications", "3D Textures (MSprites)"
            };
            return (Type >= ASMFileTypes.ASM) && (Type <= ASMFileTypes.DEFSPR) ? karSummaries[(int)Type] : "Not found";
        }

        public static string GetSummary(BINFileTypes Type)
        {
            var karSummaries = new string[] {
                "Crunch'd Graphics (CGX)", "Sound Effects (Samples) (BRR)", "Unpack Audio BIN (ABIN)"
            };
            return (Type >= BINFileTypes.COMPRESSED_CGX) && (Type <= BINFileTypes.SPC) ? karSummaries[(int)Type] : "Not found";
        }
    }
}
