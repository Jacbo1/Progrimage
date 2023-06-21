namespace Progrimage
{
    internal class Program
    {
        public static float Scale = 1;
        public static Instance ActiveInstance;
        public static bool IsCtrlPressed, IsShiftPressed, IsAltPressed;
        public const bool DEV_MODE = false;
        public const bool ALTERNATE_MODIFIER_KEYS = false;

		[STAThread]
        static void Main(string[] args)
        {
            using (var mainWindow = new MainWindow()) mainWindow.Run(args);
        }
    }
}