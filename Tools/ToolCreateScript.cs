using Progrimage.Utils;

namespace Progrimage.Tools
{
    public class ToolCreateScript : ITool
    {
        #region Fields
        // Public fields
        public const string CONST_NAME = "Create Tool Script";
        #endregion

        #region Properties
        public string Name => CONST_NAME;
        public TexPair Icon { get; private set; }
        #endregion

        #region Constructor
        public ToolCreateScript()
        {
            Icon = new(@"Assets\Textures\Tools\script.png", Defs.TOOL_ICON_SIZE);
        }
        #endregion

        #region ITool Methods
        public void OnSelect(Instance instace)
        {
            var tool = new ToolLua();
            tool.OpenSettings();
            Program.ActiveInstance.LuaTools.Add(tool);
            Program.ActiveInstance.ActiveTool = tool;
        }
        #endregion
    }
}
