using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cs_Vision
{
    public partial class Form2 : Form
    {
        CsPram csp = CsPram.Instance;
        CsTools cst = CsTools.Instance;
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            trackBar1.Value = csp.thrldVal;
            trackBar2.Value = csp.areasMin;
            trackBar3.Value = csp.areasMax;
            trackBar4.Value = csp.alike60;
            trackBar5.Value = csp.asideVal;

            label_thldval.Text = trackBar1.Value.ToString();
            label_minareaval.Text = trackBar2.Value.ToString();
            label_maxareaval.Text = trackBar3.Value.ToString();
            label_alikeval.Text = trackBar4.Value.ToString();
            label_sideval.Text = trackBar5.Value.ToString();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            csp.thrldVal = trackBar1.Value;
            label_thldval.Text = trackBar1.Value.ToString();
            csp.some_rects[csp.pickregion.index].value = trackBar1.Value;

            for (int i = csp.dict_polys.Count - 1; i >= 0; i--)
            {
                if (csp.pickregion.region.Size == csp.dict_polys[i].rect.Size)
                {
                    csp.dict_polys.RemoveAt(i);
                }
            }
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            csp.areasMin = trackBar2.Value;
            label_minareaval.Text = trackBar2.Value.ToString();
            csp.dict_polys.Clear();
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            csp.areasMax = trackBar3.Value;
            label_maxareaval.Text = trackBar3.Value.ToString();
            csp.dict_polys.Clear();
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            csp.alike60 = trackBar4.Value;
            label_alikeval.Text = trackBar4.Value.ToString();
            csp.dict_polys.Clear();
        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            csp.asideVal = trackBar5.Value;
            label_sideval.Text = trackBar5.Value.ToString();
            csp.dict_polys.Clear();
        }
    }
}
