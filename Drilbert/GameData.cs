using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Drilbert
{
    public struct TileId
    {
        public int val;
        public TileId(int val) {this.val = val;}
        public static bool operator==(TileId tile, int other) { return tile.val == other; }
        public static bool operator!=(TileId tile, int other) { return tile.val != other; }
        public static bool operator==(TileId tile, TileId other) { return tile.val == other.val; }
        public static bool operator!=(TileId tile, TileId other) { return tile.val != other.val; }
        public bool Equals(TileId other) { return val == other.val; }
        public override bool Equals(object obj) { return obj is TileId other && val == other.val; }
        public override int GetHashCode() { return val; }
    }
    public struct RenderTileId
    {
        public int val;
        public RenderTileId(int val) {this.val = val;}
    }

    public struct Tile
    {
        public TileId tileId;

        // Unique per instance of a tile, we use this to track a tile's movement across iterations
        public int tileIdentity;
        // This is used to prevent segments from merging - eg if a block of rocks falls on top of another
        // block of rocks, we want them to stay distinct even though their tileIds are the same
        public int segmentId;

        // Only present for bombs
        public int bombId;

        public int overrideRenderId;

        public Tile(TileId tileId, int tileIdentity) { this.tileId = tileId; this.tileIdentity = tileIdentity; this.segmentId = 0; this.bombId = 0; this.overrideRenderId = 0; }
        public Tile(int tileId, int tileIdentity) { this.tileId = new TileId(tileId); this.tileIdentity = tileIdentity; this.segmentId = 0; this.bombId = 0; this.overrideRenderId = 0; }
    }

    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right,
    }

    public enum SoundId
    {
        Move,
        Drill,
        Error,
        Death,
        Coin,
        BigMovement,
        Megadrill,
        EquipmentPickup,
        Diamond,
    }

    public enum FireDirection
    {
        NoFire,
        NoDirection,
        Up,
        Down,
        Left,
        Right,
    }

    public static class FireDirectionExtensions
    {
        public static Direction toDirection(this FireDirection fireDirection)
        {
            switch (fireDirection)
            {
                case FireDirection.Up: return Direction.Up;
                case FireDirection.Down: return Direction.Down;
                case FireDirection.Left: return Direction.Left;
                case FireDirection.Right: return Direction.Right;
                default: return Direction.None;
            }
        }
    }

    public struct TileTempState
    {
        public FireDirection fireDirection;
        public bool shaking;
    }

    public unsafe class Tilemap : ICloneable, IEquatable<Tilemap>
    {
        public string pathRoot;

        public string path;
        public string title;
        private UnManagedArray2D<Tile> tiles;
        private List<UnManagedArray2D<Tile>> background;
        public Vec2i dimensions;
        public Vec2i playerPosition;
        public int maxLoot;
        public int maxDiamonds;
        public int currentLoot;
        public int currentDiamonds;
        public int currentBombs;
        public int currentMegadrills;
        public bool win;
        public bool dead;
        public int nextBombId;
        public string prompt;

        public int nextTileIdentity = 1;
        public int nextSegmentId = 1;


        // Visual/sound-only state, not copied / saved
        public bool shakeScreen;
        public UnManagedArray2D<TileTempState> tileTempState;
        public HashSet<SoundId> soundEffects;

        // Map from removes tiles to a virtual point they move towards. This lets animations work
        // when the tile was actually destroyed, eg by leaving the screen or being smashed by a megadrill
        public Dictionary<int, Vec2i> removedTilesAnimationPoints;
        public Direction digDirection;

        public Tilemap(string pathRoot, string path, XmlDocument doc = null)
        {
            this.pathRoot = pathRoot;
            this.path = path;
            reload(doc);
        }

        private void initBasic()
        {
            title = null;
            tiles = null;
            dimensions = new Vec2i(0,0);
            playerPosition = new Vec2i(0,0);
            maxLoot = 0;
            maxDiamonds = 0;
            currentLoot = 0;
            currentDiamonds = 0;
            currentBombs = 0;
            currentMegadrills = 0;
            win = false;
            dead = false;
            nextBombId = 1;
            prompt = null;
            shakeScreen = false;
            tileTempState = null;
            soundEffects = new HashSet<SoundId>();
            removedTilesAnimationPoints = new Dictionary<int, Vec2i>();
            digDirection = Direction.None;
        }

        public void reload(XmlDocument doc = null)
        {
            initBasic();
            string fullPath = pathRoot != null ? pathRoot + "/" + path : path;
            doc ??= Util.openXML(fullPath);

            // I fucking hate XML.
            // And I fucking hate XML in C# even more.

            title = path;

            var map = Util.getUniqueByTagName(doc, "map");
            List<XmlElement> layers = Util.getElementsByTagName(doc, "layer");
            layers.Reverse(); // for some reason, they are stored in the opposite order to their list order in the tiled editor

            UnManagedArray2D<Tile> readLayer(XmlElement layer)
            {
                Vec2i size = new Vec2i(int.Parse(layer.GetAttribute("width")), int.Parse(layer.GetAttribute("height")));
                var data = Util.getUniqueByTagName(layer, "data");
                Util.ReleaseAssert(data.GetAttribute("encoding") == "csv");

                Tile[] tilesTemp = data.InnerText.Split(',').Select(s =>
                {
                    TileId id = new TileId(int.Parse(s.Trim()) - 1);
                    return new Tile(id, id == Constants.airTileId ? 0 : nextTileIdentity++);
                }).ToArray();
                Util.ReleaseAssert(tilesTemp.Length == size.x * size.y);

                long totalSize = sizeof(Tile) * tilesTemp.Length;
                UnManagedArray2D<Tile> layerTiles = new UnManagedArray2D<Tile>(size.x, size.y);
                fixed (Tile* tempPtr = tilesTemp)
                    NativeFuncs.memcpy((IntPtr)layerTiles.data, (IntPtr)tempPtr, totalSize);

                return layerTiles;
            }

            tiles = readLayer(layers[0]);
            tileTempState = new UnManagedArray2D<TileTempState>(tiles.width, tiles.height);
            tileTempState.zeroMemory();

            dimensions = new Vec2i(tiles.width, tiles.height);

            background = new List<UnManagedArray2D<Tile>>();
            for (int i = 1; i < layers.Count; i++)
            {
                UnManagedArray2D<Tile> layer = readLayer(layers[i]);
                Util.ReleaseAssert(layer.width == tiles.width && layer.height == tiles.height);
                background.Add(layer);
            }

            int lootTilesFound = 0;
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int x = 0; x < dimensions.x; x++)
                {
                    if (get(x, y)->tileId == Constants.playerSpawnId)
                    {
                        playerPosition = new Vec2i(x, y);
                        set(x, y, new Tile(Constants.airTileId, 0));
                    }

                    if (get(x, y)->tileId == Constants.lootTileId)
                        lootTilesFound++;

                    // empty tiles in tiled will end up as -1, we just convert them to air
                    if (get(x, y)->tileId == -1)
                        set(x, y, new Tile(Constants.airTileId, 0));

                    if (get(x, y)->tileId == Constants.diamondIds[0] ||
                        get(x, y)->tileId == Constants.diamondIds[1] ||
                        get(x, y)->tileId == Constants.diamondIds[2] ||
                        get(x, y)->tileId == Constants.diamondIds[3])
                    {
                        get(x, y)->overrideRenderId = get(x, y)->tileId.val;
                        get(x, y)->tileId.val = Constants.diamondIds[0];
                    }
                }
            }

            maxLoot = lootTilesFound;
            fixupSegments();

            var properties = Util.getUniqueByTagName(map, "properties");
            if (properties != null)
            {
                foreach (XmlElement child in properties.ChildNodes)
                {
                    Util.ReleaseAssert(child.Name == "property");

                    string name = child.GetAttribute("name");
                    string value = child.GetAttribute("value");

                    if (name == "maxLoot")
                    {
                        maxLoot = int.Parse(value);
                    }
                    if (name == "maxDiamonds")
                    {
                        maxDiamonds = int.Parse(value);
                    }
                    if (name == "currentLoot")
                    {
                        currentLoot = int.Parse(value);
                    }
                    if (name == "currentDiamonds")
                    {
                        currentDiamonds = int.Parse(value);
                    }
                    else if (name == "currentBombs")
                    {
                        currentBombs = int.Parse(value);
                    }
                    else if (name == "win")
                    {
                        win = value == "true";
                    }
                    else if (name == "dead")
                    {
                        dead = value == "true";
                    }
                    else if (name == "playerX")
                    {
                        playerPosition.x = int.Parse(value);
                    }
                    else if (name == "playerY")
                    {
                        playerPosition.y = int.Parse(value);
                    }
                    else if (name == "nextBombId")
                    {
                        nextBombId = int.Parse(value);
                    }
                    else if (name.StartsWith("tile_"))
                    {
                        string[] split = name.Split('_');
                        Util.ReleaseAssert(split.Length == 4);
                        Vec2i p = new Vec2i(int.Parse(split[2]), int.Parse(split[3]));

                        if (name.StartsWith("tile_segmentId_"))
                        {
                            int segmentId = int.Parse(value);
                            get(p)->segmentId = segmentId;

                            if (segmentId < Constants.dirtSegmentIdsStart && segmentId + 1 > nextSegmentId)
                                nextSegmentId = segmentId + 1;
                        }
                        else if (name.StartsWith("tile_bombId_"))
                            get(p)->bombId = int.Parse(value);
                        else if (name.StartsWith("tile_overrideRenderId_"))
                            get(p)->overrideRenderId = int.Parse(value);
                        else
                            Util.ReleaseAssert(false);
                    }
                    else if (name == "prompt")
                    {
                        prompt = value;
                    }
                    else if (name == "title")
                    {
                        title = value;
                    }
                }
            }
        }

        private void fixupSegments()
        {
            var segments = GameLogic.calculateSegments(this);
            foreach (var segment in segments)
            {
                if (segment.tileId == Constants.dirtTileId)
                {
                    foreach (Vec2i p in segment)
                        get(p)->segmentId = Constants.dirtSegmentIdsStart;
                }
                else if (Constants.rockTileIdSet.Contains(segment.tileId.val))
                {
                    // Don't allow rock segments to merge when they touch
                    foreach (Vec2i p in segment)
                        get(p)->segmentId = nextSegmentId;

                    nextSegmentId++;
                }
                else if (segment.tileId == Constants.diamondIds[0])
                {
                    maxDiamonds++;
                }
            }

            // Don't allow loot tiles to merge into segments
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int x = 0; x < dimensions.x; x++)
                {
                    Tile* tile = get(x, y);
                    if (tile->tileId == Constants.lootTileId)
                    {
                        tile->segmentId = nextSegmentId;
                        nextSegmentId++;
                    }
                }
            }
        }

        private Tilemap() { initBasic(); }

        public Tile* get(int x, int y) { return tiles.get(x, y); }
        public Tile* get(Vec2i p) { return tiles.get(p); }

        public Tile* get(int x, int y, int layer)
        {
            if (layer == 0)
                return get(x, y);

            return background[layer - 1].get(x, y);
        }

        public Tile* get(Vec2i p, int layer)
        {
            if (layer == 0)
                return get(p);

            return background[layer - 1].get(p);
        }

        public int backgroundLayerCount => background.Count;

        public void set(int x, int y, Tile tile)
        {
            Util.DebugAssert(tile.tileIdentity != 0 || tile.tileId == Constants.airTileId || tile.tileId == Constants.deletedPlaceholderTile);
            *tiles.get(x, y) = tile;
        }
        public void set(Vec2i p, Tile tile) { set(p.x, p.y, tile); }

        public bool isPointValid(int x, int y) { return tiles.isPointValid(x, y); }
        public bool isPointValid(Vec2i p) { return tiles.isPointValid(p); }

        public object Clone()
        {
            return clone();
        }

        public string save(Dictionary<string, object> extraValues = null)
        {
            Dictionary<string, object> properties;
            if (extraValues != null)
                properties = new Dictionary<string, object>(extraValues);
            else
                properties = new Dictionary<string, object>();

            properties["maxLoot"] = maxLoot;
            properties["maxDiamonds"] = maxDiamonds;
            properties["currentLoot"] = currentLoot;
            properties["currentDiamonds"] = currentDiamonds;
            properties["currentBombs"] = currentBombs;
            properties["win"] = win;
            properties["dead"] = dead;
            properties["playerX"] = playerPosition.x;
            properties["playerY"] = playerPosition.y;
            properties["originalPath"] = path;
            properties["nextBombId"] = nextBombId;
            properties["title"] = title;
            if (prompt != null)
                properties["prompt"] = prompt;

            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            sb.Append("<map version=\"1.5\" tiledversion=\"1.7.2\" orientation=\"orthogonal\" renderorder=\"right-down\" width=\"" + dimensions.x +
                      "\" height=\"" + dimensions.y + "\" tilewidth=\"16\" tileheight=\"16\" infinite=\"0\" nextlayerid=\"2\" nextobjectid=\"1\">\n");

            sb.Append("<tileset firstgid=\"1\" source=\"../../levels/tileset.tsx\"/>\n");
            sb.Append("<layer id=\"1\" name=\"Tile Layer 1\" width=\"" + dimensions.x + "\" height=\"" + dimensions.y + "\">");
            sb.Append("<data encoding=\"csv\">");

            for (int y = 0; y < dimensions.y; y++)
            {
                for (int x = 0; x < dimensions.x; x++)
                {
                    Tile* tile = get(x, y);

                    properties["tile_segmentId_" + x + "_" + y] = tile->segmentId;
                    if (tile->bombId != 0)
                        properties["tile_bombId_" + x + "_" + y] = tile->bombId;
                    if (tile->overrideRenderId != 0)
                        properties["tile_overrideRenderId_" + x + "_" + y] = tile->overrideRenderId;

                    sb.Append(tile->tileId.val + 1);
                    if (x != dimensions.x - 1 || y != dimensions.y - 1)
                        sb.Append(',');
                }
                sb.Append('\n');
            }

            sb.Append("</data>\n</layer>\n");

            sb.Append("<properties>\n");
            foreach (var pair in properties)
            {
                string typeName = "";
                string value = "";
                if (pair.Value is string)
                {
                    typeName = "string";
                    value = (string)pair.Value;
                }
                else if (pair.Value is bool)
                {
                    typeName = "bool";
                    value = ((bool)pair.Value) ? "true" : "false";
                }
                else if (pair.Value is int)
                {
                    typeName = "int";
                    value = "" + pair.Value;
                }
                else
                {
                    Util.ReleaseAssert(false);
                }

                sb.Append("  <property name=\"" + pair.Key +"\" type=\"" + typeName + "\" value=\"" + value + "\"/>\n");
            }
            sb.Append("</properties>\n");

            sb.Append("</map>");

            return sb.ToString();
        }

        public bool Equals(Tilemap other)
        {
            if (other == null ||
                dimensions != other.dimensions ||
                playerPosition != other.playerPosition ||
                maxLoot != other.maxLoot ||
                currentLoot != other.currentLoot ||
                currentBombs != other.currentBombs ||
                win != other.win ||
                dead != other.dead ||
                nextBombId != other.nextBombId)
            {
                return false;
            }

            for (int y = 0; y < dimensions.y; y++)
            {
                for (int x = 0; x < dimensions.x; x++)
                {
                    if (!get(x, y)->Equals(*other.get(x, y)))
                        return false;
                }
            }

            return true;
        }

        public Tilemap clone()
        {
            UnManagedArray2D<TileTempState> newTempState = new UnManagedArray2D<TileTempState>(tiles.width, tiles.height);
            newTempState.zeroMemory();

            return new Tilemap
            {
                pathRoot = pathRoot,
                path = path,
                title = title,
                tiles = (UnManagedArray2D<Tile>)tiles.Clone(),
                background = background, // NOT a copy, just referencing the original
                dimensions = dimensions,
                playerPosition = playerPosition,
                maxLoot = maxLoot,
                maxDiamonds = maxDiamonds,
                currentLoot = currentLoot,
                currentDiamonds = currentDiamonds,
                currentBombs = currentBombs,
                currentMegadrills = currentMegadrills,
                win = win,
                dead = dead,
                nextBombId = nextBombId,
                nextTileIdentity = nextTileIdentity,
                nextSegmentId = nextSegmentId,
                tileTempState = newTempState,
                prompt = prompt,
            };
        }
    }

    public enum GameAction
    {
        Up,
        Down,
        Left,
        Right,
        BombDrop,
        BombTrigger,
        MegadrillDrop,
        Reset,
    }

    public class GameState
    {
        public Tilemap originalLevel;
        public List<GameAction> moves = new List<GameAction>();
    }

    public struct Grip
    {
        public bool left;
        public bool right;
        public bool belowLeft;
        public bool belowRight;
        public bool onSolidGround;

        public bool beside => left || right;
        public bool below => belowLeft || belowRight;
        public bool any => left || right || belowLeft || belowRight || onSolidGround;
    }
}