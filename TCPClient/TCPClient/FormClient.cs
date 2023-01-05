using SuperSimpleTcp;
using System.Globalization;
using System.Text;

namespace TCPClient
{
    public partial class FormClient : Form
    {
        SimpleTcpClient client;
        FunctionCodes fc = new FunctionCodes();
        System.Diagnostics.Stopwatch executionTime = new System.Diagnostics.Stopwatch();

       
        public FormClient()
        {
            InitializeComponent();

            toolTipForm.SetToolTip(btnConnect, "Connect to the device");
            toolTipForm.SetToolTip(btnSend, "Send a request");
            toolTipForm.SetToolTip(btnClear, "Clear request and response messages from text boxes");
            toolTipForm.SetToolTip(btnHistory, "View message history");
        }

        private void FormClient_Load(object sender, EventArgs e)
        {
            btnDisconnect.Enabled = false;
            panelMessage.Enabled = false;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                client = new SimpleTcpClient(richtxtIP.Text + ":" + richtxtPort.Text);

                client.Events.Connected += Connected;
                client.Events.DataReceived += DataReceived;

                client.Connect();

                comboFunctionCode.SelectedIndexChanged += comboFunctionCode_SelectedIndexChanged;
            }
            catch
            {
                if ((richtxtIP.Text == "") && (richtxtPort.Text == ""))
                    MessageBox.Show("Please enter an IP Address and a Port Number.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show("Please enter a correct IP Address and Port Number.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Connected(object sender, ConnectionEventArgs e)
        {
            labelStatus2.Text = "Connected";
            labelStatus2.ForeColor = Color.Green;
            panelMessage.Enabled = true;
            btnSend.Enabled = true;
            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                client.Events.Disconnected += Disconnected;
                //client.Disconnect(); //crash

                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
                btnSend.Enabled = false;

                richtxtIP.Enabled = true;
                richtxtPort.Enabled = true;

                labelStatus2.Text = "Not connected";
                labelStatus2.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Disconnected(object sender, ConnectionEventArgs e)
        {
            labelStatus2.Text = "Not connected";
            labelStatus2.ForeColor = Color.Red;
            btnConnect.Enabled = true;
            richtxtIP.Enabled = true;
            richtxtPort.Enabled = true;
        }

        private void comboSlave_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboSlave.SelectedIndex == 0)
            {
                slaveId = COM100Id;
                richtxtSlaveId.Visible = false;
            }
            else
                richtxtSlaveId.Visible = true;
        }

        private void comboFunctionCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            selected03 = (comboFunctionCode.SelectedIndex == 0);
            selected06 = (comboFunctionCode.SelectedIndex == 1);
            selected16 = (comboFunctionCode.SelectedIndex == 2);

            if (selected03)
            {
                functionCode = fc03;
                
                panelValues.Enabled = false;
                panelRegsNumber.Enabled = true;
                //richtxtValues.Width = 62;
                //richtxtValues.Height = 26;
                richtxtValues.MaxLength = 4;
            }
            else if (selected06)
            {
                functionCode = fc06;
                
                panelRegsNumber.Enabled = false;
                panelValues.Enabled = true;
                //richtxtValues.Width = 62;
                //richtxtValues.Height = 26;
                richtxtValues.MaxLength = 4;
            }
            else if (selected16)
            {
                functionCode = fc16;
                
                panelRegsNumber.Enabled = true;
                panelValues.Enabled = true;
                //richtxtValues.Width = 439;
                //richtxtValues.Height = 52;
            } 
        }

        public void BuildRequest()
        {
            counterTransactionId++;
            richtxtTransactionId.Text = counterTransactionId.ToString("X4");

            if(comboSlave.SelectedIndex != 0)
                slaveId = byte.Parse(richtxtSlaveId.Text, NumberStyles.HexNumber);

            if (selected03)
            {
                bufferRequest = new byte[bufferLength03];
                fc.ReadHoldingRegisters(bufferRequest, richtxtTransactionId.Text, protocolId, slaveId, functionCode, richtxtAddress.Text, richtxtNumberRegs.Text);
            }
            else if (selected06)
            {
                bufferRequest = new byte[bufferLength06];
                fc.PresetSingleRegister(bufferRequest, richtxtTransactionId.Text, protocolId, slaveId, functionCode, richtxtAddress.Text, richtxtValues.Text);
            }
            else if (selected16)
            {
                bufferRequest = new byte[bufferLength16 + (2 * counterNoOfRegisters)];
                fc.PresetMultipleRegisters(bufferRequest, richtxtTransactionId.Text, protocolId, slaveId, functionCode, richtxtAddress.Text, richtxtNumberRegs.Text, richtxtValues.Text);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (client.IsConnected)
            {
                richtxtRequest.Text = String.Empty;
                richtxtResponse.Text = String.Empty;
                richtxtAnalyzeResponse.Text = String.Empty;

                try
                {
                    BuildRequest();
                    client.Send(bufferRequest);

                    foreach (byte element in bufferRequest)
                        richtxtRequest.Text += $" {element:X2}";
                }
                catch
                {
                    MessageBox.Show("Invalid format.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    richtxtAnalyzeResponse.Text = "Invalid format.";

                }
            }
        }

        private void DataReceived(object sender, DataReceivedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                bufferResponse = new byte[e.Data.Count];

                int indexBuffer = 0;
                foreach (byte element in e.Data)
                {
                    bufferResponse[indexBuffer] = element;
                    indexBuffer++;
                }

                foreach (byte element in bufferResponse)
                    richtxtResponse.Text += $" {element:X2}";

                AnalyzeResponse(bufferResponse, bufferRequest);
            });
        }

