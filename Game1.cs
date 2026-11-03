using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace snakebot;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _tileset;
    private Snake _snake;
    private Apple _apple;
    private KeyboardState _oldKeyboardState;
    private MathHelper.Random _random;
    private Cards _cards;
    private int _rewinds = 5;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 16*64;
        _graphics.PreferredBackBufferHeight = 12*64;
        _graphics.PreferMultiSampling = true;
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        _graphics.ApplyChanges();

        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        _snake = new Snake(ReadSaveFile("snake.txt"));
        _random = new MathHelper.Random();
        _apple = new Apple((3, 3));
        RandomizeApple();
        _cards = new Cards(_snake);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _tileset = Content.Load<Texture2D>("grasstiles");
        _snake.LoadContent(Content);
        _apple.LoadContent(Content);
        _cards.LoadContent(Content);
    }

    private void RandomizeApple()
    {
        List<(int, int)> tiles = new();
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 9; y++)
            {
                if (!_snake.Segments.Contains<(int, int)>((x, y)))
                    tiles.Add((x, y));
            }
        }
        _apple.Position = tiles[_random.Next(0, tiles.Count)];
    }

    private (int, int)[] ReadSaveFile(string filename) // in the future, I would want a more advanced save file that saves the hand, deck, apples collected, high score, etc
    {
        if (!File.Exists(filename)) return null;
        else
        {
            string[] lines = File.ReadAllLines(filename);
            List<(int, int)> segmentsList = new();
            foreach (string line in lines)
            {
                string[] split = line.Split(",");
                segmentsList.Add((Convert.ToInt32(split[0]), Convert.ToInt32(split[1])));
            }
            return segmentsList.ToArray();
        }
    }

    private void WriteSaveFile(string filename, (int, int)[] segments)
    {
        List<string> lines = new();
        foreach ((int, int) segment in segments)
        {
            lines.Add($"{segment.Item1},{segment.Item2}");
        }
        File.WriteAllLines(filename, lines);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        base.OnExiting(sender, args);

        // save snake position to file
        WriteSaveFile("snake.txt", _snake.Segments);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (_apple.Position == _snake.Segments[0])
        {
            _snake.Grow();
            RandomizeApple();
        }

        if (_snake.Segments[0].Item1 > 15 || _snake.Segments[0].Item1 < 0 || _snake.Segments[0].Item2 < 0 || _snake.Segments[0].Item2 > 8 || _snake.Segments[1..].Contains(_snake.Segments[0]))
        {
            _snake.Dead = true;
        }

        KeyboardState keyboardState = Keyboard.GetState();
        if (_snake.Dead && keyboardState.IsKeyDown(Keys.Enter) && !_oldKeyboardState.IsKeyDown(Keys.Enter)) Initialize();
        //if (keyboardState.IsKeyDown(Keys.F) && !_oldKeyboardState.IsKeyDown(Keys.F)) _snake.Flip();
        if (keyboardState.IsKeyDown(Keys.R) && !_oldKeyboardState.IsKeyDown(Keys.R)) _snake.Rewind(); // we want to only allow this on death, and to only allow so many rewinds
        _oldKeyboardState = keyboardState;

        _snake.Update(gameTime);
        _apple.Update(gameTime);
        _cards.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 9; y++)
            {
                _spriteBatch.Draw(_tileset, new Rectangle(x*64, y*64, 64, 64), new Rectangle(((x+y*16)%7)*64, 0, 64, 64), (x+y)%2 == 0 ? Color.White : Color.LightGray);
            }
        }
        _snake.Draw(gameTime, _spriteBatch, GraphicsDevice);
        _apple.Draw(gameTime, _spriteBatch);
        _cards.Draw(gameTime, _spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
