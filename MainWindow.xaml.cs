using MySql.Data.MySqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFHotel.DAO;

namespace WPFHotel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text) ||
            string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Ingresa usuario y contraseña.",
                                "Campos vacíos", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUser.Text) ||
        string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Ingresa usuario y contraseña.",
                                "Campos vacíos", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dao = new UsuarioDAO();
                string rol = dao.ObtenerRol(txtUser.Text.Trim(),
                                            txtPassword.Password);

                if (rol == null)
                {
                    MessageBox.Show("Usuario o contraseña incorrectos.",
                                    "Acceso denegado", MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                // Pasa el rol a la ventana principal
                var inicio = new Inicio(rol);
                inicio.Show();
                this.Close();
            }
            
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}",
                                "Error", MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        /*private bool VerificarCredenciales(string usuario, string password)
        {
           
            //string Contraseña = password;

            try
            {
                // Usa ConexionDB2 -> base de datos "Personal"
                using (MySqlConnection conn = new MySqlConnection(ConexionBD.ConexionDB.Cadena))
                {
                    conn.Open();

                    string query = @"SELECT COUNT(1) 
                                     FROM usuarios
                                     WHERE usuario  = @usuario 
                                       AND password = @password";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@usuario", usuario);
                        cmd.Parameters.AddWithValue("@password", password);

                        int resultado = Convert.ToInt32(cmd.ExecuteScalar());
                        return resultado > 0;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return false;
            }
        }*/




    }
}


/* 
 * 
 * registar
 * string usuario = txtUser.Text.Trim();
            string password = txtPassword.Password;

            // Validar campos vacíos
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Por favor ingresa usuario y contraseña.",
                                "Campos vacíos",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            // Verificar contra la BD
            if (VerificarCredenciales(usuario, password))
            {
                MessageBox.Show($"¡Bienvenido, {usuario}!",
                                "Acceso concedido",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                Inicio Ventana1 = new Inicio();
                Ventana1.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Usuario o contraseña incorrectos.",
                                "Acceso denegado",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                txtPassword.Clear();
                txtPassword.Focus();
            }













*/