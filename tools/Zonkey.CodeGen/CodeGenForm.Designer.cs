namespace ZonkeyCodeGen
{
    partial class CodeGenForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.cboServer = new System.Windows.Forms.ComboBox();
            this.lblDB = new System.Windows.Forms.Label();
            this.cboDB = new System.Windows.Forms.ComboBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.chkSystemDB = new System.Windows.Forms.CheckBox();
            this.lbTables = new System.Windows.Forms.ListBox();
            this.lblTables = new System.Windows.Forms.Label();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.txtNamespace = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.codeTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.vBNETToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.txtFilepath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.btnLocateServers = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.txtUserID = new System.Windows.Forms.TextBox();
            this.chkTrustedConnection = new System.Windows.Forms.CheckBox();
            this.chkSystemTables = new System.Windows.Forms.CheckBox();
            this.cboCollectionType = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.lbViews = new System.Windows.Forms.ListBox();
            this.lblViews = new System.Windows.Forms.Label();
            this.chkSystemViews = new System.Windows.Forms.CheckBox();
            this.btnGenerateVO = new System.Windows.Forms.Button();
            this.btnTableClear = new System.Windows.Forms.Button();
            this.btnViewClear = new System.Windows.Forms.Button();
            this.chkDCAdapter = new System.Windows.Forms.CheckBox();
            this.menuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 41);
            this.label1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 2);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Servers";
            // 
            // cboServer
            // 
            this.cboServer.FormattingEnabled = true;
            this.cboServer.Location = new System.Drawing.Point(12, 58);
            this.cboServer.Margin = new System.Windows.Forms.Padding(3, 1, 3, 3);
            this.cboServer.Name = "cboServer";
            this.cboServer.Size = new System.Drawing.Size(344, 21);
            this.cboServer.TabIndex = 0;
            this.cboServer.SelectedIndexChanged += new System.EventHandler(this.cboServer_SelectedIndexChanged);
            // 
            // lblDB
            // 
            this.lblDB.AutoSize = true;
            this.lblDB.Location = new System.Drawing.Point(12, 180);
            this.lblDB.Name = "lblDB";
            this.lblDB.Size = new System.Drawing.Size(58, 13);
            this.lblDB.TabIndex = 2;
            this.lblDB.Text = "Databases";
            // 
            // cboDB
            // 
            this.cboDB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDB.Enabled = false;
            this.cboDB.FormattingEnabled = true;
            this.cboDB.Location = new System.Drawing.Point(12, 198);
            this.cboDB.Name = "cboDB";
            this.cboDB.Size = new System.Drawing.Size(419, 21);
            this.cboDB.TabIndex = 2;
            this.cboDB.SelectedIndexChanged += new System.EventHandler(this.cboDB_SelectedIndexChanged);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(362, 56);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 1;
            this.btnConnect.Text = "&Connect";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // chkSystemDB
            // 
            this.chkSystemDB.AutoSize = true;
            this.chkSystemDB.Enabled = false;
            this.chkSystemDB.Location = new System.Drawing.Point(12, 226);
            this.chkSystemDB.Name = "chkSystemDB";
            this.chkSystemDB.Size = new System.Drawing.Size(144, 17);
            this.chkSystemDB.TabIndex = 6;
            this.chkSystemDB.Text = "Show System Databases";
            this.chkSystemDB.CheckedChanged += new System.EventHandler(this.chkSystemDB_CheckedChanged);
            // 
            // lbTables
            // 
            this.lbTables.Enabled = false;
            this.lbTables.FormattingEnabled = true;
            this.lbTables.Location = new System.Drawing.Point(11, 276);
            this.lbTables.Name = "lbTables";
            this.lbTables.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbTables.Size = new System.Drawing.Size(202, 173);
            this.lbTables.TabIndex = 3;
            // 
            // lblTables
            // 
            this.lblTables.AutoSize = true;
            this.lblTables.Location = new System.Drawing.Point(12, 258);
            this.lblTables.Name = "lblTables";
            this.lblTables.Size = new System.Drawing.Size(39, 13);
            this.lblTables.TabIndex = 9;
            this.lblTables.Text = "Tables";
            // 
            // btnGenerate
            // 
            this.btnGenerate.Enabled = false;
            this.btnGenerate.Location = new System.Drawing.Point(112, 652);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(97, 23);
            this.btnGenerate.TabIndex = 9;
            this.btnGenerate.Text = "Generate Code";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // txtNamespace
            // 
            this.txtNamespace.Location = new System.Drawing.Point(11, 509);
            this.txtNamespace.Name = "txtNamespace";
            this.txtNamespace.Size = new System.Drawing.Size(339, 20);
            this.txtNamespace.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 491);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Namespace";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.codeTypeToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(450, 24);
            this.menuStrip1.TabIndex = 14;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // codeTypeToolStripMenuItem
            // 
            this.codeTypeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cToolStripMenuItem,
            this.vBNETToolStripMenuItem});
            this.codeTypeToolStripMenuItem.Name = "codeTypeToolStripMenuItem";
            this.codeTypeToolStripMenuItem.Size = new System.Drawing.Size(102, 20);
            this.codeTypeToolStripMenuItem.Text = "&Code Language";
            // 
            // cToolStripMenuItem
            // 
            this.cToolStripMenuItem.Checked = true;
            this.cToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cToolStripMenuItem.Name = "cToolStripMenuItem";
            this.cToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.cToolStripMenuItem.Text = "C#";
            this.cToolStripMenuItem.Click += new System.EventHandler(this.cToolStripMenuItem_Click);
            // 
            // vBNETToolStripMenuItem
            // 
            this.vBNETToolStripMenuItem.Name = "vBNETToolStripMenuItem";
            this.vBNETToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.vBNETToolStripMenuItem.Text = "VB.NET";
            this.vBNETToolStripMenuItem.Click += new System.EventHandler(this.vBNETToolStripMenuItem_Click);
            // 
            // txtFilepath
            // 
            this.txtFilepath.Location = new System.Drawing.Point(12, 617);
            this.txtFilepath.Name = "txtFilepath";
            this.txtFilepath.Size = new System.Drawing.Size(338, 20);
            this.txtFilepath.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 600);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(83, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Output File Path";
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(356, 615);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 8;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnLocateServers
            // 
            this.btnLocateServers.Location = new System.Drawing.Point(362, 28);
            this.btnLocateServers.Name = "btnLocateServers";
            this.btnLocateServers.Size = new System.Drawing.Size(75, 23);
            this.btnLocateServers.TabIndex = 18;
            this.btnLocateServers.TabStop = false;
            this.btnLocateServers.Text = "Enumerate";
            this.btnLocateServers.UseVisualStyleBackColor = true;
            this.btnLocateServers.Click += new System.EventHandler(this.btnLocateServers_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtPassword);
            this.groupBox1.Controls.Add(this.txtUserID);
            this.groupBox1.Controls.Add(this.chkTrustedConnection);
            this.groupBox1.Location = new System.Drawing.Point(12, 93);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(419, 69);
            this.groupBox1.TabIndex = 24;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Database Credentials";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(304, 19);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Password";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(188, 19);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(43, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "User ID";
            // 
            // txtPassword
            // 
            this.txtPassword.Enabled = false;
            this.txtPassword.Location = new System.Drawing.Point(304, 36);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(100, 20);
            this.txtPassword.TabIndex = 2;
            // 
            // txtUserID
            // 
            this.txtUserID.Enabled = false;
            this.txtUserID.Location = new System.Drawing.Point(188, 36);
            this.txtUserID.Name = "txtUserID";
            this.txtUserID.Size = new System.Drawing.Size(100, 20);
            this.txtUserID.TabIndex = 1;
            // 
            // chkTrustedConnection
            // 
            this.chkTrustedConnection.AutoSize = true;
            this.chkTrustedConnection.Checked = true;
            this.chkTrustedConnection.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkTrustedConnection.Location = new System.Drawing.Point(15, 36);
            this.chkTrustedConnection.Name = "chkTrustedConnection";
            this.chkTrustedConnection.Size = new System.Drawing.Size(141, 17);
            this.chkTrustedConnection.TabIndex = 0;
            this.chkTrustedConnection.Text = "Use Trusted Connection";
            this.chkTrustedConnection.UseVisualStyleBackColor = true;
            this.chkTrustedConnection.CheckedChanged += new System.EventHandler(this.chkTrustedConnection_CheckedChanged);
            // 
            // chkSystemTables
            // 
            this.chkSystemTables.AutoSize = true;
            this.chkSystemTables.Enabled = false;
            this.chkSystemTables.Location = new System.Drawing.Point(11, 455);
            this.chkSystemTables.Name = "chkSystemTables";
            this.chkSystemTables.Size = new System.Drawing.Size(125, 17);
            this.chkSystemTables.TabIndex = 25;
            this.chkSystemTables.TabStop = false;
            this.chkSystemTables.Text = "Show System Tables";
            this.chkSystemTables.UseVisualStyleBackColor = true;
            this.chkSystemTables.CheckedChanged += new System.EventHandler(this.chkSystemTables_CheckedChanged);
            // 
            // cboCollectionType
            // 
            this.cboCollectionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCollectionType.FormattingEnabled = true;
            this.cboCollectionType.Location = new System.Drawing.Point(12, 562);
            this.cboCollectionType.Name = "cboCollectionType";
            this.cboCollectionType.Size = new System.Drawing.Size(181, 21);
            this.cboCollectionType.TabIndex = 6;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 542);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(86, 13);
            this.label6.TabIndex = 27;
            this.label6.Text = "Typed Collection";
            // 
            // lbViews
            // 
            this.lbViews.FormattingEnabled = true;
            this.lbViews.Location = new System.Drawing.Point(229, 276);
            this.lbViews.Name = "lbViews";
            this.lbViews.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbViews.Size = new System.Drawing.Size(202, 173);
            this.lbViews.TabIndex = 4;
            // 
            // lblViews
            // 
            this.lblViews.AutoSize = true;
            this.lblViews.Location = new System.Drawing.Point(229, 257);
            this.lblViews.Name = "lblViews";
            this.lblViews.Size = new System.Drawing.Size(35, 13);
            this.lblViews.TabIndex = 29;
            this.lblViews.Text = "Views";
            // 
            // chkSystemViews
            // 
            this.chkSystemViews.AutoSize = true;
            this.chkSystemViews.Enabled = false;
            this.chkSystemViews.Location = new System.Drawing.Point(229, 455);
            this.chkSystemViews.Name = "chkSystemViews";
            this.chkSystemViews.Size = new System.Drawing.Size(121, 17);
            this.chkSystemViews.TabIndex = 30;
            this.chkSystemViews.TabStop = false;
            this.chkSystemViews.Text = "Show System Views";
            this.chkSystemViews.UseVisualStyleBackColor = true;
            this.chkSystemViews.CheckedChanged += new System.EventHandler(this.chkSystemViews_CheckedChanged);
            // 
            // btnGenerateVO
            // 
            this.btnGenerateVO.Enabled = false;
            this.btnGenerateVO.Location = new System.Drawing.Point(251, 651);
            this.btnGenerateVO.Name = "btnGenerateVO";
            this.btnGenerateVO.Size = new System.Drawing.Size(88, 23);
            this.btnGenerateVO.TabIndex = 10;
            this.btnGenerateVO.Text = "Generate VO Code";
            this.btnGenerateVO.UseVisualStyleBackColor = true;
            this.btnGenerateVO.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // btnTableClear
            // 
            this.btnTableClear.Location = new System.Drawing.Point(170, 451);
            this.btnTableClear.Name = "btnTableClear";
            this.btnTableClear.Size = new System.Drawing.Size(43, 23);
            this.btnTableClear.TabIndex = 32;
            this.btnTableClear.TabStop = false;
            this.btnTableClear.Text = "Clear";
            this.btnTableClear.UseVisualStyleBackColor = true;
            this.btnTableClear.Click += new System.EventHandler(this.btnTableClear_Click);
            // 
            // btnViewClear
            // 
            this.btnViewClear.Location = new System.Drawing.Point(388, 451);
            this.btnViewClear.Name = "btnViewClear";
            this.btnViewClear.Size = new System.Drawing.Size(43, 23);
            this.btnViewClear.TabIndex = 33;
            this.btnViewClear.TabStop = false;
            this.btnViewClear.Text = "Clear";
            this.btnViewClear.UseVisualStyleBackColor = true;
            this.btnViewClear.Click += new System.EventHandler(this.btnViewClear_Click);
            // 
            // chkDCAdapter
            // 
            this.chkDCAdapter.AutoSize = true;
            this.chkDCAdapter.Location = new System.Drawing.Point(232, 565);
            this.chkDCAdapter.Name = "chkDCAdapter";
            this.chkDCAdapter.Size = new System.Drawing.Size(144, 17);
            this.chkDCAdapter.TabIndex = 34;
            this.chkDCAdapter.Text = "Generate DCAdapter<T>";
            this.chkDCAdapter.UseVisualStyleBackColor = true;
            // 
            // CodeGenForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(450, 688);
            this.Controls.Add(this.chkDCAdapter);
            this.Controls.Add(this.btnViewClear);
            this.Controls.Add(this.btnTableClear);
            this.Controls.Add(this.btnGenerateVO);
            this.Controls.Add(this.chkSystemViews);
            this.Controls.Add(this.lblViews);
            this.Controls.Add(this.lbViews);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cboCollectionType);
            this.Controls.Add(this.chkSystemTables);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnLocateServers);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtFilepath);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtNamespace);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.lblTables);
            this.Controls.Add(this.lbTables);
            this.Controls.Add(this.chkSystemDB);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.cboDB);
            this.Controls.Add(this.lblDB);
            this.Controls.Add(this.cboServer);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "CodeGenForm";
            this.Text = "Zonkey DataClass Code Generator";
            this.Load += new System.EventHandler(this.CodeGenForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cboServer;
        private System.Windows.Forms.Label lblDB;
        private System.Windows.Forms.ComboBox cboDB;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.CheckBox chkSystemDB;
        private System.Windows.Forms.ListBox lbTables;
        private System.Windows.Forms.Label lblTables;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.TextBox txtNamespace;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem codeTypeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem vBNETToolStripMenuItem;
        private System.Windows.Forms.TextBox txtFilepath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button btnLocateServers;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.TextBox txtUserID;
        private System.Windows.Forms.CheckBox chkTrustedConnection;
        private System.Windows.Forms.CheckBox chkSystemTables;
        private System.Windows.Forms.ComboBox cboCollectionType;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ListBox lbViews;
        private System.Windows.Forms.Label lblViews;
        private System.Windows.Forms.CheckBox chkSystemViews;
        private System.Windows.Forms.Button btnGenerateVO;
		private System.Windows.Forms.Button btnTableClear;
		private System.Windows.Forms.Button btnViewClear;
        private System.Windows.Forms.CheckBox chkDCAdapter;
    }
}

