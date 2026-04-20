
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Data;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;

namespace RawSocketMonitor
{
    public partial class RawSocketForm : Form
    {

        // Buffer for receiving packets
        byte[] bytes = new byte[1600];

        // Raw socket for packet capture
        Socket socket;

        // Background thread for packet processing
        Thread myThread;
        System.Threading.CancellationTokenSource cancelTokenSource;

        // Protocol/port mapping for application protocol detection
        private static readonly Dictionary<ushort, string> PortProtocolMap = new Dictionary<ushort, string>
        {
            { 53, "DNS" }, { 80, "HTTP" }, { 21, "FTP" }, { 22, "SSH" }, { 23, "Telnet" },
            { 25, "SMTP" }, { 67, "DHCP" }, { 68, "DHCP" }, { 15, "Netstat" }, { 66, "SQL*NET" },
            { 70, "GOPHER" }, { 79, "FINGER" }, { 88, "KERBEROS" }, { 109, "POP2" }, { 110, "POP3" },
            { 115, "SFTP" }, { 137, "NET-BIOS" }, { 138, "NET-BIOS" }, { 139, "NET-BIOS" },
            { 143, "IMAP" }, { 161, "SNMP" }, { 179, "BGP" }, { 220, "IMAP3" }, { 363, "RSVP" },
            { 389, "LDAP" }, { 434, "MOBILE IP" }, { 443, "SSL" }, { 458, "QUICK-TIME TV" },
            { 520, "RIP" }, { 554, "RTSP" }, { 1433, "MS-SQL" }, { 1521, "ORACLE-SQL" },
            { 1720, "H.323" }, { 1755, "Windows-Media" }, { 2049, "NFS" }, { 2543, "SIP" },
            { 3306, "My-SQL" }, { 5004, "RTP" }, { 5005, "RTP" }, { 5060, "SIP" }
        };

        private MenuStrip menuStrip;
        private ToolStripMenuItem itemStart, itemStop;

        private ListView myList;
        private Label ipLabel;
        private TextBox ipTextBox;

