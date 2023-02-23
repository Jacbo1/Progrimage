using Progrimage.Composites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Progrimage.Utils
{
	public class TextInputHandler
	{
		public string Text = "";
		public bool AcceptTextEntry;
		public int Index;
		public EventHandler TextChanged;
		public Func<string, string, string>? InputFilter;

		public void Update()
		{
			if (!AcceptTextEntry || ImGuiRenderer.TextInput.Count == 0) return;
			bool changed = false;
			for (int i = 0; i < ImGuiRenderer.TextInput.Count; i++)
			{
				var input = ImGuiRenderer.TextInput[i];
				switch (input.Key)
				{
					case Keys.Left:
						Index = Math.Max(0, Index - 1);
						break;
					case Keys.Right:
						Index = Math.Min(Text.Length, Index + 1);
						break;
					case Keys.Home:
						// Move to start of text
						Index = 0;
						break;
					case Keys.End:
						// Move to end of text
						Index = Text.Length;
						break;
					default:
						if (input.Char is null && input.Text == "") break;
						// Type or paste

						if (input.Char == '\b')
						{
							// Backspace
							if (Index == 0) break; // At start of string
							if (Index == Text.Length) Text = Text[..^1]; // At end of string
							else Text = Text[..(Index - 1)] + Text[Index..]; // In middle of string
							Index--;
							changed = true;
							break;
						}

						if (input.Char == 127)
						{
							// Delete
							if (Index == Text.Length) break; // At end of string
							if (Index == 0) Text = Text[1..]; // At start of string
							else Text = Text[..Index] + Text[(Index + 1)..]; // In middle of string
							changed = true;
							break;
						}

						// Type char or paste text
						string text;
						if (input.Char is null) text = input.Text; // Paste text
						else text = input.Char.ToString()!; // Type char

						if (InputFilter is not null)
						{
							string newText;
							if (Index == 0) newText = text + Text; // At start of string
							else if (Index == Text.Length) newText = Text + text; // At end of string
							else newText = Text[..Index] + text + Text[Index..]; // In middle of string
							text = InputFilter(text, newText);
							if (text.Length == 0) break;
						}

						if (Index == 0) Text = text + Text; // At start of string
						else if (Index == Text.Length) Text += text; // At end of string
						else Text = Text[..Index] + text + Text[Index..]; // In middle of string

						Index += text.Length;
						changed = true;
						break;
				}
			}

			if (changed) TextChanged?.Invoke(this, EventArgs.Empty);
		}

		public void Clear()
		{
			Text = "";
			Index = 0;
			TextChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
