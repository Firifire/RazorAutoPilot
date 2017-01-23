using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;

using Windows.Devices.Enumeration;
using System.Threading;
using Windows.Devices.I2c;
//using Windows.Devices.SerialCommunication;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace RazorAutoPilotHeadless
{
    public sealed class StartupTask : IBackgroundTask
    {
        const int bufsize = 13;
        private I2cDevice Device;
        private Timer periodicTimer;
        private bool busy = false; //to check if i2c is busy
        //Temperature
        private byte[] aTemp = new byte[10];
        private byte tempC = 0, tempT = 1;
        private int temp;
        byte[] SendBuf = new byte[bufsize];

        //Switch Status (maybe update automatically)
        private bool light = false, fan = false, AC = false, P_C = false, alarm = false;
        private bool[] AutoPilot = { true, true, true, true, true };  //+add a button with arduino turn autopilot off

        private static BackgroundTaskDeferral _Deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _Deferral = taskInstance.GetDeferral();
            for (int i = 3; i < 8; i++)
                SendBuf[i] = 0;
            for (int i = 0; i < AutoPilot.Length; i++)
            {
                SendBuf[i + 8] = Convert.ToByte(AutoPilot[i]);
            }
            PopulateAdapterList();
            initcomunica();
            StartListener();
        }

        private async void initcomunica()
        {
            var settings = new I2cConnectionSettings(0x40); // Arduino address
            settings.BusSpeed = I2cBusSpeed.StandardMode;
            string aqs = I2cDevice.GetDeviceSelector("I2C1");
            var dis = await DeviceInformation.FindAllAsync(aqs);
            Device = await I2cDevice.FromIdAsync(dis[0].Id, settings);
            periodicTimer = new Timer(this.TimerCallback, null, 0, 1000); // Create a timmer
        }

        //buttons
        private void BLight_Click()
        {
            AutoPilot[0] = !AutoPilot[0];
            SendBuf[8] = Convert.ToByte(AutoPilot[0]);
        }

        private void BFan_Click()
        {
            AutoPilot[1] = !AutoPilot[1];
            SendBuf[9] = Convert.ToByte(AutoPilot[1]);
        }

        private void BA_C_Click()
        {
            AutoPilot[2] = !AutoPilot[2];
            SendBuf[10] = Convert.ToByte(AutoPilot[2]);
        }

        private void BPC_Click()
        {
            AutoPilot[3] = !AutoPilot[3];
            SendBuf[11] = Convert.ToByte(AutoPilot[3]);
        }

        private void BSecurity_Click()
        {
            AutoPilot[4] = !AutoPilot[4];
            SendBuf[12] = Convert.ToByte(AutoPilot[4]);
        }

        private void BAlarm_Click()
        {
            AutoPilot[5] = !AutoPilot[5];
            SendBuf[7] = Convert.ToByte(AutoPilot[5]);
        }

        //Switches
        private void Light_Click()
        {
            if (light == true)
            {
                control(4, 0);
                light = false;
            }
            else
            {
                control(4, 1);
                light = true;
            }
            SendBuf[3] = Convert.ToByte(light);
        }

        private void Fan_Click()
        {
            if (fan == true)
            {
                control(5, 0);
                fan = false;
            }
            else
            {
                control(5, 1);
                fan = true;
            }
            SendBuf[4] = Convert.ToByte(fan);
        }

        private void A_C_Click()
        {
            if (AC == true)
            {
                //+add AC control
                AC = false;
            }
            else
            {
                //+add AC control
                AC = true;
            }
            SendBuf[5] = Convert.ToByte(AC);
        }

        private void PC_Click()
        {

        }

        private void TimerCallback(object state)
        {
            if (busy == true)
                return;
            busy = true;
            byte[] ReadBuf = new byte[64];
            try
            {
                Device.Read(ReadBuf);
            }
            catch (Exception f) { }
            busy = false;
            //char[] cArray = System.Text.Encoding.UTF8.GetString(ReadBuf, 0, 64).ToCharArray();  // Converte  Byte to Char
            //String c = new String(cArray);
            //Debug.WriteLine(c);
            // refresh the screen, note Im using a textbock @ UI
            if (ReadBuf != null)
            {
                //get the average of temperature
                temp = 0;
                aTemp[tempC++] = ReadBuf[1];
                for (byte i = 0; i < tempT; i++)
                {
                    temp += aTemp[i];
                }
                temp = temp / tempT;
                if (tempT < 10)
                    tempT++;
                if (tempC >= 10)
                    tempC = 0;
                //display
                //Debug.WriteLine(temp);
                SendBuf[0] = ReadBuf[0];
                SendBuf[1] = (byte)temp;
                SendBuf[2] = ReadBuf[2];
                //Lights
                //if (isUserInRoom() && ReadBuf[0] == 0 && DateTime.Now.Hour > 16 && light == false) //+Add userisalseep and || DateTime.Now.Hour < 7
                //{
                //    //control(4, 1);//+turn lights on
                //    light = true;
                //    Light.Content = "Light: On";
                //}
                //+recieve info from PC (timed checks for when it switches off"
                //+If security is on
                //+if triggered + alarm
            }
        }



        private void automatiion() //timed and callback
        {
            if (isUserInRoom() == true) //and awake //or when user enters room
            {
                //if lights off
                //room is dark(make call from readi2c)
                //turn lights om
            }
            //if user enter home
            //if lights and fan off
            //turn fan on, turn lights on if dark
            //user left homme
            //turn lights off, fan off AC off
            //user sleep (PC == off and time > 10 PM and isUserAtHome())
            //Room auto coooling altrenate between fan an AC or make it work only for night
        }


        private bool isUserInRoom() // timed call back or when dark or controlled callback
        {
            bool user = true;
            //use PIR motion sensor or llama or webcam or ifttt or tasker
            return user;
        }


        private void control(byte pin, byte value)
        {
            while (busy == true) ;
            busy = true;
            byte[] data = new byte[2];
            data[0] = pin;
            data[1] = value;
            try
            {
                Device.Write(data); // read the data
            }
            catch (Exception f) { }
            busy = false;
        }

        // private void userLeftHome(); //callback
        // private void userEnteredHome(); //call llama



        //Communication server
        string ServiceNameForListener = "11000";
        // List containing all available local HostName endpoints
        private List<LocalHostItem> localHostItems = new List<LocalHostItem>();

        private async void StartListener()
        {
            StreamSocketListener listener = new StreamSocketListener();
            listener.ConnectionReceived += OnConnection;

            // If necessary, tweak the listener's control options before carrying out the bind operation.
            // These options will be automatically applied to the connected StreamSockets resulting from
            // incoming connections (i.e., those passed as arguments to the ConnectionReceived event handler).
            // Refer to the StreamSocketListenerControl class' MSDN documentation for the full list of control options.
            listener.Control.KeepAlive = false;

            // Save the socket, so subsequent steps can use it.
            CoreApplication.Properties.Add("listener", listener);

            // Start listen operation.
            try
            {
                // Don't limit traffic to an address or an adapter.
                await listener.BindServiceNameAsync(ServiceNameForListener);
            }
            catch (Exception exception)
            {
                CoreApplication.Properties.Remove("listener");

                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
            }
        }
        /// <summary>
        /// Invoked once a connection is accepted by StreamSocketListener.
        /// </summary>
        /// <param name="sender">The listener that accepted the connection.</param>
        /// <param name="args">Parameters associated with the accepted connection.</param>
        private async void OnConnection(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            DataReader reader = new DataReader(args.Socket.InputStream);
            DataWriter writer = new DataWriter(args.Socket.OutputStream);
            try
            {
                while (true)
                {
                    // Read first 4 bytes (length of the subsequent string).
                    uint sizeFieldCount = await reader.LoadAsync(sizeof(uint));
                    if (sizeFieldCount != sizeof(uint))
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        return;
                    }
                    // Read the string.
                    int size = reader.ReadInt32();
                    uint actualSize = await reader.LoadAsync(Convert.ToUInt32(size));
                    byte[] recieve = new byte[actualSize];
                    if (size != actualSize)
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        return;
                    }

                    if (size == 1)
                    {
                        reader.ReadByte();
                        writer.WriteInt32(SendBuf.Length);
                        writer.WriteBytes(SendBuf);
                        try
                        {
                            await writer.StoreAsync();
                            //SendOutput.Text = "\"" + stringToSend + "\" sent successfully.";
                        }
                        catch (Exception exception)
                        {
                            if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                                throw;
                        }
                    }
                    else if (size == 3)
                    {
                        reader.ReadBytes(recieve);
                        if (recieve[0] == 1)
                        {
                            switch (recieve[1])
                            {
                                case 1:
                                    if (Convert.ToByte(AutoPilot[0]) == recieve[2])
                                        BLight_Click();
                                    break;
                                case 2:
                                    if (Convert.ToByte(AutoPilot[1]) == recieve[2])
                                        BFan_Click();
                                    break;
                                case 3:
                                    if (Convert.ToByte(AutoPilot[2]) == recieve[2])
                                        BA_C_Click();
                                    break;
                                case 4:
                                    if (Convert.ToByte(AutoPilot[3]) == recieve[2])
                                        BPC_Click();
                                    break;
                                case 5:
                                    if (Convert.ToByte(AutoPilot[4]) == recieve[2])
                                        BSecurity_Click();
                                    break; //need to add
                                default:
                                    break;
                            }
                        }
                        else if (recieve[0] == 2)
                        {
                            switch (recieve[1])
                            {
                                case 1:
                                    Light_Click();
                                    break;
                                case 2:
                                    Fan_Click();
                                    break;
                                case 3:
                                    A_C_Click();
                                    break;
                                case 4:
                                    PC_Click();
                                    break;
                                case 5:
                                    BAlarm_Click();
                                    break; //need to add
                                default:
                                    break;
                            }
                        }

                    }
                }
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {

                    throw;
                }
            }
        }

        /// <summary>
        /// Populates the NetworkAdapter list
        /// </summary>
        private void PopulateAdapterList()
        {
            localHostItems.Clear();
            //  AdapterList.ItemsSource = localHostItems;
            //  AdapterList.DisplayMemberPath = "DisplayString";

            foreach (HostName localHostInfo in NetworkInformation.GetHostNames())
            {
                if (localHostInfo.IPInformation != null)
                {
                    LocalHostItem adapterItem = new LocalHostItem(localHostInfo);
                    localHostItems.Add(adapterItem);
                }
            }
        }
        //stop server
        private void CloseSocket()
        {
            object outValue;

            if (CoreApplication.Properties.TryGetValue("listener", out outValue))
            {
                // Remove the listener from the list of application properties as we are about to close it.
                CoreApplication.Properties.Remove("listener");
                StreamSocketListener listener = (StreamSocketListener)outValue;

                // StreamSocketListener.Close() is exposed through the Dispose() method in C#.
                // The call below explicitly closes the socket.
                listener.Dispose();
            }
            CoreApplication.Properties.Remove("adapter");
            CoreApplication.Properties.Remove("serverAddress");

            //rootPage.NotifyUser("Socket and listener closed", NotifyType.StatusMessage);
        }
    }

    class LocalHostItem
    {
        public string DisplayString
        {
            get;
            private set;
        }

        public HostName LocalHost
        {
            get;
            private set;
        }

        public LocalHostItem(HostName localHostName)
        {
            if (localHostName == null)
            {
                throw new ArgumentNullException("localHostName");
            }

            if (localHostName.IPInformation == null)
            {
                throw new ArgumentException("Adapter information not found");
            }

            this.LocalHost = localHostName;
            this.DisplayString = "Address: " + localHostName.DisplayName + " Adapter: " + localHostName.IPInformation.NetworkAdapter.NetworkAdapterId;
        }
    }
}
