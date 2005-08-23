
using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace IPod {

    internal class Capacities {

        private static Hashtable caps = new Hashtable ();
        
        static Capacities () {

            using (StreamReader reader = new StreamReader (Assembly.GetExecutingAssembly ().GetManifestResourceStream ("capacities.txt"))) {

                string line = null;

                while ((line = reader.ReadLine ()) != null) {
                    string[] splitLine = line.Split(';');

                    caps[splitLine[0]] = splitLine[1];
                }
            }
        }

        public static string GetCapacity (string model) {
            if (model == null)
                return null;
            else
                return (string) caps[model.Substring (1)];
        }
    }
}
