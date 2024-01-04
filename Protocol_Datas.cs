using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using VMS.TPS.Common.Model.API;



namespace VMS.TPS
{
    public class Protocol_Datas
    {
        /* Class which represents the whole datas -> each protcol item of the template readed
         * in this class we read csv file from user previous selection
        */

        public List<Protocol_Item> lignes_protocole = new List<Protocol_Item>();

        private string path;
        private StructureSet ss;

        public Protocol_Datas(string path, StructureSet ss)  //Constructor
        {
            this.path = path;
            this.ss = ss;
            Read_datasxls();
        }

        //--------------------------------------------------------
        void Read_datasxls()
            //Method that read csv files from database
        {
            List<string[]> CSVSheet = new List<string[]>();

            ParseCSV(path);
            lignes_protocole.RemoveAt(0); //Remove fist line of csv file
        }

        void ParseCSV(string path)  // Read csv file split it to get each element
        {
            List<string[]> parsedData = new List<string[]>();

            string[] fields;

               
            var parser = new StreamReader(File.OpenRead(path));    

            while (!parser.EndOfStream)
            {
                fields = parser.ReadLine().Split(';');
                Protocol_Item item = new Protocol_Item(fields, ss); //When creating object, automatically fill each object attributes in Protocol_Item class
                                                                    //and then translate it and put in variables
                lignes_protocole.Add(item);

            }
            parser.Close();
        }
    }
}

