namespace SAFE_ROAD_DESKTOP
{
    partial class SafeConnexion
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SafeConnexion));
            lblNomAdmin = new Label();
            lblMdp = new Label();
            txtNomAdmin = new TextBox();
            txtMdp = new TextBox();
            btnConnexion = new Button();
            pbxLogo = new PictureBox();
            chkAfficherMdp = new CheckBox();
            lblStatut = new Label();
            ((System.ComponentModel.ISupportInitialize)pbxLogo).BeginInit();
            SuspendLayout();
            // 
            // lblNomAdmin
            // 
            lblNomAdmin.AutoSize = true;
            lblNomAdmin.BackColor = Color.Transparent;
            lblNomAdmin.Font = new Font("Trebuchet MS", 12F, FontStyle.Bold | FontStyle.Underline, GraphicsUnit.Point, 0);
            lblNomAdmin.ForeColor = SystemColors.ActiveCaptionText;
            lblNomAdmin.Location = new Point(42, 280);
            lblNomAdmin.Margin = new Padding(4, 0, 4, 0);
            lblNomAdmin.Name = "lblNomAdmin";
            lblNomAdmin.Size = new Size(214, 26);
            lblNomAdmin.TabIndex = 0;
            lblNomAdmin.Text = "Nom Administrateur :";
            // 
            // lblMdp
            // 
            lblMdp.AutoSize = true;
            lblMdp.BackColor = Color.Transparent;
            lblMdp.Font = new Font("Trebuchet MS", 12F, FontStyle.Bold | FontStyle.Underline, GraphicsUnit.Point, 0);
            lblMdp.ForeColor = SystemColors.ActiveCaptionText;
            lblMdp.Location = new Point(42, 365);
            lblMdp.Margin = new Padding(4, 0, 4, 0);
            lblMdp.Name = "lblMdp";
            lblMdp.Size = new Size(147, 26);
            lblMdp.TabIndex = 1;
            lblMdp.Text = "Mot de Passe :";
            // 
            // txtNomAdmin
            // 
            txtNomAdmin.Location = new Point(274, 280);
            txtNomAdmin.Margin = new Padding(4);
            txtNomAdmin.Name = "txtNomAdmin";
            txtNomAdmin.Size = new Size(482, 31);
            txtNomAdmin.TabIndex = 2;
            txtNomAdmin.TextChanged += txtNomAdmin_TextChanged;
            // 
            // txtMdp
            // 
            txtMdp.Location = new Point(274, 365);
            txtMdp.Margin = new Padding(4);
            txtMdp.Name = "txtMdp";
            txtMdp.Size = new Size(482, 31);
            txtMdp.TabIndex = 3;
            txtMdp.KeyPress += txtMdp_KeyPress;
            // 
            // btnConnexion
            // 
            btnConnexion.BackColor = Color.FromArgb(59, 98, 145);
            btnConnexion.FlatAppearance.BorderColor = Color.Black;
            btnConnexion.FlatAppearance.BorderSize = 10;
            btnConnexion.FlatStyle = FlatStyle.System;
            btnConnexion.ForeColor = SystemColors.ButtonHighlight;
            btnConnexion.Location = new Point(274, 496);
            btnConnexion.Margin = new Padding(4);
            btnConnexion.Name = "btnConnexion";
            btnConnexion.Size = new Size(482, 56);
            btnConnexion.TabIndex = 4;
            btnConnexion.Text = "Connexion";
            btnConnexion.UseVisualStyleBackColor = false;
            btnConnexion.Click += btnConnexion_Click;
            // 
            // pbxLogo
            // 
            pbxLogo.BackgroundImage = (Image)resources.GetObject("pbxLogo.BackgroundImage");
            pbxLogo.BackgroundImageLayout = ImageLayout.Stretch;
            pbxLogo.Location = new Point(42, 13);
            pbxLogo.Margin = new Padding(4);
            pbxLogo.Name = "pbxLogo";
            pbxLogo.Size = new Size(714, 242);
            pbxLogo.TabIndex = 5;
            pbxLogo.TabStop = false;
            // 
            // chkAfficherMdp
            // 
            chkAfficherMdp.AutoSize = true;
            chkAfficherMdp.Location = new Point(274, 424);
            chkAfficherMdp.Name = "chkAfficherMdp";
            chkAfficherMdp.Size = new Size(265, 30);
            chkAfficherMdp.TabIndex = 6;
            chkAfficherMdp.Text = "Afficher le mot de passe";
            chkAfficherMdp.UseVisualStyleBackColor = true;
            chkAfficherMdp.CheckedChanged += chkAfficherMdp_CheckedChanged;
            // 
            // lblStatut
            // 
            lblStatut.AutoSize = true;
            lblStatut.Location = new Point(42, 601);
            lblStatut.Name = "lblStatut";
            lblStatut.Size = new Size(581, 26);
            lblStatut.TabIndex = 7;
            lblStatut.Text = " Bienvenu(e) ! Cher/Chère Administratreur/Administratrice.";
            // 
            // SafeConnexion
            // 
            AutoScaleDimensions = new SizeF(12F, 26F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(140, 198, 231);
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(843, 653);
            Controls.Add(lblStatut);
            Controls.Add(chkAfficherMdp);
            Controls.Add(pbxLogo);
            Controls.Add(btnConnexion);
            Controls.Add(txtMdp);
            Controls.Add(txtNomAdmin);
            Controls.Add(lblMdp);
            Controls.Add(lblNomAdmin);
            DoubleBuffered = true;
            Font = new Font("Trebuchet MS", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4);
            Name = "SafeConnexion";
            Text = "SAFE CONNEXION";
            FormClosing += SafeConnexion_FormClosing_1;
            ((System.ComponentModel.ISupportInitialize)pbxLogo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblNomAdmin;
        private Label lblMdp;
        private TextBox txtNomAdmin;
        private TextBox txtMdp;
        private Button btnConnexion;
        private PictureBox pbxLogo;
        private CheckBox chkAfficherMdp;
        private Label lblStatut;
    }
}