using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Steamworks;

namespace Drilbert
{
    public enum Input
    {
        Up,
        Down,
        Left,
        Right,
        Undo,
        Reset,
        ToggleFullscreen,
        MenuActivate,
        BombDrop,
        BombTrigger,
        MegadrillDrop,
        Pause,
        MenuBack,

        MAX_INPUT
    }

    public class InputState
    {
        private InputSnapshot lastState = null;
        public InputSnapshot currentState { get; private set; } = null;

        class RepeatState { public long startMs; public long lastRepeatMs; }
        private RepeatState[] inputRepeatBuffer = new RepeatState[(int)Input.MAX_INPUT];
        private long nowMs = 0;

        private Dictionary<string, GameInputSet> lastGameInputSets = new Dictionary<string, GameInputSet>();
        private string activeInputDeviceId = null;

        private static void getControllerStatesFromMonogame(Dictionary<string, GameInputSet> states)
        {
            for (int i = 0; i < GamePad.MaximumGamePadCount; i++)
            {
                GamePadState monogameState = GamePad.GetState(i, GamePadDeadZone.Circular);
                GameInputSet state = new GameInputSet();
                state.undo = monogameState.Buttons.Y == ButtonState.Pressed;
                state.detonate = monogameState.Buttons.B == ButtonState.Pressed;
                state.bomb = monogameState.Buttons.A == ButtonState.Pressed;
                state.megadrill = monogameState.Buttons.X == ButtonState.Pressed;
                state.reset = monogameState.Buttons.Back == ButtonState.Pressed;
                state.menu = monogameState.Buttons.Start == ButtonState.Pressed;
                state.up = monogameState.DPad.Up == ButtonState.Pressed;
                state.down = monogameState.DPad.Down == ButtonState.Pressed;
                state.left = monogameState.DPad.Left == ButtonState.Pressed;
                state.right = monogameState.DPad.Right == ButtonState.Pressed;
                state.menu_activate = monogameState.Buttons.A == ButtonState.Pressed;
                state.menu_back = monogameState.Buttons.B == ButtonState.Pressed;
                state.move.x = monogameState.ThumbSticks.Left.X;
                state.move.y = monogameState.ThumbSticks.Left.Y;
                states["monogame_" + i] = state;
            }
        }

