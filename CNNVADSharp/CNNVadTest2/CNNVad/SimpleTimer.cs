using System;
using System.Timers;

namespace Pet.Ultilities
{
    public class SimpleTimer : IDisposable
    {
        #region "Dispose Implementation"        
        bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //dispose managed resources
                    timer.Enabled = false;
                    timer.Dispose();
                }
            }
            //dispose unmanaged resources
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        private Timer timer;
        private bool oneTime;
        private int interval;
        private Action action;
        public SimpleTimer(Action action, int interval, bool oneTime = false)
        {
            this.oneTime = oneTime;
            this.interval = interval;
            this.action = action;
            timer = new Timer();
            //if (sycnObj != null)
            //{
            //    timer.SynchronizingObject = sycnObj;
            //    sameThread = true;
            //    System.Windows.Forms.MessageBox.Show("yes");
            //}
            timer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                if (oneTime) this.Dispose();
                action();
            };
            SetInterval(interval);
        }
        public void StartAction()
        {
            timer.Enabled = true;
        }
        public void StopAction()
        {
            timer.Enabled = false;
        }
        public void SetInterval(int interval)
        {
            timer.Interval = interval;
        }
    }
}
