using NewMath;
using Progrimage.Composites;

namespace Progrimage
{
    public interface IInteractable : IUsesToolbar
    {
        public void OnMouseDown(int2 mousePosScreen, int2 mousePosCanvas) { }
        public void OnMouseDownCanvas(int2 mousePosCanvas) { }
        public void OnMouseUp(int2 mousePosScreen, int2 mousePosCanvas) { }
        public void OnMouseMoveScreen(int2 mousePos) { }
        public void OnMouseMoveCanvas(int2 mousePos) { }
        public void OnMouseMoveCanvasDouble(double2 mousePos) { }
        public void OnMouseEnterCanvas(int2 mousePosScreen, int2 mousePosCanvas) { }
        public void OnMouseExitConvas(int2 mousePosScreen, int2 mousePosCanvas) { }
        public void OnLayerSelect(Layer layer) { }
        public void OnLayerDeselect(Layer layer) { }
        public void OnMouseDown2Canvas() { }
        public void OnMouse2Up() { }
        public void Update(float deltaTime) { }
    }
}
