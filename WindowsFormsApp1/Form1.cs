using System;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        DateTime last = DateTime.Now;

        private void Application_Idle(object sender, System.EventArgs e)
        {
            var time = DateTime.Now;
            if(time.Subtract(last).TotalMilliseconds > 1000)
            {
                label1.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                last = DateTime.Now;
            }
        }

        TSF tsf;
        public Form1(TSF tsf)
        {
            this.tsf = tsf;
            tsf.CreateContext(this.Handle);

            tsf.SetTextExt(0, 0, 0, 0);

            this.Move += Form1_Move;

            InitializeComponent();

            Application.Idle += Application_Idle;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            switch (m.Msg)
            {
                case 0x6:
                    tsf.SetFocus();
                    break;
            }
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
            tsf.SetFocus();
            pushed = !pushed;
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
            tsf.SetEnable(enable);
            tsf.SetFocus();
        }

        private void label1_Click(object sender, System.EventArgs e)
        {
            label1.Text = "123";
        }

        ~Form1()
        {
            Application.Idle -= Application_Idle;
        }
    }
}
