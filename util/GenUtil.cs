﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace trifenix.util
{
    /// <summary>
    /// Clase estática con utilidades para la generación de typescript
    /// </summary>
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


        



    }
}
