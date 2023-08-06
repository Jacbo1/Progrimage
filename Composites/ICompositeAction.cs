using ImageSharpExtensions;
using NewMath;
using Progrimage.CoroutineUtils;

namespace Progrimage.Composites
{
    public interface ICompositeAction
    {
        public Action? DisposalDelegate { get; }
        public Composite Composite { get; }
		public int2 Pos { get; set; }
        public System.Collections.IEnumerator Run(PositionedImage<Argb32> result);
        public void RunOnceFirstRepeat(PositionedImage<Argb32> result) { }
        public void Init(Composite composite);
        public void DrawQuickActionsToolbar(PositionedImage<Argb32> result) { }
        public void Rerun()
        {
            Composite!.Layer.CancelJobs();
            JobQueue.Queue.Add(new CoroutineJob(Composite!.Layer.Changed));
        }
    }
}
