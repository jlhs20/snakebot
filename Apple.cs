using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace snakebot;

public class Apple
{
    private Texture2D _texture;
    private float _angle = 0f;

    public (int, int) Position;
    
    public Apple((int, int) position)
    {
        Position = position;
    }
    
    public void LoadContent(ContentManager content)
    {
        _texture = content.Load<Texture2D>("apple");
    }

    public void Update(GameTime gameTime)
    {
        _angle = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds*0.75f)*0.3f;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, new Vector2(Position.Item1*64 + 32, Position.Item2*64 + 32), null, Color.White, _angle, new Vector2(32, 32), 1, SpriteEffects.None, 0);
    }
}