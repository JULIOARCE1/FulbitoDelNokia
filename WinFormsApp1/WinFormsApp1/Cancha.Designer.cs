namespace WinFormsApp1
{
    partial class Cancha
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            button4 = new Button();
            panel1 = new Panel();
            LabelHola = new Label();
            button5 = new Button();
            button6 = new Button();
            MenuDropTipoCan = new ContextMenuStrip(components);
            futbol5ToolStripMenuItem = new ToolStripMenuItem();
            registrarTipoDeCanchaToolStripMenuItem = new ToolStripMenuItem();
            consultarTipoDeCanchaToolStripMenuItem = new ToolStripMenuItem();
            modificarTipoDeCanchaToolStripMenuItem = new ToolStripMenuItem();
            listarTipoDeCanchaToolStripMenuItem = new ToolStripMenuItem();
            eliminarTipoDeCanchaToolStripMenuItem = new ToolStripMenuItem();
            futbol7ToolStripMenuItem = new ToolStripMenuItem();
            futbol11ToolStripMenuItem = new ToolStripMenuItem();
            button7 = new Button();
            comboBox1 = new ComboBox();
            panel1.SuspendLayout();
            MenuDropTipoCan.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(28, 45);
            button1.Name = "button1";
            button1.Size = new Size(125, 23);
            button1.TabIndex = 0;
            button1.Text = "Registrar Cancha";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new Point(28, 110);
            button2.Name = "button2";
            button2.Size = new Size(125, 23);
            button2.TabIndex = 1;
            button2.Text = "Consultar Cancha";
            button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            button3.Location = new Point(28, 235);
            button3.Name = "button3";
            button3.Size = new Size(125, 23);
            button3.TabIndex = 2;
            button3.Text = "Listar Cancha";
            button3.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            button4.Location = new Point(28, 176);
            button4.Name = "button4";
            button4.Size = new Size(125, 23);
            button4.TabIndex = 3;
            button4.Text = "Modificar Cancha";
            button4.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.AutoScroll = true;
            panel1.Controls.Add(LabelHola);
            panel1.Location = new Point(373, 2);
            panel1.Name = "panel1";
            panel1.Size = new Size(425, 447);
            panel1.TabIndex = 4;
            // 
            // LabelHola
            // 
            LabelHola.AutoSize = true;
            LabelHola.Location = new Point(194, 190);
            LabelHola.Name = "LabelHola";
            LabelHola.Size = new Size(39, 15);
            LabelHola.TabIndex = 0;
            LabelHola.Text = "HOLA";
            // 
            // button5
            // 
            button5.Location = new Point(28, 290);
            button5.Name = "button5";
            button5.Size = new Size(125, 23);
            button5.TabIndex = 5;
            button5.Text = "Dar de baja cancha";
            button5.UseVisualStyleBackColor = true;
            // 
            // button6
            // 
            button6.Location = new Point(28, 349);
            button6.Name = "button6";
            button6.Size = new Size(125, 23);
            button6.TabIndex = 6;
            button6.Text = "Tipo de Cancha";
            button6.UseVisualStyleBackColor = true;
            // 
            // MenuDropTipoCan
            // 
            MenuDropTipoCan.Items.AddRange(new ToolStripItem[] { futbol5ToolStripMenuItem, futbol7ToolStripMenuItem, futbol11ToolStripMenuItem });
            MenuDropTipoCan.Name = "MenuDropTipoCan";
            MenuDropTipoCan.Size = new Size(124, 70);
            // 
            // futbol5ToolStripMenuItem
            // 
            futbol5ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { registrarTipoDeCanchaToolStripMenuItem, consultarTipoDeCanchaToolStripMenuItem, modificarTipoDeCanchaToolStripMenuItem, listarTipoDeCanchaToolStripMenuItem, eliminarTipoDeCanchaToolStripMenuItem });
            futbol5ToolStripMenuItem.Name = "futbol5ToolStripMenuItem";
            futbol5ToolStripMenuItem.Size = new Size(123, 22);
            futbol5ToolStripMenuItem.Text = "Futbol 5";
            // 
            // registrarTipoDeCanchaToolStripMenuItem
            // 
            registrarTipoDeCanchaToolStripMenuItem.Name = "registrarTipoDeCanchaToolStripMenuItem";
            registrarTipoDeCanchaToolStripMenuItem.Size = new Size(206, 22);
            registrarTipoDeCanchaToolStripMenuItem.Text = "Registrar tipo de cancha";
            // 
            // consultarTipoDeCanchaToolStripMenuItem
            // 
            consultarTipoDeCanchaToolStripMenuItem.Name = "consultarTipoDeCanchaToolStripMenuItem";
            consultarTipoDeCanchaToolStripMenuItem.Size = new Size(206, 22);
            consultarTipoDeCanchaToolStripMenuItem.Text = "Consultar tipo de cancha";
            // 
            // modificarTipoDeCanchaToolStripMenuItem
            // 
            modificarTipoDeCanchaToolStripMenuItem.Name = "modificarTipoDeCanchaToolStripMenuItem";
            modificarTipoDeCanchaToolStripMenuItem.Size = new Size(206, 22);
            modificarTipoDeCanchaToolStripMenuItem.Text = "Modificar tipo de cancha";
            // 
            // listarTipoDeCanchaToolStripMenuItem
            // 
            listarTipoDeCanchaToolStripMenuItem.Name = "listarTipoDeCanchaToolStripMenuItem";
            listarTipoDeCanchaToolStripMenuItem.Size = new Size(206, 22);
            listarTipoDeCanchaToolStripMenuItem.Text = "Listar tipo de cancha";
            // 
            // eliminarTipoDeCanchaToolStripMenuItem
            // 
            eliminarTipoDeCanchaToolStripMenuItem.Name = "eliminarTipoDeCanchaToolStripMenuItem";
            eliminarTipoDeCanchaToolStripMenuItem.Size = new Size(206, 22);
            eliminarTipoDeCanchaToolStripMenuItem.Text = "Eliminar tipo de cancha";
            // 
            // futbol7ToolStripMenuItem
            // 
            futbol7ToolStripMenuItem.Name = "futbol7ToolStripMenuItem";
            futbol7ToolStripMenuItem.Size = new Size(123, 22);
            futbol7ToolStripMenuItem.Text = "Futbol 7";
            // 
            // futbol11ToolStripMenuItem
            // 
            futbol11ToolStripMenuItem.Name = "futbol11ToolStripMenuItem";
            futbol11ToolStripMenuItem.Size = new Size(123, 22);
            futbol11ToolStripMenuItem.Text = "Futbol 11";
            // 
            // button7
            // 
            button7.Location = new Point(28, 398);
            button7.Name = "button7";
            button7.Size = new Size(125, 23);
            button7.TabIndex = 8;
            button7.Text = "Volver";
            button7.UseVisualStyleBackColor = true;
            button7.Click += button7_Click;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(177, 46);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(121, 23);
            comboBox1.TabIndex = 9;
            
            // 
            // Cancha
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(comboBox1);
            Controls.Add(button7);
            Controls.Add(button6);
            Controls.Add(button5);
            Controls.Add(panel1);
            Controls.Add(button4);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Name = "Cancha";
            Text = "Cancha";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            MenuDropTipoCan.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private Button button2;
        private Button button3;
        private Button button4;
        private Panel panel1;
        private Button button5;
        private Button button6;
        private ContextMenuStrip MenuDropTipoCan;
        private ToolStripMenuItem futbol5ToolStripMenuItem;
        private ToolStripMenuItem registrarTipoDeCanchaToolStripMenuItem;
        private ToolStripMenuItem consultarTipoDeCanchaToolStripMenuItem;
        private ToolStripMenuItem modificarTipoDeCanchaToolStripMenuItem;
        private ToolStripMenuItem listarTipoDeCanchaToolStripMenuItem;
        private ToolStripMenuItem eliminarTipoDeCanchaToolStripMenuItem;
        private ToolStripMenuItem futbol7ToolStripMenuItem;
        private ToolStripMenuItem futbol11ToolStripMenuItem;
        private Label LabelHola;
        private Button button7;
        private ComboBox comboBox1;
    }
}