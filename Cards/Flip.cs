using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace snakebot;

public class Flip : Card
{
    private Texture2D _texture;
    public override Texture2D Texture => _texture;

    public override void LoadContent(ContentManager content)
    {
        _texture = content.Load<Texture2D>("card_flip");
    }

    public override void Execute(Snake snake)
    {
        snake.Flip();
        MoveSteps--;
    }
}