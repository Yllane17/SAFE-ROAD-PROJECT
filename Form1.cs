using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using File = System.IO.File;
using Newtonsoft.Json;
using SocketIOClient;
using Newtonsoft.Json.Linq;

namespace SAFE_ROAD_DESKTOP
{
    public partial class Main : Form
    {
        // Tableau de Panels
        private Panel[] lesPanels = Array.Empty<Panel>();

        private int _totalAlertes = 0;
        private int _accidentsValides = 0;
        private int _faussesAlertes = 0;

        private bool _streamRunning = false;
        private string _backendUrl = "http://localhost:5000";
        private readonly HttpClient _http = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        // Utiliser la classe SocketIO du package SocketIOClient v3
        private SocketIO? _socket;
        private string? _videoFilePath;
        private CancellationTokenSource? _streamCts;
        private bool _isAnalyzing = false;

        private static readonly string FICHIER_HISTORIQUE =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
                "SAFE_ROAD", "historique.json");

        private List<HistoriqueItem> _historique = new();


        public Main()
        {
            InitializeComponent();
            InitialiserDGV();
            txtAdresseBackend.Text = _backendUrl;
            txtSeuil.Text = "93";
            this.MaximizeBox = false;

            this.Size = new Size(1203, 739);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;

            wmpFluxVideo.URL = @"D:\COURS\GESTION DES PROJETS\GRP 5 DETECTION D'ACCIDENT PAR IA\SAFE ROAD\SAFE_ROAD_DESKTOP\SAFE_ROAD_DESKTOP\videos\test_video.mp4";
            wmpFluxVideo.settings.autoStart = true;
            wmpFluxVideo.settings.setMode("loop", true);
            wmpFluxVideo.settings.setMode("stretch", true);
            wmpFluxVideo.uiMode = "none";
        }

        #region Dashboard
        private void btnDashbord_Click_1(object sender, EventArgs e)
        {
            afficherPanel(pnlDashboard);
        }
        #endregion

        #region Historique
        private void btnHistorique_Click(object sender, EventArgs e)
        {
            afficherPanel(pnlHistorique);
            _ = ChargerHistoriqueAsync();
            RemplirDGV();
        }
        #endregion

        #region Parametres
        private void btnParametres_Click(object sender, EventArgs e)
        {
            afficherPanel(pnlParametre);
            txtAdresseBackend.Text = _backendUrl;
        }
        #endregion

        #region Analyse
        private void btnAnalyse_Click_1(object sender, EventArgs e)
        {
            afficherPanel(pnlAnalyse);
            lblResult.Text = "Aucune collision";
            lblResult.ForeColor = SystemColors.ButtonHighlight;
            pgbAnalyse.Value = 0;
        }
        #endregion

