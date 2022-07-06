using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;



// This line is necessary to "write" in database
[assembly: ESAPIScript(IsWriteable = true)]
[assembly: AssemblyVersion("1.0.0.3")]

namespace VMS.TPS
{
    public class Script
    {
        /*
         * Main class
         */
        public Script()   //Constructor
        { }

        public void Execute(ScriptContext context )
        {
            bool _DebugMode = true;  //Modify this to debug mode

            var directorypath = @"\\srv015\radiotherapie\SCRIPTS_ECLIPSE\Opt_Structures\";
            //var directorypath = @"\\srv015\SF_COM\LACAZE T\Opt_Structures\";

            Patient mypatient = context.Patient;
            mypatient.BeginModifications();   //Mandatory to write in DataBase
            
            //   Check if a patient and a structure set is loaded
            if (context.Patient == null || context.StructureSet == null)
            {
                MessageBox.Show("Please load a patient, 3D image, and structure set before running this script.", "Opt_strucutres", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            //Let the user choose csv file using a browser
            string file;

            if (!System.IO.Directory.Exists(directorypath))
            {
                MessageBox.Show(String.Format("The default template file directory {0} defined by the script does not exist", directorypath));
                return;
            }
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.DefaultExt = "csv";
            fileDialog.InitialDirectory = directorypath;
            fileDialog.Multiselect = false;
            fileDialog.Title = "Selection du template a appliquer";
            fileDialog.ShowReadOnly = true;
            fileDialog.Filter = "CSV files (*.csv)|*.csv";
            fileDialog.FilterIndex = 0;
            fileDialog.CheckFileExists = true;
            if (fileDialog.ShowDialog() == false)
            {
                return;    // user canceled
            }
            file = fileDialog.FileName;

            if (!System.IO.File.Exists(file))
            {
                MessageBox.Show(string.Format("The template file '{0}' chosen does not exist.", file));
                return;
            }         
            
            //Reading protocol instructions and detect each bloc 
            Protocol_Datas protocol_structures = new Protocol_Datas(file, context.StructureSet);

            //Then create all user defined Structures with mutliple checks before (e.g. check original structures...)
            foreach (Protocol_Item el in protocol_structures.lignes_protocole)              
            {
                if (_DebugMode == true)
                {
                    MessageBox.Show(string.Format("Operation en cours de traitement = {0}", el.Structure_label));
                }

                Structure_Creator new_structure = new Structure_Creator(el, context.StructureSet);
            }
            
            
            foreach (Structure s in context.StructureSet.Structures.ToList())
            {
                //Cleaning structure set from temporary structures hard coded and user's structures deleting choice + empty structures
                if (s.Id.Contains("temp") || s.Id.Contains("&") || (s.IsEmpty && s.ApprovalHistory.FirstOrDefault().ApprovalStatus.ToString() == "UnApproved"))
                {         
                    if (s.Id.Contains("temp") || s.Id.Contains("&"))
                    {
                        context.StructureSet.RemoveStructure(s);
                    }
                    if (s.IsEmpty && s.ApprovalHistory.FirstOrDefault().ApprovalStatus.ToString() == "UnApproved")
                    {
                        if (protocol_structures.lignes_protocole.Any(o => (o.Structure_label == s.Id) && o.To_keep == true ))
                        {
                            var result = protocol_structures.lignes_protocole.FirstOrDefault(o => (o.Structure_label == s.Id) && o.To_keep == true);
                            result.Code_error = " Structure conservee selon demande utilisateur";
                        }
                        else
                        {
                                context.StructureSet.RemoveStructure(s);                            

                        }
                    }  
                }

                
            }
            foreach (Protocol_Item el in protocol_structures.lignes_protocole)
            {
                //Removing all structures if it's users choice
                if (el.To_delete == true )
                {
                    Structure tmpStruc = context.StructureSet.Structures.ToList().FirstOrDefault(s => s.Id == el.Structure_label);

                    context.StructureSet.RemoveStructure(tmpStruc); 
                    el.Comment = " Structure supprimee selon demande utilisateur";
                }
            }                        

            //Show Results in a new window
            Window window2 = new System.Windows.Window();
            var Result_View_window = new Auto_opt_Structures.Result_View(protocol_structures);
            window2.Title = "Resultats creation structures template : ";
            window2.Content = Result_View_window;
            window2.Width = 1550;
            window2.Height = 620;
            window2.Topmost = true;
            window2.ShowDialog();
        }
    }
}
