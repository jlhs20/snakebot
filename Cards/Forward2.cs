using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace snakebot;

public class Forward2 : Card
{
    private Texture2D _texture;
    public override Texture2D Texture => _texture;

    public Forward2() : base()
    {
        MoveSteps = 2;
    }

    public override void LoadContent(ContentManager content)
    {
        _texture = content.Load<Texture2D>("card_forward_2");
    }

    public override void Execute(Snake snake)
    {
        Direction direction = Direction.None;
        if (snake.Segments[0].Item1 == snake.Segments[1].Item1 - 1) 
        {
            direction = Direction.Left;
        }
        if (snake.Segments[0].Item1 == snake.Segments[1].Item1 + 1) 
        {
            direction = Direction.Right;
        }
        if (snake.Segments[0].Item2 == snake.Segments[1].Item2 - 1) 
        {
            direction = Direction.Up;
        }
        if (snake.Segments[0].Item2 == snake.Segments[1].Item2 + 1) 
        {
            direction = Direction.Down;
        }
        snake.Move(direction);
        MoveSteps--;
    }
}