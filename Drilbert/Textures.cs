using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert
{
    public static class Textures
    {
        public static Animation tileset { get; private set; }
        public static Animation playerIdle { get; private set; }
        public static Animation playerHangInPipe { get; private set; }
        public static Animation playerStandAndHang { get; private set; }
        public static Animation playerHangBeside { get; private set; }
        public static Animation playerHangAbove { get; private set; }
        public static Animation playerHangOverPipe { get; private set; }
        public static Animation playerDrill { get; private set; }
        public static Animation logo { get; private set; }
        public static Animation controls { get; private set; }
        public static Texture2D controlsBombOverlay { get; private set; }
        public static Texture2D controlsDrillOverlay { get; private set; }
        public static Animation controlsMove { get; private set; }
        public static Animation controlsUndoResetKeyboard { get; private set; }
        public static Animation controlsUndoResetXbox { get; private set; }
        public static Texture2D controlsUndoResetTopKeyboard { get; private set; }
        public static Texture2D controlsUndoResetTopXbox { get; private set; }
        public static Animation controlsUndoResetXboxNoColor { get; private set; }
        public static Animation controlsBombKeyboard { get; private set; }
        public static Animation controlsBombXbox { get; private set; }
        public static Animation controlsMegadrillKeyboard { get; private set; }
        public static Animation controlsMegadrillXbox { get; private set; }
        public static Texture2D levelSelect { get; private set; }
        public static Texture2D levelSelectClouds { get; private set; }
        public static Texture2D levelSelectNodeDone { get; private set; }
        public static Animation levelSelectNodeAvailable { get; private set; }
        public static Animation levelSelectNodeUnavailable { get; private set; }
        public static Texture2D levelSelectReticle { get; private set; }
        public static Texture2D levelSelectConnection {get; private set; }
        public static Animation diamond { get; private set; }
        public static Texture2D drilheim { get; private set; }
        public static Texture2D drilheimUpperLayer { get; private set; }
        public static Texture2D drilfatherBig { get; private set; }
        public static Texture2D drilbertBig { get; private set; }
        public static Animation drilfatherIdle { get; private set; }
        public static Animation drilfatherGogogo { get; private set; }
        public static Texture2D white { get; private set; }
        public static BmFont debugFont { get; private set; }
        public static BmFont drilbertFont { get; private set; }

#if DEMO
        public static Animation demoOverlay { get; private set; }
#endif

        public static void loadTextures()
        {
            tileset = new Animation("gfx/tileset");
            playerIdle = new Animation("gfx/player_idle");
            playerHangInPipe = new Animation("gfx/player_hang_in_pipe");
            playerStandAndHang = new Animation("gfx/player_stand_and_hang");
            playerHangBeside = new Animation("gfx/player_hang_beside");
            playerHangAbove = new Animation("gfx/player_hang_above");
            playerHangOverPipe = new Animation("gfx/player_hang_over_pipe");
            playerDrill = new Animation("gfx/player_drill", (long)(1000 * 0.1));
            logo = new Animation("gfx/logo");
            controls = new Animation("gfx/controls");
            controlsBombOverlay = loadTexture("gfx/controls_bomb_overlay.png");
            controlsDrillOverlay = loadTexture("gfx/controls_drill_overlay.png");
            controlsMove = new Animation("gfx/controls_move");
            controlsUndoResetKeyboard = new Animation("gfx/controls_undo_reset_kb");
            controlsUndoResetXbox = new Animation("gfx/controls_undo_reset_xb");
            controlsUndoResetXboxNoColor = new Animation("gfx/controls_undo_reset_xb_nocolor");
            controlsUndoResetTopKeyboard = loadTexture("gfx/controls_undo_reset_top_kb.png");
            controlsUndoResetTopXbox = loadTexture("gfx/controls_undo_reset_top_xb.png");
            controlsBombKeyboard = new Animation("gfx/controls_bomb_kb");
            controlsBombXbox = new Animation("gfx/controls_bomb_xb");
            controlsMegadrillKeyboard = new Animation("gfx/controls_megadrill_kb");
            controlsMegadrillXbox = new Animation("gfx/controls_megadrill_xb");
            levelSelect = loadTexture("gfx/level_select.png");
            levelSelectClouds = loadTexture("gfx/clouds.png");
            levelSelectNodeDone = loadTexture("gfx/level_select_node_done.png");
            levelSelectNodeAvailable = new Animation("gfx/level_select_node_available");
            levelSelectNodeUnavailable = new Animation("gfx/level_select_node_unavailable");
            levelSelectReticle = loadTexture("gfx/level_select_reticle.png");
            levelSelectConnection = loadTexture("gfx/level_select_connection.png");
            diamond = new Animation("gfx/diamond", 200);
            drilheim = loadTexture("gfx/drilheim.png");
            drilheimUpperLayer = loadTexture("gfx/drilheim_upper_layer.png");
            drilfatherBig = loadTexture("gfx/drilfather_big.png");
            drilbertBig = loadTexture("gfx/drilbert_big.png");
            drilfatherIdle = new Animation("gfx/drilfather_idle");
            drilfatherGogogo = new Animation("gfx/drilfather_gogogo", 400);
            white = loadTexture("gfx/white.png");
            debugFont = new BmFont("gfx/bmfonts/debug");
            drilbertFont = new BmFont("gfx/bmfonts/drilbert");

            #if DEMO
            demoOverlay = new Animation("gfx/demo_overlay", Constants.levelSelectReticlePeriodMs);
            #endif
        }

        private static Texture2D loadTexture(string path) => Texture2D.FromFile(Game1.game.GraphicsDevice, Path.Combine(Constants.rootPath, path));
    }
}