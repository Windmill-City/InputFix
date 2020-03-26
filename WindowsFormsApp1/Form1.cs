using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        TSF tsf;
        public Form1()
        {
            InitializeComponent();

            this.tsf = new TSF();
            tsf.AssociateFocus(this.Handle);
            tsf.CreateContext(this.Handle);
            tsf.PushContext();
            tsf.SetEnable(true);
            tsf.SetTextExt(0, 0, 0, 0);

            this.Move += Form1_Move;
            listView1.Enabled = false;//Do not let the control get the focus
            this.listView1.View = View.Details;
            this.listView1.GridLines = true;
            this.listView1.Columns.Add("Text", 100, HorizontalAlignment.Left);
            this.listView1.Columns.Add("AcpStart", 100, HorizontalAlignment.Left);
            this.listView1.Columns.Add("AcpEnd", 100, HorizontalAlignment.Left);
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
                    var text = Marshal.PtrToStringAuto(m.LParam);
                    item.Text = text;
                    listView1.Items.Add(item);
                    this.listView1.EnsureVisible(this.listView1.Items.Count - 1);
                    break;
                case EM_SETSEL:
                    item.SubItems.Add("" + m.WParam);
                    item.SubItems.Add("" + m.LParam);
                    listView1.Items.Add(item);
                    this.listView1.EnsureVisible(this.listView1.Items.Count - 1);
                    break;
            }
        }

        private void Form1_Move(object sender, System.EventArgs e)
        {
            tsf.SetTextExt(0, 0, 0, 0);
        }
    }
}
