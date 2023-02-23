namespace Progrimage.CoroutineUtils
{
    public class JobIdentifier
    {
        internal bool JobIsUnique;
        internal int Counter;
        internal bool HasRun;
        internal int JobID;

        public JobIdentifier(bool jobIsUnique = false)
        {
            JobIsUnique = jobIsUnique;
        }

        public void Cancel()
        {
            JobID = (JobID + 1) % 0xFFFFFF;
        }
    }
}