        private void AddToHistory(byte[] request, byte[] response)
        {
            addMessageToHistory += $"[{DateTime.Now}]{Environment.NewLine}->";
            
            foreach (byte element in request)
                addMessageToHistory += $" {element:X2}";
            addMessageToHistory += $"{Environment.NewLine}";
            
            addMessageToHistory += $"<-";
            foreach (byte element in response)
                addMessageToHistory += $" {element:X2}";
            addMessageToHistory += $"{Environment.NewLine}{Environment.NewLine}";
        }

        private void AnalyzeResponse(byte[] response, byte[] request)
        {
            richtxtAnalyzeResponse.Enabled = true;

            if ((response[(int)Header.TransactionId] == request[(int)Header.TransactionId]) && (response[(int)Header.ProtocolId] == request[(int)Header.ProtocolId]))
            {
                if (response[(int)Header.SlaveId] == request[(int)Header.SlaveId])
                {
                    if (response[(int)MessageStructure.FunctionCode] == request[(int)MessageStructure.FunctionCode]) 
                    { 
                        richtxtAnalyzeResponse.Text = "Correct response.";
                        
                        AddToHistory(request, response);
                        // add to history only the correct responses?
                    }
                    else if ( response[(int)MessageStructure.FunctionCode] == highestBitSet + request[(int)MessageStructure.FunctionCode] )
                    {
                        richtxtAnalyzeResponse.Text = "The function code in the response has its highest bit set.";

                        switch (response[(int)MessageStructure.ExceptionCode])
                        {
                            case 0x02:  richtxtAnalyzeResponse.Text += "\nException Code 02: Illegal Data Address. " +
                                                               "\n\n'The data address received in the query is not an allowable address for the slave.'";
                                        break;
                            
                            case 0x03:  richtxtAnalyzeResponse.Text += "\nException Code 03: Illegal Data Value. " +
                                                               "\n\n'A value contained in the query data field is not an allowable value for the slave.'";
                                        break;
                            
                            case 0x0A:  richtxtAnalyzeResponse.Text += "\nException Code 0A: Gateway Path Unavailable. " +
                                                               "\n\n'The gateway was unable to allocate an internal communication path from " +
                                                               "the input port to the output port for processing the request.'";
                                        break;
                            
                            default:    richtxtAnalyzeResponse.Text = "\nException response.";
                                        break;
                        }
                    }
                    else
                        richtxtAnalyzeResponse.Text = "Incorrect response.";
                }
                else
                    richtxtAnalyzeResponse.Text = "Different Slave ID in the response.";
            }
            else
                richtxtAnalyzeResponse.Text = "Incorrect response.";
        }

        private void btnMinus_Click(object sender, EventArgs e)
        {
            if (counterNoOfRegisters > 0)
            {
                counterNoOfRegisters--;
                richtxtNumberRegs.Text = counterNoOfRegisters.ToString("X4");
            }
            else
                btnMinus.Enabled = false;

            if (selected16)
                richtxtValues.MaxLength = 5 * counterNoOfRegisters - 1;
        }

        private void btnPlus_Click(object sender, EventArgs e)
        {
            if (counterNoOfRegisters > 0)
                btnMinus.Enabled = true;

            counterNoOfRegisters++;
            richtxtNumberRegs.Text = counterNoOfRegisters.ToString("X4");

            if (selected16)
                richtxtValues.MaxLength = 5 * counterNoOfRegisters - 1;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            richtxtRequest.Text = String.Empty;
            richtxtResponse.Text = String.Empty;
        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            //if (client.IsConnected)
            //{
                try
                {
                    FormHistory formHistory = new FormHistory();
                    formHistory.Show();
                }
                catch
                {
                    MessageBox.Show("Device disconnected.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            //}
        }
    }
}