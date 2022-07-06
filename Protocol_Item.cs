using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Linq;


//namespace Auto_opt_Structures
namespace VMS.TPS
{
    public class Protocol_Item
    /*Class which represents each items of a whole protocol. each item is represented by :
    *- label
    *- operations 
    *- original structures
    *- user's choice (convert to HR, delete structure, crop to skin...)
    *- declaration of code error if it exists to view the result in a window at the end of the code 
    *- .....
    In this class .csv files are "translated" and values are filled in variables
     */
    {
        #region DECLARATIONS
        //Datas from txt file
        string structure_label = null;
        string structure_operation = null;
        bool high_resolution = false;
        //string type = null;
        bool to_delete = false;
        bool to_keep = false;
        string sub_body = null;         

        //Datas computed from regex
        // Structure original_struc_A = null;
        Structure original_struc_A;
        Structure original_struc_B;
        string original_struc_A_label = null;
        string original_struc_B_label = null;
        string operator_A_B = null;
        string suboperator_A = null;
        string suboperator_B = null;
        int number_structures = 0;
        string code_error = null;
        string comment = null;
        double margin_A = 0.0;
        double margin_B = 0.0;
        AxisAlignedMargins asym_margin_A; 

        #endregion

        public Protocol_Item(string[] fields, StructureSet ss)   //Constructor
        {
            //StructureSet sset = ss; 
            //Datas in text file should be like :
            //Structure Label; Structure Operation; High Resolution? ; Crop body? ; Delete structure ?

            #region COLLECTING FIELDS FROM TEXT FILE

            if (string.IsNullOrEmpty(fields[5]) == true)
            {
                to_keep = false;
                structure_label = fields[0];  //TESTER ICI QUE LE LIBELLE NE DOIT PAS DEPASSER XX CARACTERES !
                structure_operation = fields[1];
                Separate_Items(fields[1], ss);  //CALLING METHOD to regex blocs A, bloc B and operator to apply  

                if (string.IsNullOrEmpty(fields[2]) != true)
                {   high_resolution = true;
                }
                if (string.IsNullOrEmpty(fields[3]) == true)
                {   sub_body = null;
                }
                else
                { sub_body = fields[3];
                }
                if (string.IsNullOrEmpty(fields[4]) == true)
                { to_delete = false;
                }
                else
                { to_delete = true;
                }
            }
            else
            {
                to_keep = true;  // Case if user wants to keep the structure at the end of the code and do no operation at the s
                structure_label = fields[0];
            }

            #endregion
        }
        #region Gets/sets
        //Get and Set to keep all class variables encapsulated***********************************************************************************************
        public string Structure_label
        {
            get { return structure_label; }
        }
        public string Structure_operation
        {
            get { return structure_operation; }
        }
        public bool High_resolution
        {
            get { return high_resolution; }
        }
        public bool To_delete
        {
            get { return to_delete; }
        }
        public bool To_keep
        {
            get { return to_keep; }
        }
        public string Sub_body
        {
            get { return sub_body; }
        }
        public Structure Original_struc_A
        {
            get { return original_struc_A; }
            set { original_struc_A = value; }
        }
        public string Original_struc_A_label
        {
            get { return original_struc_A_label; }
        }
        public Structure Original_struc_B
        {
            get { return original_struc_B; }
            set { original_struc_B = value; }
        }
        public string Original_struc_B_label
        {
            get { return original_struc_B_label; }
        }
        public string Operator_A_B
        {
            get { return operator_A_B; }
        }
        public string Suboperator_A
        {
            get { return suboperator_A; }
        }
        public string Suboperator_B
        {
            get { return suboperator_B; }
        }
        public int Number_structures
        {
            get { return number_structures; }
        }
        public double Margin_A
        {
            get { return margin_A; }
        }
        public double Margin_B
        {
            get { return margin_B; }
        }
        public string Code_error
        {
            get { return code_error; }
            set { code_error = value; }
        }
        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }
        public AxisAlignedMargins Asym_margin_A
        {
            get { return asym_margin_A; }
        }


        
        #endregion


