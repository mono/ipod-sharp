
using System;
using System.Collections;
using System.Reflection;

namespace IPod {

    internal class ObjectDumper {

        public static void Dump (object o) {
            Dump (o, 0, new ArrayList ());
        }

        private static string Pad (int level, string msg, params object[] args) {
            string val = String.Format (msg, args);
            return val.PadLeft ((level * 4) + val.Length);
        }

        private static void Dump (object o, int level, ArrayList previous) {
            Type type = null;

            if (o != null) {
                type = o.GetType ();
            }
            
            Dump (o, type, null, level, previous);
        }
        
        private static void Dump (object o, Type type, string name, int level, ArrayList previous) {
            if (o == null) {
                Console.WriteLine (Pad (level, "{0} ({1}): (null)", name, type.Name));
                return;
            }

            if (previous.Contains (o)) {
                return;
            }

            previous.Add (o);

            if (type.IsPrimitive || o is string) {
                DumpPrimitive (o, type, name, level, previous);
            } else {
                DumpComposite (o, type, name, level, previous);
            }
        }

        private static void DumpPrimitive (object o, Type type, string name, int level, ArrayList previous) {
            if (name != null) {
                Console.WriteLine (Pad (level, "{0} ({1}): {2}", name, type.Name, o));
            } else {
                Console.WriteLine (Pad (level, "({0}) {1}", type.Name, o));
            }
        }

        private static void DumpComposite (object o, Type type, string name, int level, ArrayList previous) {

            if (name != null) {
                Console.WriteLine (Pad (level, "{0} ({1}):", name, type.Name));
            } else {
                Console.WriteLine (Pad (level, "({0})", type.Name));
            }

            if (o is IDictionary) {
                DumpDictionary ((IDictionary) o, level, previous);
            } else if (o is ICollection) {
                DumpCollection ((ICollection) o, level, previous);
            } else {
                MemberInfo[] members = o.GetType ().GetMembers (BindingFlags.Instance | BindingFlags.Public |
                                                                BindingFlags.NonPublic);
                
                foreach (MemberInfo member in members) {
                    try {
                        DumpMember (o, member, level, previous);
                    } catch {}
                }
            }
        }

        private static void DumpCollection (ICollection collection, int level, ArrayList previous) {
            foreach (object child in collection) {
                Dump (child, level + 1, previous);
            }
        }

        private static void DumpDictionary (IDictionary dictionary, int level, ArrayList previous) {
            foreach (object key in dictionary.Keys) {
                Console.WriteLine (Pad (level + 1, "[{0}] ({1}):", key, key.GetType ().Name));

                Dump (dictionary[key], level + 2, previous);
            }
        }

        private static void DumpMember (object o, MemberInfo member, int level, ArrayList previous) {
            if (member is MethodInfo || member is ConstructorInfo ||
                member is EventInfo)
                return;

            if (member is FieldInfo) {
                FieldInfo field = (FieldInfo) member;

                string name = member.Name;
                if ((field.Attributes & FieldAttributes.Public) == 0) {
                    name = "#" + name;
                }
                
                Dump (field.GetValue (o), field.FieldType, name, level + 1, previous);
            } else if (member is PropertyInfo) {
                PropertyInfo prop = (PropertyInfo) member;

                if (prop.GetIndexParameters ().Length == 0 && prop.CanRead) {
                    string name = member.Name;
                    MethodInfo getter = prop.GetGetMethod ();

                    if ((getter.Attributes & MethodAttributes.Public) == 0) {
                        name = "#" + name;
                    }
                    
                    Dump (prop.GetValue (o, null), prop.PropertyType, name, level + 1, previous);
                }
            }
        }
    }
}
