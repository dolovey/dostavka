using System;
using System.Windows.Forms;

namespace DeliveryApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            try
            {
                // Принудительно показываем, что программа дошла до этого места
                MessageBox.Show("Программа запустилась. Сейчас создам базу!");
                AppState.Init(); 
            }
            catch (Exception ex)
            {
                // Если что-то сломалось — мы увидим ошибку
                MessageBox.Show("ОШИБКА ПРИ СОЗДАНИИ БД:\n" + ex.Message, "Критическая ошибка");
                return; // Останавливаем запуск
            }
            
            Application.Run(new MainForm());
        }
    }
}