﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pokemon3D.GameCore;
using Pokemon3D.Common.Input;
using Pokemon3D.UI.Transitions;
using Pokemon3D.UI.Framework;

namespace Pokemon3D.UI.Screens
{
    class MainMenuScreen2 : GameObject, Screen
    {
        private ControlGroup _buttons = new ControlGroup();
        private HexagonBackground _hexagons = new HexagonBackground();

        public void OnOpening(object enterInformation)
        {
            Game.GraphicsDeviceManager.PreferMultiSampling = true;
            Game.GraphicsDeviceManager.ApplyChanges();

            _buttons.Add(new LeftSideButton(_buttons, "Start new game", new Vector2(26, 45), b =>
            {
                Game.ScreenManager.SetScreen(typeof(GameModeLoadingScreen), typeof(BlendTransition));
            }));
            _buttons.Add(new LeftSideButton(_buttons, "Load game", new Vector2(26, 107), null)
            {
                Enabled = false
            });
            _buttons.Add(new LeftSideButton(_buttons, "GameJolt", new Vector2(26, 169), null));
            _buttons.Add(new LeftSideButton(_buttons, "Options", new Vector2(26, 231), null));
            _buttons.Add(new LeftSideButton(_buttons, "Exit game", new Vector2(26, 293), (b) =>
            {
                Game.ScreenManager.NotifyQuitGame();
            }));
            _buttons.Add(new LeftSideCheckbox(_buttons, "Checkbox test", new Vector2(26, 355), null));
        }
        
        public void OnClosing()
        {

        }

        public void OnDraw(GameTime gameTime)
        {
            Game.GraphicsDevice.Clear(Color.LightGray);
            
            Game.SpriteBatch.Begin(blendState: BlendState.NonPremultiplied);

            _hexagons.Draw(Game.SpriteBatch);

            Game.SpriteBatch.End();

            Game.SpriteBatch.Begin(samplerState: SamplerState.PointWrap, blendState: BlendState.AlphaBlend);

            _buttons.Draw();

            Game.ShapeRenderer.DrawFilledRectangle(0, Game.ScreenBounds.Height - 64, Game.ScreenBounds.Width, 64, Color.White);

            Game.SpriteBatch.Draw(Game.Content.Load<Texture2D>(ResourceNames.Textures.UI.GamePadButtons.Button_A), new Rectangle(11, Game.ScreenBounds.Height - 48, 32, 32), new Color(100, 193, 238));
            Game.SpriteBatch.DrawString(Game.Content.Load<SpriteFont>(ResourceNames.Fonts.BigFont), "Select", new Vector2(56, Game.ScreenBounds.Height - 48), new Color(100, 193, 238));

            Game.SpriteBatch.End();
        }

        public void OnUpdate(float elapsedTime)
        {
            _buttons.Update();
            _hexagons.Update();
        }

        private class ControlGroup : List<Control>
        {
            public void FocusChange(int change)
            {
                if (!this.Any(x => x.Selected))
                {
                    this[0].Select();
                }
                else
                {
                    int currentIndex = IndexOf(this.Single(x => x.Selected));
                    currentIndex += change;
                    while (currentIndex < 0)
                        currentIndex += Count;
                    while (currentIndex >= Count)
                        currentIndex -= Count;

                    this[currentIndex].Select();
                }
            }

            public void Update()
            {
                if (GameController.Instance.InputSystem.Up(true, DirectionalInputTypes.All))
                {
                    FocusChange(-1);
                }
                if (GameController.Instance.InputSystem.Down(true, DirectionalInputTypes.All))
                {
                    FocusChange(1);
                }

                ForEach(b => b.Update());
            }

            public void Draw()
            {
                ForEach(b => b.Draw());
            }
        }

        private class ColorStepper
        {
            private Color _color;
            private float _speedValue;

            public Color Color
            {
                get { return _color; }
            }

            public Color TargetColor { get; set; }

            public ColorStepper(Color startColor, float speedValue)
            {
                _color = startColor;
                TargetColor = startColor;
                _speedValue = speedValue;
            }

