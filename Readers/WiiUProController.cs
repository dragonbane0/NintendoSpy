using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Windows;

namespace NintendoSpy.Readers
{
    sealed public class WiiUProControllerReader : IControllerReader
    {
        // ----- Interface implementations with backing state -------------------------------------------------------------

        public event StateEventHandler ControllerStateChanged;
        public event EventHandler ControllerDisconnected;

        // ----------------------------------------------------------------------------------------------------------------
        Thread dataThread;
        TcpClient client;
        NetworkStream stream;

        SynchronizationContext _context;

        public WiiUProControllerReader(string ipAddress)
        {
            _context = SynchronizationContext.Current;

            dataThread = new Thread(dataStream);
            dataThread.Name = "Auto Splitter Input Display Thread";
            dataThread.Start(ipAddress);
        }

        public void Finish()
        {
            dataThread.Abort();

            //Close Gecko
            try
            {
                if (client == null)
                {
                    throw new Exception("Not connected", new NullReferenceException());
                }
                stream.Close();
                client.Close();

            }
            catch (Exception) { }
            finally
            {
                client = null;
            }
        }

        public bool IsBitSet(byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        public void dataStream(object client_obj)
        {
            //Gecko Connection
            try
            {
                client = new TcpClient();
                client.NoDelay = true;
                IAsyncResult ar = client.BeginConnect((string)client_obj, 7335, null, null); //Auto Splitter Input Display uses 7335
                System.Threading.WaitHandle wh = ar.AsyncWaitHandle;
                try
                {
                    if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3), false))
                    {
                        client.Close();

                        MessageBox.Show("WiiU: Connection Timeout!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                        if (ControllerDisconnected != null)
                            _context.Post((SendOrPostCallback)(_ => ControllerDisconnected(this, EventArgs.Empty)), null);

                        return;
                    }

                    client.EndConnect(ar);
                }
                finally
                {
                    wh.Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("WiiU: Connection Error. Check IP Address!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                if (ControllerDisconnected != null)
                    _context.Post((SendOrPostCallback)(_ => ControllerDisconnected(this, EventArgs.Empty)), null);

                return;
            }

            //Handle Connection
            stream = client.GetStream();
            EndianBinaryReader reader = new EndianBinaryReader(stream);

            try
            {
                float StickX, StickCX, StickY, StickCY = 0.0f;
                byte Directions1, Directions2 = 0;
                byte Buttons1, Buttons2 = 0;

                while (true)
                {
                    byte cmd_byte = reader.ReadByte();
                    switch (cmd_byte)
                    {
                        case 0x01: //Input Data
                            {
                                //1 = 0 bit, 2 = 1 bit, 4 = 2 bit, 8 = 3 bit, 16 = 4 bit, 32 = 5 bit, 64 = 6 bit, 128 = 7 bit        
                                              
                                /*
                                A is 80
                                B is 40
                                Y is 10
                                X is 20
                                R is 0
                                */

                                StickX = reader.ReadSingle();
                                StickY = reader.ReadSingle();
                                StickCX = reader.ReadSingle();
                                StickCY = reader.ReadSingle();

                                Directions1 = reader.ReadByte();
                                Directions2 = reader.ReadByte();
                                Buttons1 = reader.ReadByte();
                                Buttons2 = reader.ReadByte();
        
                                //Apply Inputs
                                var outState = new ControllerStateBuilder();

                                outState.SetButton("a", IsBitSet(Buttons1, 7));
                                outState.SetButton("b", IsBitSet(Buttons1, 6));
                                outState.SetButton("x", IsBitSet(Buttons1, 5));
                                outState.SetButton("y", IsBitSet(Buttons1, 4));

                                outState.SetButton("home", IsBitSet(Buttons2, 5)); //l
                                outState.SetButton("touch", IsBitSet(Buttons2, 4)); //r
                                outState.SetButton("zl", IsBitSet(Buttons2, 7));
                                outState.SetButton("zr", IsBitSet(Buttons2, 6));
                                //outState.SetButton("touch", IsBitSet(Buttons2, 6));

                                outState.SetButton("plus", IsBitSet(Buttons2, 3));
                                outState.SetButton("minus", IsBitSet(Buttons2, 2));
                                //outState.SetButton("home", IsBitSet(Buttons2, 2));

                                outState.SetButton("up", IsBitSet(Buttons1, 1));
                                outState.SetButton("down", IsBitSet(Buttons1, 0));
                                outState.SetButton("left", IsBitSet(Buttons1, 3));
                                outState.SetButton("right", IsBitSet(Buttons1, 2));

                                if (StickX <= 1.0f && StickY <= 1.0f && StickX >= -1.0f && StickY >= -1.0f)
                                {
                                    outState.SetAnalog("lstick_x", StickX);
                                    outState.SetAnalog("lstick_y", StickY);
                                }

                                if (StickCX <= 1.0f && StickCY <= 1.0f && StickCX >= -1.0f && StickCY >= -1.0f)
                                {
                                    outState.SetAnalog("cstick_x", StickCX);
                                    outState.SetAnalog("cstick_y", StickCY);
                                }

                                if (Directions2 >= 0 && Directions2 <= 255)
                                {
                                    outState.SetButton("l3", IsBitSet(Directions2, 2)); //!!Analog Stick Click is 4; C-Stick is 2!!
                                    outState.SetButton("r3", IsBitSet(Directions2, 1));
                                }

                                if (ControllerStateChanged != null)
                                    _context.Post((SendOrPostCallback)(_ => ControllerStateChanged(this, outState.Build())), null);
                           
                                break;
                            }
                        default:
                            throw new Exception("Invalid data");
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception)
            {
                if (ControllerDisconnected != null) 
                    _context.Post((SendOrPostCallback)(_ => ControllerDisconnected(this, EventArgs.Empty)), null);
            }
        }
    }
}
