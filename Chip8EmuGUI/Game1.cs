using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Chip8EmuLib;
using System;
using System.Linq;

namespace Chip8EmuGUI
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont font;
        public Texture2D whiteRect;

        public bool[] Pixels = new bool[64 * 32];
        public int PixelWidth = 64, PixelHeight = 32;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            
            Emu.ApiBeep = () =>
            {
                // todo
            };

            Emu.ApiIsKeyPressed = (ConsoleKey ck) =>
            {
                return Keyboard.GetState().IsKeyDown((Keys)ck);
            };

            Emu.ApiWaitForKey = () =>
            {
                waitagain:
                try
                {
                    while (Keyboard.GetState().GetPressedKeys().ToList().Count == 0) {
                        if (Emu.Exit) return ConsoleKey.A; // Do not block thread from exiting
                    }
                    Keys key = Keyboard.GetState().GetPressedKeys().ToList()[0];

                    while (Keyboard.GetState().GetPressedKeys().ToList().Count > 0)
                    {
                        if (Emu.Exit) return ConsoleKey.A;
                        if (Keyboard.GetState().GetPressedKeys().ToList().LastOrDefault(Keys.None) != key) break;
                    }

                    return (ConsoleKey)key;
                }catch(Exception ex)
                {
                    goto waitagain;
                }
            };

            Emu.ApiSetRenderSize = (int x, int y) =>
            {
                this.Pixels = new bool[x * y];
                this.PixelWidth = x;
                this.PixelHeight = y;
                graphics.PreferredBackBufferWidth = (x * 8) + 200;
                graphics.PreferredBackBufferHeight = (y * 8) + (200);
                graphics.ApplyChanges();
            };

            Emu.ApiSetTitle = (string str) =>
            {
                if(Window != null) Window.Title = str;
            };

            Emu.ApiSetPixel = (int x, int y, bool on) =>
            {
                Pixels[(y * this.PixelWidth) + x] = on;
            };

            Emu.ApiClearScreen = () =>
            {
                Pixels = new bool[(this.Pixels.Length)];
            };

            Emu.targetKHz = 0.5f;
            Emu.Main("CONNECT4.ch8");
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            whiteRect = new Texture2D(GraphicsDevice, 1, 1);
            whiteRect.SetData(new[] { Color.White });

            font = Content.Load<SpriteFont>("Font");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null, null, null,
                null);

            for (int i = 0; i < Pixels.Length; i++)
            {
                int y = i / PixelWidth;
                int x = i % PixelWidth;

                spriteBatch.Draw(whiteRect, new Rectangle(x * 8, y * 8, 8, 8), (Pixels[i] ? Color.White : Color.Black));
            }

            WriteDbg(1, "PC: " + Emu.pc + "    B0: " + Emu.byte0 + "    B1: " + Emu.byte1);
            WriteDbg(2, "Regs: I " + Emu.iRegister + "  0 " + Emu.registers[0] + "  1 " + Emu.registers[1] + "  2 " + Emu.registers[2] + "  3 " + Emu.registers[3]);
            WriteDbg(3, "      4 " + Emu.registers[4] + "  5 " + Emu.registers[5] + "  6 " + Emu.registers[6] + "  7 " + Emu.registers[7] + "  8 " + Emu.registers[8] + "  9 " + Emu.registers[9]);
            WriteDbg(4, "      A " + Emu.registers[10] + "  B " + Emu.registers[11] + "  C " + Emu.registers[12] + "  D " + Emu.registers[13] + "  E " + Emu.registers[14] + "  F " + Emu.registers[15]);
            WriteDbg(5, "NEAR PC: " + Emu.HexOutput(Emu.memory, Emu.pc -2, 12));

            spriteBatch.End();

            base.Draw(gameTime);
        }

        public void WriteDbg(int line, string str)
        {
            spriteBatch.DrawString(font, str, new Vector2(12, PixelHeight * 8 + (line * 16)), Color.White);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Emu.Exit = true;
        }
    }
}