            public void Update()
            {
                if (_color.R != TargetColor.R)
                    _color.R = (byte)MathHelper.Clamp(MathHelper.SmoothStep(TargetColor.R, _color.R, _speedValue), 0, 255);
                if (_color.G != TargetColor.G)
                    _color.G = (byte)MathHelper.Clamp(MathHelper.SmoothStep(TargetColor.G, _color.G, _speedValue), 0, 255);
                if (_color.B != TargetColor.B)
                    _color.B = (byte)MathHelper.Clamp(MathHelper.SmoothStep(TargetColor.B, _color.B, _speedValue), 0, 255);
            }
        }

        private class OffsetStepper
        {
            public float Offset { get; private set; }

            public float TargetOffset { get; set; }

            private float _speedValue;

            public OffsetStepper(float offset, float speedValue)
            {
                Offset = offset;
                TargetOffset = offset;
                _speedValue = speedValue;
            }

            public void Update()
            {
                Offset = MathHelper.SmoothStep(TargetOffset, Offset, _speedValue);
                if (Math.Abs(Offset - TargetOffset) < 0.1f)
                    Offset = TargetOffset;
            }
        }

        abstract private class Control : GameObject
        {
            protected ControlGroup Group { get; private set; }
            public bool Selected { get; protected set; }

            public Control(ControlGroup group)
            {
                Group = group;
                Selected = false;
            }

            public virtual void Update()
            {
                if (Game.InputSystem.Mouse.HasMoved)
                    if (GetBounds().Contains(Game.InputSystem.Mouse.Position))
                        Select();
            }

            public abstract void Draw();

            public abstract Rectangle GetBounds();

            public virtual void Deselect()
            {
                Selected = false;
            }

            public virtual void Select()
            {
                Selected = true;
                Group.ForEach(x =>
                {
                    if (x != this)
                        x.Deselect();
                });
            }
        }

        private class LeftSideButton : Control
        {
            private Texture2D _texture;
            private SpriteFont _font;

            private Vector2 _initialPosition;
            private Action<Control> _onClick;

            public string Text { get; set; }

            private ColorStepper _colorStepper;
            private OffsetStepper _offsetStepper;

            public bool Enabled { get; set; } = true;

            public LeftSideButton(ControlGroup group, string text, Vector2 position, Action<Control> onClick) : base(group)
            {
                _texture = Game.Content.Load<Texture2D>(ResourceNames.Textures.UI.Common.Button_Blank);
                _font = Game.Content.Load<SpriteFont>(ResourceNames.Fonts.NormalFont);

                Text = text;
                _initialPosition = position;
                _onClick = onClick;

                _colorStepper = new ColorStepper(new Color(255, 255, 255), 0.5f);
                _offsetStepper = new OffsetStepper(0f, 0.5f);
            }

            public override Rectangle GetBounds()
            {
                return new Rectangle((int)(_initialPosition.X + _offsetStepper.Offset), (int)_initialPosition.Y, 200, 38);
            }

            public override void Update()
            {
                base.Update();

                if (_onClick != null && Selected)
                {
                    if (Game.InputSystem.Accept(AcceptInputTypes.Buttons) ||
                        GetBounds().Contains(Game.InputSystem.Mouse.Position) && Game.InputSystem.Accept(AcceptInputTypes.LeftClick))
                    {
                        _onClick(this);
                    }
                }

                _offsetStepper.Update();
                _colorStepper.Update();
            }

            public override void Select()
            {
                base.Select();

                _offsetStepper.TargetOffset = 26;
                if (Enabled)
                {
                    _colorStepper.TargetColor = new Color(100, 193, 238);
                }
                else
                {
                    _colorStepper.TargetColor = new Color(210, 210, 210);
                }
            }

            public override void Deselect()
            {
                base.Deselect();

                _offsetStepper.TargetOffset = 0;
                _colorStepper.TargetColor = new Color(255, 255, 255);
            }

