using NewMath;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections;
using Color = SixLabors.ImageSharp.Color;
using Font = SixLabors.Fonts.Font;
using SixLabors.Fonts;
using Progrimage.Utils;
using SixLabors.ImageSharp.Drawing.Processing;
using PointF = SixLabors.ImageSharp.PointF;
using ImageSharpExtensions;
using System;
using SystemFonts = SixLabors.Fonts.SystemFonts;

namespace Progrimage.Composites
{
    public class CompText : ICompositeAction
    {
        #region Fields
        // Public
        public string Text;
        public int2 MinBound, MaxBound;
        public Font Font;
        public Color Color;
        public readonly List<List<LetterRect>> LetterBoxes = new();
        public sbyte LastMoveDir = 0;
		#endregion

		#region Properties
		public int WrappedHeight { get; private set; }
        public string WrappedText { get; private set; }
		public Action? DisposalDelegate { get; private set; }
		public Composite Composite { get; private set; }

		public int2 Pos
        {
            get => MinBound;
            set
            {
                MaxBound += value - MinBound;
                MinBound = value;
            }
        }
        #endregion

        public CompText(int2 min, int2 max, Font font, Color color)
        {
            Text = "";
            MinBound = min;
            MaxBound = max;
            Font = font;
            Color = color;
            WrapText();
        }

        #region ICompositeAction Methods
        public void Init(Composite composite)
        {
            Composite = composite;
            composite.Name = "Text";
        }

        public IEnumerator Run(PositionedImage<Argb32> result)
        {
            if (WrappedText.Length == 1) yield break;
            int2 size = new int2(
                MaxBound.x - MinBound.x + 1,
                WrappedHeight
            );

            //int2 pos = Math2.Max(MinBound, 0);
            if (result.Image is null)
            {
                //pos = int2.Zero;
                Composite.Layer.Image.ExpandToContain(MinBound, int2.One);
                Composite.Layer.Changed();
			}

			result.ExpandToContain(MinBound + result.Pos, size);
            int2 pos = MinBound - result.Pos;
			result.Mutate(op => op.DrawText(WrappedText, Font, Color, new PointF(pos.x, pos.y)));
        }
        #endregion

        #region Public Methods
        public void Changed()
        {
            WrapText();
            ((ICompositeAction)this).Rerun();
        }

        public bool IsIndexShifted(int index, int lineIndex = -1)
        {
			if (LastMoveDir != -1) return false; // Moved the wrong way to be shifted
			if (index == 0) return true; // Cursor at beginning

			int2 pos;
            int lineCharIndex;
			if (index >= Text.Length) return false; // Cursor at end of text
			if (lineIndex == -1)
			{
                // Somewhere in the middle of text
                // lineIndex unspecified
				lineIndex = GetLineIndex(index);
				lineCharIndex = GetIndexInLine(index - 1, lineIndex);
			}
			else
			{
                // Somewhere in the middle of text
                // lineIndex given
				lineCharIndex = index;
			}

            if (lineCharIndex + 1 == LetterBoxes[lineIndex].Count && lineIndex + 1 < LetterBoxes.Count)
                return true;
            return false;
		}

        public int GetLineIndex(int charIndex)
        {
            if (charIndex == 0) return 0;
            int counter = 0;
            for (int line = 0; line < LetterBoxes.Count; line++)
            {
                counter += LetterBoxes[line].Count;
                if (counter >= charIndex) return line;
            }
            return -1;
        }

        public int GetIndexInLine(int charIndex, int lineIndex)
        {
			for (int i = 0; i < lineIndex; i++)
				charIndex -= LetterBoxes[i].Count;
            return charIndex;
		}

        public int2 GetCursorPos(int index, bool local = true, int lineIndex = -1)
        {
            if (index == 0) return local ? int2.Zero : MinBound; // Cursor at beginning

            int2 pos;
            List<LetterRect> line;
            int lineCharIndex;
            if (index >= Text.Length)
            {
                // Cursor at end of text
                line = LetterBoxes[LetterBoxes.Count - 1];
                lineCharIndex = line.Count - 1;
            }
            else if (lineIndex == -1)
            {
                // Somewhere in the middle of text
                // lineIndex unspecified
                lineIndex = GetLineIndex(index);
                line = LetterBoxes[lineIndex];
                lineCharIndex = GetIndexInLine(index - 1, lineIndex);
            }
            else
            {
                // Somewhere in the middle of text
                // lineIndex given
                line = LetterBoxes[lineIndex];
                lineCharIndex = index;
            }

			LetterRect box = line[lineCharIndex];
            if (LastMoveDir == -1 && lineIndex != -1 && lineCharIndex + 1 == LetterBoxes[lineIndex].Count && lineIndex + 1 < LetterBoxes.Count)
                pos = new int2(0, box.Pos.y + box.Height);
            else pos = box.Pos + new int2(box.CharWidth, 0);

            return local ? pos : (pos + MinBound);
        }
        #endregion

