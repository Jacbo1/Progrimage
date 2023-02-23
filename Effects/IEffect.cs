using Progrimage.Composites;

namespace Progrimage.Effects
{
    public interface IEffect : IInteractable, IUsesToolbar
    {
        public void OnApply(Layer layer);
        public void OnSelect(Layer layer) => OnApply(layer);
        public void OnDeselect(Layer layer) { }
    }
}
