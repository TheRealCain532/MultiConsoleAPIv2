///
//This source is based on the original PS3Lib but has been HEAVILY modified.
///

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net;

namespace MultiLib
{
    public enum Lang
    {
        Null,
        French,
        English,
        German
    }

    public enum SelectAPI
    {
        CCAPI,
        ProDG,
        PS3MAPI,
        Xbox360,
    }

    public class MCAPI
    {
        private static string targetName = String.Empty;
        private static string targetIp = String.Empty;
        public MCAPI(SelectAPI API = SelectAPI.ProDG)
        {
            SetAPI.API = API;
            MakeInstanceAPI(API);
        }
        public MCAPI(string IP)
        {
            SetAPI.API = SelectAPI.ProDG;
            MakeInstanceAPI(SelectAPI.ProDG);
            this.IP = IP;
        }
        bool IsConnected;
        TMAPI.SCECMD stats = new TMAPI.SCECMD();

        public Boolean ConnectionStatus()
        {
            MakeInstanceAPI(GetCurrentAPI());
            if (SetAPI.API == SelectAPI.ProDG)
                IsConnected = stats.GetStatus() == "Connected";
            if (SetAPI.API == SelectAPI.CCAPI)
                    IsConnected = CCAPI.GetConnectionStatus() > 0;
            return IsConnected;
        }
        public string IP { get { return targetIp; } set { targetIp = value; } }
        public bool vsh
        {
            get {
                if (IP == "") { MessageBox.Show("You need to set an IP!"); return false; }
                else
                    return new WebClient().DownloadString($"http://{IP}/peek.lv2?0x00003B38").Contains("38 60 00 01 4E 80 00 20"); }
            set
            {
                if (IP != "")
                {
                    string data = value ? "386000014E800020" : "E92280087C0802A6";
                    string magic = $"http://{IP}/poke.lv2?0x00003B38={data}";
                    new WebClient().DownloadString(magic);
                }
            }
        }
        public void setTargetName(string value)
        {
            targetName = value;
        }
        public void QuickNotify(string input)
        {
            if (SetAPI.API == SelectAPI.CCAPI)
                Common.CcApi.Notify(CCAPI.NotifyIcon.TROPHY1, input);
            //if (SetAPI.API == SelectAPI.XboxNeighborhood)
            //    Common.XboxApi.Notify(XboxAPI.XNotify.FLASHING_HAPPY_FACE, input);
        }
        private void MakeInstanceAPI(SelectAPI API)
        {
            if (API == SelectAPI.ProDG)
                if (Common.TmApi == null)
                    Common.TmApi = new TMAPI();
            if (API == SelectAPI.CCAPI)
                if (Common.CcApi == null)
                    Common.CcApi = new CCAPI();
            if (API == SelectAPI.Xbox360)
                if (Common.XboxApi == null)
                    Common.XboxApi = new XboxAPI();
        }

        private class SetLang
        {
            public static Lang defaultLang = Lang.Null;
        }

        private class SetAPI
        {
            public static SelectAPI API;
        }

        private class Common
        {
            public static CCAPI CcApi;
            public static TMAPI TmApi;
            public static XboxAPI XboxApi;
        }

        /// <summary>Force a language for the console list popup.</summary>
        public void SetFormLang(Lang Language)
        {
            SetLang.defaultLang = Language;
        }

       /// <summary>init again the connection if you use a Thread or a Timer.</summary>
        public void InitTarget()
        {
            if (SetAPI.API == SelectAPI.ProDG)
                Common.TmApi.InitComms();
        }

        /// <summary>Connect your console with selected API.</summary>
        public bool ConnectTarget(int target = 0)
        {
            // We'll check again if the instance has been done, else you'll got an exception error.
            MakeInstanceAPI(GetCurrentAPI());

            bool result = false;
            if (SetAPI.API == SelectAPI.ProDG)
                result = Common.TmApi.ConnectTarget(target);
            if (SetAPI.API == SelectAPI.Xbox360)
                result = Common.XboxApi.ConnectTarget();
            if (SetAPI.API == SelectAPI.CCAPI)
                result = new ConsoleList(this).Show();
            return result;
        }

