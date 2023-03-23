using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Cs_Vision
{
    public partial class Form3 : Form
    {
        CsPram csp = CsPram.Instance;
        // CsTools cst = CsTools.Instance;
        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {

        }
        private void Cam_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txtBox = (TextBox)sender;
            Regex regex = new Regex(@"^-?(\d+)?(\.)?(\d+)?$");
            if (e.KeyChar != '\b')
            {
                string strInput = txtBox.Text;
                int SelectLength = txtBox.SelectionLength;//获取选中的字符长度 
                if (SelectLength == txtBox.Text.Length && txtBox.Text.Length != 0)//判断是否全部选中 
                {
                    strInput = e.KeyChar.ToString();
                }
                else
                {
                    if (e.KeyChar == '-')
                    {
                        if (txtBox.SelectionStart != 0)
                        {
                            e.Handled = true;
                        }
                        else
                        {
                            strInput = e.KeyChar.ToString() + strInput;
                        }
                    }
                    else
                    {
                        strInput = strInput + e.KeyChar.ToString();
                    }
                }

                if (!regex.IsMatch(strInput))
                {
                    e.Handled = true;
                }

            }
            if (e.KeyChar == 46)                       //小数点
            {
                if (txtBox.Text.Length <= 0)
                    e.Handled = true;           //小数点不能在第一位
                else
                {
                    float f;
                    float oldf;
                    bool b1 = false, b2 = false;
                    b1 = float.TryParse(txtBox.Text, out oldf);
                    b2 = float.TryParse(txtBox.Text + e.KeyChar.ToString(), out f);
                    if (b2 == false)
                    {
                        if (b1 == true)
                            e.Handled = true;
                        else
                            e.Handled = false;
                    }
                }
            }

        }
        private void Cam_TextChanged(object sender, EventArgs e)
        {
            TextBox txtBox = (TextBox)sender;
            Console.WriteLine(txtBox.Text);
            try
            {
                float val = 0;
                float.TryParse(txtBox.Text, out val);
                if (txtBox.Name.Contains("_x"))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (txtBox.Name.Contains(i.ToString()))
                        {
                            if (txtBox.Name.Contains("cp"))
                            {
                                csp.cam_coodinate_mat.Set<double>(i - 1, 0, val);
                            }
                            else
                            {
                                csp.rot_coodinate_mat.Set<double>(i - 1, 0, val);
                            }
                            break;
                        }
                    }
                }
                if (txtBox.Name.Contains("_y"))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (txtBox.Name.Contains(i.ToString()))
                        {
                            if (txtBox.Name.Contains("cp"))
                            {
                                csp.cam_coodinate_mat.Set<double>(i - 1, 1, val);
                            }
                            else
                            {
                                csp.rot_coodinate_mat.Set<double>(i - 1, 1, val);
                            }
                            break;
                        }
                    }
                }
                for (int i = 0; i < 9; i++)
                {
                    if (txtBox.Name.Contains("cp"))
                    {
                        Console.WriteLine((i + 1) + " CX: " + csp.cam_coodinate_mat.Get<double>(i, 0));
                        Console.WriteLine((i + 1) + " CY: " + csp.cam_coodinate_mat.Get<double>(i, 1));
                    }
                    else
                    {
                        Console.WriteLine((i + 1) + " RX: " + csp.rot_coodinate_mat.Get<double>(i, 0));
                        Console.WriteLine((i + 1) + " RY: " + csp.rot_coodinate_mat.Get<double>(i, 1));
                    }

                }
                Console.WriteLine("====================================================================");
            }
            catch
            {
                MessageBox.Show("error!", "提示");

            }

        }
    }
}
