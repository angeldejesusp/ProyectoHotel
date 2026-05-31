using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using MySql.Data.MySqlClient;
using System;
using static WPFHotel.DAO.ConexionBD;

namespace WPFHotel
{
    /// <summary>
    /// Lógica de interacción para CambiarContraseñaDialog.xaml
    /// </summary>
    public partial class CambiarContraseñaDialog : Window
    {
        public CambiarContraseñaDialog()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                var fadeIn = (Storyboard)Resources["FadeIn"];
                fadeIn.Begin();
            };
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string usuario =TxtUsuario.Text;
            string actual = PwbActual.Password;
            string nueva = PwbNueva.Password;
            string confirmar = PwbConfirmar.Password;

            // ── Validaciones ──
            if (string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(actual) ||
                string.IsNullOrWhiteSpace(nueva) ||
                string.IsNullOrWhiteSpace(confirmar))
            {
                MostrarMensaje("Por favor completa todos los campos.", esError: true);
                return;
            }

            if (nueva.Length < 4)
            {
                MostrarMensaje("La nueva contraseña debe tener al menos 4 caracteres.", esError: true);
                return;
            }

            if (nueva != confirmar)
            {
                MostrarMensaje("La nueva contraseña y su confirmación no coinciden.", esError: true);
                PwbConfirmar.Clear();
                PwbConfirmar.Focus();
                return;
            }

            if (nueva == actual)
            {
                MostrarMensaje("La nueva contraseña no puede ser igual a la actual.", esError: true);
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(ConexionDB.Cadena))
                {
                    conn.Open();

                    // Verificar que usuario + contraseña actual sean correctos
                    string hashActual = Hashear(actual);
                    string qVerificar = @"SELECT COUNT(1) FROM personal
                                          WHERE usuario  = @usuario
                                            AND password = @password";

                    var cmdV = new MySqlCommand(qVerificar, conn);
                    cmdV.Parameters.AddWithValue("@usuario", usuario);
                    cmdV.Parameters.AddWithValue("@password", hashActual);

                    int existe = Convert.ToInt32(cmdV.ExecuteScalar());
                    if (existe == 0)
                    {
                        MostrarMensaje("Usuario o contraseña actual incorrectos.", esError: true);
                        PwbActual.Clear();
                        PwbActual.Focus();
                        return;
                    }

                    // Actualizar contraseña
                    string hashNueva = Hashear(nueva);
                    string qUpdate = @"UPDATE personal
                                         SET password = @nuevaPass
                                         WHERE usuario = @usuario";

                    var cmdU = new MySqlCommand(qUpdate, conn);
                    cmdU.Parameters.AddWithValue("@nuevaPass", hashNueva);
                    cmdU.Parameters.AddWithValue("@usuario", usuario);
                    cmdU.ExecuteNonQuery();
                }

                MostrarMensaje("✔  Contraseña actualizada correctamente.", esError: false);

                // Limpiar campos
                TxtUsuario.Clear();
                PwbActual.Clear();
                PwbNueva.Clear();
                PwbConfirmar.Clear();

                // Cerrar tras 1.5 segundos
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(1500)
                };
                timer.Tick += (s, _) => { timer.Stop(); Close(); };
                timer.Start();
            }
            catch (MySqlException ex)
            {
                MostrarMensaje($"Error de base de datos: {ex.Message}", esError: true);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ── Muestra mensaje de error o éxito ──
        private void MostrarMensaje(string texto, bool esError)
        {
            TxtMensaje.Text = texto;
            TxtMensaje.Foreground = esError
                ? new SolidColorBrush(Color.FromRgb(248, 113, 113))   // rojo
                : new SolidColorBrush(Color.FromRgb(74, 222, 128));  // verde
            TxtMensaje.Visibility = Visibility.Visible;
        }

        // ── SHA-256 igual que en UsuarioDAO ──
        private string Hashear(string texto)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
