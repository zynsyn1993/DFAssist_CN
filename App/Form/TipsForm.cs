using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace App
{
    public partial class TipsForm : Form
    {
        private string Roullete;
        private string Instance;
        private string Tip;
        private string Macro;

        public TipsForm(string Roullete, string Instance, string Tip, string Macro = null)
        {
            this.Roullete = Roullete;
            this.Instance = Instance;
            this.Tip = Tip;
            this.Macro = Macro;
            InitializeComponent();
        }

        private void TipsForm_Load(object sender, EventArgs e)
        {
            textBox1.Text = Tip;
            if (Macro == null)
            {
                panel_macro.Hide();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Clipboard.SetDataObject(Macro, true);
        }

        private void TipsForm_Shown(object sender, EventArgs e)
        {
            TopMost = false;
        }
    }
}