        #region Bouton Importer
        private void btnImporter_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Choisir une vidéo",
                Filter = "Vidéos|*.mp4;*.avi;*.mov;*.mkv|Tous|*.*"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            _videoFilePath = dlg.FileName;
            wmpVideoImport.URL = _videoFilePath;
            wmpVideoImport.Ctlcontrols.play();
            wmpVideoImport.settings.setMode("stretch", true);
            wmpFluxVideo.uiMode = "none";
            lblResult.Text = "Vidéo importée — Prêt à analyser";
            lblResult.ForeColor = SystemColors.ButtonHighlight;
            pgbAnalyse.Value = 0;
        }
        #endregion

        #region Bouton Lancer
        private async void btnlancerAnalyse_ClickAsync(object sender, EventArgs e)
        {
            if (_isAnalyzing)
            {
                MessageBox.Show("Une analyse est déjà en cours.",
                    "SAFE ROAD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrEmpty(_videoFilePath) || !File.Exists(_videoFilePath))
            {
                MessageBox.Show("Importe d'abord une vidéo.",
                    "SAFE ROAD", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _isAnalyzing = true;
            btnlancerAnalyse.Enabled = false;
            btnImporter.Enabled = false;
            pgbAnalyse.Value = 0;
            lblResult.Text = "Envoi de la vidéo...";
            lblResult.ForeColor = Color.Cyan;

            // Animation progression
            var timer = new System.Windows.Forms.Timer { Interval = 600 };
            timer.Tick += (s, ev) =>
            {
                if (pgbAnalyse.Value < 88) pgbAnalyse.Value += 3;
            };
            timer.Start();

            try
            {
                using var form = new MultipartFormDataContent();
                using var fs = File.OpenRead(_videoFilePath);
                using var content = new StreamContent(fs);
                form.Add(content, "video", Path.GetFileName(_videoFilePath));

                lblResult.Text = "Analyse IA en cours...";

                var httpResp = await _http.PostAsync($"{_backendUrl}/upload", form);
                string json = await httpResp.Content.ReadAsStringAsync();

                timer.Stop();
                pgbAnalyse.Value = 100;

                if (!httpResp.IsSuccessStatusCode)
                {
                    lblResult.Text = $"Erreur serveur.";
                    lblResult.ForeColor = Color.OrangeRed;
                    return;
                }

                var res = JsonConvert.DeserializeObject<ResultatAnalyse>(json);
                if (res == null) return;

                //  Mise à jour stats dashboard
                _totalAlertes += res.NbAccidents;
                _accidentsValides += res.NbAccidentsValides;
                _faussesAlertes += res.NbFaussesAlertes;
                MettreAJourStats();

                //  Sauvegarde dans l'historique persistant
                SauvegarderDansHistorique(res);

                if (res.NbAccidents == 0)
                {
                    // Aucun accident
                    lblResult.Text = "Aucune collision détectée.";
                    lblResult.ForeColor = Color.LightGreen;
                }
                else
                {
                    //  MessageBox accident détecté
                    lblResult.Text = $" {res.NbAccidents} collision(s) détectée(s) !";
                    lblResult.ForeColor = Color.OrangeRed;

                    // Construction du détail
                    string detail = "";
                    foreach (var acc in res.Accidents)
                        detail += $"  • À {acc.TimeFmt}  —  Confiance : {acc.Confidence:F1}%\n";

                    string alerteInfo = res.SmsEnvoye
                        ? $"\n {res.NbAlertesEnvoyees} alerte(s) Email envoyé(s)."
                        : "\n Vérifiez votre boite mail s'il vous plait.";

                    //  MessageBox d'accident
                    MessageBox.Show(
                        $"  ACCIDENT DÉTECTÉ !\n\n"
                        + $"Fichier         : {res.Fichier}\n"
                        + $"Collisions      : {res.NbAccidents}\n"
                        + $"Accidents valid.: {res.NbAccidentsValides}\n"
                        + $"Fausses alertes : {res.NbFaussesAlertes}\n\n"
                        + $"Détails :\n{detail}"
                        + alerteInfo,
                        "SAFE ROAD — Collision détectée !",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    System.Media.SystemSounds.Exclamation.Play();
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                lblResult.Text = $"Erreur : {ex.Message}";
                lblResult.ForeColor = Color.OrangeRed;
            }
            finally
            {
                timer.Stop();
                timer.Dispose();
                _isAnalyzing = false;
                btnlancerAnalyse.Enabled = true;
                btnImporter.Enabled = true;
            }

        }
        #endregion

        #region Bouton Arreter
        private void btnArreterAnalyse_Click(object sender, EventArgs e)
        {
            _streamCts?.Cancel();
            _isAnalyzing = false;
            btnlancerAnalyse.Enabled = true;
            wmpVideoImport.Ctlcontrols.stop();
            lblResult.Text = "Analyse arrêtée.";
            lblResult.ForeColor = Color.Red;
            pgbAnalyse.Value = 0;
        }

        #endregion

        #region charger historique
        private async Task ChargerHistoriqueAsync()
        {
            try
            {
                string json = await _http.GetStringAsync($"{_backendUrl}/alerts");
                var liste = JsonConvert.DeserializeObject<List<AlerteModel>>(json) ?? new();
                Invoke(() =>
                {
                    dgvHistorique.Rows.Clear();
                    foreach (var a in liste)
                    {
                        string gravite = a.Confidence >= 97 ? "Haute"
                                       : a.Confidence >= 95 ? "Moyenne" : "Basse";
                        int row = dgvHistorique.Rows.Add(
                            a.Id, a.Timestamp,
                            $"{a.Confidence:F1}%",
                            gravite, a.Source);
                        dgvHistorique.Rows[row].DefaultCellStyle.BackColor =
                            a.Confidence >= 97 ? Color.FromArgb(100, 180, 30, 30)
                          : a.Confidence >= 95 ? Color.FromArgb(100, 180, 120, 20)
                          : Color.FromArgb(100, 30, 140, 70);
                        dgvHistorique.Rows[row].Cells["Gravite"].Style.ForeColor =
                            a.Confidence >= 97 ? Color.OrangeRed
                          : a.Confidence >= 95 ? Color.Orange
                          : Color.LightGreen;
                    }
                });
            }
            catch (Exception ex)
            {
                Invoke(() => MessageBox.Show($"Erreur historique :\n{ex.Message}",
                    "SAFE ROAD", MessageBoxButtons.OK, MessageBoxIcon.Warning));
            }
        }

        #endregion

        #region load
        private void Main_Load(object sender, EventArgs e)
        {
            Panel[] panDeplacer = { pnlAnalyse, pnlHistorique, pnlParametre };
            foreach (Panel pan in panDeplacer)
            {
                pnlDashboard.Controls.Remove(pan);
                this.Controls.Add(pan);
                pan.Location = pnlDashboard.Location;
                pan.Size = pnlDashboard.Size;
                pan.Visible = false;
            }
            lesPanels = new Panel[] { pnlAnalyse, pnlHistorique, pnlDashboard, pnlParametre };

            pnlDashboard.Visible = true;

            ChargerHistoriqueLocal();
            RemplirDGV();
        }
        #endregion

        #region affichage des panels
        private void afficherPanel(Panel panel)
        {
            foreach (Panel pan in lesPanels)
            {
                pan.Visible = false;
            }
            panel.Visible = true;
            panel.BringToFront();
        }
        #endregion

        // WebSocket : connexion et gestion des événements

        #region Websocket : connexion et gestion des événements

        private async Task ConnecterWebSocketAsync()
        {
            try
            {
                if (_socket != null)
                {
                    // v3 : DisconnectAsync attend CancellationToken
                    await _socket.DisconnectAsync(CancellationToken.None);
                    _socket.Dispose();
                    _socket = null;
                }

                // v3 : constructeur attend un Uri
                _socket = new SocketIO(new Uri(_backendUrl));

                // ── accident_found ────────────────────────────────────
                _socket.On("accident_found", async resp =>
                {
                    try
                    {
                        // v3 : GetValue<T>(index)
                        string json = resp.GetValue<string>(0);
                        var acc = JsonConvert.DeserializeObject<AlerteModel>(json);
                        if (acc == null) return;
                        Invoke(() =>
                        {
                            _totalAlertes++;
                            if (acc.Confidence >= 93) _accidentsValides++;
                            else _faussesAlertes++;
                            MettreAJourStats();
                            string t = !string.IsNullOrEmpty(acc.TimeFmt)
                                ? acc.TimeFmt : acc.DetectedAt;
                            lblResult.Text = $"🚨 Accident à {t} — {acc.Confidence:F1}%";
                            lblResult.ForeColor = Color.OrangeRed;
                            System.Media.SystemSounds.Exclamation.Play();
                        });
                    }
                    catch { }
                });

                // ── accident (live) ───────────────────────────────────
                _socket.On("accident", async resp =>
                {
                    try
                    {
                        string json = resp.GetValue<string>(0);
                        var acc = JsonConvert.DeserializeObject<AlerteModel>(json);
                        if (acc == null) return;
                        Invoke(() =>
                        {
                            _totalAlertes++;
                            _accidentsValides++;
                            MettreAJourStats();
                            System.Media.SystemSounds.Exclamation.Play();
                            MessageBox.Show(
                                $"🚨 ACCIDENT DÉTECTÉ !\n\n"
                                + $"Confiance : {acc.Confidence:F1}%\n"
                                + $"Heure     : {acc.Timestamp}\n\n"
                                + "SMS + Email envoyés aux urgences !",
                                "SAFE ROAD — ALERTE",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        });
                    }
                    catch { }
                });

                // ── analysis_progress ─────────────────────────────────
                _socket.On("analysis_progress", async resp =>
                {
                    try
                    {
                        string json = resp.GetValue<string>(0);
                        var prog = JsonConvert.DeserializeObject<ProgressModel>(json);
                        if (prog == null) return;
                        Invoke(() =>
                        {
                            pgbAnalyse.Value = Math.Min((int)prog.Progress, 100);
                            lblResult.Text = $"Analyse : {prog.Progress:F0}% — {prog.Accidents} accident(s)";
                        });
                    }
                    catch { }
                });

                // ── analysis_done ─────────────────────────────────────
                _socket.On("analysis_done", async resp =>
                {
                    try
                    {
                        string json = resp.GetValue<string>(0);
                        var done = JsonConvert.DeserializeObject<DoneModel>(json);
                        if (done == null) return;
                        Invoke(() =>
                        {
                            _isAnalyzing = false;
                            btnlancerAnalyse.Enabled = true;
                            pgbAnalyse.Value = 100;
                            _streamCts?.Cancel();

                            if (!string.IsNullOrEmpty(_videoFilePath))
                            {
                                wmpVideoImport.URL = _videoFilePath;
                                wmpVideoImport.Ctlcontrols.stop();
                            }

                            if (done.Accidents.Count == 0)
                            {
                                lblResult.Text = "✅ Aucune collision détectée.";
                                lblResult.ForeColor = Color.LightGreen;
                            }
                            else
                            {
                                lblResult.Text = $"🚨 {done.Accidents.Count} collision(s) — Alertes envoyées !";
                                lblResult.ForeColor = Color.OrangeRed;
                                if (done.SmsSent)
                                    MessageBox.Show(
                                        $"⚠️ {done.Accidents.Count} accident(s) détecté(s) !\n\n"
                                        + "SMS + Email envoyés aux urgences.",
                                        "SAFE ROAD — Accident !",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        });
                    }
                    catch { }
                });

                // ── analysis_error ────────────────────────────────────
                _socket.On("analysis_error", async resp =>
                {
                    Invoke(() =>
                    {
                        lblResult.Text = "❌ Erreur lors de l'analyse.";
                        lblResult.ForeColor = Color.OrangeRed;
                        _isAnalyzing = false;
                        btnlancerAnalyse.Enabled = true;
                        pgbAnalyse.Value = 0;
                    });
                });

                // Connect v3 : accepte CancellationToken
                await _socket.ConnectAsync(CancellationToken.None);
            }
            catch { }
        }
        #endregion

        // Helper 

        #region ExtraireModele générique pour v3
        private static T? ExtraireModele<T>(IEventContext ctx) where T : class
        {
            try
            {
                string raw = ctx.GetValue<string>(0).Trim();

                if (raw.StartsWith("["))
                {
                    JArray arr = JArray.Parse(raw);
                    if (arr.Count > 0)
                        return arr[0].ToObject<T>();
                }
                else if (raw.StartsWith("{"))
                {
                    return JObject.Parse(raw).ToObject<T>();
                }
            }
            catch { }
            return null;
        }
        #endregion

        // Nettoyage

        #region nettoyage des ressources
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _streamCts?.Cancel();
            try
            {
                _socket?.DisconnectAsync(CancellationToken.None).Wait();
            }
            catch { }
            _socket?.Dispose();
            _http.Dispose();
            base.OnFormClosing(e);
        }
        #endregion

        #region Initialiser le DataGridView
        private void InitialiserDGV()
        {
            dgvHistorique.Columns.Clear();
            dgvHistorique.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvHistorique.ReadOnly = true;
            dgvHistorique.AllowUserToAddRows = false;
            dgvHistorique.RowHeadersVisible = false;
            dgvHistorique.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistorique.RowTemplate.Height = 36;
            dgvHistorique.BorderStyle = BorderStyle.None;
            dgvHistorique.GridColor = Color.MediumSlateBlue;
            dgvHistorique.EnableHeadersVisualStyles = false;
            dgvHistorique.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkSlateBlue;
            dgvHistorique.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHistorique.ColumnHeadersDefaultCellStyle.Font = new Font("Trebuchet MS", 10f, FontStyle.Bold);
            dgvHistorique.DefaultCellStyle.BackColor = Color.SlateBlue;
            dgvHistorique.DefaultCellStyle.ForeColor = Color.White;
            dgvHistorique.DefaultCellStyle.SelectionBackColor = Color.MediumSlateBlue;
            dgvHistorique.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvHistorique.AlternatingRowsDefaultCellStyle.BackColor = Color.DarkSlateBlue;
            dgvHistorique.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;

            dgvHistorique.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fichier", HeaderText = "Fichier vidéo", FillWeight = 28 });
            dgvHistorique.Columns.Add(new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "Heure", FillWeight = 18 });
            dgvHistorique.Columns.Add(new DataGridViewTextBoxColumn { Name = "Confidence", HeaderText = "Confiance IA", FillWeight = 16 });
            dgvHistorique.Columns.Add(new DataGridViewTextBoxColumn { Name = "Gravite", HeaderText = "Gravité", FillWeight = 14 });
            dgvHistorique.Columns.Add(new DataGridViewTextBoxColumn { Name = "Alerte", HeaderText = "Alerte env.", FillWeight = 12 });
            dgvHistorique.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", FillWeight = 12 });
        }


        #endregion

        #region Mettre à jour les statistiques
        private void MettreAJourStats()
        {
            if (InvokeRequired) { Invoke(MettreAJourStats); return; }
            lblNbTotalAlertes.Text = _totalAlertes.ToString();
            lblNbAccidentValides.Text = _accidentsValides.ToString();
            lblNbFausseAlertes.Text = _faussesAlertes.ToString();
        }
        #endregion

        // Modèles 
        #region AlerteModel, StatusModel, ProgressModel, DoneModel
        internal class AlerteModel
        {
            [JsonProperty("id")] public int Id { get; set; }
            [JsonProperty("timestamp")] public string Timestamp { get; set; } = "";
            [JsonProperty("confidence")] public double Confidence { get; set; }
            [JsonProperty("source")] public string Source { get; set; } = "";
            [JsonProperty("time_fmt")] public string TimeFmt { get; set; } = "";
            [JsonProperty("detected_at")] public string DetectedAt { get; set; } = "";
        }

        internal class StatusModel
        {
            [JsonProperty("running")] public bool Running { get; set; }
            [JsonProperty("model_loaded")] public bool ModelLoaded { get; set; }
            [JsonProperty("message")] public string Message { get; set; } = "";
        }

        internal class ProgressModel
        {
            [JsonProperty("progress")] public double Progress { get; set; }
            [JsonProperty("accidents")] public int Accidents { get; set; }
        }

        internal class DoneModel
        {
            [JsonProperty("accidents")] public List<AlerteModel> Accidents { get; set; } = new();
            [JsonProperty("sms_sent")] public bool SmsSent { get; set; }
        }

        #endregion

        #region Confirmation de fermeture - FormClosing
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
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
        #endregion

        #region ChargerHistoriqueLocal
        private void ChargerHistoriqueLocal()
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(FICHIER_HISTORIQUE)!);

                if (File.Exists(FICHIER_HISTORIQUE))
                {
                    string json = File.ReadAllText(FICHIER_HISTORIQUE);
                    _historique = JsonConvert.DeserializeObject<List<HistoriqueItem>>(json)
                                  ?? new();
                }

                // Recalcule les stats depuis l'historique
                _totalAlertes = 0;
                _accidentsValides = 0;
                _faussesAlertes = 0;
                foreach (var item in _historique)
                {
                    _totalAlertes += item.NbAccidents;
                    _accidentsValides += item.NbAccidentsValides;
                    _faussesAlertes += item.NbFaussesAlertes;
                }
                MettreAJourStats();
            }
            catch { _historique = new(); }
        }
        #endregion

        #region RemplirDGV

        private void RemplirDGV()
        {
            dgvHistorique.Rows.Clear();
            foreach (var item in _historique)
            {
                int row = dgvHistorique.Rows.Add(
                    item.Fichier,
                    item.Timestamp,
                    item.Confidence > 0 ? $"{item.Confidence:F1}%" : "—",
                    item.Gravite,
                    item.AlerteEnvoyee ? "✅ Envoyée" : "—",
                    item.NbAccidentsValides > 0 ? "Validé" : item.NbFaussesAlertes > 0 ? "Fausse" : "Aucun"
                );

                // Colorisation
                Color bg = item.Gravite == "Haute" ? Color.FromArgb(100, 180, 30, 30)
                         : item.Gravite == "Moyenne" ? Color.FromArgb(100, 180, 120, 20)
                         : item.Gravite == "Basse" ? Color.FromArgb(100, 30, 140, 70)
                         : Color.FromArgb(50, 60, 100);

                dgvHistorique.Rows[row].DefaultCellStyle.BackColor = bg;

                dgvHistorique.Rows[row].Cells["Gravite"].Style.ForeColor =
                    item.Gravite == "Haute" ? Color.OrangeRed
                  : item.Gravite == "Moyenne" ? Color.Orange
                  : item.Gravite == "Basse" ? Color.LightGreen
                  : Color.Gray;
            }
        }

        #endregion

        #region MODELES JSON
        internal class ResultatAnalyse
        {
            [JsonProperty("succes")] public bool Succes { get; set; }
            [JsonProperty("fichier")] public string Fichier { get; set; } = "";
            [JsonProperty("total_frames")] public int TotalFrames { get; set; }
            [JsonProperty("accidents")] public List<AccidentItem> Accidents { get; set; } = new();
            [JsonProperty("nb_accidents")] public int NbAccidents { get; set; }
            [JsonProperty("nb_accidents_valides")] public int NbAccidentsValides { get; set; }
            [JsonProperty("nb_fausses_alertes")] public int NbFaussesAlertes { get; set; }
            [JsonProperty("sms_envoye")] public bool SmsEnvoye { get; set; }
            [JsonProperty("nb_alertes_envoyees")] public int NbAlertesEnvoyees { get; set; }
            [JsonProperty("message")] public string Message { get; set; } = "";
        }

        internal class AccidentItem
        {
            [JsonProperty("frame")] public int Frame { get; set; }
            [JsonProperty("time_fmt")] public string TimeFmt { get; set; } = "";
            [JsonProperty("confidence")] public double Confidence { get; set; }
            [JsonProperty("detected_at")] public string DetectedAt { get; set; } = "";
        }

        internal class HistoriqueItem
        {
            public string Fichier { get; set; } = "";
            public string Timestamp { get; set; } = "";
            public int NbAccidents { get; set; }
            public int NbAccidentsValides { get; set; }
            public int NbFaussesAlertes { get; set; }
            public double Confidence { get; set; }
            public string Gravite { get; set; } = "";
            public bool AlerteEnvoyee { get; set; }
        }



        #endregion

        #region sauvegarder dans Historique
        private void SauvegarderDansHistorique(ResultatAnalyse res)
        {
            try
            {
                // Ajoute chaque accident comme ligne dans l'historique
                if (res.NbAccidents == 0)
                {
                    _historique.Insert(0, new HistoriqueItem
                    {
                        Fichier = res.Fichier,
                        Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                        NbAccidents = 0,
                        NbAccidentsValides = 0,
                        NbFaussesAlertes = 0,
                        Confidence = 0,
                        Gravite = "Aucun",
                        AlerteEnvoyee = false,
                    });
                }
                else
                {
                    foreach (var acc in res.Accidents)
                    {
                        string gravite = acc.Confidence >= 97 ? "Haute"
                                       : acc.Confidence >= 95 ? "Moyenne" : "Basse";
                        _historique.Insert(0, new HistoriqueItem
                        {
                            Fichier = res.Fichier,
                            Timestamp = acc.DetectedAt,
                            NbAccidents = 1,
                            NbAccidentsValides = acc.Confidence >= 95 ? 1 : 0,
                            NbFaussesAlertes = acc.Confidence < 95 ? 1 : 0,
                            Confidence = acc.Confidence,
                            Gravite = gravite,
                            AlerteEnvoyee = res.SmsEnvoye,
                        });
                    }
                }

                // Sauvegarde sur disque
                string json = JsonConvert.SerializeObject(_historique, Formatting.Indented);
                File.WriteAllText(FICHIER_HISTORIQUE, json);

                // Rafraîchit le DGV
                RemplirDGV();
            }
            catch { }
        }

        #endregion

        #region Bouton Test Connexion
        private async void btnTestConnexion_Click(object sender, EventArgs e)
        {

            string url = txtAdresseBackend.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Entre une adresse valide.",
                    "SAFE ROAD", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTestConnexion.Enabled = false;
            btnTestConnexion.Text = "Test...";
            try
            {
                using var tmp = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string json = await tmp.GetStringAsync($"{url}/status");
                var st = JsonConvert.DeserializeObject<StatusModel>(json);

                if (st != null && st.Running)
                {
                    _backendUrl = url;
                    if (double.TryParse(txtSeuil.Text, out double s) && s >= 80 && s <= 100)
                        await AppliquerSeuilAsync(s / 100.0);

                    MessageBox.Show(
                        "Connexion réussie !\n\nLe backend est prêt.",
                        "SAFE ROAD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch
            {
                MessageBox.Show(
                    "Impossible de joindre le serveur.\nVérifie que python app.py est lancé.",
                    "SAFE ROAD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestConnexion.Enabled = true;
                btnTestConnexion.Text = "Tester la connexion";
            }
        }
        #endregion

        #region AppliquerSeuilAsync
        private async Task AppliquerSeuilAsync(double threshold)
        {
            try
            {
                var body = JsonConvert.SerializeObject(new { threshold });
                var c = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                await _http.PostAsync($"{_backendUrl}/config", c);
            }
            catch { }
        }
        #endregion

       
    }
}