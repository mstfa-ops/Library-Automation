using System;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    /// Masaüstü uygulamasının işletim sistemi tarafından yürütülmeye başlandığı başlangıç sınıfı.
    static class Program
    {
        /// Uygulamanın ana giriş noktasıdır (Entry Point). Bellek alanını ve UI iş parçacığını (Thread) yapılandırır.
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