            public override void Draw()
            {
                Game.SpriteBatch.Draw(_texture, GetBounds(), _colorStepper.Color);
                Game.SpriteBatch.DrawString(_font, Text, new Vector2(_initialPosition.X + _offsetStepper.Offset + 24, _initialPosition.Y + 5), Color.Black);
            }
        }

        private class LeftSideCheckbox : Control
        {
            public string Text { get; set; }

            private Texture2D _texture;
            private Texture2D _backTexture;
            private Texture2D _markTexture;
            private SpriteFont _font;

            private Action<Control> _onClick;
            private Vector2 _position;

            private ColorStepper _colorStepper;
            private OffsetStepper _offsetStepper;
            private ColorStepper _markColorStepper;

            public bool Checked { get; set; }
            private int _checkSize = 0;

            public LeftSideCheckbox(ControlGroup group, string text, Vector2 position, Action<Control> onClick) : base(group)
            {
                _texture = Game.Content.Load<Texture2D>(ResourceNames.Textures.UI.Common.Button_Blank);
                _backTexture = Game.Content.Load<Texture2D>(ResourceNames.Textures.UI.Common.Checkbox_Back);
                _markTexture = Game.Content.Load<Texture2D>(ResourceNames.Textures.UI.Common.Checkbox_Mark);
                _font = Game.Content.Load<SpriteFont>(ResourceNames.Fonts.NormalFont);
                _position = position;

                Text = text;
                _onClick = onClick;

                _colorStepper = new ColorStepper(Color.White, 0.5f);
                _markColorStepper = new ColorStepper(Color.Black, 0.5f);
                _offsetStepper = new OffsetStepper(0f, 0.5f);
            }

            public override Rectangle GetBounds()
            {
                if (Text.Length > 0)
                {
                    return new Rectangle((int)(_position.X + _offsetStepper.Offset), (int)_position.Y, 200, 38);
                }
                else
                {
                    return new Rectangle((int)(_position.X + _offsetStepper.Offset), (int)_position.Y, 42, 38);
                }
            }

            public override void Update()
            {
                base.Update();
                
                if (Selected)
                {
                    if (Game.InputSystem.Accept(AcceptInputTypes.Buttons) ||
                        GetBounds().Contains(Game.InputSystem.Mouse.Position) && Game.InputSystem.Accept(AcceptInputTypes.LeftClick))
                    {
                        Checked = !Checked;

                        if (_onClick != null)
                        {
                            _onClick(this);
                        }
                    }
                }

                if (Checked)
                {
                    if (_checkSize < 15)
                        _checkSize += 3;
                }
                else
                {
                    if (_checkSize > 0)
                        _checkSize -= 3;
                }

                _offsetStepper.Update();
                _colorStepper.Update();
                _markColorStepper.Update();
            }

            public override void Select()
            {
                base.Select();

                _offsetStepper.TargetOffset = 26;
                _colorStepper.TargetColor = new Color(100, 193, 238);
                _markColorStepper.TargetColor = Color.White;
            }

            public override void Deselect()
            {
                base.Deselect();

                _offsetStepper.TargetOffset = 0;
                _colorStepper.TargetColor = new Color(255, 255, 255);
                _markColorStepper.TargetColor = Color.Black;
            }

            public override void Draw()
            {
                if (Text.Length > 0)
                {
                    Game.SpriteBatch.Draw(_texture, GetBounds(), _colorStepper.Color);
                    Game.SpriteBatch.DrawString(_font, Text, new Vector2(_position.X + _offsetStepper.Offset + 42, _position.Y + 5), Color.Black);
                }

                Game.SpriteBatch.Draw(_backTexture, new Rectangle((int)(_position.X + _offsetStepper.Offset), (int)_position.Y, 42, 38), _colorStepper.Color);

                if (Checked || _checkSize > 0)
                    Game.SpriteBatch.Draw(_markTexture, 
                        new Rectangle((int)(_position.X + _offsetStepper.Offset + 19 - _checkSize / 2), (int)(_position.Y + 17 - _checkSize / 2), _checkSize, _checkSize),
                        _markColorStepper.Color);
            }
        }
    }
}
