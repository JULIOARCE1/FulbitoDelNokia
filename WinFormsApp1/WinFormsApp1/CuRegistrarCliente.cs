using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class CuRegistrarCliente : UserControl
    {
        public CuRegistrarCliente()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string nombre = txtNombre.Text;
            string apellido = txtApellido.Text;

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellido))
            {
                label3.ForeColor = System.Drawing.Color.Red;
                label3.Text = "Por favor, complete los campos.";
            }
            else
            {
                label3.ForeColor = System.Drawing.Color.Green;
                label3.Text = $" {nombre} {apellido} agregado";

                // Limpiamos los campos
                txtNombre.Clear();
                txtApellido.Clear();
            }
        }
    }
}
