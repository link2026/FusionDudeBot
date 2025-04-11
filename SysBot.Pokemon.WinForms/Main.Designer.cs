using PKHeX.Drawing.PokeSprite.Properties;
using SysBot.Pokemon.WinForms.Properties;
using System.Drawing;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            TC_Main = new TabControl();
            Tab_Bots = new TabPage();
            B_New = new Button();
            CB_Theme = new ComboBox();
            CB_Mode = new ComboBox();
            CB_Protocol = new ComboBox();
            FLP_Bots = new FlowLayoutPanel();
            TB_IP = new TextBox();
            CB_Routine = new ComboBox();
            NUD_Port = new NumericUpDown();
            Tab_Hub = new TabPage();
            PG_Hub = new PropertyGrid();
            Tab_Logs = new TabPage();
            RTB_Logs = new RichTextBox();
            B_Stop = new Button();
            B_Start = new Button();
            B_RebootStop = new Button();
            updater = new Button();
            PNL_Buttons = new Panel();
            TC_Main.SuspendLayout();
            Tab_Bots.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)NUD_Port).BeginInit();
            Tab_Hub.SuspendLayout();
            Tab_Logs.SuspendLayout();
            PNL_Buttons.SuspendLayout();
            SuspendLayout();
            // 
            // TC_Main
            // 
            TC_Main.Appearance = TabAppearance.Buttons;
            TC_Main.Controls.Add(Tab_Bots);
            TC_Main.Controls.Add(Tab_Hub);
            TC_Main.Controls.Add(Tab_Logs);
            TC_Main.Dock = DockStyle.Fill;
            TC_Main.Font = new Font("Calibri", 11.25F);
            TC_Main.ItemSize = new Size(88, 34);
            TC_Main.Location = new Point(0, 0);
            TC_Main.Margin = new Padding(0);
            TC_Main.Name = "TC_Main";
            TC_Main.Padding = new Point(20, 7);
            TC_Main.SelectedIndex = 0;
            TC_Main.Size = new Size(785, 483);
            TC_Main.TabIndex = 3;
            // 
            // Tab_Bots
            // 
            Tab_Bots.Controls.Add(B_New);
            Tab_Bots.Controls.Add(CB_Theme);
            Tab_Bots.Controls.Add(CB_Mode);
            Tab_Bots.Controls.Add(CB_Protocol);
            Tab_Bots.Controls.Add(FLP_Bots);
            Tab_Bots.Controls.Add(TB_IP);
            Tab_Bots.Controls.Add(CB_Routine);
            Tab_Bots.Controls.Add(NUD_Port);
            Tab_Bots.Location = new Point(4, 38);
            Tab_Bots.Margin = new Padding(3, 4, 3, 4);
            Tab_Bots.Name = "Tab_Bots";
            Tab_Bots.Padding = new Padding(1);
            Tab_Bots.Size = new Size(777, 441);
            Tab_Bots.TabIndex = 0;
            Tab_Bots.Text = "Bots";
            Tab_Bots.UseVisualStyleBackColor = true;
            // 
            // B_New
            // 
            B_New.FlatStyle = FlatStyle.Flat;
            B_New.Font = new Font("Calibri", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            B_New.Location = new Point(5, 4);
            B_New.Margin = new Padding(5, 4, 5, 4);
            B_New.Name = "B_New";
            B_New.RightToLeft = RightToLeft.No;
            B_New.Size = new Size(72, 37);
            B_New.TabIndex = 0;
            B_New.Text = "Add";
            B_New.UseVisualStyleBackColor = true;
            B_New.Click += B_New_Click;
            // 
            // CB_Theme
            // 
            CB_Theme.DropDownStyle = ComboBoxStyle.DropDownList;
            CB_Theme.DropDownWidth = 130;
            CB_Theme.Font = new Font("Calibri", 11.25F);
            CB_Theme.ForeColor = SystemColors.ControlText;
            CB_Theme.FormattingEnabled = true;
            CB_Theme.Location = new Point(626, 7);
            CB_Theme.Margin = new Padding(5, 4, 5, 4);
            CB_Theme.Name = "CB_Theme";
            CB_Theme.Size = new Size(146, 26);
            CB_Theme.TabIndex = 12;
            CB_Theme.SelectedIndexChanged += CB_Theme_SelectedIndexChanged;
            // 
            // CB_Mode
            // 
            CB_Mode.DropDownStyle = ComboBoxStyle.DropDownList;
            CB_Mode.Font = new Font("Calibri", 11.25F);
            CB_Mode.ForeColor = SystemColors.ControlText;
            CB_Mode.FormattingEnabled = true;
            CB_Mode.Location = new Point(541, 7);
            CB_Mode.Margin = new Padding(5, 4, 5, 4);
            CB_Mode.Name = "CB_Mode";
            CB_Mode.Size = new Size(76, 26);
            CB_Mode.TabIndex = 11;
            CB_Mode.SelectedIndexChanged += CB_Mode_SelectedIndexChanged;
            // 
            // CB_Protocol
            // 
            CB_Protocol.DropDownStyle = ComboBoxStyle.DropDownList;
            CB_Protocol.DropDownWidth = 76;
            CB_Protocol.Font = new Font("Calibri", 11.25F);
            CB_Protocol.ForeColor = SystemColors.ControlText;
            CB_Protocol.FormattingEnabled = true;
            CB_Protocol.Location = new Point(330, 7);
            CB_Protocol.Margin = new Padding(5, 4, 5, 4);
            CB_Protocol.Name = "CB_Protocol";
            CB_Protocol.Size = new Size(76, 26);
            CB_Protocol.TabIndex = 10;
            CB_Protocol.SelectedIndexChanged += CB_Protocol_SelectedIndexChanged;
            // 
            // FLP_Bots
            // 
            FLP_Bots.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            FLP_Bots.BackgroundImageLayout = ImageLayout.Center;
            FLP_Bots.BorderStyle = BorderStyle.FixedSingle;
            FLP_Bots.Location = new Point(1, 52);
            FLP_Bots.Margin = new Padding(0);
            FLP_Bots.Name = "FLP_Bots";
            FLP_Bots.Size = new Size(771, 384);
            FLP_Bots.TabIndex = 9;
            FLP_Bots.Resize += FLP_Bots_Resize;
            // 
            // TB_IP
            // 
            TB_IP.BorderStyle = BorderStyle.FixedSingle;
            TB_IP.Font = new Font("Calibri", 9.75F);
            TB_IP.ForeColor = SystemColors.ControlText;
            TB_IP.Location = new Point(85, 10);
            TB_IP.Margin = new Padding(5, 4, 5, 4);
            TB_IP.Name = "TB_IP";
            TB_IP.Size = new Size(153, 23);
            TB_IP.TabIndex = 8;
            TB_IP.Text = "192.168.0.1";
            // 
            // CB_Routine
            // 
            CB_Routine.DropDownStyle = ComboBoxStyle.DropDownList;
            CB_Routine.Font = new Font("Calibri", 11.25F);
            CB_Routine.ForeColor = SystemColors.ControlText;
            CB_Routine.FormattingEnabled = true;
            CB_Routine.Location = new Point(416, 7);
            CB_Routine.Margin = new Padding(5, 4, 5, 4);
            CB_Routine.Name = "CB_Routine";
            CB_Routine.Size = new Size(115, 26);
            CB_Routine.TabIndex = 7;
            // 
            // NUD_Port
            // 
            NUD_Port.BorderStyle = BorderStyle.FixedSingle;
            NUD_Port.Font = new Font("Calibri", 9.75F);
            NUD_Port.ForeColor = SystemColors.ControlText;
            NUD_Port.Location = new Point(247, 10);
            NUD_Port.Margin = new Padding(4, 3, 4, 3);
            NUD_Port.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            NUD_Port.Name = "NUD_Port";
            NUD_Port.Size = new Size(74, 23);
            NUD_Port.TabIndex = 6;
            NUD_Port.Value = new decimal(new int[] { 6000, 0, 0, 0 });
            // 
            // Tab_Hub
            // 
            Tab_Hub.Controls.Add(PG_Hub);
            Tab_Hub.Location = new Point(4, 38);
            Tab_Hub.Margin = new Padding(5, 4, 5, 4);
            Tab_Hub.Name = "Tab_Hub";
            Tab_Hub.Padding = new Padding(5, 4, 5, 4);
            Tab_Hub.Size = new Size(777, 441);
            Tab_Hub.TabIndex = 2;
            Tab_Hub.Text = "Hub";
            Tab_Hub.UseVisualStyleBackColor = true;
            // 
            // PG_Hub
            // 
            PG_Hub.Dock = DockStyle.Fill;
            PG_Hub.Location = new Point(5, 4);
            PG_Hub.Margin = new Padding(4, 3, 4, 3);
            PG_Hub.Name = "PG_Hub";
            PG_Hub.PropertySort = PropertySort.Categorized;
            PG_Hub.Size = new Size(767, 433);
            PG_Hub.TabIndex = 0;
            // 
            // Tab_Logs
            // 
            Tab_Logs.Controls.Add(RTB_Logs);
            Tab_Logs.Location = new Point(4, 38);
            Tab_Logs.Margin = new Padding(5, 4, 5, 4);
            Tab_Logs.Name = "Tab_Logs";
            Tab_Logs.Size = new Size(777, 441);
            Tab_Logs.TabIndex = 1;
            Tab_Logs.Text = "Logs";
            Tab_Logs.UseVisualStyleBackColor = true;
            // 
            // RTB_Logs
            // 
            RTB_Logs.Dock = DockStyle.Fill;
            RTB_Logs.Location = new Point(0, 0);
            RTB_Logs.Margin = new Padding(5, 4, 5, 4);
            RTB_Logs.Name = "RTB_Logs";
            RTB_Logs.ReadOnly = true;
            RTB_Logs.Size = new Size(777, 441);
            RTB_Logs.TabIndex = 0;
            RTB_Logs.Text = "";
            // 
            // B_Stop
            // 
            B_Stop.BackColor = SystemColors.Window;
            B_Stop.BackgroundImageLayout = ImageLayout.None;
            B_Stop.FlatStyle = FlatStyle.Popup;
            B_Stop.Font = new Font("Calibri", 12F, FontStyle.Bold);
            B_Stop.ForeColor = SystemColors.ControlText;
            B_Stop.Image = (Image)resources.GetObject("B_Stop.Image");
            B_Stop.ImageAlign = ContentAlignment.MiddleLeft;
            B_Stop.Location = new Point(115, 2);
            B_Stop.Margin = new Padding(0);
            B_Stop.Name = "B_Stop";
            B_Stop.Size = new Size(107, 30);
            B_Stop.TabIndex = 4;
            B_Stop.Text = "Stop Bots";
            B_Stop.TextAlign = ContentAlignment.MiddleRight;
            B_Stop.UseVisualStyleBackColor = false;
            B_Stop.Click += B_Stop_Click;
            // 
            // B_Start
            // 
            B_Start.BackColor = SystemColors.Window;
            B_Start.FlatStyle = FlatStyle.Popup;
            B_Start.Font = new Font("Calibri", 12F, FontStyle.Bold);
            B_Start.ForeColor = SystemColors.ControlText;
            B_Start.Image = (Image)resources.GetObject("B_Start.Image");
            B_Start.ImageAlign = ContentAlignment.MiddleLeft;
            B_Start.Location = new Point(5, 2);
            B_Start.Margin = new Padding(0);
            B_Start.Name = "B_Start";
            B_Start.Size = new Size(107, 30);
            B_Start.TabIndex = 3;
            B_Start.Text = "Start Bots";
            B_Start.TextAlign = ContentAlignment.MiddleRight;
            B_Start.UseVisualStyleBackColor = false;
            B_Start.Click += B_Start_Click;
            // 
            // B_RebootStop
            // 
            B_RebootStop.BackColor = SystemColors.Window;
            B_RebootStop.FlatStyle = FlatStyle.Popup;
            B_RebootStop.Font = new Font("Calibri", 12F, FontStyle.Bold);
            B_RebootStop.ForeColor = SystemColors.ControlText;
            B_RebootStop.Image = (Image)resources.GetObject("B_RebootStop.Image");
            B_RebootStop.ImageAlign = ContentAlignment.MiddleLeft;
            B_RebootStop.Location = new Point(225, 2);
            B_RebootStop.Margin = new Padding(0);
            B_RebootStop.Name = "B_RebootStop";
            B_RebootStop.Size = new Size(107, 30);
            B_RebootStop.TabIndex = 9;
            B_RebootStop.Text = "Restart";
            B_RebootStop.TextAlign = ContentAlignment.MiddleRight;
            B_RebootStop.UseVisualStyleBackColor = false;
            B_RebootStop.Click += B_RebootStop_Click;
            // 
            // updater
            // 
            updater.BackColor = SystemColors.Window;
            updater.FlatStyle = FlatStyle.Flat;
            updater.Font = new Font("Calibri", 12F, FontStyle.Bold);
            updater.ForeColor = SystemColors.ControlText;
            updater.Image = (Image)resources.GetObject("updater.Image");
            updater.ImageAlign = ContentAlignment.MiddleLeft;
            updater.Location = new Point(335, 2);
            updater.Margin = new Padding(0);
            updater.Name = "updater";
            updater.Size = new Size(107, 30);
            updater.TabIndex = 10;
            updater.Text = "Update";
            updater.TextAlign = ContentAlignment.MiddleRight;
            updater.UseVisualStyleBackColor = false;
            updater.Click += Updater_Click;
            // 
            // PNL_Buttons
            // 
            PNL_Buttons.BackColor = SystemColors.Window;
            PNL_Buttons.Controls.Add(updater);
            PNL_Buttons.Controls.Add(B_Stop);
            PNL_Buttons.Controls.Add(B_RebootStop);
            PNL_Buttons.Controls.Add(B_Start);
            PNL_Buttons.Location = new Point(334, 0);
            PNL_Buttons.Margin = new Padding(3, 4, 3, 4);
            PNL_Buttons.Name = "PNL_Buttons";
            PNL_Buttons.Size = new Size(446, 38);
            PNL_Buttons.TabIndex = 0;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(8F, 18F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Desktop;
            ClientSize = new Size(785, 483);
            Controls.Add(PNL_Buttons);
            Controls.Add(TC_Main);
            Font = new Font("Calibri", 11.25F);
            Icon = Properties.Resources.icon;
            Margin = new Padding(5, 4, 5, 4);
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FusionDudeBot";
            FormClosing += Main_FormClosing;
            TC_Main.ResumeLayout(false);
            Tab_Bots.ResumeLayout(false);
            Tab_Bots.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)NUD_Port).EndInit();
            Tab_Hub.ResumeLayout(false);
            Tab_Logs.ResumeLayout(false);
            PNL_Buttons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.TabControl TC_Main;
        private System.Windows.Forms.TabPage Tab_Bots;
        private System.Windows.Forms.TabPage Tab_Logs;
        private System.Windows.Forms.RichTextBox RTB_Logs;
        private System.Windows.Forms.TabPage Tab_Hub;
        private System.Windows.Forms.PropertyGrid PG_Hub;
        private System.Windows.Forms.Button B_Stop;
        private System.Windows.Forms.Button B_Start;
        private System.Windows.Forms.TextBox TB_IP;
        private System.Windows.Forms.ComboBox CB_Routine;
        private System.Windows.Forms.NumericUpDown NUD_Port;
        private System.Windows.Forms.Button B_New;
        private System.Windows.Forms.FlowLayoutPanel FLP_Bots;
        private System.Windows.Forms.ComboBox CB_Protocol;
        private System.Windows.Forms.ComboBox CB_Mode;
        private System.Windows.Forms.ComboBox CB_Theme;
        private System.Windows.Forms.Button B_RebootStop;
        private Button updater;
        private Panel PNL_Buttons;
    }
}

