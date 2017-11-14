using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using ZonkeyCodeGen.CodeGen;
using ZonkeyCodeGen.Utilities;

namespace ZonkeyCodeGen
{
    partial class CodeGenForm : Form
    {
        #region Private Variables

        private ServerConnection _serverConnection;
        private Server _selectedServer;
        private Database _selectedDatabase;
        private readonly Dictionary<string, string> _schemaDictionary = new Dictionary<string,string>();
        private readonly Dictionary<string, TableViewBase> _tableViewDictionary = new Dictionary<string, TableViewBase>();

        private Thread tblThread;
        private Thread vwThread;

        #endregion

        #region Form Constructor

        public CodeGenForm()
        {
            InitializeComponent();
        }

        #endregion

        #region Form Events

        private void CodeGenForm_Load(object sender, EventArgs e)
        {
            InitializeForm();
        }

        #endregion

        #region Button Events

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFilepath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void btnLocateServers_Click(object sender, EventArgs e)
        {
            try
            {
                RightToLeftLayout = true;
                Cursor = Cursors.WaitCursor;

                InitializeForm();

                //  Get a list of SQL servers available on the networks
                var dtSQLServers = SmoApplication.EnumAvailableSqlServers(false);
                foreach (DataRow drServer in dtSQLServers.Rows)
                {
                    var ServerName = drServer["Server"].ToString();

                    if (drServer["Instance"] != null && drServer["Instance"].ToString().Length > 0)
                        ServerName += @"\" + drServer["Instance"];

                    if (cboServer.Items.IndexOf(ServerName) < 0)
                        cboServer.Items.Add(ServerName);
                }

                //  By default select the local server
                var LocalServer = new Server();
                var LocalServerName = LocalServer.Name;
                if (!string.IsNullOrEmpty(LocalServer.InstanceName))
                    LocalServerName += @"\" + LocalServer.InstanceName;

                var ItemIndex = cboServer.FindStringExact(LocalServerName);
                if (ItemIndex >= 0)
                    cboServer.SelectedIndex = ItemIndex;
            }
            catch (SmoException smoException)
            {
                MessageBox.Show(smoException.ToString());
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cboServer.Text.Trim())) return;

