#if !NETFX_CORE

using System;
using System.Threading;

namespace uOSC.DotNet
{

    public class Thread : uOSC.Thread
    {
        System.Threading.Thread thread_;
        bool isRunning_ = false;
        Action loopFunc_ = null;
        public CancellationTokenSource source_;

        public override void Start(CancellationTokenSource source, Action loopFunc)
        {
            if (isRunning_ || loopFunc == null) return;

            isRunning_ = true;
            loopFunc_ = loopFunc;
            source_ = source;

            thread_ = new System.Threading.Thread(ThreadLoop);
            thread_.Start();
        }

        void ThreadLoop()
        {
            while (isRunning_)
            {
                try
                {
                    loopFunc_();
                    System.Threading.Thread.Sleep(IntervalMillisec);
                }
                catch (Exception e)
                {
                    Godot.GD.PrintErr(e.Message);
                    Godot.GD.PrintErr(e.StackTrace);
                }
            }
        }

        public override void Stop(int timeoutMilliseconds = 3000)
        {
            if (!isRunning_) return;

            isRunning_ = false;

            if (thread_.IsAlive)
            {
                thread_.Join(timeoutMilliseconds);
                if (thread_.IsAlive)
                {
                    source_.Cancel();
                }
            }
        }
    }

}

#endif