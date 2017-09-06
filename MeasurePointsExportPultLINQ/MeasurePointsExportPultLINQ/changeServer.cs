using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeasurePointsExportPultLINQ
{
    public partial class ChangeServer : Form
    {
        MainForm mainFrom;
        public ChangeServer()
        {
            InitializeComponent();
            tbServer.Select();
           
        }

        private void butOk_Click(object sender, EventArgs e)
        {
            mainFrom = this.Owner as MainForm;
            mainFrom.tbServer.Text = tbServer.Text;
            mainFrom.tbPort.Text = tbPort.Text;
            this.Close();
        }

        private void butCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ChangeServer_Load(object sender, EventArgs e)
        {
            mainFrom = this.Owner as MainForm;
            tbServer.Select();
            tbServer.Text = mainFrom.tbServer.Text;
            tbPort.Text = mainFrom.tbPort.Text;
        }
    }
}