        private GameInputSet getGameInputStateFromKeyboard(KeyboardState keyboard)
        {
            GameInputSet state = new GameInputSet();
            state.undo = keyboard.IsKeyDown(Keys.Z) || keyboard.IsKeyDown(Keys.Back);
            state.detonate = keyboard.IsKeyDown(Keys.Space);
            state.bomb = keyboard.IsKeyDown(Keys.E);
            state.megadrill = keyboard.IsKeyDown(Keys.F);
            state.reset = keyboard.IsKeyDown(Keys.R);
            state.menu = keyboard.IsKeyDown(Keys.Escape);
            state.up = keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up);
            state.down = keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down);
            state.left = keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left);
            state.right = keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right);
            state.toggleFullscreen = (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt)) && keyboard.IsKeyDown(Keys.Enter);
            state.menu_activate = !state.toggleFullscreen && keyboard.IsKeyDown(Keys.Enter);
            state.menu_back = keyboard.IsKeyDown(Keys.Escape);
            return state;
        }

        public void update(long gameTimeMs)
        {
            lastState = currentState;

            void realSetRumble(string controllerId, float left, float right)
            {
                if (controllerId.StartsWith("monogame_"))
                {
                    int index = int.Parse(controllerId.Substring("monogame_".Length));
                    GamePad.SetVibration(index, left, right);
                }
                else if (controllerId.StartsWith("steam_"))
                {
                    ulong id = ulong.Parse(controllerId.Substring("steam_".Length));
                    SteamInput.TriggerVibration(new InputHandle_t(id), (ushort)(left*ushort.MaxValue), (ushort)(right*ushort.MaxValue));
                }
            }

            KeyboardState keyboardState = Keyboard.GetStateByScancode();

            {
                Dictionary<string, GameInputSet> newGameInputs = new Dictionary<string, GameInputSet>();

                if (DrilbertSteam.usingSteam)
                    DrilbertSteam.getSteamInput(newGameInputs);

                getControllerStatesFromMonogame(newGameInputs);

                newGameInputs["keyboard"] = getGameInputStateFromKeyboard(keyboardState);

                foreach (var pair in newGameInputs)
                {
                    GameInputSet currentControllerState = pair.Value;
                    lastGameInputSets.TryGetValue(pair.Key, out GameInputSet oldGameInputSet);

                    if (oldGameInputSet != null && currentControllerState.hasChangedSince(oldGameInputSet))
                    {
                        if (activeInputDeviceId != pair.Key)
                        {
                            Logger.log("activated controller " + pair.Key);

                            if (activeInputDeviceId != null)
                                realSetRumble(activeInputDeviceId, 0, 0);
                        }

                        activeInputDeviceId = pair.Key;
                    }
                }

                if (activeInputDeviceId != null)
                    realSetRumble(activeInputDeviceId, Game1.game.currentScene.rumbleLeft, Game1.game.currentScene.rumbleRight);

                lastGameInputSets = newGameInputs;

                this.currentState = new InputSnapshot();
                this.currentState.keyboard = keyboardState;

                if (activeInputDeviceId == null || !newGameInputs.TryGetValue(activeInputDeviceId, out this.currentState.GameInputSet))
                    this.currentState.GameInputSet = new GameInputSet();
            }

            if (lastState == null)
                lastState = currentState;

            nowMs = gameTimeMs;

            for (Input i = 0; i < Input.MAX_INPUT; i++)
            {
                bool current = currentState.getInput(i);

                if (!current)
                    inputRepeatBuffer[(int)i] = null;
                if (current && inputRepeatBuffer[(int) i] == null)
                    inputRepeatBuffer[(int)i] = new RepeatState{ startMs = nowMs, lastRepeatMs = 0 };
            }

        }

        public enum InputDeviceType
        {
            Keyboard,
            Gamepad,
        }
        public InputDeviceType getLastInputDeviceUsed()
        {
            if (activeInputDeviceId == "keyboard")
                return InputDeviceType.Keyboard;
            return InputDeviceType.Gamepad;
        }

        public bool downThisFrame(Input input)
        {
            return currentState.getInput(input) && !lastState.getInput(input);
        }

        public bool downThisFrame(Keys key)
        {
            return currentState.getInput(key) && !lastState.getInput(key);
        }

        public bool activeWithRepeat(Input input, long repeatIntervalMs = Constants.keyRepeatMs)
        {
            RepeatState state = inputRepeatBuffer[(int)input];
            if (state == null)
                return false;

            long lastRepeatIndex = (state.lastRepeatMs - state.startMs) / repeatIntervalMs;
            long nowIndex = (nowMs - state.startMs) / repeatIntervalMs;

            if (nowIndex > lastRepeatIndex)
            {
                state.lastRepeatMs = nowMs;
                return true;
            }

            return false;
        }

        public class InputSnapshot
        {
            public KeyboardState keyboard;
            public GameInputSet GameInputSet;

            public bool getInput(Keys key)
            {
                return keyboard.IsKeyDown(key);
            }

            public bool getInput(Input input)
            {
                switch (input)
                {
                    case Input.Up:
                    case Input.Down:
                    case Input.Left:
                    case Input.Right:
                    {
                        if (input == Input.Up && GameInputSet.up) return true;
                        if (input == Input.Down && GameInputSet.down) return true;
                        if (input == Input.Left && GameInputSet.left) return true;
                        if (input == Input.Right && GameInputSet.right) return true;

                        List<Direction> blockedDirections = Game1.game.currentScene.blockedDirections;


                        Vec2f vec = GameInputSet.move;
                        if (vec.magnitude() < GameInputSet.joystickActiveThreshold)
                            return false;

                        float angle = vec.toAngleDegrees(); // 0 = right, 90 = up

                        float acceptClockwise = 20.0f;
                        float acceptAntiClockwise = 20.0f;
                        const float acceptIfBlocked = 90.0f;

                        if (input == Input.Up)
                        {
                            if (blockedDirections.Contains(Direction.Left)) acceptAntiClockwise = acceptIfBlocked;
                            if (blockedDirections.Contains(Direction.Right)) acceptClockwise = acceptIfBlocked;
                            return angle > 90 - acceptClockwise && angle < 90 + acceptAntiClockwise;
                        }
                        if (input == Input.Down)
                        {
                            if (blockedDirections.Contains(Direction.Left)) acceptClockwise = acceptIfBlocked;
                            if (blockedDirections.Contains(Direction.Right)) acceptAntiClockwise = acceptIfBlocked;
                            return angle > 270 - acceptClockwise && angle < 270 + acceptAntiClockwise;
                        }
                        if (input == Input.Left)
                        {
                            if (blockedDirections.Contains(Direction.Up)) acceptClockwise = acceptIfBlocked;
                            if (blockedDirections.Contains(Direction.Down)) acceptAntiClockwise = acceptIfBlocked;
                            return angle > 180 - acceptClockwise && angle < 180 + acceptAntiClockwise;
                        }
                        if (input == Input.Right)
                        {
                            if (blockedDirections.Contains(Direction.Down)) acceptClockwise = acceptIfBlocked;
                            if (blockedDirections.Contains(Direction.Up)) acceptAntiClockwise = acceptIfBlocked;
                            return angle > 360 - acceptClockwise || angle < acceptAntiClockwise;
                        }
                        break;
                    }

                    case Input.Undo:
                        return GameInputSet.undo;
                    case Input.Reset:
                        return GameInputSet.reset;
                    case Input.ToggleFullscreen:
                        return GameInputSet.toggleFullscreen;
                    case Input.MenuActivate:
                        return GameInputSet.menu_activate;
                    case Input.BombDrop:
                        return GameInputSet.bomb;
                    case Input.BombTrigger:
                        return GameInputSet.detonate;
                    case Input.MegadrillDrop:
                        return GameInputSet.megadrill;
                    case Input.Pause:
                        return GameInputSet.menu;
                    case Input.MenuBack:
                        return GameInputSet.menu_back;
                }

                Util.ReleaseAssert(false);
                return false;
            }
        }
    }

    public class GameInputSet
    {
        public const float joystickActiveThreshold = 0.5f;

        public bool undo = false;
        public bool detonate = false;
        public bool bomb = false;
        public bool megadrill = false;
        public bool reset = false;
        public bool menu = false;
        public bool up = false;
        public bool down = false;
        public bool left = false;
        public bool right = false;
        public bool toggleFullscreen = false;
        public bool menu_activate = false;
        public bool menu_back = false;
        public Vec2f move = new Vec2f();

        public bool hasChangedSince(GameInputSet old)
        {
            if (undo != old.undo) return true;
            if (detonate != old.detonate) return true;
            if (bomb != old.bomb) return true;
            if (megadrill != old.megadrill) return true;
            if (reset != old.reset) return true;
            if (menu != old.menu) return true;
            if (up != old.up) return true;
            if (down != old.down) return true;
            if (left != old.left) return true;
            if (right != old.right) return true;
            if (menu_activate != old.menu_activate) return true;
            if (menu_back != old.menu_back) return true;
            if (move.magnitude() >= joystickActiveThreshold) return true;
            return false;
        }

        public GameInputSet() {}
    }
}