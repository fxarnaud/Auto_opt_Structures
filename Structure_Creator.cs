using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace VMS.TPS
{
    class Structure_Creator
    {
        /* Class which :
         *- First check if original structures to use exists in structure set. If not display an error message but it doesn't block the process. 
         *- Copy structures into temporaries structures because it can be approved and we need to manipulate then.
         *- Check structure to create if they exists or if we need to create it.
         *- Convert to HR if user wants or if needed (e.g. if addition of 2 structures and only one is in HR)
         *- Compute structure (add, substract, intersect, mmargins....)
         *- Substract XX mm to skin of the newly created structure if user want it
         *- Delete temporaries structures used
          */

        bool flag_HR = false;

        public Structure_Creator(Protocol_Item struct_details, StructureSet ss)  //Constructor
        {
            Create(struct_details, ss);
        }

        void Create(Protocol_Item struct_details, StructureSet ss) //MAIN FUNCTION: OPERATOR'S CHOICE
        {
            bool flag_error = true;

            // MessageBox.Show(string.Format("Je cree {0} avec {1} ", struct_details.Structure_operation, struct_details.Original_struc_A_label));

            //First check if ORIGINAL structures exists
            flag_error = CreateTempoStructureAndCheckOriginal(ss, struct_details);          


            if (flag_error)
            { //Then check if structure TO CREATE already exists
                flag_error = CheckStructureToCreate(ss, struct_details);
            }


            if (flag_error)  //No error witch first checks -> let's create a new structure and apply operations on it
            {

                //Main Structure to Add 
                Structure created_structure = (from s in ss.Structures
                                               where s.Id.ToUpper().CompareTo(struct_details.Structure_label.ToUpper()) == 0
                                               select s).FirstOrDefault();    //ToUpper = convertion en majuscule. Je passe tout en majuscule pour ne pas avoir à tenir compte de la casse

                if (created_structure == null)
                {
                    created_structure = ss.AddStructure("Avoidance", struct_details.Structure_label);

                }

                HR_Conversion(created_structure, struct_details);  // Convert newly created structure and original structures to HR if it's users choice or mandatory
                
                switch (struct_details.Number_structures)                   // Then compute structure operations on the new structure
                {
                    #region OPERATIONS ON ONE SINGLE STRUCTURE 

                    case 1:
                        if (struct_details.Suboperator_A == "%")    //Adding a simple margin in one structure
                        {
                            if (struct_details.Original_struc_A != null)
                            {
                                AddMargin(ss, created_structure, struct_details.Original_struc_A, struct_details, struct_details.Margin_A);

                                if (struct_details.Margin_A == 0)
                                {
                                    //struct_details.Code_error = "Simple copie de la structure " + struct_details.Original_struc_A_label + " réalisée (marge demandée = 0)";
                                    struct_details.Code_error = "-";
                                }
                                else
                                {
                                    //struct_details.Code_error = "Marge de " + struct_details.Margin_A + " mm autour de " + struct_details.Original_struc_A_label;
                                    struct_details.Code_error = "-";
                                }
                            }
                            else
                            {
                                created_structure = null;
                                struct_details.Code_error = "ERREUR *** Structure bloc A est null lors de l'addition ";
                            }
                        }
                        else
                        {
                            if (struct_details.Suboperator_A == "#")   //Adding a ring to one structure
                            {
                                if (struct_details.Original_struc_A != null)
                                {
                                    Structure tmpA = ss.AddStructure("Avoidance", "tmpA");
                                    Structure tmpB = ss.AddStructure("Avoidance", "tmpB");
                                    if (flag_HR == true)  // if need to convert to HR do it. From flag_HR from method HR_UserChoice
                                    {
                                        tmpA.ConvertToHighResolution();
                                        tmpB.ConvertToHighResolution();
                                    }
                                    AddMargin(ss, tmpA, struct_details.Original_struc_A, struct_details, struct_details.Margin_A);
                                    AddMargin(ss, tmpB, struct_details.Original_struc_A, struct_details, struct_details.Margin_B);
                                    SubstractStructures(ss, tmpA, tmpB, created_structure, struct_details);
                                    ss.RemoveStructure(tmpA);
                                    ss.RemoveStructure(tmpB);
                                   // struct_details.Code_error = "Ring allant de " + struct_details.Margin_B + " à " + struct_details.Margin_A + " mm autour de " + struct_details.Original_struc_A_label;
                                    struct_details.Code_error = "-";
                                }
                            }
                            else
                            {
                                if (struct_details.Suboperator_A == "*")   //Adding an assymetric margin
                                {
                                    AddAssymetricMargin(ss, created_structure, struct_details.Original_struc_A, struct_details, struct_details.Asym_margin_A);
                                   // struct_details.Code_error = "Marge autour de " + struct_details.Original_struc_A_label + " realisée";
                                    struct_details.Code_error = "-";
                                }
                                else                                            //Suboperator doesn't match anything
                                {
                                        created_structure = null;
                                        struct_details.Code_error = "ERREUR*** operateur " + struct_details.Suboperator_A + " non reconnu";                                 

                                }

                            }
                            //MessageBox.Show(string.Format("{0} Operation sur une seule structure realisee", struct_details.Structure_label));

                        }

                        break;
                    #endregion


                    #region OPERATIONS BETWEEN TWO STRUCTURES TO PROCESS
                    case 2:

                        if ((struct_details.Operator_A_B != "+") && (struct_details.Operator_A_B != "-") && (struct_details.Operator_A_B != "I" && struct_details.Operator_A_B != "i"))
                        {
                            struct_details.Code_error = "ERREUR*** : Operateur (" + struct_details.Operator_A_B + ") sur 2 structures non reconnu";
                        }
                        else
                        {
                            //Main Part to add two structures (I, AND or -)
                            Structure tmpA;
                            tmpA = ss.AddStructure(struct_details.Original_struc_A.DicomType, "tmpA");
                            
                            Structure tmpB = ss.AddStructure("Avoidance", "tmpB");
                            if (flag_HR == true)  // if need to convert to HR do it. From flag_HR from method HR_UserChoice
                            {
                                tmpA.ConvertToHighResolution();
                                tmpB.ConvertToHighResolution();
                            }

                            if (struct_details.Operator_A_B == "+")
                            //Addition between two structures
                            {
                                if (struct_details.Original_struc_A.IsEmpty != true)
                                {
                                    //simple copy of structure if margin = 0
                                    tmpA.SegmentVolume = struct_details.Original_struc_A.Margin(struct_details.Margin_A);
                                }
                                if (struct_details.Original_struc_B.IsEmpty != true)
                                {
                                    tmpB.SegmentVolume = struct_details.Original_struc_B.Margin(struct_details.Margin_B);
                                }

                                AddStructures(ss, tmpA, tmpB, created_structure, struct_details);
                                //struct_details.Code_error = struct_details.Original_struc_A_label + " et " + struct_details.Original_struc_B_label + " additionnés ";
                                struct_details.Code_error = "-";
                            }

                            if (struct_details.Operator_A_B == "-")
                            //Substraction between two structures
                            {
                                tmpA.SegmentVolume = struct_details.Original_struc_A.Margin(struct_details.Margin_A);
                                //simple copy of structure if margin = 0
                                if (struct_details.Original_struc_B.IsEmpty != true)
                                {
                                    tmpB.SegmentVolume = struct_details.Original_struc_B.Margin(struct_details.Margin_B);
                                }

                                SubstractStructures(ss, tmpA, tmpB, created_structure, struct_details);

                                //***************************************************************************************
                               /* if (struct_details.Structure_label == "PTV_HDOPT1")
                                {
                                    MessageBox.Show(string.Format("structure tmpA {0} de type {1}", tmpA.Id, tmpA.DicomType));
                                }*/
                                //*********************************************************************************


                                if (tmpA.DicomType == "PTV" || tmpA.DicomType == "GTV" || struct_details.Structure_label == "RingPTVParoi" || struct_details.Structure_label == "RingPTVSein")
                                {
                                   // struct_details.Code_error = "Structures soustraites ";
                                    struct_details.Code_error = "-";
                                    struct_details.Comment = "Bloc A de type PTV/GTV ou label structure = RingPTVParoi ou RingPTVSein";
                                }
                                else
                                {
                                    double deltaV = 0.3;   //volume in cc
                                                           //two volumes from different structures won't be exactly the same. deltaV is the delta Volume to check if volume are nearly equivalent and thus consider that it's the same structure
                                    if (created_structure.Volume >= tmpA.Volume - deltaV && created_structure.Volume <= tmpA.Volume + deltaV)
                                    {   //Check if it's necessary to create new structure or not  e.g. If V(Coeur-PTV) = V(coeur)-> No need to create it, same structure as coeur
                                        //Two exceptions :  - if structure Bloc A is a type PTV
                                        //                  - if structure Bloc A is "ringPTVsein" or "ringPTV paroi" for breast template for tomotherapy

                                        struct_details.Code_error = "Warning : Structure non créée car volume structure a créer " + struct_details.Structure_label + " = volume de la structure " + struct_details.Original_struc_A_label;
                                        ss.RemoveStructure(created_structure);
                                    }
                                    else
                                    {
                                      //  struct_details.Code_error = struct_details.Original_struc_A_label + " et " + struct_details.Original_struc_B_label + " soustraits ";
                                        struct_details.Code_error = "-";
                                    }
                                }
                            }

                            if (struct_details.Operator_A_B == "I" || struct_details.Operator_A_B == "i")
                            //Union between two structures
                            {
                                tmpA.SegmentVolume = struct_details.Original_struc_A.Margin(struct_details.Margin_A); //simple copy of structure if margin = 0
                                tmpB.SegmentVolume = struct_details.Original_struc_B.Margin(struct_details.Margin_B); //simple copy of structure if margin = 0
                                IntersectionStructures(ss, tmpA, tmpB, created_structure, struct_details);
                               // struct_details.Code_error = "Intersection de " + struct_details.Original_struc_A_label + " et " + struct_details.Original_struc_B_label;
                                struct_details.Code_error = "-";
                            }

                            ss.RemoveStructure(tmpA); //remove temporary structures used for creation part
                            ss.RemoveStructure(tmpB);
                        }

                        break;
                    default:
                        break;

                        #endregion
                }


                if (struct_details.Sub_body != null)  //Crop skin
                                                      //If is not null -> Means that user wants to substract a margin. need to get this value (and it should be a negative value)
                {
                    SubBody(ss, created_structure, struct_details);
                }
            }

            //Deleting temp structures at the end of all structure creation. 
            //DO NOT REMOVE THIS !
            foreach (Structure s in ss.Structures.ToList())
            {
                //if (s.Id.Contains("temp") || s.Name.Contains("temp"))
                if (s.Id.Contains("&") || s.Name.Contains("&"))
                {
                    ss.RemoveStructure(s);
                }
            }
        }

        #region METHODS FOR STRUCTURE MODIFICATIONS

        void AddMargin(StructureSet ss, Structure created_structure, Structure original, Protocol_Item struct_details, double margin)
        //Adding a margin (positive or negative) to a structure
        {
            try
            {
                created_structure.SegmentVolume = original.Margin(margin);
            }
            catch
            {
                struct_details.Code_error = "ERREUR*** lors de la creation d'une marge a la structure";
            }
        }

        void AddAssymetricMargin(StructureSet ss, Structure created_structure, Structure original, Protocol_Item struct_details, Common.Model.Types.AxisAlignedMargins margin)
        {
            try
            {
                created_structure.SegmentVolume = original.AsymmetricMargin(margin);
            }
            catch
            {
                struct_details.Code_error = "ERREUR*** lors de la creation d'une marge a la structure";
            }
        }


        void AddStructures(StructureSet ss, Structure originalA, Structure originalB, Structure created_structure, Protocol_Item struct_details)
        //Adding two structures
        {
            try
            {
                created_structure.SegmentVolume = originalA.Or(originalB);
            }
            catch
            {
                struct_details.Code_error = "ERREUR*** lors de l'addition des structures";
            }
        }


        void SubstractStructures(StructureSet ss, Structure originalA, Structure originalB, Structure created_structure, Protocol_Item struct_details)
        {
            try
            {
                created_structure.SegmentVolume = originalA.Sub(originalB);
            }
            catch
            {
                struct_details.Code_error = "ERREUR*** lors de la soustraction des structures";
            }

        }


        void IntersectionStructures(StructureSet ss, Structure originalA, Structure originalB, Structure created_structure, Protocol_Item struct_details)
        {
            try
            {
                created_structure.SegmentVolume = originalA.And(originalB);
            }
            catch
            {
                struct_details.Code_error = "ERREUR*** lors de l'intersection des structures";
            }

        }
        #endregion

        #region HR_Conversion

        void HR_Conversion(Structure created_structure, Protocol_Item struct_details)
        {
            if (struct_details.High_resolution == true && created_structure != null)  //User want to create a high resolution structure
            {
                try
                {
                    this.flag_HR = true;
                    created_structure.ConvertToHighResolution();  //First set newly empty structure in HR
                    struct_details.Comment = struct_details.Comment + " " + struct_details.Structure_label + " en HR.";

                    //Then convert original structures if they are not yet in HR
                    if (struct_details.Original_struc_A.IsHighResolution == false)
                    {
                        struct_details.Original_struc_A.ConvertToHighResolution();
                    }
                    if (struct_details.Number_structures == 2)
                    {
                        if (struct_details.Original_struc_B.IsHighResolution == false)
                        {
                            struct_details.Original_struc_B.ConvertToHighResolution();
                        }
                    }
                }
                catch
                {
                    struct_details.Code_error = "ERREUR*** lors de la transfo HR ";
                }
            }
            else    //User doesn't want to create a high resolution structure but we have to test if original structures are in HR or not 
                    //If they are in HR -> need to convert other structures in HR too
            {
                try
                {
                    if (struct_details.Number_structures == 1)
                    {
                        if (struct_details.Original_struc_A.IsHighResolution == true)
                        {
                            this.flag_HR = true;
                            created_structure.ConvertToHighResolution();  //First set newly empty structure in HR
                            struct_details.Comment = struct_details.Comment + " " + struct_details.Structure_label + " créée en HR car structure originale bloc A en HR.";
                        }
                    }
                    if (struct_details.Number_structures == 2)
                    {
                        if (struct_details.Original_struc_A.IsHighResolution == true && struct_details.Original_struc_B.IsHighResolution == false)
                        {
                            this.flag_HR = true;
                            created_structure.ConvertToHighResolution();
                            struct_details.Comment = struct_details.Comment + " " + struct_details.Structure_label + " créée en HR car structure originale bloc A en HR.";
                            struct_details.Original_struc_B.ConvertToHighResolution();
                            struct_details.Comment = struct_details.Comment + " (" + struct_details.Original_struc_B_label + " aussi passée en HR)";
                        }
                        if (struct_details.Original_struc_A.IsHighResolution == false && struct_details.Original_struc_B.IsHighResolution == true)
                        {
                            this.flag_HR = true;
                            created_structure.ConvertToHighResolution();
                            struct_details.Comment = struct_details.Comment + " " + struct_details.Structure_label + " en HR car structure originale en HR.";
                            struct_details.Original_struc_A.ConvertToHighResolution();
                            struct_details.Comment = struct_details.Comment + " (" + struct_details.Original_struc_A_label + " aussi passée en HR)";
                        }
                        if (struct_details.Original_struc_A.IsHighResolution == true && struct_details.Original_struc_B.IsHighResolution == true)
                        {
                            this.flag_HR = true;
                            created_structure.ConvertToHighResolution();
                            struct_details.Comment = struct_details.Comment + " " + struct_details.Structure_label + " en HR car structure originale en HR.";
                            struct_details.Original_struc_A.ConvertToHighResolution();
                            struct_details.Comment = struct_details.Comment + " (" + struct_details.Original_struc_A_label + " aussi passée en HR)";
                            struct_details.Original_struc_B.ConvertToHighResolution();
                            struct_details.Comment = struct_details.Comment + " (" + struct_details.Original_struc_B_label + " aussi passée en HR)";
                        }
                    }
                }
                catch
                {
                    struct_details.Code_error = "ERREUR*** lors de la transfo HR : transfo en HR non dmeandée par l'utilisateur mais structures originales en HR";
                }


            }

        }
        #endregion


        #region SUBBODY XX mm

        void SubBody(StructureSet ss, Structure created_structure, Protocol_Item struct_details)
        //  Cropping skin - XX mm to the structure
        {
            double margin = 0.0;

            bool isNumeric = double.TryParse(struct_details.Sub_body, out _);
            if (isNumeric)
            {
                margin = Double.Parse(struct_details.Sub_body);
            }
            else
            {
                struct_details.Code_error = "ERREUR*** La marge indiquée pour rogner la structure doit être un chiffre";
                struct_details.Comment = "Structure non creee";
                ss.RemoveStructure(created_structure);
                return;
            }




            if (margin > 0)
            {
                struct_details.Code_error = "ERREUR*** La marge indiquée pour rogner la structure doit être < 0";
                struct_details.Comment = "Structure non créée";
                ss.RemoveStructure(created_structure);
            }
            else
            {
                try
                {
                    Structure body = (from s in ss.Structures
                                      where s.DicomType == "EXTERNAL"
                                      select s).FirstOrDefault();

                    Structure tmpbody = ss.AddStructure("Avoidance", "tmpbody");

                    tmpbody.SegmentVolume = body.Margin(margin);

                    if (created_structure.IsHighResolution)
                    {
                        tmpbody.ConvertToHighResolution();
                    }

                    created_structure.SegmentVolume = created_structure.And(tmpbody);
                    ss.RemoveStructure(tmpbody);
                    struct_details.Comment = struct_details.Comment + " Structure rognée de " + margin.ToString() + " mm à la peau.";

                }
                catch
                {
                    struct_details.Code_error = "Erreur lors du rognage a la peau de la structure";
                    struct_details.Comment = "Structure non créée";
                    ss.RemoveStructure(created_structure);
                }
            }


        }
        #endregion


        #region CREATE TEMPORARIES STRUCTURES AND CHECK ORIGINAL 

        bool CreateTempoStructureAndCheckOriginal(StructureSet ss, Protocol_Item struct_details)
        {
            if ((struct_details.To_keep == true) || (string.IsNullOrEmpty(struct_details.Original_struc_B_label) && string.IsNullOrEmpty(struct_details.Original_struc_A_label)))                 //String B is null -> Check A structure 
            {
                if (struct_details.To_keep== true)
                {
                    struct_details.Code_error = "Structure à conserver";
                    return false;
                }
                else
                {
                    struct_details.Code_error = "ERREUR*** : Aucune chaine de caractère pour détection des structures originales A et B";
                    return false;
                }

            }
            else
            {
                if (string.IsNullOrEmpty(struct_details.Original_struc_A_label))                 //String A is null -> Don't check anything
                {
                    struct_details.Code_error = "ERREUR***: Vous devez au moins avoir du texte dans le bloc A a detecter";
                    return false;
                }
                else
                {
                    bool flag_error = true;

                    switch (struct_details.Number_structures)
                    {
                        case 0:
                            struct_details.Code_error = "ERREUR*** : Aucune detection de structure originale lancee -> Pas de chaine de caractère reconnue";
                            flag_error = false;
                            break;


                        case 1:             // Case of 1 structure to check: Structure A                    

                            //Use of temporary structure because original structures should be approved and thus unmodifiable
                            //Temporary structure is just a copy of original structure
                            Structure temp_Original_struc_A = FindStructureFromAlias(ss, struct_details.Original_struc_A_label);

                            if (temp_Original_struc_A == null)
                            {
                                struct_details.Code_error = "ERREUR*** : " + struct_details.Original_struc_A_label + " n'existe pas";
                                flag_error = false;
                            }
                            else
                            {
                                if (temp_Original_struc_A.IsEmpty)
                                {
                                    if (struct_details.To_keep == true)  //case if we want to keep an empty structure in the structureset. e.g. "chauffe"
                                    {
                                        flag_error = true;
                                    }
                                    else
                                    {
                                        struct_details.Code_error = "ERREUR*** : " + struct_details.Original_struc_A_label + " est vide";
                                        flag_error = false;
                                    }

                                }
                                else
                                {
                                    string label = "&" + struct_details.Original_struc_A_label;  
                                    if (label.Length > 16)
                                        label = label.Substring(0, 15);

                                    try
                                    {
                                        struct_details.Original_struc_A = ss.AddStructure("AVOIDANCE", label);
                                        struct_details.Original_struc_A.SegmentVolume = temp_Original_struc_A.Margin(0.0);
                                    }
                                    catch
                                    {
                                        MessageBox.Show(string.Format("Erreur creation structure temporaire de la strcture originale : {0}", struct_details.Original_struc_A_label));
                                    }                                    
                                }
                            }
                            break;


                        case 2:             // Case of 2 structures to check

                            //Use of temporary structure because original structures should be approved and thus unmodifiable to :
                            //      - be able to convert to HR theses temporaries structures later because original structures can be apporved and thus unmodifiable
                            //      - not stop the execution of structure creation for structure sum and substraction (need an empty strucutre for these cases)
                            //Temporary structure is just a copy of original structure

                            Structure tmp_Original_struc_A = FindStructureFromAlias(ss, struct_details.Original_struc_A_label);
                            Structure tmp_Original_struc_B = FindStructureFromAlias(ss, struct_details.Original_struc_B_label);


                            if (tmp_Original_struc_A == null && tmp_Original_struc_B == null)
                            {
                                struct_details.Code_error = "ERREUR*** : " + struct_details.Original_struc_A_label + " et " + struct_details.Original_struc_B_label + " n'existent pas";
                                flag_error = false;

                            }
                            else
                            { 
                                //string labelA = "temp" + struct_details.Original_struc_A_label;  OLDOLDOLDOLDOLDOLDOLDOLDOLDOLD
                                //string labelB = "temp" + struct_details.Original_struc_B_label;
                                string labelA = "&" + struct_details.Original_struc_A_label;
                                string labelB = "&" + struct_details.Original_struc_B_label;
                                if (labelA.Length > 16)
                                    labelA = labelA.Substring(0, 15);
                                if (labelB.Length > 16)
                                    labelB = labelB.Substring(0, 15);

                                if (tmp_Original_struc_A == null)
                                {
                                    try
                                    {
                                        struct_details.Original_struc_A = ss.AddStructure("AVOIDANCE", labelA);
                                    }
                                    catch
                                    {
                                        MessageBox.Show(string.Format("Erreur lors de la creation de la structure temporaire de la structure originale :{0}", struct_details.Original_struc_A_label));
                                    }
                                }
                                else
                                //tmp_Original A exists -> keep dicom type = PTV or GTV for further use. Else type = Avoidance
                                //Not for tmp_Original B (used for case of substraction between 2 structures -> only case for blocA)
                                {
                                    if (tmp_Original_struc_A.DicomType == "PTV" || tmp_Original_struc_A.DicomType == "GTV")
                                    {
                                        try
                                        {
                                            struct_details.Original_struc_A = ss.AddStructure(tmp_Original_struc_A.DicomType, labelA);
                                        }
                                        catch
                                        {
                                            MessageBox.Show(string.Format("Erreur lors de la creation de la structure temporaire de la structure originale :{0}", struct_details.Original_struc_A_label));
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            struct_details.Original_struc_A = ss.AddStructure("AVOIDANCE", labelA);
                                        }
                                        catch
                                        {
                                            MessageBox.Show(string.Format("Erreur lors de la creation de la structure temporaire de la structure originale :{0}", struct_details.Original_struc_A_label));
                                        }
                                    }

                                    struct_details.Original_struc_A.SegmentVolume = tmp_Original_struc_A.Margin(0.0);
                                }

                                try
                                {
                                    struct_details.Original_struc_B = ss.AddStructure("AVOIDANCE", labelB);
                                }
                                catch
                                {
                                    MessageBox.Show(string.Format("Erreur lors de la creation de la structure temporaire de la structure originale :{0}", struct_details.Original_struc_B_label));
                                }

                                //No need to keep Dicom type for bloc B
                                if (tmp_Original_struc_B != null)
                                {
                                    struct_details.Original_struc_B.SegmentVolume = tmp_Original_struc_B.Margin(0.0);
                                }

                                if (struct_details.Operator_A_B == "I" || struct_details.Operator_A_B == "i")
                                //Case of intersection between two structures
                                {
                                    if ((tmp_Original_struc_A == null || tmp_Original_struc_A.IsEmpty) && (tmp_Original_struc_B == null || tmp_Original_struc_B.IsEmpty))
                                    {
                                        if (tmp_Original_struc_A == null || tmp_Original_struc_A.IsEmpty)
                                        {
                                            flag_error = false;
                                            struct_details.Code_error = "ERREUR*** : intersection impossible si " + struct_details.Original_struc_A_label + " est vide ou nulle";

                                        }
                                        else
                                        {
                                            if (tmp_Original_struc_B == null || tmp_Original_struc_B.IsEmpty)
                                            {
                                                flag_error = false;
                                                struct_details.Code_error = "ERREUR*** : intersection impossible si  " + struct_details.Original_struc_B_label + " est vide ou nulle";
                                            }
                                        }
                                    }
                                }
                                else  // All other cases for two structures : Sum (+), substraction (-)
                                {
                                    if (struct_details.Operator_A_B == "+") //Addition of two structures 
                                    {
                                        if (struct_details.Original_struc_A == null)
                                        {
                                            struct_details.Code_error = "Warning : " + struct_details.Original_struc_A_label + " n'existe pas mais non bloquant";
                                        }
                                        if (struct_details.Original_struc_B == null)
                                        {
                                            struct_details.Code_error = "Warning : " + struct_details.Original_struc_B_label + " n'existe pas mais non bloquant";
                                        }
                                    }
                                    else
                                    {
                                        if (struct_details.Operator_A_B == "-") //Substraction of two structures 
                                        {
                                            if (struct_details.Original_struc_A == null || struct_details.Original_struc_A.IsEmpty)
                                            {
                                                //Structure of bloc A need to exist for substraction
                                                flag_error = false;
                                                struct_details.Code_error = "ERREUR*** : " + struct_details.Original_struc_A_label + " ne doit pas etre vide ou inexistante pour une soustraction";
                                            }
                                            if (struct_details.Original_struc_B == null)
                                            {
                                                struct_details.Code_error = "Warning : " + struct_details.Original_struc_B_label + " n'existe pas mais non bloquant";

                                            }
                                        }
                                        else
                                        {
                                            flag_error = false;
                                            struct_details.Code_error = "Operateur " + struct_details.Operator_A_B + " non reconnu";
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            struct_details.Code_error = "ERREUR*** lors de la detection de structures originales";
                            flag_error = false;
                            break;
                    }
                    return flag_error;
                }
            }
        }

        #endregion

        #region METHOD To CHECK STRUCTURE TO CREATE

        bool CheckStructureToCreate(StructureSet ss, Protocol_Item struct_details)
        // search through the list of alias ids until we find an alias that matches an existing structure.
        {
            Structure structure = null;
            bool flag_error = true;

            structure = (from s in ss.Structures
                         where s.Id.ToUpper().CompareTo(struct_details.Structure_label.ToUpper()) == 0
                         select s).FirstOrDefault();    //ToUpper = convertion en majuscule. Je passe tout en majuscule pour ne pas avoir à tenir compte de la casse

            if (structure != null)
            {
                if (structure.IsEmpty)
                {
                    struct_details.Code_error = "ERREUR*** : " + struct_details.Structure_label + " est vide mais il faut quand même la supprimer";
                    // flag_error = false;
                }
                else
                {
                    struct_details.Code_error = "ERREUR*** : " + struct_details.Structure_label + " existe deja dans le groupe de structure";
                    flag_error = false;
                }
            }
            if (structure == null)
            {
                flag_error = true;  //No structure -> ok              
            }
            return flag_error;
        }
        #endregion

        #region FIND STRUCTURE FROM ALIAS
        Structure FindStructureFromAlias(StructureSet ss, string ID)
        {
            // search through the list of alias ids until we find an alias that matches an existing structure.
            Structure oar = null;

            oar = (from s in ss.Structures
                   where s.Id.ToUpper().CompareTo(ID.ToUpper()) == 0  //ToUpper = convertion en majuscule. Je passe tout en majuscule pour ne pas avoir à tenir compte de la casse
                   select s).FirstOrDefault();

            return oar;
        }
        #endregion
    }
}
