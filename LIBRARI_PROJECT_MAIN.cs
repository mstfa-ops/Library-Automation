using System;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Masaüstü uygulamasının işletim sistemi tarafından yürütülmeye başlandığı başlangıç sınıfı.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Uygulamanın ana giriş noktasıdır (Entry Point). Bellek alanını ve UI iş parçacığını (Thread) yapılandırır.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // İşletim sisteminin modern görsel temalarını ve metin işleme motorunu devreye alır
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // İşleyiş döngüsünü başlatır ve ana ekran olan Form1 nesnesini belleğe yükleyerek ekrana getirir
            Application.Run(new Form1());
        }
    }
}