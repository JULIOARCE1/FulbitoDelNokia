namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            MenuDropUsers.Show(button1, button1.Width, 0);
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void registrarClienteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //label2.Text = "Registrando un cliente";
            CuRegistrarCliente ucRegistro = new CuRegistrarCliente();
            MostrarPantalla(ucRegistro);
        }

        private void MostrarPantalla(UserControl pantallaNueva)
        {
            // Limpia por completo el panel derecho 
            panel1.Controls.Clear();

            // Nueva pantalla para que ocupe todo el espacio
            pantallaNueva.Dock = DockStyle.Fill;

            // Agrega al panel y muestra
            panel1.Controls.Add(pantallaNueva);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Una instancia de la nueva ventana
            Cancha ventanaCanchas = new Cancha();
            // Mostramos la pantalla
            ventanaCanchas.Show();
        }
    }
}
