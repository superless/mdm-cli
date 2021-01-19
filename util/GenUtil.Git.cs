using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace trifenix.util
{
    public static partial  class GenUtil
    {
        public static partial class Git {
            /// <summary>
            /// Realiza commits determinados por commitMessageFIleOperations
            /// genera un nuevo tag de acuerdo a la última versión.
            /// envía al git determinado por los argumentos
            /// </summary>
            /// <param name="gitAddress">dirección del git (debería incluir el token)</param>
            /// <param name="email">correo electrónico</param>
            /// <param name="username">alejandro</param>
            /// <param name="folder">carpeta donde se clonará el proyecto</param>
            /// <param name="branch">rama donde se operará</param>
            /// <param name="commitMessageFileOperations">Funciones por commit, por cada commit se ejecutará la función.</param>
            public static void StageCommitPush(string gitAddress, string email, string username, string folder, string branch, Dictionary<string, Func<bool>> commitMessageFileOperations)
            {



                Colorful.Console.WriteLine($"Clonando repositorio", Color.OrangeRed);


                using (var repo = new Repository(Repository.Clone(gitAddress, folder, new CloneOptions { BranchName = branch })))
                {

                    Colorful.Console.WriteLine($"Repositorio clonado", Color.OrangeRed);

                    // obtiene los tags desde el repositorio
                    var listTags = repo.Tags;

                    // filtra tags, con un formato legible de SemVer.
                    var fullTags = listTags.Where(s => s.FriendlyName.Split(".").Count() >= 3).Select(s => {
                        var splt = s.FriendlyName.Split(".", 3);

                        return new
                        {
                            major = splt[0],
                            minor = splt[1],
                            patch = splt[2],
                            full = s.FriendlyName

                        };

                    })
                    .Where(s => int.TryParse(s.major, out var res) && int.TryParse(s.minor, out var res2) && int.TryParse(s.patch, out var res3))
                    .Select(s => new
                    {
                        major = int.Parse(s.major),
                        minor = int.Parse(s.minor),
                        patch = int.Parse(s.patch),
                        s.full

                    });

                    string newTag = string.Empty;

                    if (fullTags.Any())
                    {
                        // obtenemos los valores más altos.
                        var maxMajor = fullTags.Max(s => s.major);
                        var maxMinor = fullTags.Max(s => s.minor);
                        var maxPatch = fullTags.Max(s => s.patch);

                        // creamos el nuevo tag.
                        newTag = $"{maxMajor}.{maxMinor}.{maxPatch + 1}";
                    }
                    else
                    {
                        newTag = "0.0.1";
                    }

                    // aplicamos el tag/
                    var tg = repo.ApplyTag(newTag);


                    Colorful.Console.WriteLine($"Generando nuevo tag : {newTag}", Color.OrangeRed);

                    // enviamos el tag al servidor 
                    repo.Network.Push(repo.Network.Remotes["origin"], tg.CanonicalName, new PushOptions { });

                    // carpeta de código fuente.
                    var srcFolder = Path.Combine(folder, "src");

                    // eliminamos archivos previos
                    Colorful.Console.WriteLine($"Eliminando archivos generados anteriormente", Color.OrangeRed);

                    // checkout a la rama.
                    Commands.Checkout(repo, branch);


                    //ejecutamos los commits del parámetro.
                    foreach (var actionMessage in commitMessageFileOperations)
                    {
                        var commit = actionMessage.Key;

                        if (actionMessage.Value.Invoke())
                        {
                            // añade todos los archivos al stage, después de la ejecución de la operación de commit
                            Commands.Stage(repo, "*");

                            // commit al servidor
                            repo.Commit(commit, new Signature(username, email, DateTimeOffset.Now), new Signature(username, email, DateTimeOffset.Now));
                        }

                    }

                    Colorful.Console.WriteLine($"Commit con archivos generados", Color.OrangeRed);

                    repo.Network.Push(repo.Network.Remotes["origin"], @$"refs/heads/{branch}", new PushOptions { });
                }
            }
        }
    }
}
