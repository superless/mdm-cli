using Colorful;
using CommandLine;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;

namespace mdm_gen
{

    /// <summary>
    /// Programa de línea de comandos para crear paquetes npn en javascript.
    /// </summary>
    public class Program
    {

        /// <summary>
        /// Se crea el verbo model, el cual permitirá tener los argumentos necesarios para ejecutar la creación del modelo de datos de mdm
        /// en typescript y poder publicarla en un github definido por los parámetros.
        /// Nuevos detalles de la definición en :
        /// https://dev.azure.com/trifenix-connect/agrofenix/_sprints/taskboard/agrofenix%20Team/agrofenix/pre-Sprint?workitem=62
        /// </summary>
        [Verb("model", HelpText = "Generación del modelo de datos de mdm-cli desde C# a typescript, haciendo un push en el git definido en los argumentos")]
        public class ModelArguments {


            /// <summary>
            /// Git donde se actualizará el modelo.
            /// </summary>
            [Value(0, Required = true, HelpText = "Url o ssh de la url de git del proyecto, el modelo resultado será públicado en este repositorio")]
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

            /// <summary>
            /// Rama que será sobrescrita en el github
            /// </summary>
            [Option('b', "branch", Required = false, HelpText = "rama que sobrescribirá en el código typescript", Default = "master")]
            public string branch { get; set; }

        }



        /// <summary>
        /// Verbo para generar el modelo de datos de trifenix connect.
        /// Este comando genera la metadata de yb proyecto que implemente el modelo de trifenix, entre elloas:
        /// Con esta metadata el cliente tendrá todos los datos necesarios para operar
        /// La estructura está formada desde los siguientes namespaces, necesarios como parámetros.
        /// 1. Index-model, el index model es el diccionario que vincula los EntitySearch con las clases del modelo y el input.
        /// 2. Input-model, el input model, es la estructura de las entradas del backend, al generar la estructura en typescript, permitirá al equipo de front, poder tener la estructura de entrada de las peticiones.
        /// 3. model, modelo de datos, clases que representan el modelo de clases que será persistido.
        /// 4. Documentación, Trifenix connect requiere de una interfaz para obtener la documentación, debe indicarse el namespace donde se encuentra la inplementación.
        /// </summary>
        [Verb("data", HelpText = "Genera el modelo de datos de un proyecto que implemente trifenix connect")]
        public class DataModelArguments {

            /// <summary>
            /// Url o ssh de la url de git del proyecto, esto permitirá modificar la rama y gatillar la generación de una nueva versión del paquete (si esta configurado el pipeline)
            /// </summary>
            [Value(0, Required = true, HelpText = "Url o ssh de la url de git del proyecto, el resultado será públicado en este repositorio")]
            public string GitAddress { get; set; }


            /// <summary>
            /// namespace del modelo de clases
            /// </summary>
            [Option('m', "model-namespace", Required = true, HelpText = "namespace del modelo de clases")]
            public string modelNamespace { get; set; }


            /// <summary>
            /// namespace del input-model
            /// </summary>
            [Option('i', "input-namespace", Required = true, HelpText = "namespace de las clases input")]
            public string inputNamespace { get; set; }


            /// <summary>
            /// namespace donde de encuentra la inplementación de IMdmDocumentation.
            /// </summary>
            [Option('d', "docs-namespace", Required = true, HelpText = "namespace donde se encuentre la implementación de IMdmDocumentation")]
            public string docsNamespace { get; set; }


            /// <summary>
            /// namespace de las enumeraciones de tipos de entitySearch.
            /// </summary>
            [Option('n', "enum-model", Required = true, HelpText = "namespace de los diccionarios de entitySearch (enums)")]
            public string esModelNamespace { get; set; }


            /// <summary>
            /// ruta del assembly
            /// </summary>
            [Option('a', "assembly", Required = true, HelpText = "ruta del assembly")]
            public string Assembly { get; set; }


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

