using NewMath;
using SixLabors.Fonts;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Progrimage.Utils
{
    public struct LetterRect
    {
        public int X, Y, Width, Height, CenterX, CharWidth;

        #region Properties
        public int2 Pos
        {
            get => new int2(X, Y);
            set
            {
                X = value.x;
                Y = value.y;
            }
        }

        public int2 Size
        {
            get => new int2(Width, Height);
            set
            {
                Width = value.x;
                Height = value.y;
            }
        }

        public int Right
        {
            get => X + Width - 1;
            set => Width = value - X + 1;
        }

        public int Bottom
        {
            get => Y + Height - 1;
            set => Height = value - Y + 1;
        }
        #endregion

        #region Constructors
        public LetterRect(int x, int y, int width, int height, int charWidth)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            CenterX = X + Width / 2;
            CharWidth = charWidth;
        }

        public LetterRect(int2 pos, int width, int height, int charWidth)
        {
            X = pos.x;
            Y = pos.y;
            Width = width;
            Height = height;
            CenterX = X + Width / 2;
            CharWidth = charWidth;
        }

        public LetterRect(int x, int y, int2 size, int charWidth)
        {
            X = x;
            Y = y;
            Width = size.x;
            Height = size.y;
            CenterX = X + Width / 2;
            CharWidth = charWidth;
        }

        public LetterRect(int2 pos, int2 size, int charWidth)
        {
            X = pos.x;
            Y = pos.y;
            Width = size.x;
            Height = size.y;
            CenterX = X + Width / 2;
            CharWidth = charWidth;
        }
        #endregion

        #region Public Methods
        public void AutoSetCenterX()
        {
            CenterX = X + Width / 2;
        }

        public bool Contains(int2 pos)
        {
            return pos.x >= X && pos.y >= Y && pos.x < X + Width && pos.y < Y + Height;
        }

        public bool OnLeft(int2 pos)
        {
            return CenterX > pos.x;
        }

        public bool Overlaps(LetterRect rect)
        {
            return X < rect.X + rect.Width &&
                Y < rect.Y + rect.Height &&
                rect.X < X + Width &&
                rect.Y < Y + Height;
        }
        #endregion

        #region Conversions
        public static implicit operator LetterRect(Rectangle rect) => new LetterRect(rect.X, rect.Y, rect.Width, rect.Height, rect.Width);
        public static implicit operator Rectangle(LetterRect rect) => new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        public static implicit operator FontRectangle(LetterRect rect) => new FontRectangle(rect.X, rect.Y, rect.Width, rect.Height);
        public static implicit operator LetterRect(FontRectangle rect)
        {
            int x = Math2.RoundToInt(rect.X);
            int y = Math2.RoundToInt(rect.Y);
            return new LetterRect(x, y, Math2.RoundToInt(rect.X + rect.Width) - x + 1, Math2.RoundToInt(rect.Y + rect.Height) - y + 1, (int)rect.Width);
        }
        #endregion
    }
}
