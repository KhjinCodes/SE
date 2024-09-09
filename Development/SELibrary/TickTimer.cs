namespace IngameScript
{
    partial class Program
    {
        public class TickTimer
        {
            private bool canUpdate = true;
            private int currTicks = 0, maxTicks = 0;

            public TickTimer(int currTicks, int maxTicks)
            {
                this.currTicks = currTicks;
                this.maxTicks = maxTicks;
            }

            public void Update()
            { if (canUpdate && currTicks < maxTicks) { currTicks++; }}

            public void Reset(bool resetMaxed = false, bool canUpdate = true)
            { 
                currTicks = resetMaxed ? maxTicks : 0;
                this.canUpdate = canUpdate;
            }

            public bool Elapsed()
            { return(currTicks == maxTicks); }

            public int GetRemainingSecs()
            { return (((maxTicks-currTicks) / 60) + 1); }
        
            public bool CanUpdate 
            { get { return canUpdate; } set { canUpdate = value; } }

            public void Configure(int currTicks, int maxTicks)
            {
                this.currTicks = currTicks;
                this.maxTicks = maxTicks;
                canUpdate = true;
            }
        }
    }
}
