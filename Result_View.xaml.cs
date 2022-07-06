using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS;


namespace Auto_opt_Structures
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Result_View : UserControl
    {
        public Result_View(VMS.TPS.Protocol_Datas items)
        {
            InitializeComponent();

            List<ToDisplay> result = new List<ToDisplay>();
            foreach (Protocol_Item el in items.lignes_protocole)
            {
                result.Add(new ToDisplay() { Label = el.Structure_label, Formule = el.Structure_operation, Code_error = el.Code_error, Comment = el.Comment });
            }
            lvResults.ItemsSource = result;  //linking list to display results
        }
    }

    public class ToDisplay
    {
        public string Label { get; set; }
        public string Formule { get; set; }
        public string Code_error { get; set; }
        public string Comment { get; set; }


    }
}
