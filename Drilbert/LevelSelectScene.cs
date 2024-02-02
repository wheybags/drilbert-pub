using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert
{
    public class LevelSelectScene : Scene
    {
        public int saveSlotIndex = 0;

        RenderTarget2D mainRenderBuffer = null;
        RenderTarget2D hudRenderBuffer = null;
        RenderTarget2D menuRenderBuffer = null;

        class LevelNode
        {
            public LevelPointer leftParent;
            public LevelPointer rightParent;
            public LevelPointer aboveParent;

            public LevelPointer leftChild;
            public LevelPointer rightChild;
            public LevelPointer belowChild;

            public Tilemap level;
        }

        class LevelRun
        {
            public string name;
            public List<LevelNode> levels = new List<LevelNode>();
            public Vec2i startPosition;
            public Vec2i endPosition;
            public int completedCount = -1;

            public int pixelLengthRecursive = 0;
            public int pixelWidthRecursive = 0;

            public LevelRun(List<Tilemap> newLevels)
            {
                for (int i = 0; i < newLevels.Count; i++)
                {
                    LevelNode node = new LevelNode();
                    node.level = newLevels[i];
                    if (i != 0)
                        node.aboveParent = new LevelPointer() { run = this, index = i-1 };
                    if (i != newLevels.Count - 1)
                        node.belowChild = new LevelPointer() { run = this, index = i+1 };
                    levels.Add(node);
                }
            }

            public LevelPointer first => new LevelPointer(){ run = this, index = 0};
            public LevelPointer last => new LevelPointer(){ run = this, index = levels.Count-1};
        }

        private LevelRun[] levelRuns = null;

        struct LevelPointer
        {
            public LevelRun run;
            public int index;

            public LevelNode get()
            {
                return run.levels[index];
            }

            public static bool operator==(LevelPointer a, LevelPointer b) { return a.run == b.run && a.index == b.index; }
            public static bool operator!=(LevelPointer a, LevelPointer b) { return !(a == b); }
            public bool Equals(LevelPointer other) { return Equals(run, other.run) && index == other.index; }
            public override bool Equals(object obj) { return obj is LevelPointer other && Equals(other); }
            public override int GetHashCode() { return HashCode.Combine(run, index); }
        }

        LevelPointer selection = new LevelPointer();
        LevelPointer drilbertPostion = new LevelPointer();

#if DEMO
        bool bannerIsSelected = false;
#endif

        float currentScrollOffset = 0;
        long lastScrollOffsetUpdate = 0;

        // This is very spaghetti. Maybe some day I'll clean it up
        public LevelSelectScene()
        {
            levelRuns = new LevelRun[Levels.allSections.Count];
            for (int i = 0; i < Levels.allSections.Count; i++)
                levelRuns[i] = new LevelRun(Levels.allSections[i]) { name = Levels.allSections[i].name };

            LevelRun get(LevelSection section)
            {
                if (section == null)
                    return null;

                for (int i = 0; i < Levels.allSections.Count; i++)
                {
                    if (Levels.allSections[i] == section)
                        return levelRuns[i];
                }

                Util.ReleaseAssert(false);
                return null;
            }

            foreach (LevelSection section in Levels.allSections)
            {
                LevelRun thisRun = get(section);
                LevelRun leftParent = get(section.leftParent);
                LevelRun rightParent = get(section.rightParent);

                if (leftParent != null)
                {
                    thisRun.first.get().leftParent = leftParent.last;
                    leftParent.last.get().rightChild = thisRun.first;
                }

                if (rightParent != null)
                {
                    thisRun.first.get().rightParent = rightParent.last;
                    rightParent.last.get().leftChild = thisRun.first;
                }
            }

            foreach (LevelRun run in levelRuns)
            {
                if (run.first.get().leftParent.run != null && run.first.get().rightParent.run == null)
                    run.first.get().aboveParent = run.first.get().leftParent;

                if (run.first.get().leftParent.run == null && run.first.get().rightParent.run != null)
                    run.first.get().aboveParent = run.first.get().rightParent;

                if (run.last.get().leftChild.run != null && run.last.get().rightChild.run == run)
                    run.last.get().belowChild = run.last.get().leftChild;

                if (run.last.get().leftChild.run == null && run.last.get().rightChild.run != run)
                    run.last.get().belowChild = run.last.get().rightChild;
            }

            int levelSpace = 48;
            int horizontalOffset = 64;
            int verticalOffset = 32;

            {
                void calculateRecursiveLengths(LevelRun run)
                {
                    int childrenLength = 0;

                    if (run.last.get().leftChild.run != null)
                    {
                        calculateRecursiveLengths(run.last.get().leftChild.run);
                        childrenLength = Math.Max(childrenLength, run.last.get().leftChild.run.pixelLengthRecursive);
                    }

                    if (run.last.get().rightChild.run != null)
                    {
                        calculateRecursiveLengths(run.last.get().rightChild.run);
                        childrenLength = Math.Max(childrenLength, run.last.get().rightChild.run.pixelLengthRecursive);
                    }

                    if (run.last.get().leftChild.run != null)
                        run.last.get().leftChild.run.pixelLengthRecursive = childrenLength;
                    if (run.last.get().rightChild.run != null)
                        run.last.get().rightChild.run.pixelLengthRecursive = childrenLength;

                    run.pixelLengthRecursive = levelSpace * run.levels.Count + childrenLength;


                    run.pixelWidthRecursive = 0;

                    if (run.last.get().leftChild.run != null)
                        run.pixelWidthRecursive += run.last.get().leftChild.run.pixelWidthRecursive;
                    if (run.last.get().rightChild.run != null)
                        run.pixelWidthRecursive += run.last.get().rightChild.run.pixelWidthRecursive;

                    if (run.pixelWidthRecursive == 0)
                        run.pixelWidthRecursive = horizontalOffset;

                    run.pixelLengthRecursive += verticalOffset;
                }

                calculateRecursiveLengths(levelRuns[0]);

                void assignPositionsRecursive(LevelRun run, Vec2i pos)
                {
                    run.startPosition = pos;

                    int childrenLength = 0;
                    if (run.last.get().leftChild.run != null)
                        childrenLength = run.last.get().leftChild.run.pixelLengthRecursive;
                    if (run.last.get().rightChild.run != null)
                        childrenLength = run.last.get().rightChild.run.pixelLengthRecursive;

                    run.endPosition = run.startPosition + new Vec2i(0, run.pixelLengthRecursive - childrenLength - verticalOffset);

                    int xoff = horizontalOffset;

                    if (run.last.get().leftChild.run != null)
                        assignPositionsRecursive(run.last.get().leftChild.run, run.endPosition + new Vec2i(-xoff, verticalOffset));

                    if (run.last.get().rightChild.run != null)
                        assignPositionsRecursive(run.last.get().rightChild.run, run.endPosition + new Vec2i(xoff, verticalOffset));
                }

                assignPositionsRecursive(levelRuns[0], new Vec2i((Constants.levelWidth * Constants.tileSize)/2, 130));
            }

            selection.run = levelRuns[0];
            selection.index = 0;

            drilbertPostion = selection;

            levelRuns[0].completedCount = 0;

            mainRenderBuffer = new RenderTarget2D(Game1.game.GraphicsDevice, Constants.levelWidth * Constants.tileSize, Constants.levelHeight * Constants.tileSize);
            hudRenderBuffer = new RenderTarget2D(Game1.game.GraphicsDevice, mainRenderBuffer.Width, Constants.tileSize);
            menuRenderBuffer = new RenderTarget2D(Game1.game.GraphicsDevice, Constants.tileSize * 32, Constants.tileSize * 32);
        }

        public bool hasSeenBomb()
        {
            if (Modding.currentMod != null)
                return true;

            foreach (LevelRun run in levelRuns)
            {
                if (run.name == "bomb")
                    return run.completedCount > 0;
            }

            Util.ReleaseAssert(false);
            return false;
        }

        public bool hasSeenMegadrill()
        {
            if (Modding.currentMod != null)
                return true;

            foreach (LevelRun run in levelRuns)
            {
                if (run.name == "megadrill")
                    return run.completedCount > 0;
            }

            Util.ReleaseAssert(false);
            return false;
        }

        string getSaveGamePath()
        {
            string saveFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            if (Modding.currentMod != null)
                saveFolder = Modding.currentMod.path;
            return Path.Combine(saveFolder, "save.json");
        }

        private class SaveData
        {
            public int selectionRun { get; set; }
            public int selectionIndex { get; set; }

            public int drilbertPosRun { get; set; }
            public int drilbertPosIndex { get; set; }

            public List<int> completionCountsByRun { get; set; }
        }

        private List<SaveData> readSavegame()
        {
            string path = getSaveGamePath();

            List<SaveData> saveData = new List<SaveData>();

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch
            {
                Logger.log("Failed reading save from " + path);
                return saveData;
            }

            JsonNode saveJson;
            try { saveJson = Util.parseJson(json); } catch { return null; }

            SaveData readOneSave(JsonNode input)
            {
                SaveData data = JsonSerializer.Deserialize<SaveData>(input);

                if (data.selectionRun >= levelRuns.Length || data.selectionRun < 0)
                    return null;
                if (data.selectionIndex >= levelRuns[data.selectionRun].levels.Count || data.selectionIndex < 0)
                    return null;
                if (data.drilbertPosRun >= levelRuns.Length || data.drilbertPosRun < 0)
                    return null;
                if (data.drilbertPosIndex >= levelRuns[data.drilbertPosRun].levels.Count || data.drilbertPosIndex < 0)
                    return null;

                while (data.completionCountsByRun.Count > levelRuns.Length)
                    data.completionCountsByRun.RemoveAt(data.completionCountsByRun.Count - 1);

                for (int i = 0; i < data.completionCountsByRun.Count(); i++)
                {
                    if (data.completionCountsByRun[i] < -1 || data.completionCountsByRun[i] > levelRuns[i].levels.Count)
                        return null;
                }

                while (data.completionCountsByRun.Count < levelRuns.Length)
                    data.completionCountsByRun.Add(-1);

                return data;
            }

            if (saveJson["version"].GetValue<int>() == 1)
            {
                saveData.Add(readOneSave(saveJson));
            }
            else
            {
                foreach (JsonNode item in saveJson["slots"].AsArray())
                    saveData.Add(readOneSave(item));
            }

            return saveData;
        }

        void saveGame(List<SaveData> data)
        {
            JsonNode json = new JsonObject();
            json["version"] = 2;

            JsonArray slots = new JsonArray();
            foreach (SaveData item in data)
                slots.Add(JsonSerializer.SerializeToNode(item));

            json["slots"] = slots;

            string path = getSaveGamePath();
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(json));
            }
            catch
            {
                Logger.log("Failed to save game to " + path);
            }
        }

        void saveGame()
        {
            List<SaveData> data = readSavegame();
            while (data.Count < saveSlotIndex + 1)
                data.Add(null);

            data[saveSlotIndex] = new SaveData()
            {
                selectionRun = Array.IndexOf(levelRuns, selection.run),
                selectionIndex = selection.index,

                drilbertPosRun = Array.IndexOf(levelRuns, drilbertPostion.run),
                drilbertPosIndex = drilbertPostion.index,

                completionCountsByRun = levelRuns.Select(x => x.completedCount).ToList(),
            };

            saveGame(data);
        }

        public class SaveSlotMenuLine : MenuLine
        {
            public int index;
        }

        public Menu createSaveSlotMenu()
        {
            List<MenuLine> lines = new List<MenuLine>();
            lines.Add(new MenuLine() { message = "Select a slot", type = MenuLine.Type.Label });
            lines.Add(new MenuLine() { message = "", type = MenuLine.Type.Label });

            List<SaveData> saves = readSavegame();
            int i = 0;
            for (; i < saves.Count; i++)
                lines.Add(new SaveSlotMenuLine() { message = "Slot " + (i+1), action = MenuAction.StartGame, type = MenuLine.Type.Button, index = i });

            if (i < 3)
                lines.Add(new SaveSlotMenuLine() { message =  "New game", action = MenuAction.StartGame, type = MenuLine.Type.Button, index = i });

            lines.Add(new MenuLine() { message = "", type = MenuLine.Type.Label });

            if (saves.Count > 0)
                lines.Add(new SaveSlotMenuLine() { message =  "Delete slot", action = MenuAction.OpenDeleteSaveSlotMenu, type = MenuLine.Type.Button });

            lines.Add(new MenuLine() { message = "Close", action = MenuAction.CloseMenu, type = MenuLine.Type.Button });

            return new Menu(lines, true);
        }

        public class DeleteSaveSlotMenuLine : MenuLine
        {
            public int index;
        }

        public Menu createDeleteSaveSlotMenu()
        {
            List<MenuLine> lines = new List<MenuLine>();
            lines.Add(new MenuLine() { message = "Select a slot to delete", type = MenuLine.Type.Label });
            lines.Add(new MenuLine() { message = "", type = MenuLine.Type.Label });

            List<SaveData> saves = readSavegame();
            for (int i = 0; i < saves.Count; i++)
                lines.Add(new DeleteSaveSlotMenuLine() { message = "Slot " + (i+1), action = MenuAction.DeleteSaveSlot, type = MenuLine.Type.Button, index = i });

            if (saves.Count > 0)
                lines.Add(new MenuLine() { message = "", type = MenuLine.Type.Label });

            lines.Add(new MenuLine() { message = "Close", action = MenuAction.CloseMenu, type = MenuLine.Type.Button });

            return new Menu(lines, true);
        }

        public void deleteSaveSlot(int index)
        {
            List<SaveData> data = readSavegame();
            data.RemoveAt(index);
            saveGame(data);
        }

        public bool hasCompletedFirstLevel()
        {
            return levelRuns[0].completedCount > 0;
        }

        public void tryLoadSavedGame()
        {
            List<SaveData> data = readSavegame();
            if (saveSlotIndex >= data.Count || data[saveSlotIndex] == null)
            {
                Logger.log("Failed loading save game from json data");
                return;
            }

            SaveData save = data[saveSlotIndex];

            selection.run = levelRuns[save.selectionRun];
            selection.index = save.selectionIndex;

            drilbertPostion.run = levelRuns[save.drilbertPosRun];
            drilbertPostion.index = save.drilbertPosIndex;

            for (int i = 0; i < levelRuns.Length; i++)
                levelRuns[i].completedCount = save.completionCountsByRun[i];
        }

        public override void update(long gameTimeMs, InputHandler inputHandler)
        {
            bool down = inputHandler.input.activeWithRepeat(Input.Down, Constants.menuKeyRepeatMs);
            bool up = inputHandler.input.activeWithRepeat(Input.Up, Constants.menuKeyRepeatMs);
            bool left = inputHandler.input.activeWithRepeat(Input.Left, Constants.menuKeyRepeatMs);
            bool right = inputHandler.input.activeWithRepeat(Input.Right, Constants.menuKeyRepeatMs);


#if DEMO
            if (down && !bannerIsSelected && selection.run != levelRuns[0])
            {
                bannerIsSelected = true;
                Sounds.soundEffects[SoundId.Move].Play();
                return;
            }
            if (up && bannerIsSelected)
            {
                bannerIsSelected = false;
                Sounds.soundEffects[SoundId.Move].Play();
                return;
            }

            if (bannerIsSelected)
                return;
#endif
            LevelPointer oldSelection = selection;

            if (down)
            {
                if (selection.get().belowChild.run != null)
                    selection = selection.get().belowChild;
            }
            else if (up)
            {
                if (selection.get().aboveParent.run != null)
                    selection = selection.get().aboveParent;
            }
            else if (left)
            {
                if (selection.get().leftChild.run != null)
                    selection = selection.get().leftChild;
                else if (selection.get().leftParent.run != null)
                    selection = selection.get().leftParent;
            }
            else if (right)
            {
                if (selection.get().rightChild.run != null)
                    selection = selection.get().rightChild;
                else if (selection.get().rightParent.run != null)
                    selection = selection.get().rightParent;
            }

#if DEMO
            // null levels just exist to draw non-existent nodes in the demo, we can't select them
            if (selection.run.levels[selection.index].level == null)
                selection = oldSelection;
#endif

            if (selection != oldSelection)
                Sounds.soundEffects[SoundId.Move].Play();

            if (selection.index <= selection.run.completedCount)
                drilbertPostion = selection;
        }

        public void unlockSelectedLevelRecursive()
        {
            void getParentRunsRecursive(LevelRun run, HashSet<LevelRun> parents)
            {
                if (run.first.get().aboveParent.run != null)
                {
                    parents.Add(run.first.get().aboveParent.run);
                    getParentRunsRecursive(run.first.get().aboveParent.run, parents);
                }

                if (run.first.get().leftParent.run != null)
                {
                    parents.Add(run.first.get().leftParent.run);
                    getParentRunsRecursive(run.first.get().leftParent.run, parents);
                }

                if (run.first.get().rightParent.run != null)
                {
                    parents.Add(run.first.get().rightParent.run);
                    getParentRunsRecursive(run.first.get().rightParent.run, parents);
                }
            }

            // Set this level and all predecessors as done
            {
                selection.run.completedCount = Math.Max(selection.run.completedCount, selection.index + 1);

                HashSet<LevelRun> parents = new HashSet<LevelRun>();
                getParentRunsRecursive(selection.run, parents);

                foreach (LevelRun run in parents)
                    run.completedCount = run.levels.Count;
            }

            // Set all unavailable levels which qualify as available
            foreach (LevelRun run in levelRuns)
            {
                HashSet<LevelRun> parents = new HashSet<LevelRun>();
                getParentRunsRecursive(run, parents);

                bool allParentsComplete = true;
                foreach(LevelRun parent in parents)
                {
                    if (parent.completedCount != parent.levels.Count)
                    {
                        allParentsComplete = false;
                        break;
                    }
                }

                if (allParentsComplete)
                    run.completedCount = Math.Max(run.completedCount, 0);
            }

            if (Modding.currentMod == null)
            {
                foreach (LevelRun run in levelRuns)
                {
                    if (run.name == "basic" && run.completedCount == run.levels.Count)
                        DrilbertSteam.unlockAchievement("MINER");
                    if (run.name == "megadrill" && run.completedCount == run.levels.Count)
                        DrilbertSteam.unlockAchievement("MEGADRILL");
                    if (run.name == "bomb" && run.completedCount == run.levels.Count)
                        DrilbertSteam.unlockAchievement("BOMB");
                    if (run.name == "final" && run.completedCount == run.levels.Count)
                        DrilbertSteam.unlockAchievement("FINISH");
                }
            }


            saveGame();
        }

        public void resetProgress()
        {
            foreach(LevelRun run in levelRuns)
                run.completedCount = -1;
            levelRuns[0].completedCount = 0;
        }

        public void returnToLevelSelect(bool levelCompleted)
        {
            MusicManager.resetDynamics();

            Game1.game.setScene(this);

            if (levelCompleted)
            {
                unlockSelectedLevelRecursive();
#if !DEMO
                if (Modding.currentMod == null)
                {
                    bool finishedAll = true;
                    foreach (LevelRun run in levelRuns)
                    {
                        if (run.completedCount != run.levels.Count)
                        {
                            finishedAll = false;
                            break;
                        }
                    }

                    if (finishedAll && selection.run.name == "final" && selection.index == selection.run.levels.Count - 1)
                        Game1.game.setScene(new EndingScene());
                }
#endif
            }
        }

        public override void processInput(long gameTimeMs, InputHandler inputHandler)
        {
            if (inputHandler.input.downThisFrame(Input.Pause))
                pushMenu(Menus.levelSelectPauseMenu());

            if (inputHandler.input.downThisFrame(Input.MenuActivate))
            {
#if DEMO
                if (bannerIsSelected)
                {
                    Util.openStorePage();
                }
                else
#endif
                if (drilbertPostion.Equals(selection))
                {
                    Tilemap level = selection.run.levels[selection.index].level;
                    Logger.log("Loading level \"" + level.title + "\" from \"" + level.path + "\"");
                    Game1.game.inGameScene.gameState.originalLevel = level;
                    Game1.game.setScene(Game1.game.inGameScene);
                    Sounds.levelSelect.Play();
                }
                else
                {
                    Sounds.soundEffects[SoundId.Error].Play();
                }
            }
        }

        Vec2i calculateLevelPosition(LevelPointer pointer)
        {
            Vec2f start = pointer.run.startPosition.f();
            if (pointer.run.levels.Count < 2)
                return new Vec2i(start.rounded());

            Vec2f end = pointer.run.endPosition.f();
            float alpha = ((float)pointer.index) / ((float)pointer.run.levels.Count-1);
            return new Vec2i(start + (end - start) * alpha);
        }

        void renderLevelConnections(MySpriteBatch spriteBatch)
        {
            foreach(LevelRun run in levelRuns)
            {
                for (int i = 0; i < run.levels.Count; i++)
                {
                    LevelPointer level = new LevelPointer(){run = run, index = i};

                    foreach (LevelPointer parentLevel in new []{level.get().aboveParent, level.get().leftParent, level.get().rightParent})
                    {
                        if (parentLevel.run == null)
                            continue;

                        Vec2i parentPos = calculateLevelPosition(parentLevel);
                        Vec2i pos = calculateLevelPosition(level);

                        Vec2f connectionVector = (pos - parentPos).f();
                        float distance = connectionVector.magnitude();
                        float rotation = MathHelper.ToDegrees(MathF.Atan2(connectionVector.y, connectionVector.x));

                        spriteBatch.r(Textures.levelSelectConnection)
                                   .pos(parentPos.f() + new Vec2f(0, -currentScrollOffset - Textures.levelSelectConnection.Height / 2f))
                                   .size(new Vec2f(distance, Textures.levelSelectConnection.Height))
                                   .rotate(rotation, new Vec2f(0.0f, Textures.levelSelectConnection.Height / 2f))
                                   .draw();
                    }
                }
            }
        }

        void renderLevelNodes(MySpriteBatch spriteBatch, long gameTimeMs)
        {
            foreach(LevelRun run in levelRuns)
            {
                for (int i = 0; i < run.levels.Count; i++)
                {
                    LevelPointer level = new LevelPointer(){run = run, index = i};
                    Vec2i pos = calculateLevelPosition(level);

                    Texture2D nodeTexture = Textures.levelSelectNodeUnavailable.getCurrentFrame(gameTimeMs);
                    if (i < run.completedCount)
                        nodeTexture = Textures.levelSelectNodeDone;
                    if (i == run.completedCount)
                        nodeTexture = Textures.levelSelectNodeAvailable.getCurrentFrame(gameTimeMs);
#if DEMO
                    if (level.get().level == null)
                        nodeTexture = Textures.levelSelectNodeUnavailable.getCurrentFrame(gameTimeMs);
#endif

                    spriteBatch.r(nodeTexture)
                               .pos((pos.f() - new Vec2f(nodeTexture.Width, nodeTexture.Height) / 2f + new Vec2f(0, -currentScrollOffset)).rounded())
                               .draw();

                    if (level.Equals(drilbertPostion))
                    {
                        Texture2D tex = Textures.playerIdle.getCurrentFrame(gameTimeMs);
                        Vec2i drawPosition = new Vec2i(pos.f() - new Vec2f(tex.Width, tex.Height) / 2f + new Vec2f(0, -currentScrollOffset));
                        spriteBatch.r(tex).pos(drawPosition).draw();
                    }

                    bool renderReticle = true;
#if DEMO
                    renderReticle = !bannerIsSelected;
#endif

                    if (renderReticle && level.Equals(selection) && gameTimeMs % (Constants.levelSelectReticlePeriodMs*2) > Constants.levelSelectReticlePeriodMs)
                    {
                        Texture2D tex = Textures.levelSelectReticle;
                        Vec2i drawPosition = new Vec2i(pos.f() - new Vec2f(tex.Width, tex.Height) / 2f + new Vec2f(0, -currentScrollOffset));
                        spriteBatch.r(tex).pos(drawPosition).draw();
                    }
                }

                // spriteBatch.r(Textures.white).color(Color.Green).pos(run.startPosition.f() + new Vec2f(0, -currentScrollOffset)).size(new Vec2f(15,15)).draw();
                // spriteBatch.r(Textures.white).color(Color.Red).pos(run.endPosition.f() + new Vec2f(0, -currentScrollOffset)).size(new Vec2f(10,10)).draw();
            }
        }

        void renderLevelSelect(MySpriteBatch spriteBatch, long gameTimeMs)
        {
            GraphicsDevice.SetRenderTarget(mainRenderBuffer);
            GraphicsDevice.Clear(Color.Red);
            spriteBatch.Begin(SpriteSortMode.Deferred, Render.alphaBlend, SamplerState.PointWrap);

            // update scroll to point at the selection - smoothly
            {
                int targetScrollOffset = calculateLevelPosition(selection).y - levelRuns[0].startPosition.y;
                long delta = gameTimeMs - lastScrollOffsetUpdate;
                if (!currentScrollOffset.Equals(targetScrollOffset))
                {
                    float fDelta = ((float)delta) * 0.35f;
                    if (currentScrollOffset < targetScrollOffset)
                        currentScrollOffset = Math.Min(targetScrollOffset, currentScrollOffset + fDelta);
                    else
                        currentScrollOffset = Math.Max(targetScrollOffset, currentScrollOffset - fDelta);
                }
                lastScrollOffsetUpdate = gameTimeMs;
            }

            long cloudOffset = (gameTimeMs / 250 % Textures.levelSelectClouds.Width);

            spriteBatch.r(Textures.levelSelectClouds).pos(0,-currentScrollOffset).offsetUv(new Vec2f(-cloudOffset, 0)).draw();
            spriteBatch.r(Textures.levelSelect).pos(0,-currentScrollOffset).draw();
            renderLevelConnections(spriteBatch);
            renderLevelNodes(spriteBatch, gameTimeMs);

#if DEMO
            long overlayTime = 0;
            if (bannerIsSelected)
                overlayTime = gameTimeMs;
            spriteBatch.r(Textures.demoOverlay.getCurrentFrame(overlayTime)).pos(0,-currentScrollOffset).draw();
#endif

            spriteBatch.End();

            Render.renderEdgeDither(spriteBatch, 0, new Vec2i(Constants.levelWidth, Constants.levelHeight));
        }

        public override void draw(MySpriteBatch spriteBatch, InputHandler inputHandler, long gameTimeMs)
        {
            renderLevelSelect(spriteBatch, gameTimeMs);

            Rect? menuArea = null;
            if (getCurrentMenu() != null)
            {
                GraphicsDevice.SetRenderTarget(menuRenderBuffer);
                menuArea = Render.renderMenu(spriteBatch, gameTimeMs, getCurrentMenu());
            }

            // render HUD
            Rect hudArea;
            {
                GraphicsDevice.SetRenderTarget(hudRenderBuffer);
                GraphicsDevice.Clear(Constants.outsideLevelBackgroundColor);

                spriteBatch.Begin(SpriteSortMode.Deferred, Render.alphaBlend);
                {
                    string hudText = selection.run.levels[selection.index].level.title;
                    if (selection.index > selection.run.completedCount)
                        hudText = "???";

                    if (Modding.currentMod != null)
                        hudText = Modding.currentMod.title + " - " + hudText;


                    int textWidth = Textures.drilbertFont.measureText(hudText);

                    int tilesRequired;
                    {
                        float temp = (float) textWidth / (float) Constants.tileSize;
                        temp += 0.5f * 2; // allow a horizontal margin
                        tilesRequired = (int)MathF.Ceiling(temp);
                    }

                    int widthUsed = tilesRequired * Constants.tileSize;

                    Render.renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.hudBorderLeftId), new Vec2i(0, 0));
                    for (int x = 1; x < tilesRequired - 1; x++)
                        Render.renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.hudBorderCentreId), new Vec2i(x, 0));
                    Render.renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.hudBorderRightId), new Vec2i(tilesRequired - 1, 0));

                    float textX = ((float)widthUsed / 2.0f) - ((float) textWidth / 2.0f);
                    float textY = ((float)Constants.tileSize / 2.0f) - ((float)Textures.drilbertFont.lineHeight / 2.0f);
                    Textures.drilbertFont.draw(hudText, new Vec2i((int)MathF.Round(textX), (int)MathF.Round(textY)), spriteBatch);

                    hudArea = new Rect(0, 0, widthUsed, hudRenderBuffer.Height);
                }
                spriteBatch.End();
            }

            // using (var f = File.OpenWrite("C:\\users\\wheybags\\desktop\\asd.png"))
            //     hudRenderBuffer.SaveAsPng(f, hudRenderBuffer.Width, hudRenderBuffer.Height);


            // Scale + draw the low resolution view textures to the screen
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Constants.outsideLevelBackgroundColor);
            spriteBatch.Begin(SpriteSortMode.Deferred, Render.alphaBlend, SamplerState.PointWrap);
            {
                float renderScale;
                {
                    float xScale = ((float) Window.ClientBounds.Width) / (mainRenderBuffer.Width + Constants.tileSize * 1);
                    float yScale = ((float) Window.ClientBounds.Height) / (mainRenderBuffer.Height + Constants.tileSize * 1);
                    renderScale = (int) Math.Max(1.0f, Math.Floor(Math.Min(xScale, yScale)));
                }

                // level select view
                {
                    Vec2f targetSize = mainRenderBuffer.Bounds.f().size * renderScale;

                    // centre on screen
                    Vec2f pos = new Vec2f(
                        Window.ClientBounds.f().w/2 - targetSize.x/2,
                        Window.ClientBounds.f().h/2 - targetSize.y/2
                    ).rounded();
                    spriteBatch.r(mainRenderBuffer).pos(pos).size(targetSize).draw();
                }

                // top hud
                {
                    Vec2f targetSize = hudArea.size * renderScale;
                    Vec2f pos = new Vec2f(Window.ClientBounds.Width/2f - targetSize.x/2f, 0).rounded();
                    spriteBatch.r(hudRenderBuffer).pos(pos).size(targetSize).uv(hudArea).draw();
                }

                // menu
                if (getCurrentMenu() != null)
                {
                    float menuScale = MathF.Max(1.0f, MathF.Floor(((float)Window.ClientBounds.Width) / (24 * Constants.tileSize)));
                    Vec2f targetSize = menuArea.Value.size * menuScale;
                    Vec2f pos = (Window.ClientBounds.f().size/2 - targetSize/2).rounded();
                    spriteBatch.r(menuRenderBuffer).size(targetSize).pos(pos).uv(menuArea.Value).draw();
                }
            }
            spriteBatch.End();
        }
    }
}