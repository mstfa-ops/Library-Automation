using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    #region 1. VERİ EKSTRAKSİYON VE BAĞLANTI KATMANI (DATA ACCESS LAYER)
    /// Veritabanı bağlantı döngülerini ve parametrik SQL sorgularını yöneten soyutlanmış erişim sınıfı.
    public class VeritabaniBaglantisi
    {
        // KutuphaneDB_Final veritabanına işaret eden güncel bağlantı dizesi
        string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=KutuphaneDB_Final;Integrated Security=True;";

        /// Geriye veri döndürmeyen (INSERT, UPDATE, DELETE) parametrik sorguları ve yordamları çalıştırır.
        public void ParametreliSorguCalistir(string sorgu, params SqlParameter[] parametreler)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(sorgu, con);
                if (parametreler != null && parametreler.Length > 0)
                {
                    cmd.Parameters.AddRange(parametreler);
                }
                con.Open(); 
                cmd.ExecuteNonQuery();
            }
        }

        /// Veritabanından veri kümelerini (SELECT) parametrik olarak çeker ve DataTable nesnesi halinde döndürür.
        public DataTable VeriGetir(string sorgu, params SqlParameter[] parametreler)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(sorgu, con);
                if (parametreler != null && parametreler.Length > 0)
                {
                    cmd.Parameters.AddRange(parametreler);
                }
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable(); 
                da.Fill(dt); 
                return dt;
            }
        }

        /// Saklı yordam (Stored Procedure) kullanarak yeni bir kullanıcı hesabı kaydeder.
        public void UyeOl(string adSoyad, string kulNo, string email, string sifre, int rolId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_UyeOl", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@AdSoyad", adSoyad); 
                cmd.Parameters.AddWithValue("@KullaniciNo", kulNo);
                cmd.Parameters.AddWithValue("@Email", email); 
                cmd.Parameters.AddWithValue("@Sifre", sifre); 
                cmd.Parameters.AddWithValue("@RolID", rolId);
                con.Open(); 
                cmd.ExecuteNonQuery();
            }
        }

        /// Kullanıcı kimlik doğrulamasını (Authentication) gerçekleştirir. 
        /// Sadece Durum=1 (Aktif) olan hesapların yetkilendirilmesine izin verilir.
        public int[] GirisYap(string email, string sifre)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("SELECT RolID, KullaniciID FROM Kullanicilar WHERE Email=@e AND Sifre=@s AND Durum=1", con);
                cmd.Parameters.AddWithValue("@e", email); 
                cmd.Parameters.AddWithValue("@s", sifre);
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read()) return new int[] { Convert.ToInt32(dr["RolID"]), Convert.ToInt32(dr["KullaniciID"]) };
                    else return new int[] { 0, 0 };
                }
            }
        }

        /// Envanterden kullanıcı üzerine kitap zimmetleme yordamını tetikler.
        public void KitapOduncAl(int kullaniciId, int kitapId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_KitapOduncAl", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@KullaniciID", kullaniciId); 
                cmd.Parameters.AddWithValue("@KitapID", kitapId);
                con.Open(); 
                cmd.ExecuteNonQuery();
            }
        }

        /// Envantere yeni bir yayın veya materyal kaydeder.
        public void KitapEkle(string ad, string yazar, int kategori, int stok, int basimYili, string isbn)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_KitapEkle", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@KitapAdi", ad); 
                cmd.Parameters.AddWithValue("@Yazar", yazar);
                cmd.Parameters.AddWithValue("@KategoriID", kategori); 
                cmd.Parameters.AddWithValue("@StokAdedi", stok);
                cmd.Parameters.AddWithValue("@BasimYili", basimYili); 
                cmd.Parameters.AddWithValue("@ISBN", isbn);
                con.Open(); 
                cmd.ExecuteNonQuery();
            }
        }

        /// Mevcut bir materyalin stok ve başlık bilgilerini günceller.
        public void KitapGuncelle(int id, string yeniAd, int yeniStok)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("UPDATE Kitaplar SET KitapAdi=@a, StokAdedi=@s WHERE KitapID=@id", con);
                cmd.Parameters.AddWithValue("@a", yeniAd); 
                cmd.Parameters.AddWithValue("@s", yeniStok); 
                cmd.Parameters.AddWithValue("@id", id);
                con.Open(); 
                cmd.ExecuteNonQuery();
            }
        }
    }
    #endregion

    public partial class Form1 : Form
    {
        // Sistem Durum Değişkenleri (State Variables)
        VeritabaniBaglantisi db = new VeritabaniBaglantisi();
        int aktifKullaniciID = 0;
        int aktifRolID = 0;

        #region Arayüz (UI) Araçlarının Bellekte Tanımlanması
        Panel pnlGiris = new Panel(); Panel pnlKayit = new Panel();
        TabControl tabAdmin = new TabControl(); TabControl tabOgrenci = new TabControl();

        TextBox txtEmail = new TextBox(); TextBox txtSifre = new TextBox();
        TextBox txtKayitAd = new TextBox(); TextBox txtKayitNo = new TextBox();
        TextBox txtKayitMail = new TextBox(); TextBox txtKayitSifre = new TextBox();
        ComboBox cmbRol = new ComboBox();

        DataGridView dgvAdminUyeler = new DataGridView(); DataGridView dgvYeniTalepler = new DataGridView(); DataGridView dgvAdminKitaplar = new DataGridView();
        TextBox txtKitapID = new TextBox(); TextBox txtKitapAdi = new TextBox(); TextBox txtYazar = new TextBox(); TextBox txtKategori = new TextBox();
        TextBox txtStok = new TextBox(); TextBox txtBasim = new TextBox(); TextBox txtIsbn = new TextBox();

        DataGridView dgvOgrenciKutuphane = new DataGridView(); DataGridView dgvOgrenciAldiklarim = new DataGridView();
        #endregion

        public Form1()
        {
            // Ana Pencere Başlangıç Konfigürasyonları
            this.Text = "Üniversite Kütüphane Otomasyonu V4";
            this.Size = new Size(950, 750);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Form Yapılarının Dinamik Olarak Üretilmesi
            GirisEkraniniOlustur();
            KayitEkraniniOlustur();
            AdminPaneliOlustur();
            OgrenciPaneliOlustur();

            // Başlangıç Görünürlük Ayarları
            pnlKayit.Visible = false; 
            tabAdmin.Visible = false; 
            tabOgrenci.Visible = false;
        }

        #region 2. GÖRSEL ARAYÜZ (UI) MİMARİSİNİN OLUŞTURULMASI
        /// Kullanıcı kimlik doğrulama panelini dinamik olarak çizer.
        private void GirisEkraniniOlustur()
        {
            pnlGiris.Dock = DockStyle.Fill;
            Label lbl1 = new Label() { Text = "SİSTEME GİRİŞ", Font = new Font("Arial", 16, FontStyle.Bold), Location = new Point(350, 150), AutoSize = true };
            Label lblGirisMail = new Label() { Text = "Email:", Location = new Point(350, 205), AutoSize = true }; txtEmail.Location = new Point(350, 220); txtEmail.Width = 200;
            Label lblGirisSifre = new Label() { Text = "Şifre:", Location = new Point(350, 255), AutoSize = true }; txtSifre.Location = new Point(350, 270); txtSifre.Width = 200; txtSifre.PasswordChar = '*';

            Button btnGiris = new Button() { Text = "Giriş Yap", Location = new Point(350, 310), Width = 200, Height = 35, BackColor = Color.LightGreen }; 
            btnGiris.Click += BtnGiris_Click;
            
            Button btnKaydaGit = new Button() { Text = "Üye değil misin? Üye Ol", Location = new Point(350, 360), Width = 200, Height = 30 }; 
            btnKaydaGit.Click += (s, e) => { pnlGiris.Visible = false; pnlKayit.Visible = true; };
            
            pnlGiris.Controls.AddRange(new Control[] { lbl1, lblGirisMail, txtEmail, lblGirisSifre, txtSifre, btnGiris, btnKaydaGit }); 
            this.Controls.Add(pnlGiris);
        }

        /// Yeni kayıt formunu ve veri giriş elemanlarını çizer.
        private void KayitEkraniniOlustur()
        {
            pnlKayit.Dock = DockStyle.Fill;
            Label lbl2 = new Label() { Text = "YENİ ÜYE KAYDI", Font = new Font("Arial", 16, FontStyle.Bold), Location = new Point(350, 80), AutoSize = true };
            Label lblKayitAd = new Label() { Text = "Ad Soyad:", Location = new Point(350, 125), AutoSize = true }; txtKayitAd.Location = new Point(350, 140); txtKayitAd.Width = 200;
            Label lblKayitNo = new Label() { Text = "Öğrenci / Sicil No:", Location = new Point(350, 175), AutoSize = true }; txtKayitNo.Location = new Point(350, 190); txtKayitNo.Width = 200;
            Label lblKayitMail = new Label() { Text = "Email:", Location = new Point(350, 225), AutoSize = true }; txtKayitMail.Location = new Point(350, 240); txtKayitMail.Width = 200;
            Label lblKayitSifre = new Label() { Text = "Şifre:", Location = new Point(350, 275), AutoSize = true }; txtKayitSifre.Location = new Point(350, 290); txtKayitSifre.Width = 200; txtKayitSifre.PasswordChar = '*';
            Label lblKayitRol = new Label() { Text = "Rolünüz:", Location = new Point(350, 325), AutoSize = true };

            cmbRol.Location = new Point(350, 340); cmbRol.Width = 200; cmbRol.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRol.Items.AddRange(new string[] { "Lisans Öğrencisi", "Yüksek Lisans", "Akademisyen" }); cmbRol.SelectedIndex = 0;

            Button btnKayit = new Button() { Text = "Üye Ol", Location = new Point(350, 380), Width = 200, Height = 35, BackColor = Color.LightSkyBlue }; 
            btnKayit.Click += BtnKayit_Click;
            
            Button btnGiriseGit = new Button() { Text = "Zaten üye misin? Giriş Yap", Location = new Point(350, 425), Width = 200, Height = 30 }; 
            btnGiriseGit.Click += (s, e) => { pnlKayit.Visible = false; pnlGiris.Visible = true; };
            
            pnlKayit.Controls.AddRange(new Control[] { lbl2, lblKayitAd, txtKayitAd, lblKayitNo, txtKayitNo, lblKayitMail, txtKayitMail, lblKayitSifre, txtKayitSifre, lblKayitRol, cmbRol, btnKayit, btnGiriseGit }); 
            this.Controls.Add(pnlKayit);
        }

        /// Yöneticilere (Admin) ayrılmış sekmeli envanter ve kullanıcı denetim sayfasını çizer.
        private void AdminPaneliOlustur()
        {
            tabAdmin.Dock = DockStyle.Fill; 
            TabPage tpKullanicilar = new TabPage("Kullanıcı Yönetimi"); 
            TabPage tpTalepler = new TabPage("Kitap Talepleri"); 
            TabPage tpKitapYonetimi = new TabPage("Kitap Yönetimi");

            // Kullanıcı Yönetimi Sekmesi
            dgvAdminUyeler.Location = new Point(20, 20); dgvAdminUyeler.Size = new Size(800, 300); dgvAdminUyeler.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            
            Button btnKullaniciSil = new Button() { Text = "Sil", Location = new Point(20, 340), Width = 150, BackColor = Color.LightCoral }; 
            btnKullaniciSil.Click += BtnKullaniciSil_Click;
            
            Button btnCikisAdmin = new Button() { Text = "Çıkış Yap", Location = new Point(670, 340), Width = 150 }; 
            btnCikisAdmin.Click += (s, e) => { tabAdmin.Visible = false; pnlGiris.Visible = true; aktifKullaniciID = 0; aktifRolID = 0; };
            tpKullanicilar.Controls.AddRange(new Control[] { dgvAdminUyeler, btnKullaniciSil, btnCikisAdmin });

            // Kitap Satın Alma Talepleri Sekmesi
            dgvYeniTalepler.Location = new Point(20, 20); dgvYeniTalepler.Size = new Size(800, 300); dgvYeniTalepler.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            Button btnTalepTamamla = new Button() { Text = "Seçili Talebi 'Kütüphaneye Eklendi' İşaretle", Location = new Point(20, 340), Width = 250, BackColor = Color.LightGreen }; 
            btnTalepTamamla.Click += BtnTalepTamamla_Click;
            tpTalepler.Controls.AddRange(new Control[] { dgvYeniTalepler, btnTalepTamamla });

            // Envanter (Kitap) Yönetimi Sekmesi
            dgvAdminKitaplar.Location = new Point(20, 20); dgvAdminKitaplar.Size = new Size(850, 250); dgvAdminKitaplar.SelectionMode = DataGridViewSelectionMode.FullRowSelect; 
            dgvAdminKitaplar.CellClick += DgvAdminKitaplar_CellClick;

            Label l1 = new Label() { Text = "ID:", Location = new Point(20, 300), AutoSize = true }; txtKitapID.Location = new Point(100, 297); txtKitapID.Width = 50; txtKitapID.Enabled = false;
            Label l2 = new Label() { Text = "Kitap Adı:", Location = new Point(20, 330), AutoSize = true }; txtKitapAdi.Location = new Point(100, 327); txtKitapAdi.Width = 200;
            Label l3 = new Label() { Text = "Yazar:", Location = new Point(20, 360), AutoSize = true }; txtYazar.Location = new Point(100, 357); txtYazar.Width = 200;
            Label l4 = new Label() { Text = "Kat.ID:", Location = new Point(20, 390), AutoSize = true }; txtKategori.Location = new Point(100, 387); txtKategori.Width = 50;
            Label l5 = new Label() { Text = "Stok:", Location = new Point(350, 330), AutoSize = true }; txtStok.Location = new Point(410, 327); txtStok.Width = 100;
            Label l6 = new Label() { Text = "Basım Yılı:", Location = new Point(350, 360), AutoSize = true }; txtBasim.Location = new Point(410, 357); txtBasim.Width = 100;
            Label l7 = new Label() { Text = "ISBN:", Location = new Point(350, 390), AutoSize = true }; txtIsbn.Location = new Point(410, 387); txtIsbn.Width = 100;

            Button btnEkle = new Button() { Text = "Yeni Ekle", Location = new Point(550, 320), Width = 100, BackColor = Color.LightGreen }; 
            btnEkle.Click += BtnEkle_Click;
            
            Button btnGuncelle = new Button() { Text = "Stoğu/Adı Güncelle", Location = new Point(660, 320), Width = 120, BackColor = Color.LightSkyBlue }; 
            btnGuncelle.Click += BtnGuncelle_Click;
            
            Button btnSil = new Button() { Text = "Sil", Location = new Point(550, 360), Width = 100, BackColor = Color.LightCoral }; 
            btnSil.Click += BtnSil_Click;

            tpKitapYonetimi.Controls.AddRange(new Control[] { dgvAdminKitaplar, l1, l2, l3, l4, l5, l6, l7, txtKitapID, txtKitapAdi, txtYazar, txtKategori, txtStok, txtBasim, txtIsbn, btnEkle, btnGuncelle, btnSil });
            tabAdmin.TabPages.Add(tpKullanicilar); 
            tabAdmin.TabPages.Add(tpTalepler); 
            tabAdmin.TabPages.Add(tpKitapYonetimi); 
            this.Controls.Add(tabAdmin);
        }

        /// Öğrenci ve Akademisyenlere ayrılmış zimmet, rezervasyon ve talep arayüzünü çizer.
        private void OgrenciPaneliOlustur()
        {
            tabOgrenci.Dock = DockStyle.Fill; 
            TabPage tpKutuphane = new TabPage("Kütüphane & Rezervasyon"); 
            TabPage tpAldiklarim = new TabPage("Aldıklarım & İade"); 
            TabPage tpTalep = new TabPage("Yeni Kitap Talep Et");

            // Kütüphane Listesi ve Rezervasyon Sekmesi
            dgvOgrenciKutuphane.Location = new Point(20, 20); dgvOgrenciKutuphane.Size = new Size(800, 300); dgvOgrenciKutuphane.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            Button btnOduncAl = new Button() { Text = "Ödünç Al", Location = new Point(20, 340), Width = 100, BackColor = Color.LightGreen }; 
            btnOduncAl.Click += BtnOduncAl_Click;
            
            Button btnRezervasyon = new Button() { Text = "Stok Yoksa Rezervasyon Yap", Location = new Point(130, 340), Width = 200, BackColor = Color.Orange }; 
            btnRezervasyon.Click += BtnRezervasyon_Click;
            
            Button btnCikisOgr = new Button() { Text = "Çıkış Yap", Location = new Point(670, 340), Width = 150 }; 
            btnCikisOgr.Click += (s, e) => { tabOgrenci.Visible = false; pnlGiris.Visible = true; aktifKullaniciID = 0; aktifRolID = 0; };
            tpKutuphane.Controls.AddRange(new Control[] { dgvOgrenciKutuphane, btnOduncAl, btnRezervasyon, btnCikisOgr });

            // Üzerimdeki Materyaller ve İade Sekmesi
            dgvOgrenciAldiklarim.Location = new Point(20, 20); dgvOgrenciAldiklarim.Size = new Size(800, 300); dgvOgrenciAldiklarim.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            Button btnIade = new Button() { Text = "Seçili Kitabı İade Et", Location = new Point(20, 340), Width = 150, BackColor = Color.LightSkyBlue }; 
            btnIade.Click += BtnIade_Click;
            tpAldiklarim.Controls.AddRange(new Control[] { dgvOgrenciAldiklarim, btnIade });

            // Yeni Kütüphane Materyali İstek Sekmesi
            Label lblTalepKitap = new Label() { Text = "İstediğiniz Kitabın Adı:", Location = new Point(20, 25), AutoSize = true }; TextBox txtTalepKitap = new TextBox() { Location = new Point(20, 40), Width = 200 };
            Label lblTalepYazar = new Label() { Text = "Kitabın Yazarı:", Location = new Point(20, 65), AutoSize = true }; TextBox txtTalepYazar = new TextBox() { Location = new Point(20, 80), Width = 200 };
            Button btnTalepGonder = new Button() { Text = "Talebi Kütüphaneye Gönder", Location = new Point(20, 120), Width = 200 };
            
            btnTalepGonder.Click += (s, e) => {
                db.ParametreliSorguCalistir("INSERT INTO KitapTalepleri(KullaniciID, IstenenKitapAdi, Yazar) VALUES(@k,@a,@y)", new SqlParameter("@k", aktifKullaniciID), new SqlParameter("@a", txtTalepKitap.Text), new SqlParameter("@y", txtTalepYazar.Text));
                MessageBox.Show("Talebiniz kütüphane yetkilisine başarıyla iletildi!", "Talep Alındı"); 
                txtTalepKitap.Text = ""; 
                txtTalepYazar.Text = "";
            };
            tpTalep.Controls.AddRange(new Control[] { lblTalepKitap, txtTalepKitap, lblTalepYazar, txtTalepYazar, btnTalepGonder });

            tabOgrenci.TabPages.Add(tpKutuphane); 
            tabOgrenci.TabPages.Add(tpAldiklarim); 
            tabOgrenci.TabPages.Add(tpTalep); 
            this.Controls.Add(tabOgrenci);
        }
        #endregion

        #region 3. İŞ KULLANIM KONTROLLERİ VE OLAY YÖNETİMİ (BUSINESS LOGIC LAYER)
        /// Kullanıcı giriş sürecini, yetki denetimlerini ve sayfa yönlendirmelerini yönetir.
        private void BtnGiris_Click(object sender, EventArgs e)
        {
            int[] sonuc = db.GirisYap(txtEmail.Text, txtSifre.Text);
            aktifRolID = sonuc[0]; 
            aktifKullaniciID = sonuc[1];

            // Yönetici Yetki Kontrolü (1: Kurucu, 2: Admin)
            if (aktifRolID == 1 || aktifRolID == 2) 
            { 
                pnlGiris.Visible = false; 
                tabAdmin.Visible = true; 
                TablolariYenile(); 
            }
            // Standart Üye Kontrolü (3: Lisans, 4: YL, 5: Akademisyen)
            else if (aktifRolID >= 3 && aktifRolID <= 5) 
            { 
                pnlGiris.Visible = false; 
                tabOgrenci.Visible = true; 
                TablolariYenile(); 
                BildirimleriKontrolEt(); // Bekleyen dinamik bildirimler tetiklenir
            }
            else 
            { 
                MessageBox.Show("Hatalı veya silinmiş kimlik bilgileri! Lütfen e-posta veya şifrenizi kontrol ediniz.", "Erişim Reddedildi", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        /// Yeni üye kaydı form verilerini doğrulayarak sisteme aktarır.
        private void BtnKayit_Click(object sender, EventArgs e)
        {
            try 
            { 
                int secilenRol = cmbRol.SelectedIndex + 3; 
                db.UyeOl(txtKayitAd.Text, txtKayitNo.Text, txtKayitMail.Text, txtKayitSifre.Text, secilenRol); 
                MessageBox.Show("Üyelik işlemi başarıyla tamamlandı! Sisteme giriş yapabilirsiniz.", "Kayıt Başarılı"); 
                pnlKayit.Visible = false; 
                pnlGiris.Visible = true; 
            }
            catch (Exception ex) 
            { 
                MessageBox.Show("Kayıt işlemi gerçekleştirilemedi. Kullanıcı numarası veya e-posta adresi sistemde zaten kayıtlı olabilir.\nDetay: " + ex.Message, "Kayıt Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning); 
            }
        }

        /// Yöneticilerin sistemdeki bir hesabı pasife alması sırasındaki hiyerarşik yetki kurallarını denetler.
        private void BtnKullaniciSil_Click(object sender, EventArgs e)
        {
            if (dgvAdminUyeler.SelectedRows.Count > 0)
            {
                int silinecekRol = Convert.ToInt32(dgvAdminUyeler.SelectedRows[0].Cells["RolID"].Value);
                int silinecekID = Convert.ToInt32(dgvAdminUyeler.SelectedRows[0].Cells["KullaniciID"].Value);

                // Standart yöneticilerin, diğer yöneticileri veya kurucuyu silmesi engellenir.
                if (aktifRolID == 2 && silinecekRol <= 2) 
                { 
                    MessageBox.Show("Güvenlik İhlali: Standart yöneticiler, üst düzey veya eş düzey yetkili hesaplarını sistemden kaldıramaz!", "Yetki Hatası", MessageBoxButtons.OK, MessageBoxIcon.Stop); 
                    return; 
                }
                // Kurucu hesabının sistemden kaldırılması engellenir.
                if (aktifRolID == 1 && silinecekRol == 1) 
                { 
                    MessageBox.Show("Sistem Bütünlüğü: Kurucu yönetici hesabı sistemden silinemez!", "İşlem Engellendi", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                    return; 
                }

                db.ParametreliSorguCalistir("EXEC sp_KullaniciSil @id", new SqlParameter("@id", silinecekID));
                MessageBox.Show("Kullanıcı hesabı dolaşımdan başarıyla kaldırıldı.", "İşlem Tamamlandı"); 
                TablolariYenile();
            }
        }

        /// Satın alma taleplerini yönetici onayı ile tamamlanmış statüye geçirir.
        private void BtnTalepTamamla_Click(object sender, EventArgs e)
        {
            if (dgvYeniTalepler.SelectedRows.Count > 0)
            {
                int talepId = Convert.ToInt32(dgvYeniTalepler.SelectedRows[0].Cells["TalepID"].Value);
                db.ParametreliSorguCalistir("UPDATE KitapTalepleri SET Durum='Tamamlandı' WHERE TalepID=@tid", new SqlParameter("@tid", talepId));
                MessageBox.Show("Talep statüsü güncellendi! İlgili kullanıcıya sisteme ilk girişinde otomatik bildirim iletilecektir.", "Talep Tamamlandı"); 
                TablolariYenile();
            }
        }

        /// Yönetici envanter tablosunda tıklanan satırdaki verileri düzenleme kutucuklarına aktarır.
        private void DgvAdminKitaplar_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Tıklanan satırın yeni ekleme satırı (boş satır) olup olmadığı denetlenir
            if (e.RowIndex >= 0 && !dgvAdminKitaplar.Rows[e.RowIndex].IsNewRow)
            {
                DataGridViewRow row = dgvAdminKitaplar.Rows[e.RowIndex];
                txtKitapID.Text = row.Cells["KitapID"].Value.ToString(); 
                txtKitapAdi.Text = row.Cells["KitapAdi"].Value.ToString();
                txtYazar.Text = row.Cells["Yazar"].Value.ToString(); 
                txtStok.Text = row.Cells["StokAdedi"].Value.ToString();
                txtBasim.Text = row.Cells["BasimYili"].Value.ToString(); 
                txtIsbn.Text = row.Cells["ISBN"].Value.ToString();
                txtKategori.Text = row.Cells["KategoriID"].Value.ToString();
            }
        }

        /// Form kutucuklarındaki verileri kontrol ederek kütüphaneye yeni yayın kaydeder.
        private void BtnEkle_Click(object sender, EventArgs e)
        {
            try 
            { 
                db.KitapEkle(txtKitapAdi.Text, txtYazar.Text, Convert.ToInt32(txtKategori.Text), Convert.ToInt32(txtStok.Text), Convert.ToInt32(txtBasim.Text), txtIsbn.Text); 
                MessageBox.Show("Yeni materyal envantere eklendi.", "Kayıt Başarılı"); 
                TablolariYenile(); 
            }
            catch (Exception ex) 
            { 
                MessageBox.Show("Veri girişi hatalı! Lütfen tüm sayısal ve metinsel alanların uygun doldurulduğundan emin olunuz.\nDetay: " + ex.Message, "İşlem Başarısız", MessageBoxButtons.OK, MessageBoxIcon.Warning); 
            }
        }

        /// Seçili kitabın stok miktarı veya isim verilerini veritabanında günceller.
        private void BtnGuncelle_Click(object sender, EventArgs e)
        {
            try 
            { 
                if (txtKitapID.Text == "") return; 
                db.KitapGuncelle(Convert.ToInt32(txtKitapID.Text), txtKitapAdi.Text, Convert.ToInt32(txtStok.Text)); 
                MessageBox.Show("Materyal bilgileri başarıyla güncellendi.", "Güncelleme Tamamlandı"); 
                TablolariYenile(); 
            }
            catch (Exception ex) 
            { 
                MessageBox.Show("Güncelleme hatası: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        /// Seçilen kitabı arayüzden ve dolaşımdan gizler (Soft Delete).
        private void BtnSil_Click(object sender, EventArgs e)
        {
            try 
            { 
                if (txtKitapID.Text == "") return; 
                if (MessageBox.Show("Seçili materyali sistemden kaldırmak istediğinize emin misiniz?", "Kaldırma Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) 
                { 
                    db.ParametreliSorguCalistir("EXEC sp_KitapSil @id", new SqlParameter("@id", Convert.ToInt32(txtKitapID.Text))); 
                    TablolariYenile(); 
                } 
            }
            catch (Exception ex) 
            { 
                MessageBox.Show("Silme işlemi sırasında hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        /// Kullanıcının seçtiği materyali ödünç alma isteğini işler. Stok durumunu denetler.
        private void BtnOduncAl_Click(object sender, EventArgs e)
        {
            // Boş satıra tıklama hatalarını engellemek için IsNewRow kontrolü yapılır
            if (dgvOgrenciKutuphane.SelectedRows.Count > 0 && !dgvOgrenciKutuphane.SelectedRows[0].IsNewRow)
            {
                int stok = Convert.ToInt32(dgvOgrenciKutuphane.SelectedRows[0].Cells["StokAdedi"].Value);
                if (stok > 0)
                {
                    int kitapId = Convert.ToInt32(dgvOgrenciKutuphane.SelectedRows[0].Cells["KitapID"].Value);
                    db.KitapOduncAl(aktifKullaniciID, kitapId); 
                    MessageBox.Show("Materyal üzerinize zimmetlenmiştir!\n\nÖNEMLİ: Akademik sunum testi amacıyla teslim süreniz 5 saniye olarak konfigüre edilmiştir.", "İşlem Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                    TablolariYenile();
                }
                else 
                { 
                    MessageBox.Show("Seçilen materyalin tüm kopyaları dolaşımdadır! Dilerseniz rezervasyon yaparak iade sırasına girebilirsiniz.", "Stok Yetersiz", MessageBoxButtons.OK, MessageBoxIcon.Warning); 
                }
            }
        }

        /// Stoku bulunmayan kitaplar için kullanıcıyı veritabanında bekleme sırasına alır.
        private void BtnRezervasyon_Click(object sender, EventArgs e)
        {
            if (dgvOgrenciKutuphane.SelectedRows.Count > 0 && !dgvOgrenciKutuphane.SelectedRows[0].IsNewRow)
            {
                int kitapId = Convert.ToInt32(dgvOgrenciKutuphane.SelectedRows[0].Cells["KitapID"].Value);
                db.ParametreliSorguCalistir("INSERT INTO Rezervasyonlar(KullaniciID, KitapID) VALUES(@k,@i)", new SqlParameter("@k", aktifKullaniciID), new SqlParameter("@i", kitapId));
                MessageBox.Show("Rezervasyon işleminiz kaydedildi. Materyal kütüphaneye iade edildiği an sistem üzerinden bilgilendirileceksiniz.", "Rezervasyon Aktif");
            }
        }

        /// Kullanıcı üzerindeki materyali kütüphaneye iade eder. Gecikme ve ceza tahakkukunu denetler.
        private void BtnIade_Click(object sender, EventArgs e)
        {
            if (dgvOgrenciAldiklarim.SelectedRows.Count > 0 && !dgvOgrenciAldiklarim.SelectedRows[0].IsNewRow)
            {
                int islemId = Convert.ToInt32(dgvOgrenciAldiklarim.SelectedRows[0].Cells["IslemID"].Value);
                
                // İade yordamı çalıştırılır (Arka planda tetikleyici -Trigger- devreye girer)
                db.ParametreliSorguCalistir("UPDATE OduncIslemleri SET GercekIadeTarihi = GETDATE() WHERE IslemID = @id", new SqlParameter("@id", islemId));

                // İade anında tetikleyicinin oluşturduğu bir ceza faturası olup olmadığı denetlenir
                DataTable cezaKontrol = db.VeriGetir("SELECT CezaTutari FROM CezaIslemleri WHERE IslemID=@id", new SqlParameter("@id", islemId));
                if (cezaKontrol.Rows.Count > 0)
                {
                    string ceza = cezaKontrol.Rows[0]["CezaTutari"].ToString();
                    MessageBox.Show($"Materyal iade alınmıştır.\n\nSİSTEM UYARISI: Belirlenen teslim süresini aştığınız tespit edilmiştir. Gecikme bedeli olarak hesabınıza {ceza} TL ceza tahakkuk ettirilmiştir.", "Gecikme Cezası Tahakkuku", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else 
                { 
                    MessageBox.Show("Materyal zamanında ve cezasız olarak kütüphaneye iade edilmiştir. Teşekkür ederiz.", "İade Başarılı"); 
                }
                TablolariYenile();
            }
        }

        /// Kullanıcı sisteme girdiğinde, geçmiş taleplerinin veya bekleyen rezervasyonlarının 
        /// durumunu kontrol ederek pasif uyarı pencereleri (Push Notifications) üretir.
        private void BildirimleriKontrolEt()
        {
            // 1. Yönetici tarafından onaylanmış yeni yayın taleplerinin bildirimi
            DataTable talepler = db.VeriGetir("SELECT TalepID, IstenenKitapAdi FROM KitapTalepleri WHERE KullaniciID=@id AND Durum='Tamamlandı'", new SqlParameter("@id", aktifKullaniciID));
            foreach (DataRow row in talepler.Rows)
            {
                MessageBox.Show($"BİLDİRİM: Talep etmiş olduğunuz '{row["IstenenKitapAdi"]}' isimli yayın kütüphane envanterine eklenmiştir!", "Talep Sonuçlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                db.ParametreliSorguCalistir("UPDATE KitapTalepleri SET Durum='Bildirildi' WHERE TalepID=@tid", new SqlParameter("@tid", row["TalepID"]));
            }

            // 2. Kullanıcının sırada beklediği ve o an kütüphaneye iade edilmiş materyallerin bildirimi
            DataTable rezervasyonlar = db.VeriGetir("SELECT R.RezervasyonID, K.KitapAdi FROM Rezervasyonlar R JOIN Kitaplar K ON R.KitapID=K.KitapID WHERE R.KullaniciID=@id AND R.Durum='Sırada Bekliyor' AND K.StokAdedi > 0", new SqlParameter("@id", aktifKullaniciID));
            foreach (DataRow row in rezervasyonlar.Rows)
            {
                MessageBox.Show($"BİLDİRİM: Rezervasyon yaptığınız '{row["KitapAdi"]}' materyali şu an raflarda mevcuttur! Kütüphaneden zimmetinize alabilirsiniz.", "Rezervasyon Durumu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                db.ParametreliSorguCalistir("UPDATE Rezervasyonlar SET Durum='Bildirildi' WHERE RezervasyonID=@rid", new SqlParameter("@rid", row["RezervasyonID"]));
            }
        }

        /// Veritabanındaki güncel veri durumunu çekerek arayüzdeki DataGridView tablolarını tazeler.
        private void TablolariYenile()
        {
            // Yönetici Görünümleri (Sadece henüz silinmemiş aktif hesaplar ve kitaplar filtrelenir)
            if (aktifRolID == 1 || aktifRolID == 2)
            {
                dgvAdminUyeler.DataSource = db.VeriGetir("SELECT KullaniciID, AdSoyad, KullaniciNo, Email, RolID FROM Kullanicilar WHERE Durum = 1");
                dgvYeniTalepler.DataSource = db.VeriGetir("SELECT T.TalepID, K.AdSoyad, T.IstenenKitapAdi, T.Yazar, T.Durum, T.TalepTarihi FROM KitapTalepleri T JOIN Kullanicilar K ON T.KullaniciID = K.KullaniciID WHERE T.Durum != 'Bildirildi'");
                dgvAdminKitaplar.DataSource = db.VeriGetir("SELECT * FROM Kitaplar WHERE Durum = 1");
            }
            // Öğrenci ve Akademisyen Görünümleri
            else if (aktifRolID >= 3)
            {
                // Kullanıcıya fiziksel miktar yerine gerçekçi kütüphane durumu (Rafta / Ödünç Verildi) yansıtılır.
                string sorgu = @"SELECT KitapID, KitapAdi, Yazar, BasimYili, ISBN, 
                                 CASE WHEN StokAdedi > 0 THEN 'Rafta' ELSE 'Ödünç Verildi' END AS [Mevcut Durum], 
                                 StokAdedi FROM Kitaplar WHERE Durum = 1";
                
                dgvOgrenciKutuphane.DataSource = db.VeriGetir(sorgu);
                
                // Arka plandaki zimmetleme algoritmasının çalışması için stok bilgisi bellekte tutulur, arayüzden gizlenir.
                dgvOgrenciKutuphane.Columns["StokAdedi"].Visible = false;

                dgvOgrenciAldiklarim.DataSource = db.VeriGetir($"SELECT O.IslemID, K.KitapAdi, O.AlisTarihi, O.PlanlananIadeTarihi FROM OduncIslemleri O JOIN Kitaplar K ON O.KitapID=K.KitapID WHERE O.KullaniciID={aktifKullaniciID} AND O.GercekIadeTarihi IS NULL");
            }
        }

        #region DESIGNER.CS UYUM KÖPRÜSÜ
        /// Form1.Designer.cs dosyasının otomatik aradığı ve hata vermesini önleyen boş yükleme olayıdır.
        private void Form1_Load(object sender, EventArgs e)
        {
            // UI kısımları yapıcı metotta (Constructor) yüklendiği için bu alan boş bırakılmıştır.
        }
        #endregion

        #endregion
    }
}
