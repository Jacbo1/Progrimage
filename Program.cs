namespace Progrimage
{
    internal class Program
    {
        public static float Scale = 1;
        public static Instance ActiveInstance;
        public static bool IsCtrlPressed, IsShiftPressed, IsAltPressed;
        public const bool DEV_MODE = true;

		[STAThread]
        static void Main(string[] args)
        {
            using (var mainWindow = new MainWindow()) mainWindow.Run(args);
        }

        //private static Image<Argb32> LoadURL(string url)
        //{
        //    using WebClient wc = new WebClient();
        //    using Stream s = wc.OpenRead(url);
        //    return Image.Load<Argb32>(s);
        //}
    }
}