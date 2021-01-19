using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace trifenix.util
{
    public static partial  class GenUtil
    {
        /// <summary>
        /// Borra recursivamente una carpeta
        /// </summary>
        /// <param name="baseDir">carpeta a borrar</param>
        public static bool RecursiveDelete(DirectoryInfo baseDir)
        {
            // si no existe sale
            if (!baseDir.Exists)
                return false;


            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RecursiveDelete(dir);
            }


            var files = baseDir.GetFiles();

            if (!files.Any()) return false;


            foreach (var file in files)
            {
                file.IsReadOnly = false;
                file.Delete();
            }
            baseDir.Delete();
            return true;
        }


        /// <summary>
        /// Obtiene los tipos de un assembly (dll).
        /// </summary>
        /// <param name="assembly">dll obtenido desde una ruta</param>
        /// <returns></returns>
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }



    }
}
