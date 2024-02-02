using System;
using System.Linq;
using System.Xml;
using Drilbert;

namespace Tests
{
    static unsafe class TestsMain
    {
        static void Main(string[] args)
        {
            GameLogic.printUpdateTiming = false;

            foreach (string map in new string[]
                     {
                        "BasicDropOutput",
                        "DropOnPlayerOutput",
                        "PlayerDropWithDirtOutput",
                        "PushPlayerDownOutput",
                        "DirtMergingOutput",
                        "DropOnPlayerDigDownOutput",
                        "DigOverTrenchDropOutput",
                        "CrushDirtAgainstEdgeOutput",
                        "CrushPlayerWithBombLaunchOutput",
                        "CrushDirtWithRockOutput",
                        "CrushLootWithRockOutput",
                        "FallOffMapOutput",
                        "BombPushOffMapOutput",
                        "BombPlaceholderOutput",
                        "MultiBombOutput",
                        "TriggerBombItemOutput",
                        "PushPlayerWithDirtOutput",
                        "TriggerBombImStandingOnOutput",
                        "PushCoinIntoPlayerWithBombOutput",
                        "DropCoinOnPlayerOutput",
                        "DropOntoCoinOutput",
                        "DrillCutCantMergeStoneOutput",
                        "TestMegadrillCutBasicOutput",
                        "MegadrillCutSidewaysOutput",
                        "MegadrillCutSidewaysTwoBombsOutput",
                        "MegadrillCutSidewaysDirtOutput",
                        "FirePastMegadrillOutput",
                        "PushDiamondIntoMegadrillOutput",
                        "DropDiamondOnMegadrillOutput",
                        "PlaceholderFromDiamondOutput",
                        "PlaceholderFromDiamondOutput1",
                        "CrushedDeathPositionOutput",
                     })
            {
                runTestMap(map);
            }

            testRealLevelSolutions();

            Console.WriteLine("All tests passed!");
        }

        static void normaliseTilemap(Tilemap state)
        {
            var segments = GameLogic.calculateSegments(state);
            for (int i = 0; i < segments.Count; i++)
            {
                foreach (Vec2i p in segments[i])
                    state.get(p)->segmentId = i;
            }

            state.nextTileIdentity = 1;
            for (int y = 0; y < state.dimensions.y; y++)
            {
                for (int x = 0; x < state.dimensions.x; x++)
                {
                    Tile* tile = state.get(x, y);
                    if (tile->tileId != Constants.airTileId)
                        tile->tileIdentity = state.nextTileIdentity++;
                }
            }
        }

        static void runTestMap(string name)
        {
            string path = Constants.rootPath + "/src/Tests/TestLevels/" + name + ".tmx";
            Console.Write(path + ": ");
            XmlDocument expectedDoc = Util.openXML(path);
            Tilemap expected = new Tilemap(Constants.rootPath, path, expectedDoc);

            string originalPath = null;
            string movesString = null;
            XmlElement properties = Util.getUniqueByTagName(expectedDoc, "properties");
            foreach (XmlElement child in properties.ChildNodes)
            {
                if (child.GetAttribute("name") == "originalPath")
                    originalPath = child.GetAttribute("value");
                if (child.GetAttribute("name") == "moves")
                    movesString = child.GetAttribute("value");
            }

            GameState input = new GameState()
            {
                originalLevel = new Tilemap(Constants.rootPath, originalPath),
                moves = GameLogic.gameActionsFromString(movesString),
            };

            Tilemap actual = GameLogic.evaluate(input).tilemaps.Last();

            normaliseTilemap(expected);
            normaliseTilemap(actual);

            Util.ReleaseAssert(expected.Equals(actual));
            Console.WriteLine("PASS");
        }

        static void testRealLevelSolutions()
        {
            foreach (Tilemap level in Levels.allLevels)
            {
                if (level.path.EndsWith("winner.tmx") || level.path.StartsWith("Tests"))
                    continue;

                Console.Write(level.path + ": ");

                string solutionString = null;
                {
                    XmlDocument levelXml = Util.openXML(Constants.rootPath + "/" + level.path);
                    XmlElement properties = Util.getUniqueByTagName(levelXml, "properties");
                    foreach (XmlElement child in properties.ChildNodes)
                    {
                        if (child.GetAttribute("name") == "solution")
                            solutionString = child.GetAttribute("value");
                    }
                }

                Util.ReleaseAssert(solutionString != null);

                GameState input = new GameState()
                {
                    originalLevel = level,
                    moves = GameLogic.gameActionsFromString(solutionString),
                };

                Tilemap evaluated = GameLogic.evaluate(input).tilemaps.Last();
                Util.ReleaseAssert(evaluated.win);
                Console.WriteLine("PASS");
            }
        }
    }
}