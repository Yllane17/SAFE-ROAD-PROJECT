using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace SAFE_ROAD_DESKTOP
{
    public partial class SafeConnexion : Form
    {
        private const string ADMIN_USERNAME = "SAFE ADMIN";
        private const string HASHED_PASSWORD = "YjFjNzU4ZjA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5YzA5Ng=="; // Hash du mot de passe "Admin123!"
        private const string SALT = "SafeRoadSalt2026";
        public SafeConnexion()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.txtMdp.UseSystemPasswordChar = true;
        }

        private void btnConnexion_Click(object sender, EventArgs e)
        {
            string username = txtNomAdmin.Text.Trim();
            string password = txtMdp.Text;

            //Pour se proteger contre une attaque brute force 
            System.Threading.Thread.Sleep(500);

            if (AuthentifierUtilisateur(username, password))
            {
                lblStatut.Text = "";
                this.Hide();
                Main acceuil = new Main();
                acceuil.Show();
                acceuil.BringToFront();
                acceuil.Activate();
                //this.Close();

            }
            else
            {
                lblStatut.Text = "Nom d'utilisateur ou mot de passe incorrect.";
                txtMdp.Text = "";
                txtMdp.Focus();
            }
        }

        private void chkAfficherMdp_CheckedChanged(object sender, EventArgs e)
        {
            txtMdp.UseSystemPasswordChar = !chkAfficherMdp.Checked;
        }

        private void txtMdp_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                btnConnexion_Click(sender, e);
            }
        }

        #region AuthentifierUtilisateur

        private bool AuthentifierUtilisateur(string username, string password)
        {
            if (username.Equals(ADMIN_USERNAME, StringComparison.OrdinalIgnoreCase) && password == "Admin123!")
            {
                return true;
            }
            // string HashedInput = HashPasswordSHA256(password + SALT);
            // return VerifyPassword(HashedInput, HASHED_PASSWORD);
            return false;
        }

        #endregion

        #region HashPasswordSHA256
        private string HashPasswordSHA256(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
        #endregion

        #region VerifyPassword
        private bool VerifyPassword(string hashedInput, string storedHash)
        {
            if (hashedInput.Length != storedHash.Length)
                return false;

            int result = 0;
            for (int i = 0; i < hashedInput.Length; i++)
            {
                result = hashedInput[i] ^ storedHash[i];
            }
            return result == 0;
        }

        #endregion



        private void SafeConnexion_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = MessageBox.Show("Voulez-vous vraiment quitter ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void txtNomAdmin_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
