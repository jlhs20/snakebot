using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace snakebot;

public class Cards
{    
    private Card[] _hand = new Card[8];
    private MathHelper.Random _random; 
    private KeyboardState _oldKeyboardState;
    private int _selectedIndex = 0;
    private int _cardsRaised = 0;
    private float _timer = 0f;
    private Snake _snake;
    private bool _running = false;
    private ContentManager _content;
    private const int _runLength = 5;

    public Cards(Snake snake)
    {
        _random = new();
        _snake = snake;
        Shuffle();
        _hand[0].Selected = true;
        _oldKeyboardState = Keyboard.GetState();
    }

    private void Shuffle()
    {
        // in the future I would like to have a deck that is added to and shuffled 
        // but for now we'll just pick randomly
        for (int i = 0; i < _hand.Length; i++)
        {
            int t = _random.Next(0, 5);
            switch (t)
            {
                case 0:
                    _hand[i] = new Forward1();
                    break;
                case 1:
                    _hand[i] = new Forward2();
                    break;
                case 2:
                    _hand[i] = new Left();
                    break;
                case 3:
                    _hand[i] = new Right();
                    break;
                case 4:
                    _hand[i] = new Flip(); // in the future this should be a card you have to buy from the shop
                    break;
            }

            _hand[i].MoveTo(new Vector2(64 + 128*i, 96));
        }
    }

    private void Swap(int i, int j)
    {
        Card tempCard = _hand[j];
        Vector2 tempPos = _hand[j].Position;

        _hand[j].MoveTo(new Vector2(_hand[i].Position.X, _hand[j].Position.Y));
        _hand[i].MoveTo(new Vector2(tempPos.X, _hand[i].Position.Y));

        _hand[j] = _hand[i];
        _hand[i] = tempCard;
    }

    public void LoadContent(ContentManager content)
    {
        _content = content;
        content.Load<Texture2D>("card_forward_1");
        content.Load<Texture2D>("card_forward_2");
        content.Load<Texture2D>("card_left");
        content.Load<Texture2D>("card_right");
        content.Load<Texture2D>("card_flip");

        for (int i = 0; i < _hand.Length; i++)
        {
            _hand[i].LoadContent(content);
        }
    }

    public void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();

        if (!_running && !_snake.Dead)
        {
            if (keyboardState.IsKeyDown(Keys.Right) && !_oldKeyboardState.IsKeyDown(Keys.Right)) 
            {
                if (_selectedIndex < _hand.Length - 1) 
                {
                    if (_hand[_selectedIndex].Raised)
                    {
                        Swap(_selectedIndex, _selectedIndex + 1);
                    }

                    _hand[_selectedIndex].Selected = false;
                    _selectedIndex++;
                    _hand[_selectedIndex].Selected = true;
                }
                else
                {
                    if (_hand[_selectedIndex].Raised)
                    {
                        Swap(_selectedIndex, 0);
                    }

                    _hand[_selectedIndex].Selected = false;
                    _selectedIndex = 0;
                    _hand[_selectedIndex].Selected = true;
                }
            }
            else if (keyboardState.IsKeyDown(Keys.Up) && !_oldKeyboardState.IsKeyDown(Keys.Up)) 
            {
                if (_cardsRaised < 4 && !_hand[_selectedIndex].Raised)
                {
                    _hand[_selectedIndex].Raised = true;
                    _cardsRaised++;
                    _hand[_selectedIndex].MoveTo(_hand[_selectedIndex].Position + new Vector2(0, -64));
                }
            }
            else if (keyboardState.IsKeyDown(Keys.Left) && !_oldKeyboardState.IsKeyDown(Keys.Left)) 
            {
                if (_selectedIndex > 0) 
                {
                    if (_hand[_selectedIndex].Raised)
                    {
                        Swap(_selectedIndex, _selectedIndex - 1);
                    }

                    _hand[_selectedIndex].Selected = false;
                    _selectedIndex--;
                    _hand[_selectedIndex].Selected = true;
                }
                else
                {
                    if (_hand[_selectedIndex].Raised)
                    {
                        Swap(_selectedIndex, _hand.Length - 1);
                    }

                    _hand[_selectedIndex].Selected = false;
                    _selectedIndex = _hand.Length - 1;
                    _hand[_selectedIndex].Selected = true;
                }
            }
            else if (keyboardState.IsKeyDown(Keys.Down) && !_oldKeyboardState.IsKeyDown(Keys.Down)) 
            {
                if (_cardsRaised > 0 && _hand[_selectedIndex].Raised)
                {
                    _hand[_selectedIndex].Raised = false;
                    _cardsRaised--;
                    _hand[_selectedIndex].MoveTo(_hand[_selectedIndex].Position + new Vector2(0, 64));
                }
            }
            if (keyboardState.IsKeyDown(Keys.Enter) && !_oldKeyboardState.IsKeyDown(Keys.Enter)) 
            {
                _running = true;
                _hand[_selectedIndex].Selected = false;
                _selectedIndex = 0;
                _hand[_selectedIndex].Selected = true;
                _hand[_selectedIndex].Execute(_snake);
                _timer = 0.1f;
            }
        }

        if (_running && !_snake.Dead)
        {
            if (_timer <= 0f)
            {
                if (_selectedIndex < _runLength)
                {
                    if (_hand[_selectedIndex].MoveSteps > 0)
                    {
                        _hand[_selectedIndex].Execute(_snake);
                        _timer = 0.1f;
                    }
                    else
                    {
                        _hand[_selectedIndex].Selected = false;
                        _selectedIndex++;
                        if (_selectedIndex < _runLength) _hand[_selectedIndex].Selected = true;
                        _timer = 0.1f;
                    }
                }
                else if (_selectedIndex == _runLength)
                {
                    for (int i = 0; i < _hand.Length; i++)
                    {
                        _hand[i].MoveTo(new Vector2(512, 800));
                    }
                    _selectedIndex++;
                    _timer = 0.2f;
                }
                else
                {
                    _running = false;
                    _timer = 0f;
                    Shuffle();
                    LoadContent(_content); // kinda hacky solution
                    _selectedIndex = 0;
                    _hand[0].Selected = true;
                }
            }
            else _timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        for (int i = 0; i < _hand.Length; i++)
        {
            _hand[i].Update(gameTime);
        }

        _oldKeyboardState = keyboardState;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _hand.Length; i++)
        {
            _hand[i].Draw(gameTime, spriteBatch, i);
        }
    }
}