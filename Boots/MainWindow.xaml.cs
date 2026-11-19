using Boots.helper;
using Boots.Data;
using Boots.Static;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace Boots;

public partial class MainWindow : Window
{

    private AppDBContext _db = new AppDBContext();
    private Helper _mesHelp = new Helper();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void LogInButton_Click(object sender, RoutedEventArgs e)
    {
        string login = LoginEnter.Text;
        


        var user = _db.Users.Where(u => u.Login == login).FirstOrDefault();
        if(user is null)
        {
            _mesHelp.ShowErr("Введите верный логин");
            return;
        } 
        else
        {
            CurrentUserSession.CurrentUser = user;
            new ProductWindow(user).Show();
            Close();
        }

    }

    private void TextBlock_MouseDown(object sender, RoutedEventArgs e)
    {
        new ProductWindow().Show();
        Close();
    }
}