        #region REGEX TO 'TRANSLATE' TEXT FILE AND  GET EACH ELEMENTS FROM IT  

        void Separate_Items(string item, StructureSet ss)
        {

            try
            {
                // List of regular expressions in order to translate .csv files and put each detected values in variables

                string two_blocs_pattern = @"^\((?<strucA>[^\)]+)\)(?<operator>(?i)[I|+|-](?-i))\((?<strucB>[^\)]+)\)";
                string one_bloc_pattern_operator = @"^(?<strucA>[^(\%|\#|\-]+)(?<operator>(\%|\#|\-))(?<value>[0-9]+\-[0-9]+|[0-9]+)";
                string one_bloc_pattern_asymetric = @"^(?<strucA>[^(\%|\#|\-|\*]+)(?<operator>(\*))\[(?<value>[0-9]+\,[0-9]+\,[0-9]+\,[0-9]+\,[0-9]+\,[0-9]+)\]";
                string one_bloc_pattern = @"^(?<strucA>[^(\%|\#|\-|\*]+)";

                var match_two_blocs = Regex.Match(item, two_blocs_pattern);
                var match_one_bloc_operator = Regex.Match(item, one_bloc_pattern_operator);
                var match_one_bloc_asymetric = Regex.Match(item, one_bloc_pattern_asymetric);
                var match_one_bloc = Regex.Match(item, one_bloc_pattern);

                if (match_two_blocs.Success == false && match_one_bloc.Success == false && match_one_bloc_operator.Success == false && match_one_bloc_asymetric.Success == false)
                {
                    code_error = "Expression " + item + " non reconnue";
                }
                else
                {
                    #region MATCH TWO BLOCS
                    if (match_two_blocs.Success)  //Two blocs detected with each time parenthesis : e.g. (Nerfopt+3) + (VO+5)
                    {
                        //First get both blocs A and B
                        original_struc_A_label = match_two_blocs.Groups["strucA"].Value;
                        operator_A_B = match_two_blocs.Groups["operator"].Value;
                        original_struc_B_label = match_two_blocs.Groups["strucB"].Value;

                        //Then Check if structure already exist : e.g. canal+10 exists -> use it. If not check if canal exist and process +10 directly
                        Structure check_structureA = (from s in ss.Structures
                                                      where s.Id.ToUpper().CompareTo(original_struc_A_label.ToUpper()) == 0
                                                      select s).FirstOrDefault();
                        if (check_structureA == null)
                        {
                            var one_bloc_operator = Regex.Match(original_struc_A_label, one_bloc_pattern_operator);
                            if (one_bloc_operator.Success)
                            {
                                original_struc_A_label = one_bloc_operator.Groups["strucA"].Value;
                                suboperator_A = one_bloc_operator.Groups["operator"].Value;
                                margin_A = Double.Parse(one_bloc_operator.Groups["value"].Value);
                            }
                        }

                        Structure check_structureB = (from s in ss.Structures
                                                      where s.Id.ToUpper().CompareTo(original_struc_B_label.ToUpper()) == 0
                                                      select s).FirstOrDefault();
                        if (check_structureB == null)
                        {
                            var one_bloc_operator = Regex.Match(original_struc_B_label, one_bloc_pattern_operator);
                            if (one_bloc_operator.Success)
                            {
                                original_struc_B_label = one_bloc_operator.Groups["strucA"].Value;
                                suboperator_B = one_bloc_operator.Groups["operator"].Value;
                                margin_B = Double.Parse(one_bloc_operator.Groups["value"].Value);
                            }
                        }

                        number_structures = 2;
                        // MessageBox.Show(string.Format("struct A {0}, operateur {1} et struct B {2}", original_struc_A_label, operator_A_B, original_struc_B_label));
                        #endregion
                    }
                    else    //Cases of one bloc detected corresponding to one structure with operator : +, - or #
                    {
                        #region MATCH ONE SINGLE BLOC

                        if (match_one_bloc_operator.Success)
                        {
                            original_struc_A_label = match_one_bloc_operator.Groups["strucA"].Value;
                            suboperator_A = match_one_bloc_operator.Groups["operator"].Value;
                            //  MessageBox.Show(string.Format("detection de struct A {0}, operateur {1} et value {2}", original_struc_A_label, suboperator_A, match_one_bloc_operator[3].Groups["value"].Value));

                            if (suboperator_A == "%" || suboperator_A == "#")
                            {
                                number_structures = 1;

                                if (suboperator_A == "%")
                                {
                                    try
                                    {
                                        margin_A = Double.Parse(match_one_bloc_operator.Groups["value"].Value);
                                    }
                                    catch
                                    {
                                        code_error = "Consersion de la marge " + match_one_bloc_operator.Groups["value"].Value + " impossible à réaliser";
                                    }

                                }
                                if (suboperator_A == "#")
                                {
                                    string val = match_one_bloc_operator.Groups["value"].Value;
                                    string one_bloc_pattern_ring = @"^(?<value1>[0-9]+)\-(?<value2>[0-9]+)";
                                    var match_two_values = Regex.Match(val, one_bloc_pattern_ring);
                                    if (match_two_values.Success)
                                    {
                                        try
                                        {
                                            margin_A = Double.Parse(match_two_values.Groups["value1"].Value);
                                        }
                                        catch
                                        {
                                            code_error = "Consersion de la marge " + match_two_values.Groups["value1"].Value + " impossible à réaliser";
                                        }
                                        try
                                        {
                                            margin_B = Double.Parse(match_two_values.Groups["value2"].Value);
                                        }
                                        catch
                                        {
                                            code_error = "Consersion de la marge " + match_two_values.Groups["value2"].Value + " impossible à réaliser";
                                        }

                                    }
                                }
                            }
                            else
                            {
                                code_error = "Expression " + item + " non reconnue";
                            }
                        }
                        else
                        {
                            if (match_one_bloc_asymetric.Success) //One bloc detected with assymetric margin to add : e.g. PTV+[5;5;7;5;5;7]
                            {
                                original_struc_A_label = match_one_bloc_asymetric.Groups["strucA"].Value;
                                suboperator_A = match_one_bloc_asymetric.Groups["operator"].Value;
                                string val = match_one_bloc_asymetric.Groups["value"].Value;
                                string one_bloc_pattern_assymetric_margin = @"^(?<x1>[0-9]+)\,(?<y1>[0-9]+)\,(?<z1>[0-9]+)\,(?<x2>[0-9]+)\,(?<y2>[0-9]+)\,(?<z2>[0-9]+)";
                                var match_six_values = Regex.Match(val, one_bloc_pattern_assymetric_margin);
                                if (match_six_values.Success)
                                {
                                    this.asym_margin_A = new AxisAlignedMargins(StructureMarginGeometry.Outer, Double.Parse(match_six_values.Groups["x1"].Value),
                                                                                                                    Double.Parse(match_six_values.Groups["y1"].Value),
                                                                                                                    Double.Parse(match_six_values.Groups["z1"].Value),
                                                                                                                    Double.Parse(match_six_values.Groups["x2"].Value),
                                                                                                                    Double.Parse(match_six_values.Groups["y2"].Value),
                                                                                                                    Double.Parse(match_six_values.Groups["z2"].Value));
                                }
                                number_structures = 1;
                            }
                            else
                            {
                                if (match_one_bloc.Success)  //One bloc detected with no parenthesis and no margin to add : e.g. Nerfopt => simple copy (margin 0)
                                {

                                    code_error = "Expression " + item + " non reconnue, il manque un operateur est des valeurs";
                                }
                                else
                                {
                                    code_error = "Expression " + item + " non reconnue";
                                }
                            }

                        }
                        #endregion
                    }
                }
            }
            catch
            {
                code_error = "ERREUR*** : Problème lors du processus de traduction de la saisie utilisateur dans .csv (regular expressions)";
            }

        }
        #endregion
    }
}
