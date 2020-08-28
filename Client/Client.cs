using Swissranks.Orion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Client : Form
    {
        public Client()
        {
            InitializeComponent();      
        }


        /// <summary>
        /// Class objects from swissranks.orion API
        /// </summary>
        ULTMConnection connection; 
        ULTMServices services = new ULTMServices();
        ErrorLogger errorLogger = new ErrorLogger();
        CommandLogger commandLogger = new CommandLogger();
        

        /// <summary>
        /// Static objects for configured devices 
        /// </summary>
        RobotModule R1 = (RobotModule)ToolConfiguration.ConfiguredDevices["R1"];
        StationModule LP1 = (StationModule)ToolConfiguration.ConfiguredDevices["P1"];
        StationModule LP2 = (StationModule)ToolConfiguration.ConfiguredDevices["P2"];

        /// <summary>
        /// Reading thread from server
        /// </summary>
        Thread ReadTR; 


        /// <summary>
        /// A button click event,for connecting to server which is listening to given IP address and port in the text boxes.
        /// Once connection is success ReadTR will start to receive data from server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            connection = new ULTMConnection();
            
            string ip = IPAddressTB.Text;
            int port = Convert.ToInt32(PortNumberTB.Text);

            try
            {
                if (connection.ConnectToServer(ip, port))
                {
                    TerminalRTB.Text = "Connected to Simulator .." + Environment.NewLine;
                    commandLogger.Log("Connected to Simulator ..");
                    this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(services.ReadReceive() + "\n"); }));

                    ReadTR = new Thread(readThredServer);
                    ReadTR.Start();

                }
            }
            catch (Exception)
            {

                MessageBox.Show("Could not connect to server");
                errorLogger.Log("Connection to server fails");
            }
            
        }


        /// <summary>
        /// A button click event,to disconnect the client from server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisconnectBTN_Click(object sender, EventArgs e)
        {


            try
            {
                connection.Disconnect();

                ReadTR.Join();
                Console.WriteLine("disconnected");
                this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "Client disconnected from server"); }));
                this.Rbt_status.Invoke(new MethodInvoker(delegate ()
                { Rbt_status.Text = "--"; }));
                this.Lp_status.Invoke(new MethodInvoker(delegate ()
                { Lp_status.Text = "--"; }));
            }
            catch (Exception ex)
            {
                errorLogger.Log(ex.ToString());
            }

        }



        /// <summary>
        /// A button click event to get know the connection of configured devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InitBTN_Click(object sender, EventArgs e)
        {

            this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "Tool Initialize"); }));
            string cmd = ToolConfiguration.ToolInitialize();
            ULTMServices.sendCommand(cmd);
        }




        /// <summary>
        /// This method will continuously read receive data from the server,until the client is connected to server.
        /// </summary>
        private void readThredServer()
        {
            /*connection.isConnected() is returning bool true if the client is connected to the server or false if the client is disconnected from the server*/
            while (connection.isConnected())
            {
                try
                {
                    /*services.ReadReceive() is returning a string which received from server to client*/
                    string receive = services.ReadReceive();
                   
                    if (!String.IsNullOrEmpty(receive))
                    {
                        this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText("\r\n"+receive ); }));
                        /*Check R1 and P1 are online or offline , update module status lables accordingly*/
                        if (R1.Connection == ModuleConnection.Online)
                        {
                            this.Rbt_status.Invoke(new MethodInvoker(delegate ()
                            { Rbt_status.Text = R1.State.ToString(); }));
                        }
                        else if(R1.Connection == ModuleConnection.Offline)
                        {
                            this.Rbt_status.Invoke(new MethodInvoker(delegate ()
                            { Rbt_status.Text = "--"; }));
                        }

                        if (LP1.Connection == ModuleConnection.Online)
                        {
                            this.Lp_status.Invoke(new MethodInvoker(delegate ()
                            { Lp_status.Text = LP1.State.ToString(); }));
                        }
                        else if (LP1.Connection == ModuleConnection.Offline)
                        {
                            this.Lp_status.Invoke(new MethodInvoker(delegate ()
                            { Lp_status.Text = "--"; }));
                        }

                        commandLogger.Log(receive);
                        receive = "";
                    }

                }
                catch (Exception ex)
                {
                    errorLogger.Log(ex.ToString());
                    
                    
                }
            }
            
                
            



        }

        
        /// <summary>
        /// A button click event to perform Get Wafer from a location (station+slot)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetBTN_Click(object sender, EventArgs e)
        {
            if (connection.isConnected())
            {
                string rbt = RobotCB.Text;
                string hand = RbtHandCB.Text;
                string lp = LoadPortCB.Text;
                string slot = LPSlotCB.Text;
                if (String.IsNullOrEmpty(rbt))
                {
                    MessageBox.Show("Please select Robot");
                }
                
                if (String.IsNullOrEmpty(lp))
                {
                    MessageBox.Show("Please select a Load Port");
                }
                if (String.IsNullOrEmpty(hand))
                {
                    MessageBox.Show("Please select a Load Port");
                }
                
                /*Get selected robot hand in type of RobotArm enum */
                RobotArm Hand =new RobotArm();
                switch (hand)
                {
                    case "H1":
                        Hand = RobotArm.ARM_1;
                        break;
                    case "H2":
                        Hand = RobotArm.ARM_2;
                        break;
                }
                
                /*Construct and send the command to server accordingly */
                switch (lp)
                {
                    case "Load Port1":
                        string cmd1 = R1.GetWafer(Hand, LP1, (LoadPortSlot)Convert.ToInt32(slot));
                        ULTMServices.sendCommand(cmd1);
                        commandLogger.Log($"{cmd1}");
                        break;
                    case "Load Port2":
                        string cmd2 = R1.GetWafer(Hand, LP2, (LoadPortSlot)Convert.ToInt32(slot));
                        ULTMServices.sendCommand(cmd2);
                        commandLogger.Log($"{cmd2}");
                        break;
                };
                
                this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine+"Client>>Server:Get" ); }));

            }
            else
            {
                errorLogger.Log("Robot is not connected");
            }
        }



        /// <summary>
        /// A button click event to perform Put Wafer to a location (station+slot)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PutBTN_Click(object sender, EventArgs e)
        {
            if (connection.isConnected())
            {
                string rbt = RobotCB.Text;
                string hand = RbtHandCB.Text;
                string lp = LoadPortCB.Text;
                string slot = LPSlotCB.Text;
                if (String.IsNullOrEmpty(rbt))
                {
                    MessageBox.Show("Please select Robot");
                }

                if (String.IsNullOrEmpty(lp))
                {
                    MessageBox.Show("Please select a Load Port");
                }
                if (String.IsNullOrEmpty(hand))
                {
                    MessageBox.Show("Please select a Load Port");
                }
                /*Get selected robot hand in type of RobotArm enum */
                RobotArm Hand = new RobotArm();
                switch (hand)
                {
                    case "H1":
                        Hand = RobotArm.ARM_1;
                        break;
                    case "H2":
                        Hand = RobotArm.ARM_2;
                        break;
                }
                string cmd = "";
                /*Construct and send the command to server accordingly */
                switch (lp)
                {
                    
                    case "Load Port1":
                        cmd = R1.PutWafer(Hand, LP1, (LoadPortSlot)Convert.ToInt32(slot));
                        ULTMServices.sendCommand(cmd);
                        commandLogger.Log(cmd);
                        break;
                    case "Load Port2":
                        cmd = R1.PutWafer(Hand, LP2, (LoadPortSlot)Convert.ToInt32(slot));
                        ULTMServices.sendCommand(cmd);
                        commandLogger.Log(cmd);
                        break;
                }
                this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "Client>>Server:Put" ); }));

            }
            else
            {
                errorLogger.Log("Robot is not connected");
            }
        }


        /// <summary>
        ///  A button click event to perform Transfer a wafer from a location(station+slot) to an another location(station+slot)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TransferBTN_Click(object sender, EventArgs e)
        {
			if (connection.isConnected())
            {
                string rbt = RobotCB.Text;
                string hand = RbtHandCB.Text;
                
                string source = SourceCB.Text;
                string sourSlot = SrcSlotCB.Text;
                string destination = DestinationCB.Text;
                string destSlot = DstSlotCB.Text;
                if (String.IsNullOrEmpty(rbt))
                {
                    MessageBox.Show("Please select Robot");
                }

 

                if (String.IsNullOrEmpty(hand))
                {
                    MessageBox.Show("Please select a Load Port");
                }

                /*Get selected robot hand in type of RobotArm enum */
                RobotArm Hand = new RobotArm();
                switch (hand)
                {
                    case "H1":
                        Hand = RobotArm.ARM_1;
                        break;
                    case "H2":
                        Hand = RobotArm.ARM_2;
                        break;
                }
                /*Construct and send the command to server accordingly */
                string cmd = "";
                switch (source)
                {
                    case"Load Port1":
                        switch (destination)
                        {
                            case "Load Port1":
                                cmd = R1.Tranfer(Hand, LP1, (LoadPortSlot)Convert.ToInt32(sourSlot), LP1, (LoadPortSlot)Convert.ToInt32(destSlot));
                                ULTMServices.sendCommand(cmd);
                                commandLogger.Log(cmd);
                                break;
                            case "Load Port2":
                                cmd = R1.Tranfer(Hand, LP1, (LoadPortSlot)Convert.ToInt32(sourSlot), LP2, (LoadPortSlot)Convert.ToInt32(destSlot));
                                ULTMServices.sendCommand(cmd);
                                commandLogger.Log(cmd);
                                break;
                        }
                        break;
                    case "Load Port2":
                        switch (destination)
                        {
                            case "Load Port1":
                                cmd = R1.Tranfer(Hand, LP2, (LoadPortSlot)Convert.ToInt32(sourSlot), LP1, (LoadPortSlot)Convert.ToInt32(destSlot));
                                ULTMServices.sendCommand(cmd);
                                commandLogger.Log(cmd);
                                break;
                            case "Load Port2":
                                cmd = R1.Tranfer(Hand, LP2, (LoadPortSlot)Convert.ToInt32(sourSlot), LP2, (LoadPortSlot)Convert.ToInt32(destSlot));
                                ULTMServices.sendCommand(cmd);
                                commandLogger.Log(cmd);
                                break;
                        }
                        break;
                }
                
 

                this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine+Environment.NewLine + "Client>>Server:Transfer" ); }));

 

            }
            else
            {
                errorLogger.Log("Robot is not connected");
            }
           
        }


        /// <summary>
        /// A button click event to perform Robot map for a selected station
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapBTN_Click(object sender, EventArgs e)
        {
            if (connection.isConnected())
            {
                
                string lp = LoadPortCB.Text;
                string cmd = "";


                /*Construct and send the command to server accordingly */
                if (String.IsNullOrEmpty(lp))
                {
                    MessageBox.Show("Please select a Load Port");
                }
                
                else if(lp=="Load Port1")
                {
                    cmd = R1.Map("P1");
                    ULTMServices.sendCommand(cmd);
                    commandLogger.Log(cmd);
                }
                else if (lp == "Load Port2")
                {
                    cmd = R1.Map("P2");
                    ULTMServices.sendCommand(cmd);
                    commandLogger.Log(cmd);
                }
                

                this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "Client>>Server:Map" ); }));

            }
            else
            {
                errorLogger.Log("Robot is not connected");
            }
        }


        /// <summary>
        /// A button click event to perform robot move to a selected location code of selected slot of selected station
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveBTN_Click(object sender, EventArgs e)
        {
            if (connection.isConnected())
            {
                string rbt = RobotCB.Text;
                string hand = RbtHandCB.Text;
                string lp = LoadPortCB.Text;
                string slot = LPSlotCB.Text;
                string locationCode = locationCodeCB.Text;
               
                if (String.IsNullOrEmpty(locationCode))
                {
                    MessageBox.Show("Please select Location Code");
                }

                if (String.IsNullOrEmpty(rbt))
                {
                    MessageBox.Show("Please select Robot");
                }

                if (String.IsNullOrEmpty(lp))
                {
                    MessageBox.Show("Please select a Load Port");
                }
                if (String.IsNullOrEmpty(hand))
                {
                    MessageBox.Show("Please select a Load Port");
                }
                /*Get selected robot hand in type of RobotArm enum */
                RobotArm Hand = new RobotArm();
                switch (hand)
                {
                    case "H1":
                        Hand = RobotArm.ARM_1;
                        break;
                    case "H2":
                        Hand = RobotArm.ARM_2;
                        break;
                }
                /*Get selected location code in type of LocationCode enum */
                LocationCode LC = new LocationCode();
                switch (locationCode)
                {
                    case "pbx":
                        LC = LocationCode.pbx;
                        break;
                    case "gbx":
                        LC = LocationCode.gbx;
                        break;
                }
                /*Construct and send the command to server accordingly */
                string cmd = "";
                switch (lp)
                {
                    case "Load Port1":
                        ULTMServices.sendCommand(R1.Move(Hand, LP1, (LoadPortSlot)Convert.ToInt32(slot),LC));
                        
                        break;
                    case "Load Port2":
                        ULTMServices.sendCommand(R1.Move(Hand, LP1, (LoadPortSlot)Convert.ToInt32(slot), LC));
                        break;
                }
                
                this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "Client>>Server:Move"); }));

            }
            else
            {
                errorLogger.Log("Robot is not connected");
            }
        }


        /// <summary>
        /// A button click event to get the status of robot and get robot current possition
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetStatusBtn_Click(object sender, EventArgs e)
        {
            string rbt = RobotCB.Text;
            string hand = RbtHandCB.Text;
            string lp = LoadPortCB.Text;
            string slot = LPSlotCB.Text;
            
            if (String.IsNullOrEmpty(rbt))
            {
                MessageBox.Show("Please select Robot");
            }

            if (String.IsNullOrEmpty(lp))
            {
                MessageBox.Show("Please select a Load Port");
            }
            if (String.IsNullOrEmpty(hand))
            {
                MessageBox.Show("Please select a Load Port");
            }

            string[] statusCmd=R1.GetStatus();
            ULTMServices.sendCommand(statusCmd[0]);
            ULTMServices.sendCommand(statusCmd[1]);
            
            this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "Client>>Server:Ishome,QRYPOS"); }));
        }


        /// <summary>
        /// A button click event to serv on the robot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServBTN_Click(object sender, EventArgs e)
        {
            string rbt = RobotCB.Text;
            
            ULTMServices.sendCommand(R1.RobotServON());
            this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "Client>>Server:ServoON" ); }));

        }

        /// <summary>
        /// A button click event to serv off the robot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServOffBTN_Click(object sender, EventArgs e)
        {
            string rbt = RobotCB.Text;
           
            ULTMServices.sendCommand(R1.RobotServOFF());
            this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "Client>>Server:ServoOFF"); }));

        }




        /// <summary>
        /// A button click event to load given station
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void LoadBTN_Click(object sender, EventArgs e)
        {
            string lp = LoadPortCB.Text;
            this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "Client>>Server:Load"); }));
            /*Check selected load port connection is online before sending the command*/
            if (String.IsNullOrEmpty(lp))
            {
                MessageBox.Show("Please select a Load Port");
            }
            else if (lp == "Load Port1" && LP1.Connection == ModuleConnection.Online)
            {
                ULTMServices.sendCommand(LP1.Load());
            }
            else if (lp == "Load Port1" && LP1.Connection == ModuleConnection.Offline)
            {
                this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "LoadPort 1 Offline"); }));
            }
            else if (lp == "Load Port2" && LP2.Connection == ModuleConnection.Online)
            {
                ULTMServices.sendCommand(LP2.Load());
            }
            else if (lp == "Load Port2" && LP2.Connection == ModuleConnection.Offline)
            {
                this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "LoadPort 2 Offline"); }));
            }

        }

        /// <summary>
        /// A button click event to unload given station
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnloadBTN_Click(object sender, EventArgs e)
        {
            string lp = LoadPortCB.Text;
            this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "Client>>Server:Unload"); }));
            /*Check selected load port connection is online before sending the command*/
            if (String.IsNullOrEmpty(lp))
            {
                MessageBox.Show("Please select a Load Port");
            }
            else if (lp == "Load Port1" && LP1.Connection == ModuleConnection.Online)
            {
                ULTMServices.sendCommand(LP1.Unload());
            }
            else if (lp == "Load Port1" && LP1.Connection == ModuleConnection.Offline)
            {
                this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "LoadPort 1 Offline"); }));
            }
            else if (lp == "Load Port2" && LP2.Connection == ModuleConnection.Online)
            {
                ULTMServices.sendCommand(LP2.Unload());
            }
            else if (lp == "Load Port1" && LP2.Connection == ModuleConnection.Offline)
            {
                this.TerminalRTB.Invoke(new MethodInvoker(delegate () { TerminalRTB.AppendText(Environment.NewLine + Environment.NewLine + "LoadPort 2 Offline"); }));
            }
        }

        
    }
}
