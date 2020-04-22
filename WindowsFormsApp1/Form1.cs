using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private const int EM_REPLACESEL = 0x00C2;
        private const int EM_SETSEL = 0x00B1;
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            ListViewItem item = new ListViewItem();
            switch (m.Msg)
            {
                case EM_REPLACESEL:
                    break;
                case EM_SETSEL:
                    break;
            }
        }

        private void Form1_Move(object sender, System.EventArgs e)
        {

        }
    }
}