        /// <summary>Connect your console with CCAPI.</summary>
        public bool ConnectTarget(string ip)
        {
            MakeInstanceAPI(GetCurrentAPI());
            bool result = false;
            // We'll check again if the instance has been done.
            if (SetAPI.API == SelectAPI.CCAPI)
            {
                result = Common.CcApi.SUCCESS(Common.CcApi.ConnectTarget(ip));
                if (result)
                    targetIp = ip;
            }
            return result;
        }
        /// <summary>Disconnect Target with selected API.</summary>
        public void DisconnectTarget()
        {
            switch(SetAPI.API)
            {
                case SelectAPI.ProDG: Common.TmApi.DisconnectTarget(); break;
                case SelectAPI.CCAPI: Common.CcApi.DisconnectTarget(); break;
                case SelectAPI.Xbox360: Common.XboxApi.Disconnect(); break;
            }
        }

        /// <summary>Attach the current process (current Game) with selected API.</summary>
        public bool AttachProcess()
        {
            // We'll check again if the instance has been done.
            MakeInstanceAPI(GetCurrentAPI());

            bool AttachResult = false;
            if (SetAPI.API == SelectAPI.ProDG)
                AttachResult = Common.TmApi.AttachProcess();
            else if (SetAPI.API == SelectAPI.CCAPI)
                AttachResult = Common.CcApi.SUCCESS(Common.CcApi.AttachProcess());
            return AttachResult;
        }

        public string GetConsoleName()
        {
            if (SetAPI.API == SelectAPI.ProDG)
                return Common.TmApi.SCE.GetTargetName();
            else
            {
                if (targetName != String.Empty)
                    return targetName;

                if (targetIp != String.Empty)
                {
                    List<CCAPI.ConsoleInfo> Data = new List<CCAPI.ConsoleInfo>();
                    Data = Common.CcApi.GetConsoleList();
                    if (Data.Count > 0)
                    {
                        for (int i = 0; i < Data.Count; i++)
                            if (Data[i].Ip == targetIp)
                                return Data[i].Name;
                    }
                }
                return targetIp;
            }
        }

        /// <summary>Set memory to offset with selected API.</summary>
        public void SetMemory(uint offset, byte[] buffer)
        {
            if (SetAPI.API == SelectAPI.ProDG)
                Common.TmApi.SetMemory(offset, buffer);
            else if (SetAPI.API == SelectAPI.CCAPI)
                Common.CcApi.SetMemory(offset, buffer);
            else if (SetAPI.API == SelectAPI.Xbox360)
                Common.XboxApi.SetMemory(offset, buffer);
        }

        /// <summary>Get memory from offset using the Selected API.</summary>
        public void GetMemory(uint offset, byte[] buffer)
        {
            if (SetAPI.API == SelectAPI.ProDG)
                Common.TmApi.GetMemory(offset, buffer);
            else if (SetAPI.API == SelectAPI.CCAPI)
                Common.CcApi.GetMemory(offset, buffer);
            else if (SetAPI.API == SelectAPI.Xbox360)
                Common.XboxApi.GetMemory(offset, buffer);
        }
        public int GetMemory(ulong offset, byte[] buffer)
        {
            if (SetAPI.API == SelectAPI.ProDG)
                return Convert.ToInt32(NET.PS3TMAPI.ProcessGetMemory(0, NET.PS3TMAPI.UnitType.PPU, TMAPI.Parameters.ProcessID, 0, offset, ref buffer));
            else if (SetAPI.API == SelectAPI.CCAPI)
                return Common.CcApi.GetMemory(offset, buffer);
            else if (SetAPI.API == SelectAPI.Xbox360)
                return Common.XboxApi.GetMemory(offset, buffer);
            else return 0;
        }
        /// <summary>Get memory from offset with a length using the Selected API.</summary>
        public byte[] GetBytes(uint offset, int length)
        {
            byte[] buffer = new byte[length];
            if (SetAPI.API == SelectAPI.ProDG)
                Common.TmApi.GetMemory(offset, buffer);
            else if (SetAPI.API == SelectAPI.CCAPI)
                Common.CcApi.GetMemory(offset, buffer);
            else if (SetAPI.API == SelectAPI.Xbox360)
                Common.XboxApi.GetMemory(offset, buffer);
            return buffer;
        }
        /// <summary>Change current API.</summary>
        public void ChangeAPI(SelectAPI API)
        {
            SetAPI.API = API;
            MakeInstanceAPI(GetCurrentAPI());
        }