        public RawSocketForm()
        {
            try
            {
                myList = new ListView();

                // Create MenuStrip and menu items
                                // IP Address input controls
                                ipLabel = new Label();
                                ipLabel.Text = "Local IPv4:";
                                ipLabel.Location = new Point(10, 24);
                                ipLabel.Size = new Size(70, 20);

                                ipTextBox = new TextBox();
                                ipTextBox.Location = new Point(85, 22);
                                ipTextBox.Size = new Size(120, 24);
                                ipTextBox.Text = "127.0.0.1"; // Default value, user can change

                                Controls.Add(ipLabel);
                                Controls.Add(ipTextBox);
                menuStrip = new MenuStrip();
                var fileMenu = new ToolStripMenuItem("&File");
                var viewMenu = new ToolStripMenuItem("&View");

                itemStart = new ToolStripMenuItem("&Start", null, this.Start, Keys.Control | Keys.S);
                itemStart.Checked = false;
                fileMenu.DropDownItems.Add(itemStart);

                itemStop = new ToolStripMenuItem("Sto&p", null, this.Stop, Keys.Control | Keys.P);
                itemStop.Checked = true;
                fileMenu.DropDownItems.Add(itemStop);

                var itemExit = new ToolStripMenuItem("E&xit", null, this.Close, Keys.Control | Keys.X);
                fileMenu.DropDownItems.Add(itemExit);

                menuStrip.Items.Add(fileMenu);
                menuStrip.Items.Add(viewMenu);
                this.MainMenuStrip = menuStrip;
                Controls.Add(menuStrip);

                Text = "Network Monitor";
                this.AutoScroll = true;
                this.Size = new Size(600, 400);
                CenterToScreen();
                this.BackColor = Color.LightSteelBlue;
                this.FormClosed += new FormClosedEventHandler(ShutDown);

                // Set to details view.
                myList.View = View.Details;
                myList.Anchor = AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left;
                myList.Columns.Add("Source Addr", -2, HorizontalAlignment.Left);
                myList.Columns.Add("Dest Addr", -2, HorizontalAlignment.Left);
                myList.Columns.Add("Source Port", -2, HorizontalAlignment.Left);
                myList.Columns.Add("Dest Port", -2, HorizontalAlignment.Left);
                myList.Columns.Add("Transport Protocol", -2, HorizontalAlignment.Left);
                myList.Columns.Add("Application Protocol", -2, HorizontalAlignment.Left);
                myList.Columns.Add("Packet Length", -2, HorizontalAlignment.Left);
                myList.Columns.Add("PayLoad", -2, HorizontalAlignment.Left);
                myList.Size = new Size(550, 300);
                myList.Location = new Point(0, 54); // below menu and IP input
                Controls.Add(myList);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in RawSocketForm constructor:\n" + ex.ToString());
                MessageBox.Show("Exception in RawSocketForm constructor:\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ToolBar event handler not needed
        // (No Main method; use Program.cs as entry point)

        public void Start(object sender, EventArgs e)
        {
            if (myThread != null) return;
            string ipInput = ipTextBox.Text.Trim();
            if (!System.Net.IPAddress.TryParse(ipInput, out var ipAddr) || ipAddr.AddressFamily != AddressFamily.InterNetwork)
            {
                MessageBox.Show("Please enter a valid IPv4 address.", "Invalid IP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                // (Re)create and bind the socket
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                socket.Bind(new IPEndPoint(ipAddr, 5000));
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, 1);
                byte[] IN = new byte[4] { 1, 0, 0, 0 };
                byte[] OUT = new byte[4];
                int SIO_RCVALL = unchecked((int)0x98000001);
                int ret_code = socket.IOControl(SIO_RCVALL, IN, OUT);
            }
            catch (SocketException ex)
            {
                MessageBox.Show($"Could not create a socket with the IP address you entered.\nDetails: {ex.Message}",
                "Socket Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception creating socket:\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            cancelTokenSource = new System.Threading.CancellationTokenSource();
            myThread = new Thread(() => DoPacket(cancelTokenSource.Token));
            myThread.Start();
            itemStart.Checked = true;
            itemStop.Checked = false;
        }

        public void Stop(object sender, EventArgs e)
        {
            if (myThread != null && cancelTokenSource != null)
            {
                cancelTokenSource.Cancel();
                if (socket != null)
                {
                    try { socket.Close(); } catch { }
                }
                myThread.Join();
                myThread = null;
                cancelTokenSource.Dispose();
                cancelTokenSource = null;
                itemStop.Checked = true;
                itemStart.Checked = false;
            }
        }

        public void Close(object sender, EventArgs e)
        {
            this.Close();
        }

        public void ShutDown(object sender, EventArgs e)
        {
            if (myThread != null && cancelTokenSource != null)
            {
                cancelTokenSource.Cancel();
                if (socket != null)
                {
                    try { socket.Close(); } catch { }
                }
                myThread.Join();
                myThread = null;
                cancelTokenSource.Dispose();
                cancelTokenSource = null;
            }
            else if (socket != null)
            {
                try { socket.Close(); } catch { }
            }
        }

        /// <summary>
        /// Convert a short from network order to little endian order.
        /// </summary>
        public ushort Ntohs(int offset)
        {
            uint temp1 = bytes[offset];
            uint temp2 = bytes[offset + 1];
            temp1 <<= 8;
            return (ushort)(temp1 | temp2);
        }


        /// <summary>
        /// Process a received packet and add its details to the ListView.
        /// </summary>
        public void ProcessPacket(byte[] buffer)
        {
            try
            {
                // Extract IP header fields
                byte protocolNum = buffer[9];
                ushort totalLength = Ntohs(2);
                uint ipHeaderLength = (uint)((buffer[0] & 0x0F) * 4);
                uint tcpHeaderLength = (uint)((buffer[ipHeaderLength + 12] >> 4) * 4);
                uint payLoad = 0;
                string protocolStr;

                // Determine transport protocol and payload length
                if (protocolNum == 17) // UDP
                {
                    protocolStr = "UDP";
                    payLoad = totalLength - ipHeaderLength - 8;
                }
                else if (protocolNum == 6) // TCP
                {
                    protocolStr = "TCP";
                    payLoad = totalLength - ipHeaderLength - tcpHeaderLength;
                }
                else if (protocolNum == 1) // ICMP
                {
                    protocolStr = "ICMP";
                    payLoad = totalLength - ipHeaderLength - 8; // ICMP header is typically 8 bytes
                }
                else if (protocolNum == 2) // IGMP
                {
                    protocolStr = "IGMP";
                    payLoad = totalLength - ipHeaderLength - 8; // IGMP header is typically 8 bytes
                }
                else
                {
                    protocolStr = protocolNum.ToString();
                }

                // Source and destination IP addresses
                string sourceStr = $"{buffer[12]}.{buffer[13]}.{buffer[14]}.{buffer[15]}";
                string destStr = $"{buffer[16]}.{buffer[17]}.{buffer[18]}.{buffer[19]}";

                // Source and destination ports
                ushort sourcePort = Ntohs((ushort)ipHeaderLength);
                ushort destPort = Ntohs((ushort)ipHeaderLength + 2);

                // Application protocol detection
                string appProtocol = "";
                if (PortProtocolMap.TryGetValue(destPort, out var proto))
                    appProtocol = proto;
                else if (PortProtocolMap.TryGetValue(sourcePort, out proto))
                    appProtocol = proto;

                // Add packet info to ListView (thread-safe)
                ListViewItem listItem = new ListViewItem(sourceStr);
                listItem.SubItems.Add(destStr);
                listItem.SubItems.Add(sourcePort.ToString());
                listItem.SubItems.Add(destPort.ToString());
                listItem.SubItems.Add(protocolStr);
                listItem.SubItems.Add(appProtocol);
                listItem.SubItems.Add(totalLength.ToString());
                listItem.SubItems.Add(payLoad.ToString());

                if (myList.InvokeRequired)
                    myList.Invoke(new Action(() => myList.Items.Add(listItem)));
                else
                    myList.Items.Add(listItem);
            }
            catch (Exception ex)
            {
                // Log or display error if needed
                Console.WriteLine($"Error processing packet: {ex.Message}");
            }
        }

        public void DoPacket(System.Threading.CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRec = socket.Receive(bytes, 0, bytes.Length, SocketFlags.None);
                    //Console.WriteLine("#Bytes Received: " + bytesRec);
                    ProcessPacket(bytes);
                }
            }
            catch (SocketException)
            {
                // Handle socket exceptions if needed
            }
            catch (ObjectDisposedException)
            {
                // Socket closed, exit thread
            }
        }
    }
}
