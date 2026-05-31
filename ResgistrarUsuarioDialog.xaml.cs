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
using static WPFHotel.DAO.ConexionBD;
using MySql.Data.MySqlClient;

namespace WPFHotel
{
    public partial class RegistrarUsuarioDialog : Window
    {
        public RegistrarUsuarioDialog()
        {
            InitializeComponent();
        }

        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            // ── Validaciones ──
            if (string.IsNullOrWhiteSpace(TxtNombre.Text) ||
                string.IsNullOrWhiteSpace(TxtUsuario.Text) ||
                string.IsNullOrWhiteSpace(PwbPassword.Password))
            {
                MessageBox.Show("Por favor completa todos los campos.",
                                "Campos vacíos", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (PwbPassword.Password != PwbConfirmar.Password)
            {
                MessageBox.Show("Las contraseñas no coinciden.",
                                "Error", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                PwbConfirmar.Clear();
                PwbConfirmar.Focus();
                return;
            }

            if (PwbPassword.Password.Length < 4)
            {
                MessageBox.Show("La contraseña debe tener al menos 4 caracteres.",
                                "Contraseña débil", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            string rol = ((ComboBoxItem)CbbRol.SelectedItem).Content.ToString();

            // Confirmar antes de guardar
            var confirmacion = MessageBox.Show(
                $"¿Registrar el siguiente usuario?\n\n" +
                $"Nombre:  {TxtNombre.Text.Trim()}\n" +
                $"Usuario: {TxtUsuario.Text.Trim()}\n" +
                $"Rol:     {rol}",
                "Confirmar registro",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmacion != MessageBoxResult.Yes) return;

            try
            {
                using (var conn = new MySqlConnection(ConexionDB.Cadena))
                {
                    conn.Open();

                    // Verificar que el nombre de usuario no exista ya
                    string queryCheck = "SELECT COUNT(1) FROM personal WHERE usuario = @usuario";
                    var cmdCheck = new MySqlCommand(queryCheck, conn);
                    cmdCheck.Parameters.AddWithValue("@usuario", TxtUsuario.Text.Trim());

                    int existe = Convert.ToInt32(cmdCheck.ExecuteScalar());
                    if (existe > 0)
                    {
                        MessageBox.Show("Ese nombre de usuario ya está en uso.\nElige otro.",
                                        "Usuario duplicado", MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        TxtUsuario.Focus();
                        return;
                    }

                    // Insertar nuevo usuario con contraseña hasheada
                    string query = @"INSERT INTO personal (nombre, usuario, password, rol)
                                     VALUES (@nombre, @usuario, @password, @rol)";

                    var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@nombre", TxtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@usuario", TxtUsuario.Text.Trim());
                    cmd.Parameters.AddWithValue("@password", HashearPassword(PwbPassword.Password));
                    cmd.Parameters.AddWithValue("@rol", rol);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Usuario registrado correctamente.",
                                "Éxito", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}",
                                "Error de base de datos", MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // SHA-256 igual que en UsuarioDAO para que coincidan
        private string HashearPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
