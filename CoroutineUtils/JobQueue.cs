using System.Collections;

namespace Progrimage.CoroutineUtils
{
    public static class JobQueue
    {
        public static List<CoroutineJob> Queue = new();
        public static int MaxProcessingTime = 1000 / 60 - 4;
		public static bool UnlimitedTime = false;

        private static long _processingStartTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        private static long _nextYieldTime = _processingStartTime + MaxProcessingTime;

        public static bool ShouldYield
        {
            get => !UnlimitedTime && DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond >= _nextYieldTime;
        }

        public static void UpdateTime()
        {
            _processingStartTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            _nextYieldTime = _processingStartTime + MaxProcessingTime;
        }

        public static void Work()
        {
            while (Queue.Any())
            {
                CoroutineJob job = Queue[0];
                if (job.JobIdentifier != null && job.JobIdentifier!.JobID != job.JobID)
                {
                    // Job cancelled
                    if (!job.IsStarted && job.JobIdentifier != null && job.JobIdentifier!.JobIsUnique)
                    {
                        // Job should be unique
                        job.JobIdentifier!.Counter--;
                    }

                    job.Dispose?.Invoke(); // Run disposal delegate
                    Queue.RemoveAt(0);
                    continue;
                }

                if (job.IsStarted)
                {
                    // Run yielded job
                    if (job.Work.MoveNext()) return;
                    else Queue.RemoveAt(0);
                    continue;
                }

                // Start new job
                if (job.JobIdentifier != null && job.JobIdentifier!.JobIsUnique)
                {
                    // Job should be unique
                    job.JobIdentifier!.Counter--;
                    if (job.JobIdentifier!.Counter == 0)
                    {
                        // Only job of this kind queued
                        job.JobIdentifier!.HasRun = true;
                        job.IsStarted = true;
                        Queue[0] = job;
                        if (job.Work.MoveNext()) return;
                        else Queue.RemoveAt(0);
                    }
                    else if (job.JobIdentifier!.HasRun)
                    {
                        // Multiple jobs of this kind queued but this is the first after a successful run
                        job.JobIdentifier!.HasRun = false;
                        job.FirstDuplicateRun?.Invoke();
                        Queue.RemoveAt(0);
                    }
                    else Queue.RemoveAt(0); // Ignore this job because there are multiple
                }
                else
                {
                    // Job is not unique
                    job.IsStarted = true;
                    Queue[0] = job;
                    if (job.Work.MoveNext()) return;
                    else Queue.RemoveAt(0);
                }
            }
        }

        public static IEnumerator EmptyEnumerator() { yield break; }
    }
}
