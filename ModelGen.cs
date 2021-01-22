using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using trifenix.util;
using TypeGen.Core.Converters;
using TypeGen.Core.Generator;
using TypeGen.Core.SpecGeneration;

namespace mdm_gen
{
    /// <summary>
    /// Crea el modelo de entitySearch en Typescript.
    /// </summary>
    public static class ModelGen {
        /// <summary>
        /// Genera las clases en typescript desde el repositorio que se le asigne
        /// para luego actualizar la rama develop del componente
        /// </summary>
        /// <param name="gitAddress">repositorio git donde se actualizará</param>
        /// <param name="email">correo del usuario git</param>
        /// <param name="username">correo del usuario</param>
        /// <param name="branch">correo del usuario</param>
        public static void GenerateMdm(string gitAddress, string email, string username, string branch)
        {


            // carpeta temporal donde se almacenará, ojo, esto puede causar complicaciones en ambientes linux.
            var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"mdm-gen-{Guid.NewGuid()}")).FullName;


            // carpeta donde se incorporará el código fuente generado.
            var srcFolder = Path.Combine(folder, "src");


            // elimina y agrega el contenido.
            GenUtil.Git.StageCommitPush(gitAddress, email, username, folder, branch, new Dictionary<string, Func<bool>> {
                { "Eliminando archivos generados anteriormente", ()=>GenUtil.RecursiveDelete(new DirectoryInfo(srcFolder)) },
                { "+semver: patch", () => GenerateTsModel(srcFolder) }

            });

            Colorful.Console.WriteLine($"Eliminando archivos generados anteriormente", Color.OrangeRed);
        }

        /// <summary>
        /// Genera modelo typescript de acuerdo a un generator spec        /// </summary>
        /// <param name="directory">carpeta donde generará el modelo</param>
        /// <returns></returns>
        private static bool GenerateTsModel(string directory)
        {
            try
            {
                // configuración general del spec
                var options = new GeneratorOptions
                {
                    BaseOutputDirectory = directory,
                    CreateIndexFile = true,
                    PropertyNameConverters = new MemberNameConverterCollection(new IMemberNameConverter[] { new JsonMemberNameConverter(), new PascalCaseToCamelCaseConverter() }),
                    FileNameConverters = new TypeNameConverterCollection(new ITypeNameConverter[] { }),
                    SingleQuotes = true
                };


                var gen = new Generator(options);

                // captura de evento
                gen.FileContentGenerated += (s,e) => Colorful.Console.WriteLine($"Generando archivo {new FileInfo(e.FilePath).Name}", Color.DarkGoldenrod);


                // genera typescript, de acuerdo a un spec.
                gen.Generate(new List<GenerationSpec>() { new ModelSpec() });

                return true;
            }
            catch (Exception)
            {
                return false;

            }

        }

    }



    
}
