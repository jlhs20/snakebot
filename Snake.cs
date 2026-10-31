using System;
using System.Collections.Generic;
using System.IO.Compression;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace snakebot;

public enum Direction
{
    None, Right, Up, Left, Down
}

public class Snake
{
    private const float finalR = 0.95f;
    private const float finalG = 0.65f;
    private const float finalB = 0.1f;
    private int[] vertexOrder = new[] {0, 1, 1, 0, 0, 1};

    private (int, int)[] _segments;
    private (int, int)[] _oldSegments;
    private Texture2D _body;
    private Texture2D _head;
    private Texture2D _blink;
    private Texture2D _tail;
    private Texture2D _yum;
    private Texture2D _dead;
    private double _blinkTimer;
    private double _yumTimer;
    private int _blinkFrames = 0;
    private MathHelper.Random _random;
    private float _tween = 1f;

    public (int, int)[] Segments => _segments;
    public bool Dead { get; set; } = false;

    public Snake((int, int)[] segments)
    {
        _segments = segments;
        _oldSegments = segments;
        _random = new MathHelper.Random();
        _blinkTimer = _random.NextFloat()*5.0f + 3.0f;
    }

    public void LoadContent(ContentManager content)
    {
        _body = content.Load<Texture2D>("snaketiles");
        _head = content.Load<Texture2D>("head");
        _tail = content.Load<Texture2D>("tail");
        _blink = content.Load<Texture2D>("blink");
        _yum = content.Load<Texture2D>("yum");
        _dead = content.Load<Texture2D>("dead");
    }

    public void Update(GameTime gameTime)
    {
        _blinkTimer -= gameTime.ElapsedGameTime.TotalSeconds;
        if (_blinkTimer < 0f)
        {
            _blinkTimer = _random.NextFloat()*5.0f + 3.0f;
            _blinkFrames = _random.Next(7, 15);
        }
        if (_blinkFrames > 0) _blinkFrames--;
        
        if (_tween < 1f)
        {
            _tween += (float)gameTime.ElapsedGameTime.TotalSeconds/0.15f;
        }
        if (_tween > 1f) _tween = 1f;

        if (_yumTimer > 0) _yumTimer -= gameTime.ElapsedGameTime.TotalSeconds;
    }

    public void Grow()
    {
        (int, int)[] newSegments = new (int, int)[_segments.Length + 1];
        _segments.CopyTo(newSegments, 0);
        newSegments[_segments.Length] = _oldSegments[_segments.Length - 1];
        _segments = newSegments;

        _yumTimer = 1f;
    }

    public void Move(Direction direction)
    {
        _oldSegments = new (int, int)[_segments.Length];
        _segments.CopyTo(_oldSegments, 0);

        //(int, int) tail = _segments[_segments.Length - 1];
        //if (grow)
        //{
        //    _segments = new (int, int)[_segments.Length+1];
        //    _oldSegments.CopyTo(_segments, 0);
        //    _segments[_segments.Length-1] = tail;
        //}

        for (int i = _segments.Length-1; i > 0; i--)
        {
            _segments[i] = _segments[i-1];
        }

        switch (direction)
        {
            case Direction.Right:
                _segments[0].Item1++;
                break;
            case Direction.Up:
                _segments[0].Item2--;
                break;
            case Direction.Left:
                _segments[0].Item1--;
                break;
            case Direction.Down:
                _segments[0].Item2++;
                break;
        }

        _tween = 0f;
    }

    private static float LerpAngle(float current, float target, float amount)
    {
        float difference = target - current;
        while (difference < -MathHelper.Pi) difference += MathHelper.TwoPi;
        while (difference > MathHelper.Pi) difference -= MathHelper.TwoPi;

        return current + difference * amount;
    }

