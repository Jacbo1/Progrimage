using System.Collections;

namespace Progrimage.CoroutineUtils
{
    public struct CoroutineJob
    {
        public IEnumerator Work { get; private set; }
        public Action? Dispose { get; private set; }
        public Action? FirstDuplicateRun { get; private set; }
        public JobIdentifier? JobIdentifier { get; private set; }
        public bool IsStarted { get; internal set; }
        internal readonly int JobID;

        public CoroutineJob(IEnumerator work, JobIdentifier? jobIdentifier = null, Action? dispose = null, Action? firstDuplicateRun = null)
        {
            Work = work;
            Dispose = dispose;
            FirstDuplicateRun = firstDuplicateRun;
            JobIdentifier = jobIdentifier;
            if (jobIdentifier != null)
            {
                JobID = jobIdentifier!.JobID;
                jobIdentifier.Counter++;
            }
            else JobID = 0;
            IsStarted = false;
        }

        public CoroutineJob(Action work, JobIdentifier? jobIdentifier = null, Action? dispose = null, Action? firstDuplicateRun = null)
        {
            Work = ActionEnumerator(work);
            Dispose = dispose;
            FirstDuplicateRun = firstDuplicateRun;
            JobIdentifier = jobIdentifier;
            if (jobIdentifier != null)
            {
                JobID = jobIdentifier!.JobID;
                jobIdentifier.Counter++;
            }
            else JobID = 0;
            IsStarted = false;
        }

        private static IEnumerator ActionEnumerator(Action action)
        {
            action();
            yield break;
        }
    }
}
