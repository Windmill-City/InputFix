using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        TSF tsf;
        public Form1(TSF tsf)
        {
            this.tsf = tsf;
            tsf.CreateContext(this.Handle);

            tsf.AssociateFocus(this.Handle);

            tsf.SetTextExt(0, 0, 0, 0);

            this.Move += Form1_Move;

            InitializeComponent();
        }

        private void Form1_Move(object sender, System.EventArgs e)
        {
            tsf.SetTextExt(0, 0, 0, 0);
        }
        bool pushed = false;
        private void button1_Click(object sender, System.EventArgs e)
        {
            if (pushed)
            {
                tsf.PopContext();
                button1.Text = "PushContext";
            }
            else
            {
                tsf.PushContext();
                button1.Text = "PopContext";

            }
            pushed = !pushed;
            tsf.SetFocus();
        }
        bool enable = false;
        private void button3_Click(object sender, System.EventArgs e)
        {
            if (enable)
            {
                button3.Text = "EnableInput";
            }
            else
            {
                button3.Text = "DisableInput";
            }
            enable = !enable;
            //tsf.SetEnable(enable);
            tsf.SetFocus();
        }
    }
}
