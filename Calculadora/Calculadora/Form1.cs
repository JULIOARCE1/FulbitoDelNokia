namespace Calculadora
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void botonCalcular_Click(object sender, EventArgs e)
        {
            double nota1 = double.Parse(this.textBox1.Text);
            double nota2 = double.Parse(this.textBox2.Text);

            double suma = nota1 + nota2;
            label1.Text = "Tu suma es: " + suma.ToString();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
