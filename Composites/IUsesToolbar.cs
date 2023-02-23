namespace Progrimage.Composites
{
    public interface IUsesToolbar
    {
        public string Name { get; }
        public void DrawQuickActionsToolbar() { }
        public string[] DrawBottomBar() => new string[] { };
    }
}
