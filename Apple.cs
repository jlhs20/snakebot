using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace snakebot;

public class Apple
{
    private Texture2D _texture;
    private Model _model;
    private float _angle = 0f;
    private const float _modelScale = 1f;
    private const int _cellSize = 64;

    public (int, int) Position;
    
    public Apple((int, int) position)
    {
        Position = position;
    }
    
    public void LoadContent(ContentManager content)
    {
        // using model from https://www.thingiverse.com/thing:22725
        _texture = content.Load<Texture2D>("apple");
        _model = content.Load<Model>("appul");
    }

    public void Update(GameTime gameTime)
    {
        _angle = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds*0.75f)*0.3f;
    }

    public void Draw(GameTime gameTime, GraphicsDevice graphicsDevice)
    {
        var viewport = graphicsDevice.Viewport;
        Matrix projection = Matrix.CreateOrthographicOffCenter(
            0, viewport.Width, viewport.Height, 0, -1000f, 1000f);
        Matrix view = Matrix.Identity;

        var center = new Vector3(
            Position.Item1 * _cellSize + _cellSize / 2f,
            Position.Item2 * _cellSize + _cellSize / 2f,
            0f);

        Matrix world =
            Matrix.CreateScale(_modelScale, -_modelScale, _modelScale) *
            Matrix.CreateRotationZ(_angle + (float)Math.PI/2) *
            Matrix.CreateTranslation(center);

        graphicsDevice.DepthStencilState = DepthStencilState.Default;
        graphicsDevice.BlendState = BlendState.Opaque;
        graphicsDevice.RasterizerState = RasterizerState.CullClockwise;
        graphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

        foreach (ModelMesh mesh in _model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.World = world;
                effect.View = view;
                effect.Projection = projection;
                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
            }
            mesh.Draw();
        }
    }
}