            /// <summary>
            /// Rama que será sobrescrita en el github
            /// </summary>
            [Option('b', "branch", Required = false, HelpText = "rama que sobrescribirá en el código typescript", Default = "master")]
            public string branch  { get; set; }



        }

       
       /// <summary>
       /// Punto de entrada de la aplicación donde automáticamente se determina el flujo de acuerdo 
       /// a los argumentos en la ejecución.
       /// </summary>
       /// <param name="args"></param>
        static void Main(string[] args)
        {   

            // vincula los argumentos de la ejecución con los tipos de los argumentos.
            var result = Parser.Default.ParseArguments<ModelArguments, object>(args);

            if (result.Tag != ParserResultType.NotParsed)
            {
                // procesa los resultados, usando los argumentos como entrada.
                result.WithParsed<ModelArguments>(ProcessArgs);
                return;
            }

            var resultData = Parser.Default.ParseArguments<DataModelArguments, object>(args);

            if (resultData.Tag != ParserResultType.NotParsed)
            {
                // procesa los resultados, usando los argumentos como entrada.
                resultData.WithParsed<DataModelArguments>(ProcessDataArgs);
                
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ts"></param>
        public static void ProcessDataArgs(DataModelArguments ts) {
            var currentDitectory = AppDomain.CurrentDomain.BaseDirectory;
            var fontPath = Path.Combine(currentDitectory, "figlet/small");
            var fontTitle = new Figlet(Colorful.FigletFont.Load(fontPath));

            Colorful.Console.WriteLine(fontTitle.ToAscii("Trifenix Connect"), Color.Red);

            // metadata model
            Colorful.Console.WriteLine(fontTitle.ToAscii("MDM"), Color.Purple);

            // generación de código para trifenix connect.
            Colorful.Console.WriteLine("Bienvenido a la generación de código de trifenix connect mdm", Color.BlueViolet);

            Colorful.Console.WriteLine("Usted ha seleccionado la generación de paquetes de Typescript para generar el modelo", Color.DarkGreen);

            Colorful.Console.WriteLine("Generación de paquete con la información", Color.DarkGreen);

            Colorful.Console.WriteLine("Generación datos del modelo", Color.DarkGreen);

            CreateDataModel(ts.Assembly, ts.modelNamespace, ts.inputNamespace, ts.docsNamespace, ts.esModelNamespace , ts.GitAddress, ts.username, ts.email, ts.branch);

        }


        /// <summary>
        /// Procesa el modelo de mdm en typescript
        /// </summary>
        /// <param name="ts"></param>
        public static void ProcessArgs(ModelArguments ts) {

            //FIGlet es una aplicación informática que genera banners de texto, en varias tipografías, formadas por letras compuestas por conglomerados de caracteres ASCII más pequeños.

            var currentDitectory = AppDomain.CurrentDomain.BaseDirectory;
            var fontPath = Path.Combine(currentDitectory, "figlet/small");
            var fontTitle = new Figlet(Colorful.FigletFont.Load(fontPath));

            Colorful.Console.WriteLine(fontTitle.ToAscii("Trifenix Connect"), Color.Red);

            // metadata model
            Colorful.Console.WriteLine(fontTitle.ToAscii("MDM"), Color.Purple);

            // generación de código para trifenix connect.
            Colorful.Console.WriteLine("Bienvenido a la generación de código de trifenix connect mdm", Color.BlueViolet);

            Colorful.Console.WriteLine("Usted ha seleccionado la generación de paquetes de Typescript", Color.DarkGreen);

            Colorful.Console.WriteLine("Generación de paquete con los tipos base de MDM", Color.DarkGreen);

            // Usa typegen para generar el módelo.
            CreateBaseModelPackage(ts.GitAddress, ts.email, ts.username, ts.branch);

        }


        /// <summary>
        /// Genera modelo mdm en typescript y lo sube al github de los argumentos
        /// </summary>
        /// <param name="gitAddress">dirección del github, debe incluír el token</param>
        /// <param name="email">correo electrónico del usuario que registrará el commit</param>
        /// <param name="username">nombre de usuario</param>
        /// <param name="branch">rama del repositorio git</param>
        public static void CreateBaseModelPackage(string gitAddress, string email, string username, string branch) {
            ModelGen.GenerateMdm(gitAddress, email, username, branch);
        }


        /// <summary>
        /// Genera el modelo de datos de un proyecto particular que use trifenix connect, 
        /// utiliza como parámetro de entrada la ruta de la dll y los namespaces para capturar la metadata
        /// </summary>
        /// <param name="assembly">Assembly del programa</param>
        /// <param name="modelNamespace">namespace del modelo</param>
        /// <param name="inputNamespace">namespace del input</param>
        /// <param name="documentNamespace">namespace del documento</param>
        /// <param name="index_model_namespace">mdm diccionario</param>
        /// <param name="gitRepo">repositorio del git</param>
        /// <param name="user">usuario git</param>
        /// <param name="email">correo del usuario git</param>
        /// <param name="branch">Rama master</param>
        public static void CreateDataModel(string assembly, string modelNamespace, string inputNamespace, string documentNamespace, string index_model_namespace, string gitRepo, string user, string email, string branch) {
            DataGen.GenerateDataMdm(assembly, modelNamespace, inputNamespace, index_model_namespace, documentNamespace, gitRepo, user, email, branch);
        }


    }

    
}