        #region Private Methods
        private List<LetterRect> GenerateLetterRects(string line, bool lastLineHadBreak, float lineHeight, TextOptions textOptions)
        {
			List<LetterRect> rects = new(line.Length + (lastLineHadBreak ? 1 : 0));
			int maxWidth = MaxBound.x - MinBound.x + 1;
			int x = 0;
			int y = Math2.RoundToInt(lineHeight * LetterBoxes.Count);
			int height = Math2.RoundToInt(lineHeight * (LetterBoxes.Count + 1)) - y;
			string current = "";
            WrappedHeight = y + height;

            if (lastLineHadBreak) rects.Add(new LetterRect(0, y, new int2(0, height), 0));

			for (int i = 0; i < line.Length; i++)
			{
				current += line[i];
				LetterRect rect = TextMeasurer.Measure(current, textOptions);
				rect.CharWidth = rect.Right - x;
				if (i + 1 == line.Length) rect.Width = maxWidth - x + 1; // Extend to right edge of textbox
				else rect.Width += rect.X - x; // Extend to not move the right edge after moving the left
				rect.X = x; // Touch last letter or min bound
				rect.Y = y; // Touch top of text
				rect.Height = height; // Extend for line height
				rects.Add(rect);
				x += rect.Width;
			}

            return rects;
		}

        private struct Line
        {
            public string Text;
            public bool EndsWithLineBreak;

            public Line(string text)
            {
                Text = text;
                EndsWithLineBreak = false;
            }

            public Line(string text, bool hasLineBreak)
            {
                Text = text;
                EndsWithLineBreak = hasLineBreak;
            }
        }

        private void WrapText()
        {
            var textOptions = new TextOptions(Font);
            int maxWidth = MaxBound.x - MinBound.x + 1;

            var textSplit = Text.Split('\n');
			List<Line> lines = new(textSplit.Length);
            for (int i = 0; i < textSplit.Length - 1; i++)
				lines.Add(new Line(textSplit[i], true));
            if (textSplit.Length != 0) lines.Add(new Line(textSplit[textSplit.Length - 1]));

			float lineHeight = Font.FontMetrics.LineHeight * Font.Size / Font.FontMetrics.UnitsPerEm;
			LetterBoxes.Clear();
            int lineIndex = 0;
            bool lastLineHadLineBreak = false;
            WrappedText = "";
            while (lineIndex < lines.Count)
            {
                Line line = lines[lineIndex];

                if (line.Text.Length < 2 || TextMeasurer.Measure(line.Text, textOptions).Width <= maxWidth)
                {
                    // Empty line or line fits within bounds
                    WrappedText += line.Text + "\n";
                    LetterBoxes.Add(GenerateLetterRects(line.Text, lastLineHadLineBreak, lineHeight, textOptions));
                    lastLineHadLineBreak = line.EndsWithLineBreak;
                    lineIndex++;
                    continue;
                }

                // Line needs to be wrapped
                string[] spaceSplit = line.Text.Split(' ');
                List<string> words = new();
                List<char> separators = new();

                // Split tabs and spaces
                bool firstSpace = true;
                for (int j = 0; j < spaceSplit.Length; j++)
                {
                    if (!firstSpace) separators.Add(' ');
                    firstSpace = false;
                    var split = spaceSplit[j].Split('\t');
                    bool firstSplit = true;
                    for (int k = 0; k < split.Length; k++)
                    {
                        if (!firstSplit) separators.Add('\t');
                        firstSplit = false;
                        words.Add(split[k]);
                    }
                }

                string current, next, remainder;
                if (words.Count == 1 || TextMeasurer.Measure(words[0], textOptions).Width > maxWidth)
                {
                    // Wrap single word
                    current = line.Text[0].ToString();
                    next = current + line.Text[1];

                    // Iterate through string until the next char exceeds the bounds
                    int length = 2;
                    while (length < line.Text.Length && TextMeasurer.Measure(next, textOptions).Width <= maxWidth)
                    {
                        current = next;
                        next += line.Text[length];
                        length++;
                    }
                    length--;

                    remainder = line.Text[length..];
					WrappedText += current + "\n";
					LetterBoxes.Add(GenerateLetterRects(current, lastLineHadLineBreak, lineHeight, textOptions));
					lastLineHadLineBreak = false;

					if (lineIndex + 1 == lines.Count) lines.Add(new Line(remainder, line.EndsWithLineBreak));
                    else lines.Insert(lineIndex + 1, new Line(remainder, line.EndsWithLineBreak));
                    lineIndex++;
                    continue;
                }

                // Wrap multiple words
                current = words[0];
                next = current + separators[0] + words[1];

                // Iterate through string until the next char exceeds the bounds
                int i = 2;
                while (i < words.Count && TextMeasurer.Measure(next, textOptions).Width <= maxWidth)
                {
                    current = next;
                    next += separators[i - 1] + words[i];
                    i++;
                }
                i--;

                remainder = words[i];
                for (int j = i + 1; j < words.Count; j++)
                    remainder += separators[j - 1] + words[j];

				WrappedText += current + "\n";
				LetterBoxes.Add(GenerateLetterRects(current, lastLineHadLineBreak, lineHeight, textOptions));
				lastLineHadLineBreak = true;
				if (lineIndex + 1 == lines.Count) lines.Add(new Line(remainder, line.EndsWithLineBreak));
                else lines.Insert(lineIndex + 1, new Line(remainder, line.EndsWithLineBreak));
                lineIndex++;
            }
            if (lastLineHadLineBreak) LetterBoxes.Add(GenerateLetterRects("", true, lineHeight, textOptions));
        }
        #endregion
    }
}
