using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFHotel
{
    /// <summary>
    /// Lógica de interacción para AgregarHabitacionDialog.xaml
    /// </summary>
    public partial class AgregarHabitacionDialog : Window
    {
        public int Numero { get; private set; }
        public string Tipo { get; private set; }
        public double CostoBase { get; private set; }

        public AgregarHabitacionDialog()
        {
            InitializeComponent();
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(TxtCosto.Text, out double costo) || costo < 0)
            {
                MessageBox.Show("Ingresa un costo base válido.");
                return;
            }
            if (int.TryParse(TxtNumero.Text, out int num) && num > 0)
            {
                Numero = num;
                Tipo = (CbbTipo.SelectedIndex == 0) ? "A" : "B";
                CostoBase = costo;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Ingresa un número de habitación válido.");
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}