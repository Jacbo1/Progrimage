using NewMath;

namespace Progrimage.DrawingShapes
{
	public class DrawingShapeCollection : IDisposable
    {
        #region Fields
        public List<IShape> Shapes;
        public bool AttachedToLayer, Hidden;
        public double2 Pos;
        internal Layer Layer;
        #endregion

        #region Constructors
        public DrawingShapeCollection(Layer layer)
        {
            Shapes = new();
            Layer = layer;
        }

        public DrawingShapeCollection(Layer layer, double2 pos)
        {
            Shapes = new();
            Pos = pos;
            Layer = layer;
        }

        public DrawingShapeCollection(Layer layer, params IShape[] shapes)
        {
            Shapes = new(shapes);
            Layer = layer;
        }

        public DrawingShapeCollection(Layer layer, List<IShape> shapes)
        {
            Shapes = shapes;
            Layer = layer;
        }

        public DrawingShapeCollection(Layer layer, double2 pos, params IShape[] shapes)
        {
            Shapes = new(shapes);
            Pos = pos;
            Layer = layer;
        }

        public DrawingShapeCollection(Layer layer, double2 pos, List<IShape> shapes)
        {
            Shapes = shapes;
            Pos = pos;
            Layer = layer;
        }
        #endregion

        #region Public Methods
        public void Draw(IImageProcessingContext context)
        {
            if (Hidden) return;
            double2 origin = Pos;
            if (AttachedToLayer) origin += Layer.Pos;
            for (int i = 0; i < Shapes.Count; i++)
            {
                var shape = Shapes[i];
                if (shape.Hidden) continue;
                double2 pos = shape.Pos;
                shape.Pos += origin;
                shape.Draw(context);
                shape.Pos = pos;
            }
        }

        public void DrawToRender(IImageProcessingContext context)
        {
			if (Hidden) return;
			double2 origin = AttachedToLayer ? (Pos + Layer.Pos) : Pos;
            for (int i = 0; i < Shapes.Count; i++)
            {
                var shape = Shapes[i];
                if (shape.Hidden) continue;
                double2 pos = shape.Pos;
                shape.Pos += origin;
                shape.DrawToRender(context);
                shape.Pos = pos;
            }
        }

        public void Dispose()
        {
			if (Layer.OverlayShapes.Remove(this)) Program.ActiveInstance.Changed();
			if (Layer.RenderOverlayShapes.Remove(this)) Program.ActiveInstance.OverlayChanged();
        }
        #endregion
    }
}
