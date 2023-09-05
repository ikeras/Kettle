using Emulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Display
{
    public class Game1 : Game
    {
        private readonly IChip8Emulator emulator;
        private readonly GraphicsDeviceManager graphics;
        private Rectangle destRect;
        private Task emulationTask;
        private SpriteBatch spriteBatch;
        private Texture2D texture;

        private int displayWidth;
        private int displayHeight;

        private readonly List<Keys>[] emulatorKeyToKeys = new List<Keys>[]
        {
            new() { Keys.X }, // 0x0
            new() { Keys.D1 }, // 0x1
            new() { Keys.D2 }, // 0x2
            new() { Keys.D3 }, // 0x3
            new() { Keys.Q }, // 0x4
            new() { Keys.W, Keys.Up }, // 0x5
            new() { Keys.E, Keys.Space }, // 0x6
            new() { Keys.A, Keys.Left }, // 0x7
            new() { Keys.S, Keys.Down }, // 0x8
            new() { Keys.D, Keys.Right }, // 0x9
            new() { Keys.Z }, // 0xa
            new() { Keys.C }, // 0xb
            new() { Keys.D4 }, // 0xc
            new() { Keys.R }, // 0xd
            new() { Keys.F }, // 0xe
            new() { Keys.V } // 0xf
        };

        public Game1(IChip8Emulator emulator)
        {
            this.graphics = new GraphicsDeviceManager(this);

            this.Window.AllowUserResizing = true;
            this.Window.ClientSizeChanged += (_, _) => this.CacluateDestRect();

            this.emulator = emulator;
            this.IsMouseVisible = true;
        }

        public async Task LoadRomAsync(string path)
        {
            this.emulationTask = null;

            await this.emulator.LoadRomAsync(path);

            this.emulationTask = new Task(() => this.emulator.StartOrContinue(700), default, TaskCreationOptions.LongRunning);
            this.emulationTask.Start();
        }

        protected override void LoadContent()
        {
            this.spriteBatch = new SpriteBatch(this.GraphicsDevice);
            this.UpdateDisplayDimensions();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState state = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || state.IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }

            this.emulator.Tick();

            for (int emulatorKey = 0; emulatorKey < this.emulatorKeyToKeys.Length; emulatorKey++)
            {
                if (this.emulatorKeyToKeys[emulatorKey].Any(key => state.IsKeyDown(key)))
                {
                    this.emulator.PressKey(emulatorKey);
                }
                else
                {
                    this.emulator.ReleaseKey(emulatorKey);
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            this.GraphicsDevice.Clear(Color.Black);

            if (this.displayWidth != this.emulator.DisplayWidth || this.displayHeight != this.emulator.DisplayHeight)
            {
                this.UpdateDisplayDimensions();
            }

            this.texture.SetData(this.emulator.GetDisplay());
            this.spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null);
            this.spriteBatch.Draw(this.texture, this.destRect, Color.White);
            this.spriteBatch.End();

            base.Draw(gameTime);
        }

        private void CacluateDestRect()
        {
            double imageRatio = (double)this.displayWidth / this.displayHeight;
            double windowRatio = (double)this.Window.ClientBounds.Width / this.Window.ClientBounds.Height;

            (int destX, int destY) = windowRatio > imageRatio ?
                (this.displayWidth * this.Window.ClientBounds.Height / this.displayHeight, this.Window.ClientBounds.Height) :
                (this.Window.ClientBounds.Width, this.displayHeight * this.Window.ClientBounds.Height / this.displayWidth);

            this.destRect = new Rectangle(0, 0, destX, destY);
        }

        private void UpdateDisplayDimensions()
        {
            this.displayHeight = this.emulator.DisplayHeight;
            this.displayWidth = this.emulator.DisplayWidth;
            this.CacluateDestRect();
            this.texture = new Texture2D(this.GraphicsDevice, this.displayWidth, this.displayHeight, false, SurfaceFormat.Color);
        }
    }
}