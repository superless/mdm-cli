using Colorful;
using CommandLine;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;

namespace mdm_gen
{
    public class Program
    {

        /// <summary>
        /// Tipo de generación data o modelo.
        /// </summary>
        [Flags]
        public enum GenKind{ 
            data = 0,            
            model = 1,
        }


        /// <summary>
        /// Determina el tipo de rama a la que accederá.
        /// </summary>
        public enum BranchType { 
            production,
            release,
            hotfix,
            develop
        }
        

        /// <summary>
        /// El verbo determina el primer argumento después del nombre del progrmama, en este caso
        /// mdm-gen typescrio [comandos]
        /// </summary>
        [Verb("typescript", HelpText = "Generación de mdm para typescript")]
        public class TypeScriptArguments {


            /// <summary>
            /// namespace del modelo de clases
            /// </summary>
            [Option('m', "model-namespace", Required = false, HelpText = "namespace del modelo de clases")]
            public string modelNamespace { get; set; }


            /// <summary>
            /// namespace del input-model
            /// </summary>
            [Option('i', "input-namespace", Required = false, HelpText = "namespace de las clases input")]
            public string inputNamespace { get; set; }


            /// <summary>
            /// namespace donde de encuentra la inplementación de IMdmDocumentation.
            /// </summary>
            [Option('d', "docs-namespace", Required = false, HelpText = "namespace donde se encuentre la implementación de IMdmDocumentation")]
            public string docsNamespace { get; set; }


            /// <summary>
            /// ruta del assembly
            /// </summary>
            [Option('a', "assembly", Required = false, HelpText = "ruta del assembly")]
            public string Assembly { get; set; }


            /// <summary>
            /// tipo de generación
            /// </summary>
            [Option('t', "type", Required = false, HelpText = "tipo de generación, si es del modelo y no se indica el namespace, ni el ")]
            public GenKind GenKind { get; set; } = GenKind.model;


            /// <summary>
            /// Url o ssh de la url de git del proyecto, esto permitirá modificar la rama y gatillar la generación de una nueva versión del paquete (si esta configurado el pipeline)
            /// </summary>
            [Value(0, Required = true, HelpText = "Url o ssh de la url de git del proyecto, esto permitirá modificar la rama y gatillar la generación de una nueva versión del paquete (si esta configurado el pipeline)")]
            public string GitAddress { get; set; }

            /// <summary>
            /// Usuario que registra el cambio en el repositorio del componente
            /// </summary>

            [Option('u', "user", Required = true, HelpText = "Usuario que registra el cambio en el repositorio del componente")]
            public string username { get; set; }


            /// <summary>
            /// correo que registra el cambio en el repositorio del componente
            /// </summary>

            [Option('e', "email", Required = true, HelpText = "correo que registra el cambio en el repositorio del componente")]
            public string email { get; set; }



        }

       
       /// <summary>
       /// Punto de entrada de la aplicación donde automáticamente se determina el flujo de acuerdo 
       /// a los argumentos en la ejecución.
       /// </summary>
       /// <param name="args"></param>
        static void Main(string[] args)
        {

            // vincula los argumentos de la ejecución con los tipos de los argumentos.
            var result = Parser.Default.ParseArguments<TypeScriptArguments, object>(args);

            // procesa los resultados, usando los argumentos como entrada.
            result.WithParsed<TypeScriptArguments>(ProcessArgs);


            result.WithParsed<object>((obj)=> { });


        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="ts"></param>
        public static void ProcessArgs(TypeScriptArguments ts) {



            //FIGlet es una aplicación informática que genera banners de texto, en varias tipografías, formadas por letras compuestas por conglomerados de caracteres ASCII más pequeños.


            var fontTitle = new Figlet(Colorful.FigletFont.Load("./figlet/small"));            
            
            var mdmFiglet = new Figlet(Colorful.FigletFont.Load("./figlet/small"));


            // trifenix con figlet :)
            Colorful.Console.WriteLine(fontTitle.ToAscii("Trifenix Connect"), Color.Red);

            // metadata model
            Colorful.Console.WriteLine(mdmFiglet.ToAscii("MDM"), Color.Purple);

            // generación de código para trifenix connect.
            Colorful.Console.WriteLine("Bienvenido a la generación de código de trifenix connect mdm", Color.BlueViolet);

            Colorful.Console.WriteLine("Usted ha seleccionado la generación de paquetes de Typescript", Color.DarkGreen);




            if (ts.GenKind == GenKind.model && string.IsNullOrWhiteSpace(ts.modelNamespace))
            {
                Colorful.Console.WriteLine("Generación de paquete con los tipos base de MDM", Color.DarkGreen);
                CreateBaseModelPackage(ts.GitAddress, ts.email, ts.username);
            }
            else if (ts.GenKind == GenKind.data) {

                Colorful.Console.WriteLine("Generación datos del modelo", Color.DarkGreen);
                CreateDataModel(ts.Assembly, ts.modelNamespace, ts.inputNamespace, ts.docsNamespace, ts.GitAddress, ts.username, ts.email);

                
            }



         
        }


        public static void CreateBaseModelPackage(string gitAddress, string email, string username) {
            MdmGen.GenerateMdm(gitAddress, email, username);
        }

        public static void CreateDataModel(string assembly, string modelNamespace, string inputNamespace, string documentNamespace, string gitRepo, string user, string email) {
            MdmGen.GenerateDataMdm(assembly, modelNamespace, inputNamespace, documentNamespace, gitRepo, user, email);
        }


    }

    
}
