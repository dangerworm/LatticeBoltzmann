namespace LatticeBoltzmann.Controls
{
    partial class IntSettingControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblSettingName = new System.Windows.Forms.Label();
            this.txtSettingValue = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lblSettingName
            // 
            this.lblSettingName.AutoSize = true;
            this.lblSettingName.Location = new System.Drawing.Point(0, 0);
            this.lblSettingName.Name = "lblSettingName";
            this.lblSettingName.Size = new System.Drawing.Size(71, 13);
            this.lblSettingName.TabIndex = 0;
            this.lblSettingName.Text = "Setting Name";
            // 
            // txtSettingValue
            // 
            this.txtSettingValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSettingValue.Location = new System.Drawing.Point(3, 16);
            this.txtSettingValue.Name = "txtSettingValue";
            this.txtSettingValue.Size = new System.Drawing.Size(144, 20);
            this.txtSettingValue.TabIndex = 1;
            // 
            // DoubleSettingControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtSettingValue);
            this.Controls.Add(this.lblSettingName);
            this.Name = "DoubleSettingControl";
            this.Size = new System.Drawing.Size(150, 39);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblSettingName;
        private System.Windows.Forms.TextBox txtSettingValue;
    }
}
