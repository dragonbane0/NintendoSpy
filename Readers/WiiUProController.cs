using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NintendoSpy.Readers
{
    sealed public class WiiUProControllerReader : IControllerReader
    {
    // ----- Interface implementations with backing state -------------------------------------------------------------

        public event StateEventHandler ControllerStateChanged;
        public event EventHandler ControllerDisconnected;

    // ----------------------------------------------------------------------------------------------------------------

        const double TIMER_MS = 16;

        DispatcherTimer _timer;

        public WiiUProControllerReader()
        {
            _timer = new DispatcherTimer ();
            _timer.Interval = TimeSpan.FromMilliseconds (TIMER_MS);
            _timer.Tick += tick;
            _timer.Start ();
        }

        void tick (object sender, EventArgs e)
        {
            //read from socket

            try
            {
                var outState = new ControllerStateBuilder();

                /*
                outState.SetButton("a", (state.wButtons & 0x1000) != 0);
                outState.SetButton("b", (state.wButtons & 0x2000) != 0);
                outState.SetButton("x", (state.wButtons & 0x4000) != 0);
                outState.SetButton("y", (state.wButtons & 0x8000) != 0);
                outState.SetButton("up", (state.wButtons & 0x0001) != 0);
                outState.SetButton("down", (state.wButtons & 0x0002) != 0);
                outState.SetButton("left", (state.wButtons & 0x0004) != 0);
                outState.SetButton("right", (state.wButtons & 0x0008) != 0);
                outState.SetButton("start", (state.wButtons & 0x0010) != 0);
                outState.SetButton("back", (state.wButtons & 0x0020) != 0);
                outState.SetButton("l3", (state.wButtons & 0x0040) != 0);
                outState.SetButton("r3", (state.wButtons & 0x0080) != 0);
                outState.SetButton("l", (state.wButtons & 0x0100) != 0);
                outState.SetButton("r", (state.wButtons & 0x0200) != 0);

                outState.SetAnalog("lstick_x", (float)state.sThumbLX / 32768);
                outState.SetAnalog("lstick_y", (float)state.sThumbLY / 32768);
                outState.SetAnalog("rstick_x", (float)state.sThumbRX / 32768);
                outState.SetAnalog("rstick_y", (float)state.sThumbRY / 32768);
                outState.SetAnalog("trig_l", (float)state.bLeftTrigger / 255);
                outState.SetAnalog("trig_r", (float)state.bRightTrigger / 255);
                */

                if (ControllerStateChanged != null)
                    ControllerStateChanged(this, outState.Build());
            }
            catch
            {
                if (ControllerDisconnected != null)
                    ControllerDisconnected(this, EventArgs.Empty);

                Finish();
            }
        }

        public void Finish ()
        {
            if (_timer != null) {
                _timer.Stop ();
                _timer = null;
            }
        }
    }
}
