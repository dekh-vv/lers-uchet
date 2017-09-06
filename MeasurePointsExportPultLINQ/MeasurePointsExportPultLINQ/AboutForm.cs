using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeasurePointsExportPultLINQ
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            lblVersion.Text = String.Format("версия: {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

        private void butOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
