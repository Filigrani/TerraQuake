using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalloonInvasion
{
    internal class SpriteFont
    {
        public int LetterWidth = 9;
        public int LetterHeight = 14;
        public int Padding = 2;
        public string Chars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-+=#_><.,!?*&)(/\|;$^%:";
        public Dictionary<char, int> CharsIndexes = new Dictionary<char, int>();
        private bool IndexesReady = false;
        public string CurrentText = "";
        public List<int> LettersOffsets = new List<int>();
        public Vector2 Position = new Vector2(0, 0);
        public Texture2D Font;
        public Color TextColor = Color.White;
        public float Angle = 0;
        public static List<SpriteFont> SpriteFonts = new List<SpriteFont>();
        public SpriteFont()
        {
            SpriteFonts.Add(this);
        }
        public void Destory()
        {
            SpriteFonts.Remove(this);
        }

        private void InitIndexs()
        {
            CharsIndexes = new Dictionary<char, int>();

            for (int i = 0; i < Chars.Length; i++)
            {
                CharsIndexes.Add(Chars[i], i);
            }
            IndexesReady = true;
        }

        public int GetLetterIndex(char InputLetter)
        {
            if (!IndexesReady)
            {
                InitIndexs();
            }
            char Letter = char.ToUpper(InputLetter);
            int Index = -1;

            if (InputLetter == '\n')
            {
                return -2;
            }

            if (CharsIndexes.ContainsKey(Letter))
            {
                CharsIndexes.TryGetValue(Letter, out Index);
            }

            return Index;
        }
        public int GetLetterIndex(string Input)
        {
            return GetLetterIndex(Input[0]);
        }
        public int GetLetterOffset(int Index)
        {
            if (Index == -1)
            {
                return -1;
            }
            if (Index == -2)
            {
                return -2;
            }

            return Index * LetterWidth;
        }
        public int GetLetterOffset(char Input)
        {
            return GetLetterOffset(GetLetterIndex(Input));
        }
        public int GetLetterOffset(string Input)
        {
            return GetLetterOffset(GetLetterIndex(Input));
        }
        public void SetText(string Text)
        {
            if (CurrentText != Text)
            {
                LettersOffsets = new List<int>();
                foreach (char Letter in Text)
                {
                    int LetterOffset = GetLetterOffset(Letter);
                    LettersOffsets.Add(LetterOffset);
                }
                CurrentText = Text;
            }
        }
        public void Render(SpriteBatch _s)
        {
            if (LettersOffsets.Count > 0)
            {
                float StartX = Position.X;
                float StartY = Position.Y;
                float X = StartX;
                float Y = StartY;
                _s.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, null);
                foreach (int CurrentOffset in LettersOffsets)
                {
                    if (CurrentOffset >= 0)
                    {
                        Rectangle rect = new Rectangle(CurrentOffset, 0, LetterWidth, LetterHeight);

                        _s.Draw(Font, new Vector2(X, Y) * LayersManager.Scaler, rect, TextColor, 0, Vector2.Zero, LayersManager.Scaler, SpriteEffects.None, 0);
                    }
                    X = X + LetterWidth + Padding;

                    if (CurrentOffset == -2)
                    {
                        Y += LetterHeight + Padding;
                        X = StartX;
                    }
                }
                _s.End();
            }
        }
    }

}