            try
            {
                Cursor = Cursors.WaitCursor;

                //  Fill the databases combo
                cboDB.Items.Clear();
                lbTables.Items.Clear();

                _serverConnection = (chkTrustedConnection.Checked) 
                                    ? new ServerConnection(cboServer.Text) 
                                    : new ServerConnection(cboServer.Text, txtUserID.Text, txtPassword.Text);

                _selectedServer = new Server(_serverConnection);

                var DBCount = 0;
                foreach (Database db in _selectedServer.Databases)
                {
                    if (chkSystemDB.Checked)
                    {
                        DBCount++;
                        cboDB.Items.Add(db.Name);
                    }
                    else if (!db.IsSystemObject)
                    {
                        DBCount++;
                        cboDB.Items.Add(db.Name);
                    }
                }

                if (DBCount > 0)
                {
                    lblDB.Text = "Databases - " + DBCount + " found. Select one from the following list:";
                    cboDB.Enabled = true;
                }
                else
                    lblDB.Text = "Databases";

                chkSystemDB.Enabled = true;
            }
            catch (SmoException smoException)
            {
                MessageBox.Show("Error connecting to database", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DebugLog.WriteException(smoException);
                lblDB.Text = "Databases";
                cboDB.Enabled = false;
                chkSystemDB.Enabled = false;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Unknown Error in ZonkeyCodeGen", "Unknown Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DebugLog.WriteException(exception);
                lblDB.Text = "Databases";
                cboDB.Enabled = false;
                chkSystemDB.Enabled = false;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (lbTables.SelectedIndices.Count == 0 && lbViews.SelectedIndices.Count == 0)
            {
                MessageBox.Show("Please select at least one table or view", "Required Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            if (!chkTrustedConnection.Checked && string.IsNullOrEmpty(txtUserID.Text))
            {
                MessageBox.Show("User ID is required if not using Trusted Connections", "Required Field", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var files = new StringBuilder();
                var sConnStr = string.Format("Server={0};Database={1};", _selectedServer.Name, cboDB.SelectedItem);

                if (chkTrustedConnection.Checked)
                    sConnStr += "Trusted_Connection=Yes;";
                else
                    sConnStr += string.Format("User ID={0};Password={1};", txtUserID.Text, txtPassword.Text);

                //sConnStr += "Network Library=dbmssocn;";

                var factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
                var cnxn = factory.CreateConnection();
                cnxn.ConnectionString = sConnStr;
                cnxn.Open();

                string fileExtension;
                ClassGenerator gen;

                if (cToolStripMenuItem.Checked)
                {
                    gen = new Sql2CSGenerator();
                    fileExtension = ".cs";
                }
                else
                {
                    if (((Button)sender).Name == "btnGenerateVO")
                        throw new NotSupportedException("VB.NET not supported for VO generation.");

                    gen = new Sql2VBGenerator();
                    fileExtension = ".vb";
                }

                gen.Connection = cnxn;
                gen.GenerateCollections = (GenerateCollectionMode)cboCollectionType.SelectedValue;
                gen.GenerateTypedAdapters = chkDCAdapter.Checked;

                if (!string.IsNullOrEmpty(txtNamespace.Text.Trim()))
                    gen.Namespace = txtNamespace.Text;

                gen.VirtualProperties = false;
                gen.PrivateFieldsAtTop = false;

                for (var i = 0; i < lbTables.SelectedItems.Count; i++)
                {
                    var name = lbTables.SelectedItems[i].ToString();
                    gen.TableName = name;

                    if (_schemaDictionary[name] != "dbo")
                        gen.SchemaName = _schemaDictionary[name];

                    gen.ClassName = name.Replace(".","_").TrimEnd('s');

                    if (_tableViewDictionary.ContainsKey(name))
                        gen.KeyFieldName = FindPrimaryKey(_tableViewDictionary[name]);

                    var sFileName = (((Button)sender).Name == "btnGenerateVO") 
                                    ? string.Format("{0}\\{1}{2}", txtFilepath.Text, gen.ClassName, fileExtension) 
                                    : string.Format("{0}\\{1}{2}", txtFilepath.Text, gen.ClassName, fileExtension);

                    gen.Output = new StreamWriter(sFileName, false);
                    files.AppendLine("File Saved: " + sFileName);

                    gen.Generate();
                    gen.Output.Close();
                }

                for (var i = 0; i < lbViews.SelectedItems.Count; i++)
                {
                    var name = lbViews.SelectedItems[i].ToString();
                    gen.TableName = name;

                    if (_schemaDictionary[name] != "dbo")
                        gen.SchemaName = _schemaDictionary[name];

                    gen.ClassName = name.Replace(".", "_").TrimEnd('s');
                    gen.KeyFieldName = new List<string> {gen.ClassName + "ID"};

                    var sFileName = string.Format("{0}\\{1}{2}", txtFilepath.Text, gen.ClassName, fileExtension);
                    gen.Output = new StreamWriter(sFileName, false);
                    files.AppendLine("File Saved: " + sFileName);

                    gen.Generate();
                    gen.Output.Close();
                }

                cnxn.Close();

                MessageBox.Show(files.ToString(), "Success", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating Zonkey Object", "Object Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DebugLog.WriteException(ex);
            }
        }

        private void btnTableClear_Click(object sender, EventArgs e)
        {
            lbTables.SelectedIndex = -1;
            //lbTables_SelectedIndexChanged(lbTables, e);
        }

        private void btnViewClear_Click(object sender, EventArgs e)
        {
            lbViews.SelectedIndex = -1;
            //lbViews_SelectedIndexChanged(lbViews, e);
        }

        #endregion

        #region ComboBox Events

        private void cboDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // Get the database properties for selected database
                Cursor = Cursors.WaitCursor;

                lblTables.Text = "Tables";
                lbTables.Items.Clear();
                lblViews.Text = "Views";
                lbViews.Items.Clear();
                
                _schemaDictionary.Clear();
                _tableViewDictionary.Clear();

                _selectedDatabase = _selectedServer.Databases[cboDB.Text];

                tblThread = new Thread(EnumerateTables) { Priority = ThreadPriority.Highest };
                vwThread = new Thread(EnumerateViews) { Priority = ThreadPriority.Lowest };

                SafeStartThread(tblThread, EnumerateType.Tables);
                SafeStartThread(vwThread, EnumerateType.Views);
            }
            catch (SmoException smoException)
            {
                MessageBox.Show(smoException.ToString());
                lblTables.Text = "Tables";
                lbTables.Enabled = false;
                chkSystemTables.Enabled = false;
                lblViews.Text = "Views";
                lbViews.Enabled = false;
                chkSystemViews.Enabled = false;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
                lblTables.Text = "Tables";
                lbTables.Enabled = false;
                chkSystemTables.Enabled = false;
                lblViews.Text = "Views";
                lbViews.Enabled = false;
                chkSystemViews.Enabled = false;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void cboServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            cboDB.Items.Clear();
            lblDB.Text = "Databases";
            lbTables.Items.Clear();
            lblTables.Text = "Tables";
            lbViews.Items.Clear();
            lblViews.Text = "Views";
        }

        #endregion

        #region CheckBox Events

        private void chkSystemTables_CheckedChanged(object sender, EventArgs e)
        {
            _selectedDatabase = _selectedServer.Databases[cboDB.Text];
            EnumerateTables();
        }

        private void chkSystemViews_CheckedChanged(object sender, EventArgs e)
        {
            _selectedDatabase = _selectedServer.Databases[cboDB.Text];
            EnumerateViews();
        }

        private void chkTrustedConnection_CheckedChanged(object sender, EventArgs e)
        {
            txtUserID.Enabled = txtPassword.Enabled = !chkTrustedConnection.Checked;
        }

        private void chkSystemDB_CheckedChanged(object sender, EventArgs e)
        {
            btnConnect_Click(sender, e);
        }

        #endregion

        #region ToolStrip Events

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cToolStripMenuItem.Checked = true;
            vBNETToolStripMenuItem.Checked = false;
        }

        private void vBNETToolStripMenuItem_Click(object sender, EventArgs e)
        {
            vBNETToolStripMenuItem.Checked = true;
            cToolStripMenuItem.Checked = false;
        }

        #endregion

        #region Private Functions

        private void InitializeForm()
        {
            label1.Text = "Servers";
            lblDB.Text = "Databases";
            lblTables.Text = "Tables";
            cboServer.Text = string.Empty;
            cboServer.Items.Clear();
            cboDB.Items.Clear();
            cboDB.Enabled = false;
            chkSystemDB.Checked = false;
            chkSystemDB.Enabled = false;
            lbTables.Items.Clear();
            lbTables.Enabled = false;
            lbViews.Items.Clear();
            lbViews.Enabled = false;
            txtFilepath.Text = folderBrowserDialog1.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            btnGenerate.Enabled = false;
            btnGenerateVO.Enabled = false;

            cboCollectionType.Items.Clear();
            cboCollectionType.ValueMember = "ID";
            cboCollectionType.DisplayMember = "Label";
            cboCollectionType.DataSource = SupportData.GetCollectionTypes();
        }

        private void EnumerateTables()
        {
            lock (this)
            {
                EnableControl(cboDB, false);
                EnableControl(lbTables, false);
                EnableControl(btnGenerate, false);
                EnableControl(btnGenerateVO, false);

                var tableCount = 0;

                foreach (Table table in _selectedDatabase.Tables)
                {
                    string fullName;

                    if (chkSystemTables.Checked)
                    {
                        tableCount++;
                        fullName = FormatName(table.Name, table.Schema);
                        _tableViewDictionary.Add(fullName, table);
                        AddItem(lbTables, fullName);
                    }
                    else if (!table.IsSystemObject)
                    {
                        tableCount++;
                        fullName = FormatName(table.Name, table.Schema);
                        _tableViewDictionary.Add(fullName, table);
                        AddItem(lbTables, fullName);
                    }
                }

                SetText(lblTables, string.Format("Tables - {0} found.", tableCount));

                if (tableCount > 0)
                    EnableControl(lbTables, true);

                EnableControl(chkSystemTables, true);

                EnableControl(cboDB, true);
                EnableControl(btnGenerate, true);
                EnableControl(btnGenerateVO, true);
            }
        }

        private void EnumerateViews()
        {
            lock (this)
            {
                EnableControl(cboDB, false);
                EnableControl(lbViews, false);
                EnableControl(btnGenerate, false);
                EnableControl(btnGenerateVO, false);

                var viewCount = 0;

                foreach (Microsoft.SqlServer.Management.Smo.View view in _selectedDatabase.Views)
                {
                    string fullName;

                    if (chkSystemViews.Checked)
                    {
                        viewCount++;
                        fullName = FormatName(view.Name, view.Schema);
                        //_tableViewDictionary.Add(fullName, view);
                        AddItem(lbViews, fullName);
                    }
                    else if (!view.IsSystemObject)
                    {
                        viewCount++;
                        fullName = FormatName(view.Name, view.Schema);
                        //_tableViewDictionary.Add(fullName, view);
                        AddItem(lbViews, fullName);
                    }
                }

                SetText(lblViews, string.Format("Views - {0} found.", viewCount));

                if (viewCount > 0)
                    EnableControl(lbViews, true);

                EnableControl(chkSystemViews, true);

                EnableControl(cboDB, true);
                EnableControl(btnGenerate, true);
                EnableControl(btnGenerateVO, true);
            }
        }

        private static List<string> FindPrimaryKey<T>(T obj) where T : TableViewBase
        {
            var pKeys = new List<string>();
            /*foreach (Index index in obj.Indexes)
            {
                if (index.IndexKeyType == IndexKeyType.DriPrimaryKey)
                {
                    pKeys.Add(index.IndexedColumns[0].Name);
                    //continue;
                }
            }*/
            foreach (Column col in obj.Columns)
            {
                if (col.InPrimaryKey)
                    pKeys.Add(col.Name);
            }

            return pKeys;
        }

        private string FormatName(string tableName, string schemaName)
        {
            var fullName = (schemaName != "dbo") ? string.Format("{0}.{1}", schemaName, tableName) : tableName;

            if (!_schemaDictionary.ContainsKey(fullName))
                _schemaDictionary.Add(fullName, schemaName);

            return fullName;
        }

        private void SafeStartThread(Thread obj, EnumerateType et)
        {
            switch (obj.ThreadState)
            {
                case ThreadState.Unstarted:
                    obj.Start();
                    break;

                case ThreadState.Stopped:
                    obj = (et == EnumerateType.Tables)
                          ? new Thread(EnumerateTables) { Priority = ThreadPriority.Highest }
                          : new Thread(EnumerateViews) { Priority = ThreadPriority.Lowest };

                    obj.Start();
                    break;
            }
        }

        #endregion

        private enum EnumerateType
        {
            Tables = 0,
            Views = 1
        }

        #region Delegate Functions

        delegate void AddItemCallback(Control ctrl, string text);
        private static void AddItem(Control ctrl, string text)
        {
            if (ctrl.InvokeRequired)
            {
                var cb = new AddItemCallback(AddItem);
                ctrl.Invoke(cb, new object[] { ctrl, text });
            }
            else
            {
                var obj = ctrl as ListBox;
                if (obj != null)
                    obj.Items.Add(text);
            }
        }

        delegate void SetTextCallback(Control ctrl, string text);
        private static void SetText(Control ctrl, string text)
        {
            if (ctrl.InvokeRequired)
            {
                var cb = new SetTextCallback(SetText);
                ctrl.Invoke(cb, new object[] { ctrl, text });
            }
            else
            {
                var obj = ctrl as Label;
                if (obj != null)
                    obj.Text = text;
            }
        }

        delegate void EnableControlCallback(Control ctrl, bool enabled);
        private static void EnableControl(Control ctrl, bool enabled)
        {
            if (ctrl.InvokeRequired)
            {
                var cb = new EnableControlCallback(EnableControl);
                ctrl.Invoke(cb, new object[] { ctrl, enabled });
            }
            else
            {
                ctrl.Enabled = enabled;
            }
        }

        #endregion
    }
}