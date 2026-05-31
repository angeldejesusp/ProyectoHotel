using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFHotel
{
    /// <summary>
    /// Lógica de interacción para Splash.xaml
    /// </summary>
    public partial class Splash : Window
    {
        public Splash()
        {
            InitializeComponent();
            Loaded += Splash_Loaded;
        }

        private async void Splash_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Fade in + pulso del ícono
            var fadeIn = (Storyboard)FindResource("FadeIn");
            var pulso = (Storyboard)FindResource("Pulso");
            var progreso = (Storyboard)FindResource("Progreso");

            fadeIn.Begin();
            pulso.Begin();

            // Pequeña pausa para que el fade in se vea antes del progreso
            await Task.Delay(600);

            // 2. Simular pasos de carga con mensajes
            await SimularCarga();

            // 3. Iniciar barra de progreso (su Completed dispara FadeOut)
            progreso.Begin();
        }

        private async Task SimularCarga()
        {
            var pasos = new[]
            {
                (300,  "Conectando a la base de datos..."),
                (500,  "Cargando configuración..."),
                (400,  "Verificando habitaciones..."),
                (400,  "Cargando reservaciones activas..."),
                (300,  "Preparando interfaz..."),
                (300,  "¡Listo!")
            };

            foreach (var (delay, mensaje) in pasos)
            {
                await Task.Delay(delay);
                TxtEstado.Text = mensaje;
            }
        }

        // Se dispara cuando la barra llega al 100%
        private void Progreso_Completed(object sender, EventArgs e)
        {
            var fadeOut = (Storyboard)FindResource("FadeOut");
            fadeOut.Begin();
        }

        // Se dispara cuando el fade out termina
        private void FadeOut_Completed(object sender, EventArgs e)
        {
            // Abrir LoginWindow con su propio fade in
            var login = new MainWindow();
            login.Opacity = 0;
            login.Show();

            // Fade in del login
            var anim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500)
            };
            login.BeginAnimation(OpacityProperty, anim);

            this.Close();
        }
    }
}
