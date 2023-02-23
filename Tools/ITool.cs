using Progrimage.Composites;
using Progrimage.Utils;

namespace Progrimage.Tools
{
    public interface ITool : IInteractable
    {
        public TexPair Icon { get; }
        public void OnSelect(Instance instance) { }
        public void OnDeselect() { }
        public void EnterPressed() { }
        public void EscapePressed() { }
    }
}
