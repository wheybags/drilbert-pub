using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert
{
    public class Game1 : Game
    {
        public static Game1 game { get; private set; }

        private GraphicsDeviceManager graphics;
        private MySpriteBatch spriteBatch;
        private InputHandler inputHandler = new InputHandler();

        public LevelSelectScene levelSelectScene;
        public InGameScene inGameScene;
        public MainMenuScene mainMenuScene;

        public Scene currentScene = null;

        public Game1()
        {
            game = this;
            graphics = new GraphicsDeviceManager(this);
            Window.AllowUserResizing = true;

            Exiting += (_, __) =>
            {
                currentScene?.stop();
            };
        }

        public void setScene(Scene scene)
        {
            currentScene?.stop();
            currentScene = scene;
            currentScene.start();
        }

        public Scene getScene()
        {
            return currentScene;
        }

        public void toggleFullscreen()
        {
            setFullscreen(!graphics.IsFullScreen);
        }

        void setFullscreen(bool fullscreen)
        {
            GameSettings.fullscreen = fullscreen;

            if (fullscreen)
            {
                graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
                graphics.HardwareModeSwitch = false;
                graphics.IsFullScreen = true;
                IsMouseVisible = false;
                graphics.ApplyChanges();
            }
            else
            {
                graphics.IsFullScreen = false;
                IsMouseVisible = true;
                graphics.PreferredBackBufferWidth = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width * 0.8);
                graphics.PreferredBackBufferHeight = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height * 0.8);
                graphics.ApplyChanges();
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new MySpriteBatch(GraphicsDevice);
            Textures.loadTextures();
            Sounds.loadSoundEffects();
            GameSettings.tryLoadSettings();

            setFullscreen(GameSettings.fullscreen);

            levelSelectScene = new LevelSelectScene();
            inGameScene = new InGameScene();
            mainMenuScene = new MainMenuScene();
            setScene(mainMenuScene);

            MusicManager.nextTrack();

            string[] cliArgs = Environment.GetCommandLineArgs();

            if (cliArgs.Length > 1 && cliArgs[1] == "export_level_png")
            {
                string levelPath = cliArgs[2];
                string outputPath = cliArgs[3];
                LevelRender.randomiseDirt = cliArgs[4] == "true";

                Tilemap tilemap = new Tilemap(null, levelPath);
                RenderTarget2D mainRenderBuffer = new RenderTarget2D(GraphicsDevice, tilemap.dimensions.x * Constants.tileSize, tilemap.dimensions.y * Constants.tileSize);


                GraphicsDevice.SetRenderTarget(mainRenderBuffer);
                LevelRender.render(spriteBatch,
                                   0,
                                   tilemap,
                                   tilemap,
                                   0,
                                   false,
                                   false,
                                   LevelRender.EdgeEffect.None);

                using (var f = System.IO.File.OpenWrite(outputPath))
                    mainRenderBuffer.SaveAsPng(f, mainRenderBuffer.Width, mainRenderBuffer.Height);

                Exit();
            }
        }

        protected override void Update(GameTime _)
        {
            long gameTimeMs = Time.getMs();
            inputHandler.update(gameTimeMs);

            if (currentScene.getCurrentMenu() == null)
                currentScene.processInput(gameTimeMs, inputHandler);
            else
                inputHandler.processMenuInput(currentScene, gameTimeMs);

            inputHandler.processDebugInput(inGameScene.gameState, gameTimeMs);

            MusicManager.update(gameTimeMs);
            currentScene.update(gameTimeMs, inputHandler);

            base.Update(_);
        }

        protected override void Draw(GameTime _)
        {
            long gameTimeMs = Time.getMs();
            currentScene.draw(spriteBatch, inputHandler, gameTimeMs);

            if (gameTimeMs - inputHandler.reloadTexturesMs < 500)
            {
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.Clear(new Color(255, 255, 255, 255));
            }

            GraphicsDevice.SetRenderTarget(null);
            base.Draw(_);

            DrilbertSteam.runCallbacks();
        }
    }
}
