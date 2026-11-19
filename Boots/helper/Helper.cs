using System.Windows;

namespace Boots.helper
{
    public class Helper
    {
        public void ShowInfo(string message)
        {
            MessageBox.Show(message, "Информация", MessageBoxButton.OK,
                MessageBoxImage.Information);
   
        }
        public void ShowErr(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK,
                MessageBoxImage.Error);

        }
        public void ShowWarning(string message)
        {
            MessageBox.Show(message, "Внимание", MessageBoxButton.OK,
                MessageBoxImage.Warning);

        }
    }
}