        /// <summary>Return selected API.</summary>
        public SelectAPI GetCurrentAPI()
        {
            return SetAPI.API;
        }

        /// <summary>Return selected API into string format.</summary>
        public string GetCurrentAPIName()
        {
            string output = String.Empty;
            if (SetAPI.API == SelectAPI.ProDG)
                output = Enum.GetName(typeof(SelectAPI), SelectAPI.ProDG).Replace("Manager", " Manager");
            else if (SetAPI.API == SelectAPI.CCAPI)
                output = Enum.GetName(typeof(SelectAPI), SelectAPI.CCAPI).Replace("Console", " Console");
            else if (SetAPI.API == SelectAPI.Xbox360)
                output = Enum.GetName(typeof(SelectAPI), SelectAPI.Xbox360).Replace("Neighborhood", "Neighborhood");
            return output;
        }

        /// <summary>This will find the dll ps3tmapi_net.dll for TMAPI.</summary>
        public Assembly PS3TMAPI_NET()
        {
            return Common.TmApi.PS3TMAPI_NET();
        }

        /// <summary>Use the extension class with your selected API.</summary>
        public Extension Extension
        {
            get { return new Extension(SetAPI.API); }
        }

        /// <summary>Access to all TMAPI functions.</summary>
        public TMAPI TMAPI
        {
            get { return new TMAPI(); }
        }

        /// <summary>Access to all CCAPI functions.</summary>
        public CCAPI CCAPI
        {
            get { return new CCAPI(); }
        }
        public XboxAPI XboxAPI
        {
            get { return new XboxAPI(); }
        }
        public GameShark GameShark
        {
            get { return new GameShark(); }
        }
        public class ConsoleList
        {
            private MCAPI Api;
            private List<CCAPI.ConsoleInfo> data;

            public ConsoleList(MCAPI f)
            {
                Api = f;
                data = Api.CCAPI.GetConsoleList();
            }

            /// <summary>Return the systeme language, if it's others all text will be in english.</summary>
            private Lang getSysLanguage()
            {
                if (SetLang.defaultLang == Lang.Null)
                {
                    if (CultureInfo.CurrentCulture.ThreeLetterWindowsLanguageName.StartsWith("FRA"))
                        return Lang.French;
                    else if (CultureInfo.CurrentCulture.ThreeLetterWindowsLanguageName.StartsWith("GER"))
                        return Lang.German;
                    return Lang.English;
                }
                else return SetLang.defaultLang;
            }

            private string strTraduction(string keyword)
            {
                Lang lang = getSysLanguage();
                if (lang == Lang.French)
                {
                    switch (keyword)
                    {
                        case "btnConnect": return "Connexion";
                        case "btnRefresh": return "Rafraîchir";
                        case "errorSelect": return "Vous devez d'abord sélectionner une console.";
                        case "errorSelectTitle": return "Sélectionnez une console.";
                        case "selectGrid": return "Sélectionnez une console dans la grille.";
                        case "selectedLbl": return "Sélection :";
                        case "formTitle": return "Choisissez une console...";
                        case "noConsole": return "Aucune console disponible, démarrez CCAPI Manager (v2.60+) et ajoutez une nouvelle console.";
                        case "noConsoleTitle": return "Aucune console disponible.";
                    }
                }
                else if(lang == Lang.German)
                {
                    switch (keyword)
                    {
                        case "btnConnect": return "Verbinde";
                        case "btnRefresh": return "Wiederholen";
                        case "errorSelect": return "Du musst zuerst eine Konsole auswählen.";
                        case "errorSelectTitle": return "Wähle eine Konsole.";
                        case "selectGrid": return "Wähle eine Konsole innerhalb dieses Gitters.";
                        case "selectedLbl": return "Ausgewählt :";
                        case "formTitle": return "Wähle eine Konsole...";
                        case "noConsole": return "Keine Konsolen verfügbar - starte CCAPI Manager (v2.60+) und füge eine neue Konsole hinzu.";
                        case "noConsoleTitle": return "Keine Konsolen verfügbar.";
                    }
                }
                else
                {
                    switch (keyword)
                    {
                        case "btnConnect": return "Connection";
                        case "btnRefresh": return "Refresh";
                        case "errorSelect": return "You need to select a console first.";
                        case "errorSelectTitle": return "Select a console.";
                        case "selectGrid": return "Select a console within this grid.";
                        case "selectedLbl": return "Selected :";
                        case "formTitle": return "Select a console...";
                        case "noConsole": return "None consoles available, run CCAPI Manager (v2.60+) and add a new console.";
                        case "noConsoleTitle": return "None console available.";
                    }
                }
                return "?";
            }

