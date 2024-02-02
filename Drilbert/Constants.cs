using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace Drilbert
{

    public static class Constants
    {
        public static readonly string rootPath = getRootPath();
        public const int tileSize = 16;

        public const int dirtSegmentIdsStart = 4096;

        public const int deletedPlaceholderTile = -1;
        public const int dirtTileId = 29;
        public const int playerSpawnId = 6;
        public const int airTileId = 0;
        public const int lootTileId = 5;
        public const int bombItemTileId = 12;
        public const int levelEndTileId = 4;
        public const int bombTileId = 10;
        public const int fireTileId = 120;
        public const int megadrillItemTileId = 2;
        public const int megadrillTileId = 3;

        public const int rockGraphicalBaseTile = 77;
        public const int bedrockTileId = 53;
        public static readonly HashSet<int> rockTileIdSet = new HashSet<int>{77, 11};

        public static readonly int[] diamondIds = new[] { 102, 103, 110, 111 };

        public const int ditherTopLeftId = 96;
        public const int ditherTopId = 97;
        public const int ditherTopRightId = 98;
        public const int ditherLeftId = 104;
        public const int ditherRightId = 106;
        public const int ditherBottomLeftId = 112;
        public const int ditherBottomId = 113;
        public const int ditherBottomRightId = 114;

        public static readonly int[] oneTileDither = new[] { 127, 134 };

        public const int guiBorderTopLeftId = 99;
        public const int guiBorderTopId = 100;
        public const int guiBorderTopRightId = 101;
        public const int guiBorderLeftId = 107;
        public const int guiBorderRightId = 109;
        public const int guiBorderBottomLeftId = 115;
        public const int guiBorderBottomId = 116;
        public const int guiBorderBottomRightId = 117;

        public const int hudBorderLeftId = 13;
        public const int hudBorderCentreId = 14;
        public const int hudBorderRightId = 15;

        public const long keyRepeatMs = (long)(1000 * 0.125);
        public const long defaultAnimationFrameIntervalMs = 1000 / 2;
        public const long moveInterpolationMs = (long) (1000 * 0.13);
        public const long hangBetweenStatesMs = (long) (1000 * 0.3);
        public const long menuKeyRepeatMs = (long) (1000 * 0.3);
        public const long levelSelectReticlePeriodMs = 250;

        public const int levelWidth = 32;
        public const int levelHeight = 16;

        public static readonly Color levelBackgroundColor = new Color(16, 25, 28, 255);
        public static readonly Color outsideLevelBackgroundColor = new Color(11, 17, 19, 255);
        public static readonly Color drilbertWhite = new Color(255, 249, 228);

        private static string getRootPath() {
            string root = Assembly.GetEntryAssembly()!.Location;
            while (!Directory.Exists(root + "/gfx"))
                root = Directory.GetParent(root).FullName;
            return root;
        }
    }
}