    public VertexPositionColorTexture[] IndexToVertices((int, int)[] segments, int i)
    {
        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[] {};

        float frontRadius = 10f - 10f/(segments.Length-2)*(i-1) + 18;
        float backRadius = 10f - 10f/(segments.Length-2)*i + 18;

        float frontR = 1f - (1f - finalR)/(segments.Length-2)*(i-1);
        float frontG = 1f - (1f - finalG)/(segments.Length-2)*(i-1);
        float frontB = 1f - (1f - finalB)/(segments.Length-2)*(i-1);
        float backR = 1f - (1f - finalR)/(segments.Length-2)*i;
        float backG = 1f - (1f - finalG)/(segments.Length-2)*i;
        float backB = 1f - (1f - finalB)/(segments.Length-2)*i;
        Color frontColor = new Color(frontR, frontG, frontB); // towards head
        Color backColor = new Color(backR, backG, backB); // towards tail
        
        // directions from this tile toward its head / tail neighbours, e.g. (0,-1) = up
        Vector2 headDir = new Vector2(segments[i-1].Item1 - segments[i].Item1, segments[i-1].Item2 - segments[i].Item2);
        Vector2 tailDir = new Vector2(segments[i+1].Item1 - segments[i].Item1, segments[i+1].Item2 - segments[i].Item2);
        Vector2 origin = new Vector2(segments[i].Item1*64, segments[i].Item2*64);
        float cross = headDir.X*tailDir.Y - headDir.Y*tailDir.X;

        // tile-local position -> vertex; texture coordinate is the local position / 256
        VertexPositionColorTexture Vert(Vector2 local, Color color) =>
            new(new Vector3(origin + local, 0), color, (origin + local)/256);

        
        List<VertexPositionColorTexture> vertexList = new();

        // straight (head and tail are on opposite sides of this tile)
        if (headDir + tailDir == Vector2.Zero)
        {
            Vector2 perp = new Vector2(-headDir.Y, headDir.X);
            Vector2 headEdge = new Vector2(32, 32) + headDir*32; // midpoint of the tile edge facing the head
            Vector2 tailEdge = new Vector2(32, 32) - headDir*32; // midpoint of the tile edge facing the tail

            Vector2 headA = headEdge + perp*frontRadius;
            Vector2 headB = headEdge - perp*frontRadius;
            Vector2 tailA = tailEdge + perp*backRadius;
            Vector2 tailB = tailEdge - perp*backRadius;

            for (int j = 0; j < 8; j++) // split into 8 quads to allow tweening with curves
            {
                Color subBackColor = Color.Lerp(backColor, frontColor, j/8f); 
                Color subFrontColor = Color.Lerp(backColor, frontColor, (j+1)/8f); 

                vertexList.Add(Vert(Vector2.Lerp(tailA, headA, j/8f), subBackColor));
                vertexList.Add(Vert(Vector2.Lerp(tailB, headB, j/8f), subBackColor));
                vertexList.Add(Vert(Vector2.Lerp(tailB, headB, (j+1)/8f), subFrontColor));

                vertexList.Add(Vert(Vector2.Lerp(tailA, headA, (j+1)/8f), subFrontColor));
                vertexList.Add(Vert(Vector2.Lerp(tailA, headA, j/8f), subBackColor));
                vertexList.Add(Vert(Vector2.Lerp(tailB, headB, (j+1)/8f), subFrontColor));
            }
        }
        
        // curve: quarter-ellipse around the tile corner shared by the head and tail edges
        // eight quads / sixteen triangles, with the colour gradient divided across them
        else
        { 
            // the tile corner both edges touch, in tile-local coordinates
            Vector2 corner = new Vector2(32, 32) + 32f*(tailDir + headDir);

            // at the tail end the cross-section lies along headDir (width = backRadius);
            // at the head end it lies along tailDir (width = frontRadius)
            float tailInner = 32 - backRadius,  tailOuter = 32 + backRadius;
            float headInner = 32 - frontRadius, headOuter = 32 + frontRadius;

            // angle 0 = tail edge midpoint, pi/2 = head edge midpoint
            Vector2 ArcPoint(float angle, float radiusAlongHeadDir, float radiusAlongTailDir) =>
                corner
                - headDir * (radiusAlongHeadDir * (float)Math.Cos(angle))
                - tailDir * (radiusAlongTailDir * (float)Math.Sin(angle));

            for (int j = 0; j < 8; j++)
            {
                float a0 = j/8f * (float)Math.PI/2;
                float a1 = (j+1)/8f * (float)Math.PI/2;

                Vector2 inner0 = ArcPoint(a0, tailInner, headInner);
                Vector2 outer0 = ArcPoint(a0, tailOuter, headOuter);
                Vector2 inner1 = ArcPoint(a1, tailInner, headInner);
                Vector2 outer1 = ArcPoint(a1, tailOuter, headOuter);

                Color subBackColor = Color.Lerp(backColor, frontColor, j/8f); 
                Color subFrontColor = Color.Lerp(backColor, frontColor, (j+1)/8f); 
                
                if (cross > 0)
                {
                    vertexList.Add(Vert(inner0, subBackColor));
                    vertexList.Add(Vert(outer0, subBackColor));
                    vertexList.Add(Vert(outer1, subFrontColor));

                    vertexList.Add(Vert(inner1, subFrontColor));
                    vertexList.Add(Vert(inner0, subBackColor));
                    vertexList.Add(Vert(outer1, subFrontColor));
                }
                else
                {
                    vertexList.Add(Vert(outer0, subBackColor));
                    vertexList.Add(Vert(inner0, subBackColor));
                    vertexList.Add(Vert(inner1, subFrontColor));

                    vertexList.Add(Vert(outer1, subFrontColor));
                    vertexList.Add(Vert(outer0, subBackColor));
                    vertexList.Add(Vert(inner1, subFrontColor));
                }
            }
        }
        
        vertices = vertexList.ToArray();

        return vertices;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        // draw tail
        float newTailRotation = 0;
        float oldTailRotation = 0;
        if (_oldSegments[_oldSegments.Length - 1].Item2 < _oldSegments[_oldSegments.Length - 2].Item2) oldTailRotation = (float)-Math.PI/2;
        if (_oldSegments[_oldSegments.Length - 1].Item1 < _oldSegments[_oldSegments.Length - 2].Item1) oldTailRotation = (float)Math.PI;
        if (_oldSegments[_oldSegments.Length - 1].Item2 > _oldSegments[_oldSegments.Length - 2].Item2) oldTailRotation = (float)Math.PI/2;
        if (_segments[_segments.Length - 1].Item2 < _segments[_segments.Length - 2].Item2) newTailRotation = (float)-Math.PI/2;
        if (_segments[_segments.Length - 1].Item1 < _segments[_segments.Length - 2].Item1) newTailRotation = (float)Math.PI;
        if (_segments[_segments.Length - 1].Item2 > _segments[_segments.Length - 2].Item2) newTailRotation = (float)Math.PI/2;

        Vector2 oldTailPosition = new Vector2(_oldSegments[_oldSegments.Length - 1].Item1*64 + 32, _oldSegments[_oldSegments.Length - 1].Item2*64 + 32);
        Vector2 newTailPosition = new Vector2(_segments[_segments.Length - 1].Item1*64 + 32, _segments[_segments.Length - 1].Item2*64 + 32);

        Vector2 tailPosition = Vector2.Lerp(oldTailPosition, newTailPosition, _tween);
        float tailRotation = LerpAngle(oldTailRotation, newTailRotation, _tween);

        spriteBatch.Draw(_tail, tailPosition, null, new Color(finalR, finalG, finalB), tailRotation, new Vector2(32, 32), 1, SpriteEffects.None, 1);

        spriteBatch.End();
        graphicsDevice.RasterizerState = RasterizerState.CullNone;
        graphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

        BasicEffect basicEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = true,
            VertexColorEnabled = true,
            Texture = _body,
            Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 0, 1)
        };

        // draw body
        // 56 pixels wide at head, 36 pixels wide at tail = narrows by 20 over length of body
        // _segments.Length - 1 divisions of that narrowing

        // draw second to last segment if snake grew
        if (_segments.Length == _oldSegments.Length + 1)
        {
            VertexPositionColorTexture[] newVertices = IndexToVertices(_segments, _segments.Length - 2);
            List<VertexPositionColorTexture> vertexList = new();
            for (int j = 0; j < newVertices.Length; j++)
            {
                VertexPositionColorTexture oldVertex = newVertices[vertexOrder[j%6]];
                vertexList.Add(new(
                    Vector3.Lerp(oldVertex.Position, newVertices[j].Position, _tween),
                    Color.Lerp(oldVertex.Color, newVertices[j].Color, _tween),
                    Vector2.Lerp(oldVertex.TextureCoordinate, newVertices[j].TextureCoordinate, _tween)
                ));
            }

            VertexPositionColorTexture[] vertices = vertexList.ToArray();

            // render
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                if (vertices.Length > 0)
                {
                    pass.Apply();

                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.TriangleList,
                        vertices,
                        0,
                        vertices.Length / 3
                    );
                }
            }
        }

        // draw rest of the snake
        for (int i = _oldSegments.Length - 2; i > 0; i--)
        {
            List<VertexPositionColorTexture> vertexList = new();
            VertexPositionColorTexture[] oldVertices = IndexToVertices(_oldSegments, i);
            VertexPositionColorTexture[] newVertices = IndexToVertices(_segments, i);
            
            for (int j = 0; j < newVertices.Length; j++)
            {
                vertexList.Add(new(
                    Vector3.Lerp(oldVertices[j].Position, newVertices[j].Position, _tween),
                    Color.Lerp(oldVertices[j].Color, newVertices[j].Color, _tween),
                    Vector2.Lerp(oldVertices[j].TextureCoordinate, newVertices[j].TextureCoordinate, _tween)
                ));
            }

            VertexPositionColorTexture[] vertices = vertexList.ToArray();

            // render
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                if (vertices.Length > 0)
                {
                    pass.Apply();

                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.TriangleList,
                        vertices,
                        0,
                        vertices.Length / 3
                    );
                }
            }
        }


        spriteBatch.Begin();

        // draw head
        Texture2D head = _head;
        if (_blinkFrames > 0) head = _blink;
        if (_yumTimer > 0f) head = _yum;
        if (Dead) head = _dead;

        float newHeadRotation = 0;
        float oldHeadRotation = 0;
        if (_oldSegments[1].Item2 == _oldSegments[0].Item2 - 1) oldHeadRotation = (float)-Math.PI/2;
        if (_oldSegments[1].Item1 == _oldSegments[0].Item1 - 1) oldHeadRotation = (float)Math.PI;
        if (_oldSegments[1].Item2 == _oldSegments[0].Item2 + 1) oldHeadRotation = (float)Math.PI/2;
        if (_segments[1].Item2 == _segments[0].Item2 - 1) newHeadRotation = (float)-Math.PI/2;
        if (_segments[1].Item1 == _segments[0].Item1 - 1) newHeadRotation = (float)Math.PI;
        if (_segments[1].Item2 == _segments[0].Item2 + 1) newHeadRotation = (float)Math.PI/2;

        Vector2 oldHeadPosition = new Vector2(_oldSegments[0].Item1*64 + 32, _oldSegments[0].Item2*64 + 32);
        Vector2 newHeadPosition = new Vector2(_segments[0].Item1*64 + 32, _segments[0].Item2*64 + 32);

        Vector2 headPosition = Vector2.Lerp(oldHeadPosition, newHeadPosition, _tween);
        float headRotation = LerpAngle(oldHeadRotation, newHeadRotation, _tween);
        
        spriteBatch.Draw(head, headPosition, null, Color.White, headRotation, new Vector2(24, 32), 1, SpriteEffects.None, 1);
    }
}