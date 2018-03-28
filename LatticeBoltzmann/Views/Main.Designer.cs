namespace LatticeBoltzmann.Views
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gbxSettings = new System.Windows.Forms.GroupBox();
            this.secSettingsEditor = new LatticeBoltzmann.Controls.SettingsEditorControl();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tssStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnRun = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.nudIterations = new System.Windows.Forms.NumericUpDown();
            this.txtConsole = new System.Windows.Forms.TextBox();
            this.pbxSolids = new System.Windows.Forms.PictureBox();
            this.gbxSettings.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbxSolids)).BeginInit();
            this.SuspendLayout();
            // 
            // gbxSettings
            // 
            this.gbxSettings.Controls.Add(this.secSettingsEditor);
            this.gbxSettings.Location = new System.Drawing.Point(12, 12);
            this.gbxSettings.Name = "gbxSettings";
            this.gbxSettings.Size = new System.Drawing.Size(280, 229);
            this.gbxSettings.TabIndex = 0;
            this.gbxSettings.TabStop = false;
            this.gbxSettings.Text = "Settings";
            // 
            // secSettingsEditor
            // 
            this.secSettingsEditor.Location = new System.Drawing.Point(6, 19);
            this.secSettingsEditor.Name = "secSettingsEditor";
            this.secSettingsEditor.Size = new System.Drawing.Size(268, 204);
            this.secSettingsEditor.TabIndex = 2;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tssStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 419);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(786, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tssStatus
            // 
            this.tssStatus.Name = "tssStatus";
            this.tssStatus.Size = new System.Drawing.Size(0, 17);
            // 
            // btnRun
            // 
            this.btnRun.Location = new System.Drawing.Point(547, 30);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(75, 23);
            this.btnRun.TabIndex = 3;
            this.btnRun.Text = "Run";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(307, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "How many iterations?";
            // 
            // nudIterations
            // 
            this.nudIterations.Location = new System.Drawing.Point(421, 32);
            this.nudIterations.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudIterations.Name = "nudIterations";
            this.nudIterations.Size = new System.Drawing.Size(120, 20);
            this.nudIterations.TabIndex = 6;
            this.nudIterations.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // txtConsole
            // 
            this.txtConsole.Location = new System.Drawing.Point(310, 88);
            this.txtConsole.Multiline = true;
            this.txtConsole.Name = "txtConsole";
            this.txtConsole.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtConsole.Size = new System.Drawing.Size(462, 319);
            this.txtConsole.TabIndex = 7;
            // 
            // pbxSolids
            // 
            this.pbxSolids.Location = new System.Drawing.Point(18, 247);
            this.pbxSolids.Name = "pbxSolids";
            this.pbxSolids.Size = new System.Drawing.Size(274, 160);
            this.pbxSolids.TabIndex = 8;
            this.pbxSolids.TabStop = false;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(786, 441);
            this.Controls.Add(this.pbxSolids);
            this.Controls.Add(this.txtConsole);
            this.Controls.Add(this.nudIterations);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.gbxSettings);
            this.Name = "Main";
            this.Text = "Main";
            this.gbxSettings.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbxSolids)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox gbxSettings;
        private Controls.SettingsEditorControl secSettingsEditor;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tssStatus;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudIterations;
        private System.Windows.Forms.TextBox txtConsole;
        private System.Windows.Forms.PictureBox pbxSolids;
    }
}