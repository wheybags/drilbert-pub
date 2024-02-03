using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Drilbert
{

    public class EvaluationResult
    {
        public List<Tilemap> tilemaps;
    }

    public static unsafe class GameLogic
    {
        public static bool printUpdateTiming = true;
        private static string cachedLevelPath = null;

        // Is it ok to store so many cache entries?
        // Well, let's do some quick maths. How long would it take to use half a gig?
        // 448 iterations = 8916 kib (measured primitively by looking at task manager while moving back and forth)
        //   -> ~20 kib / it
        //   -> 512 mib = 26214 its
        //   -> ~7 hrs to use 512 mib @ 1 it/s
        // I think we're fine.
        public static readonly Dictionary<string, EvaluationResult> evaluationCache = new Dictionary<string, EvaluationResult>();

        public static EvaluationResult evaluate(GameState state)
        {
            return evaluate(state.originalLevel, new MySlice<GameAction>(state.moves));
        }

        public static MySlice<GameAction> trimGameActionsToLastRunAfterReset(MySlice<GameAction> actions)
        {
            for (int i = actions.length - 1; i >= 0; i--)
            {
                if (actions[i] == GameAction.Reset)
                {
                    int actionsEnd = actions.startIndex + actions.length;
                    actions.startIndex = i + 1;
                    actions.length = actionsEnd - actions.startIndex;
                    break;
                }
            }

            return actions;
        }

        public static EvaluationResult evaluate(Tilemap state, MySlice<GameAction> actions, bool cacheOnly = false)
        {
            actions = trimGameActionsToLastRunAfterReset(actions);

            if (cachedLevelPath != state.path)
            {
                cachedLevelPath = state.path;
                lock(evaluationCache)
                    evaluationCache.Clear();
            }
            return evaluateRecursive(actions, state, cacheOnly);
        }

        public static bool tryAddMove(GameState state, GameAction action)
        {
            if (evaluate(state).tilemaps.Last().dead)
                return false;

            state.moves.Add(action);
            if (evaluate(state) == null)
            {
                state.moves.RemoveAt(state.moves.Count - 1);
                return false;
            }

            return true;
        }

        private static readonly bool enableUndoReset = true;

        public static bool tryUndo(GameState state)
        {
            if (state.moves.Count == 0)
                return false;

            if (!enableUndoReset)
            {
                // This is useful when you want to repeat an action for debugging, but incompatible with enabling undo reset
                lock(evaluationCache)
                    evaluationCache.Remove(gameActionsToString(new MySlice<GameAction>(state.moves)));
            }

            state.moves.RemoveAt(state.moves.Count - 1);
            return true;
        }

        public static void reset(GameState state)
        {
            if (enableUndoReset)
            {
                if (state.moves.Count > 0 && state.moves[state.moves.Count - 1] != GameAction.Reset)
                    state.moves.Add(GameAction.Reset);
            }
            else
            {
                lock(evaluationCache)
                    evaluationCache.Clear();
                state.moves.Clear();
            }
        }

        public static string gameActionsToString(MySlice<GameAction> actions)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (GameAction move in actions)
            {
                switch (move)
                {
                    case GameAction.Right:
                        stringBuilder.Append('R');
                        break;
                    case GameAction.Left:
                        stringBuilder.Append('L');
                        break;
                    case GameAction.Down:
                        stringBuilder.Append('D');
                        break;
                    case GameAction.Up:
                        stringBuilder.Append('U');
                        break;
                    case GameAction.BombDrop:
                        stringBuilder.Append('B');
                        break;
                    case GameAction.BombTrigger:
                        stringBuilder.Append('T');
                        break;
                    case GameAction.MegadrillDrop:
                        stringBuilder.Append('M');
                        break;
                    case GameAction.Reset:
                        Util.ReleaseAssert(false);
                        break;
                }
            }

            return stringBuilder.ToString();
        }

        public static List<GameAction> gameActionsFromString(string str)
        {
            List<GameAction> gameActions = new List<GameAction>();
            foreach (char c in str)
            {
                switch (c)
                {
                    case 'R':
                        gameActions.Add(GameAction.Right);
                        break;
                    case 'L':
                        gameActions.Add(GameAction.Left);
                        break;
                    case 'D':
                        gameActions.Add(GameAction.Down);
                        break;
                    case 'U':
                        gameActions.Add(GameAction.Up);
                        break;
                    case 'B':
                        gameActions.Add(GameAction.BombDrop);
                        break;
                    case 'T':
                        gameActions.Add(GameAction.BombTrigger);
                        break;
                    case 'M':
                        gameActions.Add(GameAction.MegadrillDrop);
                        break;
                    default:
                        Util.ReleaseAssert(false);
                        break;
                }
            }

            return gameActions;
        }

        static EvaluationResult evaluateRecursive(MySlice<GameAction> actions, Tilemap input, bool cacheOnly = false)
        {
            if (actions.length == 0)
                return new EvaluationResult{ tilemaps = new List<Tilemap>{input} };

            string cacheKey = gameActionsToString(actions);

            lock (evaluationCache)
            {
                if (evaluationCache.TryGetValue(cacheKey, out EvaluationResult cached))
                    return cached;
                if (cacheOnly)
                    return null;
            }

            Tilemap tilemap = evaluateRecursive(new MySlice<GameAction>(actions, 0, actions.length - 1), input).tilemaps.Last();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            EvaluationResult updated = update(tilemap, actions[actions.length - 1]);
            stopwatch.Stop();

            if (printUpdateTiming)
                Logger.log("Update took " + stopwatch.ElapsedMilliseconds + "ms");

            if (updated != null)
            {
                lock(evaluationCache)
                    evaluationCache[cacheKey] = updated;
            }
            return updated;
        }

        static Direction gameActionToDirection(GameAction action)
        {
            switch (action)
            {
                case GameAction.Right:
                    return Direction.Right;
                case GameAction.Left:
                    return Direction.Left;
                case GameAction.Down:
                    return Direction.Down;
                case GameAction.Up:
                    return Direction.Up;
            }

            return Direction.None;
        }

        public static Vec2i directionToVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return new Vec2i(0, -1);
                case Direction.Down:
                    return new Vec2i(0, 1);
                case Direction.Left:
                    return new Vec2i(-1, 0);
                case Direction.Right:
                    return new Vec2i(1, 0);
            }

            return new Vec2i(0, 0);
        }

        public static bool checkMoveDisallowJump(Tilemap state, Grip grip, Direction moveDirection, TileId targetTileId)
        {
           bool standingOnBomb = state.get(state.playerPosition)->tileId == Constants.bombTileId;

            // don't allow jumping (up, or sideways off a wall hang)
            if ((moveDirection == Direction.Left && targetTileId != Constants.dirtTileId && !grip.belowLeft && !grip.onSolidGround) ||
                (moveDirection == Direction.Right && targetTileId != Constants.dirtTileId && !grip.belowRight && !grip.onSolidGround) ||
                (moveDirection == Direction.Up && !grip.beside && !standingOnBomb))
                return false;

            return true;
        }

        static EvaluationResult update(Tilemap state, GameAction action)
        {
            if (state.dead || state.win)
                return null;

            EvaluationResult newState = new EvaluationResult {tilemaps = new List<Tilemap>{state.clone()}};
            state = null;

            Grip grip = getGrip(newState.tilemaps.Last());

            // Bypass movement mechanic allows us to fall by pressing left or right, in the case that we are suspended, gripless in the air.
            // This can happen after we collect a diamond by walking into one of its upper two tiles.
            bool bypassMovement = !grip.any;

            if (!bypassMovement)
            {
                Direction moveDirection = gameActionToDirection(action);

                Vec2i move = directionToVector(moveDirection);

                Vec2i newPosition = newState.tilemaps.Last().playerPosition + move;

                if (!newState.tilemaps.Last().isPointValid(newPosition))
                    return null;

                TileId targetTileId = newState.tilemaps.Last().get(newPosition)->tileId;

                if (!checkMoveDisallowJump(newState.tilemaps.Last(), grip, moveDirection, targetTileId))
                    return null;

                newState.tilemaps.Last().playerPosition = newPosition;

                if (action == GameAction.Up || action == GameAction.Down || action == GameAction.Left || action == GameAction.Right)
                {
                    if (targetTileId == Constants.diamondIds[0])
                    {
                        newState.tilemaps.Last().soundEffects.Add(SoundId.Move);
                        newState.tilemaps.Last().soundEffects.Add(SoundId.Diamond);

                        List<Segment> segments = calculateSegments(newState.tilemaps.Last());

                        Segment diamondSegment = null;
                        foreach (Segment segment in segments)
                        {
                            foreach (Vec2i point in segment)
                            {
                                if (newPosition == point)
                                {
                                    diamondSegment = segment;
                                    goto diamond_search_done;
                                }
                            }
                        }

                        diamond_search_done:

                        Util.ReleaseAssert(diamondSegment != null);
                        foreach (Vec2i point in diamondSegment)
                            newState.tilemaps.Last().set(point, new Tile(Constants.deletedPlaceholderTile, 0));

                        newState.tilemaps.Last().currentDiamonds++;
                    }
                    else
                    {
                        if (!tileIsSolid(targetTileId))
                            newState.tilemaps.Last().soundEffects.Add(SoundId.Move);

                        // Digging
                        if (tileIsSolid(targetTileId))
                        {
                            if (targetTileId == Constants.dirtTileId)
                            {
                                newState.tilemaps.Last().soundEffects.Add(SoundId.Drill);
                                int newTileId = Constants.airTileId;

                                bool diggingIntoSectionThatIsAboutToFall = true;
                                {
                                    List<Segment> segments = calculateSegments(newState.tilemaps.Last());
                                    HashSet<int> cantFall = calculateFixedSegments(newState.tilemaps.Last(), segments, new Vec2i(0, 1));

                                    foreach (int i in cantFall)
                                    {
                                        foreach (Vec2i point in segments[i])
                                        {
                                            if (point == newState.tilemaps.Last().playerPosition)
                                            {
                                                diggingIntoSectionThatIsAboutToFall = false;
                                                goto breakCantFallOuter;
                                            }
                                        }
                                    }
                                    breakCantFallOuter: ;
                                }

                                if (!diggingIntoSectionThatIsAboutToFall)
                                {
                                    Vec2i tileAbovePlayer = newState.tilemaps.Last().playerPosition + new Vec2i(0, -1);
                                    if (newState.tilemaps.Last().isPointValid(tileAbovePlayer) && tileIsSolid(newState.tilemaps.Last().get(tileAbovePlayer)->tileId))
                                    {
                                        // This tile will be magically static for the rest of the turn
                                        // It is how we implement the "coyote time" for getting of a hole you dug undercutting a chunk of rock / dirt
                                        newTileId = Constants.deletedPlaceholderTile;
                                    }
                                }

                                newState.tilemaps.Last().set(newState.tilemaps.Last().playerPosition, new Tile(newTileId, 0));
                                newState.tilemaps.Last().digDirection = moveDirection;

                                // Insert a pause between digging sideways + falling because you dug out your grip
                                if (!getGrip(newState.tilemaps.Last()).any)
                                {
                                    newState.tilemaps.Add(newState.tilemaps.Last().clone());
                                    newState.tilemaps.Last().set(newState.tilemaps.Last().playerPosition, new Tile(Constants.airTileId, 0));
                                }
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }

            int bombPlacedThisTurnId = 0;
            if (action == GameAction.BombDrop)
            {
                Tilemap t = newState.tilemaps.Last();
                if (t.currentBombs == 0)
                    return null;

                Tile* tilePtr = t.get(t.playerPosition);
                if (tilePtr->tileId != Constants.airTileId)
                    return null;

                tilePtr->tileId = new TileId(Constants.bombTileId);
                tilePtr->tileIdentity = t.nextTileIdentity++;
                tilePtr->bombId = t.nextBombId++;
                t.currentBombs--;

                bombPlacedThisTurnId = tilePtr->bombId;
                newState.tilemaps.Last().soundEffects.Add(SoundId.EquipmentPickup);
            }

            if (action == GameAction.MegadrillDrop)
            {
                Tilemap t = newState.tilemaps.Last();
                Tile* tilePtr = t.get(t.playerPosition);

                if (tilePtr->tileId == Constants.megadrillTileId)
                {
                    tilePtr->tileId = new TileId(Constants.airTileId);
                    tilePtr->tileIdentity = 0;
                    t.currentMegadrills++;
                }
                else
                {
                    if (t.currentMegadrills == 0)
                        return null;
                    if (tilePtr->tileId != Constants.airTileId)
                        return null;

                    tilePtr->tileId = new TileId(Constants.megadrillTileId);
                    tilePtr->tileIdentity = t.nextTileIdentity++;
                    t.currentMegadrills--;
                }

                newState.tilemaps.Last().soundEffects.Add(SoundId.EquipmentPickup);
            }

            // Handle bombs exploding
            if (action == GameAction.BombTrigger)
            {
                bool didBomb = false;

                // We need to loop our bombing because bombs can "create" new bombs, by hitting bomb items
                bool doneBombing = false;
                while (!doneBombing)
                {
                    doneBombing = true;

                    List<int> bombIds = new List<int>();
                    for (int y = 0; y < newState.tilemaps.Last().dimensions.y; y++)
                    {
                        for (int x = 0; x < newState.tilemaps.Last().dimensions.x; x++)
                        {
                            Tile* tilePtr = newState.tilemaps.Last().get(x, y);
                            if (tilePtr->tileId == Constants.bombTileId)
                                bombIds.Add(tilePtr->bombId);
                        }
                    }

                    // Make sure the bombs go off in the order they were placed
                    bombIds.Sort();
                    foreach (int bombId in bombIds)
                    {
                        for (int y = 0; y < newState.tilemaps.Last().dimensions.y; y++)
                        {
                            for (int x = 0; x < newState.tilemaps.Last().dimensions.x; x++)
                            {
                                Tile* tilePtr = newState.tilemaps.Last().get(x, y);
                                if (tilePtr->bombId == bombId)
                                {
                                    newState.tilemaps.Add(newState.tilemaps.Last().clone());
                                    newState.tilemaps.Last().shakeScreen = true;


                                    // Dirt can be crushed by explosions, but the crushing logic needs every crushable tile to be alone in its segment
                                    splitDirtSegments(newState.tilemaps.Last());

                                    doneBombing = false;
                                    didBomb = true;
                                    tryPushFromExplosion(new Vec2i(x, y), newState);
                                    newState.tilemaps.Last().set(x, y, new Tile(Constants.airTileId, 0));

                                    allowDirtToMerge(newState.tilemaps.Last());

                                    if (newState.tilemaps.Last().dead)
                                        goto BREAK_OUT_OF_ALL_BOMBS;
                                }
                            }
                        }
                    }
                }
                BREAK_OUT_OF_ALL_BOMBS: ;

                if (!didBomb)
                    return null;
            }

            if (newState.tilemaps.Last().dead)
                return newState;

            // We don't want dirt to merge segments while it's moving, so we disable it now, and then reenable when all the movement is resolved.
            // This prevents bits of dirt "sticking" to other bits of dirt that they are falling past
            forceDirtNotToMerge(newState.tilemaps.Last());

            newState.tilemaps.Add(newState.tilemaps.Last().clone());
            if (!tryDropRocks(newState) && action != GameAction.BombTrigger)
                newState.tilemaps.RemoveAt(newState.tilemaps.Count - 1);

            // Player falling logic
            while (true)
            {
                if (newState.tilemaps.Last().playerPosition.y + 1 >= newState.tilemaps.Last().dimensions.y)
                {
                    newState.tilemaps.Last().dead = true;
                    break;
                }

                if (tileIsSolid(newState.tilemaps.Last().get(newState.tilemaps.Last().playerPosition + new Vec2i(0, 1))->tileId))
                    break;

                Grip newGrip = getGrip(newState.tilemaps.Last());
                if (newGrip.beside || newGrip.below)
                    break;

                newState.tilemaps.Last().playerPosition += new Vec2i(0, 1);
            }

            {
                // Calculate which tiles are shaking because they are about to fall
                List<Segment> segments = calculateSegments(newState.tilemaps.Last());
                List<SegmentStackNode> segmentStackNodes = calculateSegmentStacks(newState.tilemaps.Last(), segments);
                for (int segmentIndex = 0; segmentIndex < segmentStackNodes.Count; segmentIndex++)
                {
                    if (isSegmentSupportedByPlaceholder(segmentStackNodes, segmentIndex))
                    {
                        foreach (Vec2i point in segments[segmentIndex])
                            newState.tilemaps.Last().tileTempState.get(point)->shaking = true;
                    }
                }

                // Cleanup placeholder tiles
                for (int y = 0; y < newState.tilemaps.Last().dimensions.y; y++)
                {
                    for (int x = 0; x < newState.tilemaps.Last().dimensions.x; x++)
                    {
                        if (newState.tilemaps.Last().get(x, y)->tileId == Constants.deletedPlaceholderTile)
                            newState.tilemaps.Last().set(x, y, new Tile(Constants.airTileId, 0));
                    }
                }
            }

            handlePlayerCollision(newState, bombPlacedThisTurnId);

            allowDirtToMerge(newState.tilemaps.Last());

            return newState;
        }

        private static bool handlePlayerCollision(EvaluationResult newState, int bombPlacedThisTurnId)
        {
            Tile* tile = newState.tilemaps.Last().get(newState.tilemaps.Last().playerPosition);
            if (tileIsSolid(tile->tileId) &&
                (tile->tileId != Constants.bombTileId || tile->bombId != bombPlacedThisTurnId) &&
                tile->tileId != Constants.deletedPlaceholderTile)
            {
                newState.tilemaps.Last().dead = true;
                return false;
            }

            if (tile->tileId == Constants.lootTileId)
            {
                newState.tilemaps.Last().removedTilesAnimationPoints.Add(tile->tileIdentity, newState.tilemaps.Last().playerPosition);
                newState.tilemaps.Last().set(newState.tilemaps.Last().playerPosition, new Tile(Constants.airTileId, 0));
                newState.tilemaps.Last().currentLoot++;
                newState.tilemaps.Last().soundEffects.Add(SoundId.Coin);
                return true;
            }

            if (tile->tileId == Constants.bombItemTileId)
            {
                newState.tilemaps.Last().removedTilesAnimationPoints.Add(tile->tileIdentity, newState.tilemaps.Last().playerPosition);
                newState.tilemaps.Last().set(newState.tilemaps.Last().playerPosition, new Tile(Constants.airTileId, 0));
                newState.tilemaps.Last().currentBombs++;
                newState.tilemaps.Last().soundEffects.Add(SoundId.EquipmentPickup);
                return true;
            }

            if (tile->tileId == Constants.megadrillItemTileId)
            {
                newState.tilemaps.Last().removedTilesAnimationPoints.Add(tile->tileIdentity, newState.tilemaps.Last().playerPosition);
                newState.tilemaps.Last().set(newState.tilemaps.Last().playerPosition, new Tile(Constants.airTileId, 0));
                newState.tilemaps.Last().currentMegadrills++;
                newState.tilemaps.Last().soundEffects.Add(SoundId.EquipmentPickup);
                return true;
            }

            if (tile->tileId == Constants.levelEndTileId &&
                newState.tilemaps.Last().currentLoot == newState.tilemaps.Last().maxLoot &&
                newState.tilemaps.Last().currentDiamonds == newState.tilemaps.Last().maxDiamonds)
            {
                newState.tilemaps.Last().win = true;
                return false;
            }

            return false;
        }

        private static void forceDirtNotToMerge(Tilemap state)
        {
            var segments = calculateSegments(state);
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].tileId == Constants.dirtTileId)
                {
                    foreach (Vec2i p in segments[i])
                        state.get(p)->segmentId = i + Constants.dirtSegmentIdsStart;
                }
            }
        }

        private static void splitDirtSegments(Tilemap state)
        {
            int segmentId = Constants.dirtSegmentIdsStart;
            for (int y = 0; y < state.dimensions.y; y++)
            {
                for (int x = 0; x < state.dimensions.x; x++)
                {
                    Tile* tile = state.get(x, y);
                    if (tile->tileId == Constants.dirtTileId)
                    {
                        tile->segmentId = segmentId;
                        segmentId++;
                    }
                }
            }
        }

        private static void allowDirtToMerge(Tilemap state)
        {
            for (int y = 0; y < state.dimensions.y; y++)
            {
                for (int x = 0; x < state.dimensions.x; x++)
                {
                    Tile* tile = state.get(x, y);
                    if (tile->tileId == Constants.dirtTileId)
                        tile->segmentId = Constants.dirtSegmentIdsStart;
                }
            }
        }

        public static bool compatible(Tile* a, Tile* b) { return a->tileId == b->tileId && a->segmentId == b->segmentId; }

        public class Segment : HashSet<Vec2i> { public TileId tileId; }
        public static List<Segment> calculateSegments(Tilemap state)
        {
            int nextId = 1;
            Dictionary<Vec2i, int> assignments = new Dictionary<Vec2i, int>();

            for (int y = 0; y < state.dimensions.y; y++)
            {
                for (int x = 0; x < state.dimensions.x; x++)
                {
                    Tile* thisTile = state.get(x, y);
                    int assigned = 0;

                    if (!tileIdIsItem(thisTile->tileId) && thisTile->tileId != Constants.bombTileId)
                    {
                        if (x > 0 && compatible(thisTile, state.get(x - 1, y)))
                        {
                            assigned = assignments[new Vec2i(x - 1, y)];
                        }

                        if (y > 0 && compatible(thisTile, state.get(x, y - 1)))
                        {
                            int newAssigned = assignments[new Vec2i(x, y - 1)];

                            if (assigned != 0 && assigned != newAssigned)
                            {
                                List<Vec2i> assignedPoints = assignments.Keys.ToList();
                                foreach (Vec2i p in assignedPoints)
                                {
                                    if (assignments[p] == assigned)
                                        assignments[p] = newAssigned;
                                }
                            }

                            assigned = newAssigned;
                        }
                    }

                    if (assigned == 0)
                    {
                        assigned = nextId;
                        nextId++;
                    }

                    assignments[new Vec2i(x, y)] = assigned;
                }
            }

            Dictionary<int, Segment> segments = new Dictionary<int, Segment>();
            foreach (var pair in assignments)
            {
                Segment segment = null;
                if (!segments.TryGetValue(pair.Value, out segment))
                {
                    segment = new Segment();
                    segment.tileId = state.get(pair.Key)->tileId;
                    segments[pair.Value] = segment;
                }

                segment.Add(pair.Key);
            }

            return segments.Values.ToList();
        }

        private static Dictionary<Vec2i, int> calculatePointToSegmentDict(List<Segment> segments)
        {
            Dictionary<Vec2i, int> lookup = new Dictionary<Vec2i, int>();
            for (int i = 0; i < segments.Count; i++)
            {
                foreach (Vec2i p in segments[i])
                    lookup[p] = i;
            }
            return lookup;
        }

        private static bool megadrillCanCut(TileId tileId)
        {
            return tileId != Constants.megadrillTileId && tileId != Constants.diamondIds[0];
        }

        static void tryPushFromExplosion(Vec2i origin, EvaluationResult state)
        {
            bool crushable(TileId tileId) => tileIdIsItem(tileId) || tileId == Constants.dirtTileId || tileId == Constants.deletedPlaceholderTile;
            int standingOnBomb = state.tilemaps.Last().get(state.tilemaps.Last().playerPosition)->bombId;

            state.tilemaps.Last().set(origin, new Tile(Constants.airTileId, 0));

            bool didMove = true;
            while (didMove)
            {
                didMove = false;

                foreach (FireDirection fireDirection in new FireDirection[] {FireDirection.Up, FireDirection.Down, FireDirection.Left, FireDirection.Right})
                {
                    Vec2i direction = directionToVector(fireDirection.toDirection());
                    List<Segment> segments = calculateSegments(state.tilemaps.Last());
                    Dictionary<Vec2i, int> segmentLookup = calculatePointToSegmentDict(segments);
                    HashSet<int> cantFall = calculateFixedSegments(state.tilemaps.Last(), segments, direction, id => id != Constants.airTileId && !crushable(id) && id != Constants.megadrillTileId );

#if DEBUG
                    foreach (Segment segment in segments)
                    {
                        if (crushable(segment.tileId))
                            Util.ReleaseAssert(segment.Count == 1);
                    }
#endif

                    state.tilemaps.Last().tileTempState.get(origin)->fireDirection = FireDirection.NoDirection;

                    List<int> pushed = new List<int>();
                    {
                        Vec2i directHitPoint = origin;
                        while (true)
                        {
                            if (directHitPoint == state.tilemaps.Last().playerPosition)
                            {
                                state.tilemaps.Last().dead = true;
                                goto breakOuter;
                            }

                            directHitPoint+= direction;
                            if (!state.tilemaps.Last().isPointValid(directHitPoint))
                                break;


                            Tile* tile = state.tilemaps.Last().get(directHitPoint);

                            // Trigger any bombs we hit along the way
                            if (tile->tileId == Constants.bombItemTileId)
                            {
                                tile->tileId = new TileId(Constants.bombTileId);
                                tile->bombId = state.tilemaps.Last().nextBombId++;
                                foreach (Segment segment in segments)
                                {
                                    if (segment.tileId == Constants.bombItemTileId && segment.Contains(directHitPoint))
                                        segment.tileId = new TileId(Constants.bombTileId);
                                }
                            }

                            TileId tileId = tile->tileId;
                            if (tileIdCanFall(tileId) || tileId == Constants.deletedPlaceholderTile)
                            {
                                int segment = segmentLookup[directHitPoint];
                                if (!cantFall.Contains(segment) || crushable(tileId))
                                {
                                    pushed.Add(segment);
                                    state.tilemaps.Last().tileTempState.get(directHitPoint)->fireDirection = fireDirection;
                                }

                                break;
                            }
                            else if (tileId == Constants.airTileId || tileId == Constants.megadrillTileId)
                            {
                                state.tilemaps.Last().tileTempState.get(directHitPoint)->fireDirection = fireDirection;
                            }
                            else
                            {
                                break;
                            }
                        }

                        for (int pushingSegmentIndex = 0; pushingSegmentIndex < pushed.Count; pushingSegmentIndex++)
                        {
                            Segment pushingSegment = segments[pushed[pushingSegmentIndex]];
                            foreach (Vec2i p in pushingSegment)
                            {
                                if (state.tilemaps.Last().isPointValid(p + direction) && tileIdCanFall(state.tilemaps.Last().get(p + direction)->tileId))
                                {
                                    int pushedSegmentIndex = segmentLookup[p + direction];

                                    if (!pushed.Contains(pushedSegmentIndex) && !cantFall.Contains(pushedSegmentIndex))
                                    {
                                        // items can push items but are crushed if they try to push anything else
                                        if (!crushable(pushingSegment.tileId) || crushable(segments[pushedSegmentIndex].tileId))
                                            pushed.Add(pushedSegmentIndex);
                                    }
                                }
                            }
                        }
                    }

                    Tilemap beforeDrop = state.tilemaps.Last().clone();

                    // First clear out the map
                    for (int y = 0; y < state.tilemaps.Last().dimensions.y; y++)
                    {
                        for (int x = 0; x < state.tilemaps.Last().dimensions.x; x++)
                        {
                            state.tilemaps.Last().set(x, y, new Tile(Constants.airTileId, 0));
                        }
                    }

                    bool pushPlayer = false;

                    // And then repaint every segment's tiles, shifted down if it is able to move (except crushable tiles)
                    for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                    {
                        TileId segmentTileId = segments[segmentIndex].tileId;

                        if (segmentTileId != Constants.airTileId && !crushable(segmentTileId))
                        {
                            Vec2i offset = new Vec2i(0, 0);
                            if (pushed.Contains(segmentIndex))
                            {
                                offset += direction;
                                didMove = true;
                            }

                            bool didCut = false;
                            List<Vec2i> newPoints = new List<Vec2i>();

                            foreach (Vec2i point in segments[segmentIndex])
                            {
                                Vec2i newPoint = point + offset;
                                if (!state.tilemaps.Last().isPointValid(newPoint))
                                {
                                    Tile* tile = beforeDrop.get(point);

                                    Vec2i visualTargetPoint = point;
                                    switch (fireDirection)
                                    {
                                        case FireDirection.Up: visualTargetPoint += new Vec2i(0, -state.tilemaps.Last().dimensions.y/2); break;
                                        case FireDirection.Down: visualTargetPoint += new Vec2i(0, state.tilemaps.Last().dimensions.y/2); break;
                                        case FireDirection.Left: visualTargetPoint += new Vec2i(-state.tilemaps.Last().dimensions.x/2, 0); break;
                                        case FireDirection.Right: visualTargetPoint += new Vec2i(state.tilemaps.Last().dimensions.x/2, 0); break;
                                        default: Util.ReleaseAssert(false); break;
                                    }
                                    state.tilemaps.Last().removedTilesAnimationPoints.Add(tile->tileIdentity, visualTargetPoint);
                                    newPoints.Add(newPoint);
                                    continue;
                                }

                                // megadrill cut
                                if (beforeDrop.get(newPoint)->tileId == Constants.megadrillTileId && megadrillCanCut(beforeDrop.get(point)->tileId))
                                {
                                    Tile* tile = beforeDrop.get(point);
                                    state.tilemaps.Last().removedTilesAnimationPoints.Add(tile->tileIdentity, newPoint);
                                    state.tilemaps.Last().soundEffects.Add(SoundId.Megadrill);
                                    didCut = true;
                                    continue;
                                }

                                if (!tileIsSolid(state.tilemaps.Last().get(newPoint)->tileId))
                                {
                                    state.tilemaps.Last().set(newPoint, *beforeDrop.get(point));
                                    newPoints.Add(newPoint);

                                    // push the player if we are pressing on him
                                    if (offset != new Vec2i(0,0) && newPoint == state.tilemaps.Last().playerPosition)
                                        pushPlayer = true;
                                }
                            }

                            if (segments[segmentIndex].tileId != Constants.dirtTileId)
                                resegmentIfNeeded(state, didCut, newPoints);
                        }
                    }

                    // crushable tiles placed after if they haven't been crushed
                    for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                    {
                        TileId segmentTileId = segments[segmentIndex].tileId;

                        if (crushable(segmentTileId))
                        {
                            Vec2i offset = new Vec2i(0, 0);
                            if (pushed.Contains(segmentIndex))
                            {
                                offset += direction;
                                didMove = true;
                            }

                            foreach (Vec2i point in segments[segmentIndex])
                            {
                                Vec2i newPoint = point + offset;
                                if (state.tilemaps.Last().isPointValid(newPoint))
                                {
                                    // megadrill cut
                                    if (state.tilemaps.Last().get(newPoint)->tileId == Constants.megadrillTileId)
                                    {
                                        Tile* tile = beforeDrop.get(point);
                                        state.tilemaps.Last().removedTilesAnimationPoints.Add(tile->tileIdentity, newPoint);
                                        state.tilemaps.Last().soundEffects.Add(SoundId.Megadrill);state.tilemaps.Last().soundEffects.Add(SoundId.Megadrill);
                                        continue;
                                    }

                                    if (!tileIsSolid(state.tilemaps.Last().get(newPoint)->tileId))
                                    {
                                        state.tilemaps.Last().set(newPoint, *beforeDrop.get(point));

                                        if (!tileIdIsItem(segmentTileId))
                                        {
                                            // push the player if we are pressing on him
                                            if (offset != new Vec2i(0,0) && newPoint == state.tilemaps.Last().playerPosition)
                                                pushPlayer = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (pushPlayer && state.tilemaps.Last().isPointValid(state.tilemaps.Last().playerPosition + direction) &&
                        !tileIsSolid(state.tilemaps.Last().get(state.tilemaps.Last().playerPosition + direction)->tileId))
                    {
                        state.tilemaps.Last().playerPosition += direction;
                    }

                    if (handlePlayerCollision(state, standingOnBomb))
                        didMove = true;

                    if (state.tilemaps.Last().dead)
                        goto breakOuter;
                }
            }

            breakOuter: ;
        }

        static bool tryDropRocks(EvaluationResult state)
        {
            int standingOnBomb = state.tilemaps.Last().get(state.tilemaps.Last().playerPosition)->bombId;

            bool didAnyMove = false;

            bool continueFalling = true;
            while (continueFalling)
            {
                continueFalling = false;

                List<Segment> segments = calculateSegments(state.tilemaps.Last());
                HashSet<int> cantFall = calculateFixedSegments(state.tilemaps.Last(), segments, new Vec2i(0, 1));

                Tilemap beforeDrop = state.tilemaps.Last().clone();

                // First clear out the map
                for (int y = 0; y < state.tilemaps.Last().dimensions.y; y++)
                {
                    for (int x = 0; x < state.tilemaps.Last().dimensions.x; x++)
                    {
                        state.tilemaps.Last().set(x, y, new Tile(Constants.airTileId, 0));
                    }
                }

                bool pushPlayerDown = false;

                // And then repaint every segment's tiles, shifted down if it is able to move
                for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                {
                    TileId segmentTileId = segments[segmentIndex].tileId;

                    if (segmentTileId != Constants.airTileId)
                    {
                        Vec2i offset = new Vec2i(0, 0);
                        if (!cantFall.Contains(segmentIndex))
                        {
                            offset.y++;
                            continueFalling = true;
                            didAnyMove = true;
                            if (!tileIdIsItem(segmentTileId))
                                state.tilemaps.Last().shakeScreen = true;
                        }

                        bool didCut = false;
                        List<Vec2i> newPoints = new List<Vec2i>();

                        foreach (Vec2i point in segments[segmentIndex])
                        {
                            Vec2i newPoint = point + offset;

                            if (!state.tilemaps.Last().isPointValid(newPoint))
                            {
                                Tile* tile = beforeDrop.get(point);
                                newPoints.Add(newPoint);
                                state.tilemaps.Last().removedTilesAnimationPoints.Add(tile->tileIdentity, point + new Vec2i(0, state.tilemaps.Last().dimensions.y / 2));
                                continue;
                            }

                            // megadrill cut
                            if (beforeDrop.get(newPoint)->tileId == Constants.megadrillTileId && beforeDrop.get(point)->tileId != Constants.megadrillTileId)
                            {
                                Tile* tile = beforeDrop.get(point);
                                state.tilemaps.Last().removedTilesAnimationPoints.Add(tile->tileIdentity, newPoint);
                                state.tilemaps.Last().soundEffects.Add(SoundId.Megadrill);
                                didCut = true;
                                continue;
                            }

                            if (!tileIsSolid(state.tilemaps.Last().get(newPoint)->tileId))
                            {
                                state.tilemaps.Last().set(newPoint, *beforeDrop.get(point));
                                newPoints.Add(newPoint);

                                // push the player down if we are pressing on top of him
                                if (offset != new Vec2i(0,0) && newPoint == state.tilemaps.Last().playerPosition && !tileIdIsItem(state.tilemaps.Last().get(newPoint)->tileId))
                                    pushPlayerDown = true;
                            }
                        }

                        if (segments[segmentIndex].tileId != Constants.dirtTileId)
                            resegmentIfNeeded(state, didCut, newPoints);
                    }
                }

                if (pushPlayerDown && state.tilemaps.Last().playerPosition.y + 1 < state.tilemaps.Last().dimensions.y &&
                    !tileIsSolid(state.tilemaps.Last().get(state.tilemaps.Last().playerPosition.x, state.tilemaps.Last().playerPosition.y + 1)->tileId))
                {
                    state.tilemaps.Last().playerPosition.y++;
                }

                if (handlePlayerCollision(state, standingOnBomb))
                    continueFalling = true;
            }

            return didAnyMove;
        }

        // If we did a megadrill cut, or pushed a chunk partially offscreen, we might have created a new segment of stone, which
        // will have the same segment ID as the old one. That's not ok, because then it will merge if it ever touches the other
        // half of its old self, so here we try to detect that and assign a new id.
        private static void resegmentIfNeeded(EvaluationResult state, bool didMegadrillCut, List<Vec2i> newPoints)
        {
            if (!didMegadrillCut)
            {
                bool pushedOffScreenEdge = false;
                foreach (Vec2i point in newPoints)
                {
                    if (!state.tilemaps.Last().isPointValid(point))
                    {
                        pushedOffScreenEdge = true;
                        break;
                    }
                }

                if (!pushedOffScreenEdge)
                    return;
            }

            HashSet<Segment> newSegmentsAfterCut = new HashSet<Segment>();
            {
                List<Segment> allSegmentsAfterCut = calculateSegments(state.tilemaps.Last());
                Dictionary<Vec2i, int> segmentLookup = calculatePointToSegmentDict(allSegmentsAfterCut);

                foreach (Vec2i point in newPoints)
                {
                    if (state.tilemaps.Last().isPointValid(point))
                        newSegmentsAfterCut.Add(allSegmentsAfterCut[segmentLookup[point]]);
                }
            }

            if (newSegmentsAfterCut.Count > 1)
            {
                foreach(Segment newSegment in newSegmentsAfterCut.Skip(1))
                {
                    foreach (Vec2i point in newSegment)
                        state.tilemaps.Last().get(point)->segmentId = state.tilemaps.Last().nextSegmentId;

                    state.tilemaps.Last().nextSegmentId++;
                }
            }
        }

        enum SupportType
        {
            PlaceHolder,
            NotPlaceholder,
            Skipped,
        }

        private static bool isSegmentSupportedByPlaceholder(List<SegmentStackNode> stackNodes, int segmentIndex)
        {
            SupportType inner(int _segmentIndex)
            {
                if (stackNodes[_segmentIndex].segment.tileId == Constants.megadrillTileId)
                    return SupportType.Skipped;

                if (stackNodes[_segmentIndex].segment.tileId == Constants.deletedPlaceholderTile)
                    return SupportType.PlaceHolder;

                if (stackNodes[_segmentIndex].segment.tileId == Constants.bedrockTileId)
                    return SupportType.NotPlaceholder;

                if (stackNodes[_segmentIndex].belowSegmentIndices.Count == 0)
                    return SupportType.NotPlaceholder;

                stackNodes[_segmentIndex].visited = true;

                SupportType belowAggregated = SupportType.Skipped;
                foreach (int belowIndex in stackNodes[_segmentIndex].belowSegmentIndices)
                {
                    if (stackNodes[belowIndex].visited)
                        continue;

                    SupportType belowType = inner(belowIndex);
                    if (belowType == SupportType.NotPlaceholder)
                        return SupportType.NotPlaceholder;
                    else if (belowType == SupportType.PlaceHolder)
                        belowAggregated = SupportType.PlaceHolder;
                }

                return belowAggregated;
            }

            foreach (SegmentStackNode node in stackNodes)
                node.visited = false;

            return inner(segmentIndex) == SupportType.PlaceHolder;
        }

        class SegmentStackNode
        {
            public Segment segment;
            public HashSet<int> belowSegmentIndices = new HashSet<int>();
            public bool visited = false;
        }

        private static List<SegmentStackNode> calculateSegmentStacks(Tilemap state, List<Segment> segments)
        {
            List<SegmentStackNode> stackNodes = new List<SegmentStackNode>();
            for (int segmentIndex = 0; segmentIndex < segments.Capacity; segmentIndex++)
                stackNodes.Add(new SegmentStackNode(){segment = segments[segmentIndex]});

            for (int segmentIndex = 0; segmentIndex < segments.Capacity; segmentIndex++)
            {
                if (segments[segmentIndex].tileId == Constants.airTileId)
                    continue;

                foreach (Vec2i point in segments[segmentIndex])
                {
                    if (!state.isPointValid(point + new Vec2i(0, 1)))
                        continue;

                    for (int segmentIndex2 = 0; segmentIndex2 < segments.Count; segmentIndex2++)
                    {
                        if (segmentIndex2 == segmentIndex)
                            continue;
                        if (segments[segmentIndex2].tileId == Constants.airTileId)
                            continue;
                        if (tileIdIsItem(segments[segmentIndex2].tileId) && !tileIdIsItem(segments[segmentIndex].tileId))
                            continue;

                        if (segments[segmentIndex2].Contains(new Vec2i(point.x, point.y) + new Vec2i(0, 1)))
                            stackNodes[segmentIndex].belowSegmentIndices.Add(segmentIndex2);
                    }
                }
            }

            return stackNodes;
        }

        // A segment is fixed if it touches the screen edge, or if it is supported from beneath by a fixed segment.
        public static HashSet<int> calculateFixedSegments(Tilemap state, List<Segment> segments, Vec2i direction, Func<TileId, bool> isSolid = null)
        {
            if (isSolid == null)
                isSolid = tileIsSolid;

            HashSet<int> cantFall = new HashSet<int>();

            bool cantFallChanged = true;
            while (cantFallChanged)
            {
                cantFallChanged = false;

                for (int segmentIndex = 0; segmentIndex < segments.Capacity; segmentIndex++)
                {
                    if (cantFall.Contains(segmentIndex))
                        goto CONTINUE;

                    TileId segmentTileId = segments[segmentIndex].tileId;

                    if (!tileIdCanFall(segmentTileId))
                    {
                        cantFall.Add(segmentIndex);
                        cantFallChanged = true;
                        goto CONTINUE;
                    }

                    foreach (Vec2i point in segments[segmentIndex])
                    {
                        if (!state.isPointValid(point + direction))
                            continue;

                        // find the segment of the block underneath us
                        int segmentIndexUnderUs = -1;
                        for (int segmentIndex2 = 0; segmentIndex2 < segments.Count; segmentIndex2++)
                        {
                            if (segments[segmentIndex2].Contains(new Vec2i(point.x, point.y) + direction))
                            {
                                segmentIndexUnderUs = segmentIndex2;
                                break;
                            }
                        }
                        Util.ReleaseAssert(segmentIndexUnderUs != -1);

                        // If the segment under this block can't fall, then this segment also can't fall
                        if (segmentIndexUnderUs != segmentIndex)
                        {
                            if ((
                                    isSolid(segments[segmentIndexUnderUs].tileId) ||
                                    (tileIdIsItem(segments[segmentIndexUnderUs].tileId) && tileIdIsItem(segments[segmentIndex].tileId)) ||
                                    (segments[segmentIndexUnderUs].tileId == Constants.megadrillTileId && segmentTileId == Constants.diamondIds[0])
                                ) &&
                                cantFall.Contains(segmentIndexUnderUs))
                            {
                                cantFall.Add(segmentIndex);
                                cantFallChanged = true;
                                goto CONTINUE;
                            }
                        }
                    }
                    CONTINUE: ;
                }
            }

            return cantFall;
        }


        public static Grip getGrip(Tilemap state)
        {
            bool gripAtOffset(int offX, int offY)
            {
                int x = state.playerPosition.x + offX;
                int y = state.playerPosition.y + offY;
                return x >= 0 && x < state.dimensions.x && y >= 0 && y < state.dimensions.y && tileIsSolid(state.get(x, y)->tileId);
            }

            return new Grip()
            {
                left = gripAtOffset(-1, 0),
                right = gripAtOffset(1, 0),
                belowLeft = gripAtOffset(-1, 1),
                belowRight = gripAtOffset(1, 1),
                onSolidGround = gripAtOffset(0, 1),
            };
        }

        static bool tileIdCanFall(TileId tileId)
        {
            return tileId != Constants.airTileId && tileId != Constants.deletedPlaceholderTile && tileId != Constants.bedrockTileId && tileId != Constants.megadrillTileId;
        }

        static bool tileIdIsItem(TileId tileId)
        {
            return tileId == Constants.lootTileId || tileId == Constants.bombItemTileId || tileId == Constants.megadrillItemTileId || tileId == Constants.levelEndTileId;
        }

        public static bool tileIsSolid(TileId tileId)
        {
            return tileId != Constants.airTileId && !tileIdIsItem(tileId) && tileId != Constants.megadrillTileId;
        }
    }
}