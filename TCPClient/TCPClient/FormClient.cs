using SuperSimpleTcp;
using System.Globalization;
using System.Text;

namespace TCPClient
{
    public partial class FormClient : Form
    {     
        SimpleTcpClient client;

        public FormClient()
        {
            InitializeComponent();
        }
        
        private void FormClient_Load(object sender, EventArgs e)
        {
            //txtBoxTransactionId.CharacterCasing = CharacterCasing.Upper;
            //txtBoxProtocolId.CharacterCasing = CharacterCasing.Upper;

            txtBoxUnitId.CharacterCasing = CharacterCasing.Upper;
            txtBoxFunctionCode.CharacterCasing = CharacterCasing.Upper;
            txtBoxData.CharacterCasing = CharacterCasing.Upper;

            btnDisconnect.Enabled = false;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                client = new SimpleTcpClient(txtIP.Text + ":" + txtPort.Text);

                client.Events.Connected += Connected;
                client.Events.DataReceived += DataReceived;

                client.Connect();

                txtIP.Enabled = false;
                txtPort.Enabled = false;
                txtBoxFunctionCode.Enabled = true;

                //txtBoxTransactionId.Enabled = true;
                //txtBoxProtocolId.Enabled = true;

                comboFunctionCode.Enabled = true;
                txtBoxData.Enabled = true;
                txtBoxUnitId.Enabled = true;
            }
            catch
            {
                if((txtIP.Text == "") && (txtPort.Text == ""))
                    MessageBox.Show("Please enter an IP Address and a Port Number.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show("Please enter a correct IP Address and Port Number.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

                txtIP.Enabled = true;
                txtPort.Enabled = true;
                txtBoxFunctionCode.Enabled = false;

                //txtBoxTransactionId.Enabled = false;
                //txtBoxProtocolId.Enabled = false;

                comboFunctionCode.Enabled = false;
                txtBoxData.Enabled = false;
                txtBoxUnitId.Enabled = false;

                labelStatus.Text = "Not connected";
                labelStatus.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //def length
        public const byte unitIdLength = 0x01;
        public const byte functionCodeLength = 0x01;

        public static void addTwoBytesToBuffer(short number, byte[] arr, int indexArr)
        {
            arr[indexArr] = (byte)(number >> 8);
            arr[indexArr + 1] = (byte)(number);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (client.IsConnected)
            {
                try
                {
                    txtRequest.Text = String.Empty;
                    txtResponse.Text = String.Empty;

                    //short transactionId = short.Parse(txtBoxTransactionId.Text, NumberStyles.HexNumber);
                    //short protocolId = short.Parse(txtBoxProtocolId.Text, NumberStyles.HexNumber);
                    short transactionId = 0x0001;
                    short protocolId = 0x0000;
                    
                    byte unitId = byte.Parse(txtBoxUnitId.Text, NumberStyles.HexNumber);
                    byte functionCode = byte.Parse(txtBoxFunctionCode.Text, NumberStyles.HexNumber);
                    short[] dataFrame = txtBoxData.Text.Split(' ')
                            .Select(hex => short.Parse(hex, NumberStyles.HexNumber))
                            .ToArray();
                    short lengthOfMessage = (short)(unitIdLength + functionCodeLength + 2 * dataFrame.Length); //vf asta pe site la FC 

                    byte[] buffer = new byte[8 + 2 * dataFrame.Length]; //256 e mai corect si apoi tin cont de indexul unde
                                                                        //se termina mesajul in dataFrame
                    addTwoBytesToBuffer(transactionId, buffer, 0);
                    addTwoBytesToBuffer(protocolId, buffer, 2);
                    addTwoBytesToBuffer(lengthOfMessage, buffer, 4);
                    buffer[6] = unitId;
                    buffer[7] = functionCode;

                    int indexNumber = 8;
                    foreach (short dataElement in dataFrame)
                    {
                        addTwoBytesToBuffer(dataElement, buffer, indexNumber);
                        indexNumber += 2;
                    }

                    foreach (byte element in buffer)
                    {
                        txtRequest.Text += $" {element:X2}";
                    }

                    client.Send(buffer);
                    
                    //txtRequest.Text += Environment.NewLine;
                }
                catch
                {
                MessageBox.Show("Invalid format", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                //txtBoxTransactionId.Text = string.Empty;
                //txtBoxProtocolId.Text = string.Empty;
                //txtBoxUnitId.Text = string.Empty;
                //txtBoxFunctionCode.Text = string.Empty;
                //txtBoxData.Text = string.Empty;
            }
        }

        private void DataReceived(object sender, DataReceivedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                StringBuilder stringBuilder = new StringBuilder();

                foreach (var i in e.Data)
                    stringBuilder.Append(i.ToString("X2") + " ");

                txtResponse.Text += $" {stringBuilder.ToString()}{Environment.NewLine}{Environment.NewLine}";
            });
        }

        private void Connected(object sender, ConnectionEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                //txtInfo.Text += $"Connected to [{e.IpPort}].{Environment.NewLine}{Environment.NewLine}";

                labelStatus.Text = "Connected";
                labelStatus.ForeColor = Color.Green; 
                btnSend.Enabled = true;
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
            });
        }

        private void Disconnected(object sender, ConnectionEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                //txtInfo.Text += $"[{e.IpPort}] disconnected.{Environment.NewLine}";

                labelStatus.Text = "Not connected";
                labelStatus.ForeColor = Color.Red;
                btnConnect.Enabled = true;
                txtIP.Enabled = true;
                txtPort.Enabled = true;
            });
        }

        private void txtInfo_TextChanged(object sender, EventArgs e)
        {
            txtRequest.SelectionStart = txtRequest.TextLength;
            txtRequest.ScrollToCaret();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtRequest.Text = String.Empty;
            txtResponse.Text = String.Empty;
        }
    }
}