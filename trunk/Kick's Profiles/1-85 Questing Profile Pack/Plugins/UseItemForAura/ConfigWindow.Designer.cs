namespace UseItemForAura
{
    partial class ConfigWindow
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.TBQuestID = new System.Windows.Forms.TextBox();
            this.TBItemID = new System.Windows.Forms.TextBox();
            this.TBAuraID = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.CBCombat = new System.Windows.Forms.CheckBox();
            this.BSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Quest ID";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Item ID";
            // 
            // TBQuestID
            // 
            this.TBQuestID.Location = new System.Drawing.Point(68, 13);
            this.TBQuestID.Name = "TBQuestID";
            this.TBQuestID.Size = new System.Drawing.Size(100, 20);
            this.TBQuestID.TabIndex = 2;
            // 
            // TBItemID
            // 
            this.TBItemID.Location = new System.Drawing.Point(68, 40);
            this.TBItemID.Name = "TBItemID";
            this.TBItemID.Size = new System.Drawing.Size(100, 20);
            this.TBItemID.TabIndex = 3;
            // 
            // TBAuraID
            // 
            this.TBAuraID.Location = new System.Drawing.Point(68, 67);
            this.TBAuraID.Name = "TBAuraID";
            this.TBAuraID.Size = new System.Drawing.Size(100, 20);
            this.TBAuraID.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 70);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Aura ID";
            // 
            // CBCombat
            // 
            this.CBCombat.AutoSize = true;
            this.CBCombat.Location = new System.Drawing.Point(36, 99);
            this.CBCombat.Name = "CBCombat";
            this.CBCombat.Size = new System.Drawing.Size(95, 17);
            this.CBCombat.TabIndex = 6;
            this.CBCombat.Text = "Use in Combat";
            this.CBCombat.UseVisualStyleBackColor = true;
            // 
            // BSave
            // 
            this.BSave.Location = new System.Drawing.Point(56, 122);
            this.BSave.Name = "BSave";
            this.BSave.Size = new System.Drawing.Size(75, 23);
            this.BSave.TabIndex = 7;
            this.BSave.Text = "Save";
            this.BSave.UseVisualStyleBackColor = true;
            this.BSave.Click += new System.EventHandler(this.BSave_Click);
            // 
            // ConfigWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(190, 157);
            this.Controls.Add(this.BSave);
            this.Controls.Add(this.CBCombat);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.TBAuraID);
            this.Controls.Add(this.TBItemID);
            this.Controls.Add(this.TBQuestID);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "ConfigWindow";
            this.Text = "Config";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TBQuestID;
        private System.Windows.Forms.TextBox TBItemID;
        private System.Windows.Forms.TextBox TBAuraID;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox CBCombat;
        private System.Windows.Forms.Button BSave;
    }
}

