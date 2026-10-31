using System;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace snakebot;

public abstract class Card
{
    private Vector2 _oldPosition;
    private Vector2 _newPosition;

    public abstract Texture2D Texture { get; }
    public Vector2 Position => _newPosition; //Vector2.Lerp(_oldPosition, _newPosition, _tween);
    public float Rotation { get; set; } = 0f;
    public int MoveSteps { get; set; } = 1;
    public bool Raised { get; set; } = false;
    public bool Selected { get; set; } = false;
    private float _tween = 1f;

    public abstract void LoadContent(ContentManager content);

    public abstract void Execute(Snake snake);

    public Card()
    {
        _oldPosition = new Vector2(512, 800);
        _newPosition = new Vector2(512, 800);
    }

    public void MoveTo(Vector2 position)
    {
        _tween = 0f;
        _oldPosition = _newPosition;
        _newPosition = position;
    }

    public void Update(GameTime gameTime)
    {
        if (_tween < 1f)
        {
            _tween += (float)gameTime.ElapsedGameTime.TotalSeconds/0.15f;
        }
        if (_tween > 1f) _tween = 1f;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, int index)
    {
        float scale = Selected ? 1f : 0.9f;
        Color color = index < 5 ? Color.White : new Color(192, 192, 192);
        spriteBatch.Draw(Texture, new Vector2(0, 576) + Vector2.Lerp(_oldPosition, _newPosition, _tween), null, color, Rotation, new Vector2(64, 96), scale, SpriteEffects.None, 0);
    }
}