// Currently not used. Will be part of something in the future.

namespace Progrimage
{
    public struct KeyEvent
    {
        public Keys Key;
        public bool Pressed, CtrlDown, ShiftDown;

        public KeyEvent(Keys key, bool pressed, bool ctrlDown, bool shiftDown)
        {
            Key = key;
            Pressed = pressed;
            CtrlDown = ctrlDown;
            ShiftDown = shiftDown;
        }

        //public char GetChar()
        //{
        //    switch (Key)
        //    {
        //        Keys
        //    }
        //}
    }
}
