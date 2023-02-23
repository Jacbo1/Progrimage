using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace ProgrimageImGui
{
    public struct TextInput
    {
        public char? Char = null;
        public Keys? Key = null;
        public bool Copy = false;
        public bool Paste = false;
        public bool CtrlDown = false;
        public string Text = "";

        public TextInput(char c)
        {
            Char = c;
        }

        public TextInput(Keys key, bool ctrlDown)
        {
            Key = key;
            CtrlDown = ctrlDown;
        }

        public TextInput(bool copy)
        {
            Copy = copy;
        }

        public TextInput(string text)
        {
            Text = text;
        }
    }
}