            public bool Show()
            {
                bool Result = false;
                int tNum = -1;

                // Instance of widgets
                Label lblInfo = new Label();
                Button btnConnect = new Button();
                Button btnRefresh = new Button();
                ListViewGroup listViewGroup = new ListViewGroup("Consoles", HorizontalAlignment.Left);
                ListView listView = new ListView();
                Form formList = new Form();

                // Create our button connect
                btnConnect.Location = new Point(12, 254);
                btnConnect.Name = "btnConnect";
                btnConnect.Size = new Size(198, 23);
                btnConnect.TabIndex = 1;
                btnConnect.Text = strTraduction("btnConnect");
                btnConnect.UseVisualStyleBackColor = true;
                btnConnect.Enabled = false;
                btnConnect.Click += (sender, e) =>
                {
                    if(tNum > -1)
                    {
                        if (Api.ConnectTarget(data[tNum].Ip))
                        {
                            Api.setTargetName(data[tNum].Name);
                            Result = true;
                        }
                        else Result = false;
                        formList.Close();
                    }
                    else
                        MessageBox.Show(strTraduction("errorSelect"), strTraduction("errorSelectTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                // Create our button refresh
                btnRefresh.Location = new Point(216, 254);
                btnRefresh.Name = "btnRefresh";
                btnRefresh.Size = new Size(86, 23);
                btnRefresh.TabIndex = 1;
                btnRefresh.Text = strTraduction("btnRefresh");
                btnRefresh.UseVisualStyleBackColor = true;
                btnRefresh.Click += (sender, e) =>
                {
                    tNum = -1;
                    listView.Clear();
                    lblInfo.Text = strTraduction("selectGrid");
                    btnConnect.Enabled = false;
                    data = Api.CCAPI.GetConsoleList();
                    int sizeD = data.Count();
                    for (int i = 0; i < sizeD; i++)
                    {
                        ListViewItem item = new ListViewItem(" " + data[i].Name + " - " + data[i].Ip);
                        item.ImageIndex = 0;
                        listView.Items.Add(item);
                    }
                };

                // Create our list view
                listView.Font = new Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                listViewGroup.Header = "Consoles";
                listViewGroup.Name = "consoleGroup";
                listView.Groups.AddRange(new ListViewGroup[] {listViewGroup});
                listView.HideSelection = false;
                listView.Location = new Point(12, 12);
                listView.MultiSelect = false;
                listView.Name = "ConsoleList";
                listView.ShowGroups = false;
                listView.Size = new Size(290, 215);
                listView.TabIndex = 0;
                listView.UseCompatibleStateImageBehavior = false;
                listView.View = View.List;
                listView.ItemSelectionChanged += (sender, e) =>
                {
                    tNum = e.ItemIndex;
                    btnConnect.Enabled = true;
                    string Name, Ip = "?";
                    if (data[tNum].Name.Length > 18)
                        Name = data[tNum].Name.Substring(0, 17) + "...";
                    else Name = data[tNum].Name;
                    if (data[tNum].Ip.Length > 16)
                        Ip = data[tNum].Name.Substring(0, 16) + "...";
                    else Ip = data[tNum].Ip;
                    lblInfo.Text = strTraduction("selectedLbl") + " " + Name + " / " + Ip;
                };

                // Create our label
                lblInfo.AutoSize = true;
                lblInfo.Location = new Point(12, 234);
                lblInfo.Name = "lblInfo";
                lblInfo.Size = new Size(158, 13);
                lblInfo.TabIndex = 3;
                lblInfo.Text = strTraduction("selectGrid");

                // Create our form
                formList.MinimizeBox = false;
                formList.MaximizeBox = false;
                formList.ClientSize = new Size(314, 285);
                formList.AutoScaleDimensions = new SizeF(6F, 13F);
                formList.AutoScaleMode = AutoScaleMode.Font;
                formList.FormBorderStyle = FormBorderStyle.FixedSingle;
                formList.StartPosition = FormStartPosition.CenterScreen;
                formList.Text = strTraduction("formTitle");
                formList.Controls.Add(listView);
                formList.Controls.Add(lblInfo);
                formList.Controls.Add(btnConnect);
                formList.Controls.Add(btnRefresh);

                // Start to update our list
                ImageList imgL = new ImageList();
                //imgL.Images.Add(Resources.ps3);
                //listView.SmallImageList = imgL;
                int sizeData = data.Count();

                for (int i = 0; i < sizeData; i++)
                {
                    ListViewItem item = new ListViewItem(" " + data[i].Name + " - " + data[i].Ip);
                    item.ImageIndex = 0;
                    listView.Items.Add(item);
                }

                // If there are more than 0 targets we show the form
                // Else we inform the user to create a console.
                if (sizeData > 0)
                    formList.ShowDialog();
                else
                {
                    Result = false;
                    formList.Close();
                    MessageBox.Show(strTraduction("noConsole"), strTraduction("noConsoleTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return Result;
            }
        }

        public class ProcessList
        {
            public ProcessList() { }
            public bool isAttached { get { return TMAPI.Parameters.ProcessID > 0;  } }
            private bool AttachProcess(ulong process)
            {
                bool isOK = false;
                PS3TMAPI.GetProcessList(TMAPI.Target, out TMAPI.Parameters.processIDs);
                if (TMAPI.Parameters.processIDs.Length > 0)
                    isOK = true;
                else isOK = false;
                if (isOK)
                {
                    ulong uProcess = process;
                    TMAPI.Parameters.ProcessID = Convert.ToUInt32(uProcess);
                    PS3TMAPI.ProcessAttach(TMAPI.Target, PS3TMAPI.UnitType.PPU, TMAPI.Parameters.ProcessID);
                    PS3TMAPI.ProcessContinue(TMAPI.Target, TMAPI.Parameters.ProcessID);
                    TMAPI.Parameters.info = $"The Process 0x{TMAPI.Parameters.ProcessID:X8} Has Been Attached !";
                }
                return isOK;
            }
            private string[] ProcessNames
            {
                get
                {
                    PS3TMAPI.ProcessInfo current;
                    string[] res = new string[TMAPI.Parameters.processIDs.Length];
                    for (int i = 0; i < res.Length; i++)
                    {
                        PS3TMAPI.GetProcessInfo(0, TMAPI.Parameters.processIDs[i], out current);
                        res[i] = current.Hdr.ELFPath == null ? $"0x{TMAPI.Parameters.processIDs[i]:X} | NULL" : $"0x{TMAPI.Parameters.processIDs[i]:X} | {current.Hdr.ELFPath.Split('/').Last()}";
                    }

                    return res;
                }
            }
            public string CurrentProcessName
            {
                get
                {
                    PS3TMAPI.ProcessInfo current; PS3TMAPI.GetProcessInfo(TMAPI.Target, TMAPI.Parameters.ProcessID, out current);
                    return current.Hdr.ELFPath.Split('/').Last();
                }
            }
            public bool Show()
            {
                bool Result = false;
                uint SelectedProcess = 0;
                PS3TMAPI.GetProcessList(0, out TMAPI.Parameters.processIDs);
                //foreach (var item in TMAPI.Parameters.processIDs) Console.WriteLine(item.ToString("X"));
                // Instance of widgets
                Label lblInfo = new Label();
                Button btnConnect = new Button();
                Button btnRefresh = new Button();
                ListViewGroup listViewGroup = new ListViewGroup("Processes", HorizontalAlignment.Left);
                ListView listView = new ListView();
                Form formList = new Form();

                // Create our button connect
                btnConnect.Location = new Point(12, 254);
                btnConnect.Name = "btnConnect";
                btnConnect.Size = new Size(198, 23);
                btnConnect.TabIndex = 1;
                btnConnect.Text = "Attach";
                btnConnect.UseVisualStyleBackColor = true;
                btnConnect.Enabled = false;
                btnConnect.Click += (sender, e) =>
                {
                    if (SelectedProcess > 0)
                        if (AttachProcess(SelectedProcess))
                        {
                            formList.Close();
                            Result = true;
                        }
                        else MessageBox.Show("Failed somehow");
                    else
                        MessageBox.Show("No Process Selected!");
                };

                // Create our button refresh
                btnRefresh.Location = new Point(216, 254);
                btnRefresh.Name = "btnRefresh";
                btnRefresh.Size = new Size(198, 23);
                btnRefresh.TabIndex = 1;
                btnRefresh.Text = "Refresh";
                btnRefresh.UseVisualStyleBackColor = true;
                btnRefresh.Click += (sender, e) =>
                {
                    listView.Clear();
                    foreach (var item in ProcessNames) listView.Items.Add(item);
                };

                // Create our list view
                listView.Font = new Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                listViewGroup.Header = "Consoles";
                listViewGroup.Name = "consoleGroup";
                listView.Groups.AddRange(new ListViewGroup[] { listViewGroup });
                listView.HideSelection = false;
                listView.Location = new Point(12, 12);
                listView.MultiSelect = false;
                listView.Name = "ConsoleList";
                listView.ShowGroups = false;
                listView.Size = new Size(400, 215);
                listView.TabIndex = 0;
                listView.UseCompatibleStateImageBehavior = false;
                listView.View = View.List;
                listView.ItemSelectionChanged += (sender, e) =>
                {
                    SelectedProcess = Convert.ToUInt32(ProcessNames[e.ItemIndex].Split('|')[0].Trim(), 16);
                    btnConnect.Enabled = true;
                    lblInfo.Text = $"\"{ProcessNames[e.ItemIndex].Split('/').Last().Replace("\n", "")}\" Selected";
                    //print pData
                };

                // Create our label
                lblInfo.AutoSize = true;
                lblInfo.Location = new Point(12, 234);
                lblInfo.Name = "lblInfo";
                lblInfo.Size = new Size(158, 13);
                lblInfo.TabIndex = 3;
                lblInfo.Text = "Select a Process from this list!";

                // Create our form
                formList.MinimizeBox = false;
                formList.MaximizeBox = false;
                formList.ClientSize = new Size(424, 285);
                formList.AutoScaleDimensions = new SizeF(6F, 13F);
                formList.AutoScaleMode = AutoScaleMode.Font;
                formList.FormBorderStyle = FormBorderStyle.FixedSingle;
                formList.StartPosition = FormStartPosition.CenterScreen;
                formList.Text = "Select Process";
                formList.Controls.Add(listView);
                formList.Controls.Add(lblInfo);
                formList.Controls.Add(btnConnect);
                formList.Controls.Add(btnRefresh);

                // Start to update our list
                ImageList imgL = new ImageList();
                //imgL.Images.Add(Resources.ps3);
                //listView.SmallImageList = imgL;


                int sizeData = new TMAPI().SCE.ProcessIDs().Length;
                for (int i = 0; i < sizeData; i++)
                {
                    ListViewItem item = new ListViewItem($" {ProcessNames[i]} ");
                    item.ImageIndex = 0;
                    listView.Items.Add(item);
                }

                // If there are more than 0 targets we show the form
                // Else we inform the user to create a console.
                if (sizeData > 0)
                    formList.ShowDialog();
                else
                {
                    Result = false;
                    formList.Close();
                    //MessageBox.Show(strTraduction("noConsole"), strTraduction("noConsoleTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return Result;
            }

        }
    }
}
