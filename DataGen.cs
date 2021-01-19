using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using trifenix.connect.mdm.enums;
using trifenix.connect.mdm.resources;
using trifenix.connect.mdm.ts_model;
using trifenix.connect.mdm_attributes;
using trifenix.connect.util;
using trifenix.util;
using TypeGen.Core.Converters;
using TypeGen.Core.Generator;
using TypeGen.Core.SpecGeneration;


namespace mdm_gen
{


    /// <summary>
    /// Programa de línea de comandos 
    /// </summary>
    public static class DataGen
    {

        /// <summary>
        /// Obtiene los tipos de un namespace en partícular
        /// </summary>
        /// <param name="assmbl">assembly a analizar</param>
        /// <param name="ns">namespace a buscar</param>
        /// <returns></returns>
        private static IEnumerable<Type> GetTypesFromNameSpace(Assembly assmbl, string ns) {

            var types = assmbl.GetLoadableTypes().ToList();
            var otherTypes1 = assmbl.GetTypes();


            var rtnTypes = types.Where(x => x.FullName.StartsWith($"{ns}."));


            return rtnTypes;
        }


        /// <summary>
        /// Muestra pantalla por cada evento de generación de archivo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">detalles del archivo generado</param>
        private static void Gen_FileContentGenerated(object sender, FileContentGeneratedArgs e)
        {
            Colorful.Console.WriteLine($"Generando archivo {new FileInfo(e.FilePath).Name}", Color.DarkGoldenrod);
        }

       

        /// <summary>
        /// Obtiene la documentación desde un assembly, que implemente IMdmdDocumentation.
        /// </summary>
        /// <param name="assembly">assembly donde se buscará la implementación</param>
        /// <param name="ns">namespace</param>
        /// <returns>elemento de documentación desde el assemby</returns>
        private static IMdmDocumentation GetDocumentation(Assembly assembly, string ns) {
            var docImplementationType = GetDocumentationType(assembly, ns);
            
            return (IMdmDocumentation)Mdm.Reflection.Collections.CreateEntityInstance(docImplementationType);
        }

              


        /// <summary>
        /// Tipo que implementa documentación.
        /// </summary>
        /// <param name="assembly">assembly del proyecto, donde se buscará</param>
        /// <param name="ns">namespace donde buscar</param>
        /// <returns>Tipo de la implementación de documentación</returns>
        private static Type GetDocumentationType(Assembly assembly, string ns) => GetTypesFromNameSpace(assembly, ns).FirstOrDefault(s => s.GetInterface("IMdmDocumentation")!=null);


        /// <summary>
        /// Obtiene el modelo de una implementación de trifenix connect
        /// </summary>
        /// <param name="propertySearchInfo">información de cada propiedad de la entidad</param>
        /// <param name="doc">Implementación de la documetación</param>
        /// <param name="index">índice de la entidad</param>
        /// <returns></returns>
        public static EntityMetadata GetModel(IEnumerable<PropertySearchInfo> propertySearchInfo, IMdmDocumentation doc, int index)
        {

            // obtiene el modelo general de la entidad
            var modelInfo = doc.GetInfoFromEntity(index);


            // genera modelo de datos
            var modelDictionary = new EntityMetadata()
            {
                Index = index,
                Description = modelInfo.Description,
                ShortName = modelInfo.ShortName,
                Title = modelInfo.Title,
                BoolData = GetDictionaryFromRelated(propertySearchInfo,false, (int)KindProperty.BOOL),
                StringData = GetDictionaryFromRelated(propertySearchInfo, false, (int)KindProperty.STR),
                DateData = GetDictionaryFromRelated(propertySearchInfo, false, (int)KindProperty.DATE),
                DoubleData = GetDictionaryFromRelated(propertySearchInfo, false, (int)KindProperty.DBL),
                EnumData = GetEnumDictionaryFromRelated(propertySearchInfo),
                GeoData = GetDictionaryFromRelated(propertySearchInfo, false, (int)KindProperty.GEO),
                NumData = GetDictionaryFromRelated(propertySearchInfo, false, (int)KindProperty.NUM32),
                relData = GetDictionaryFromRelated(propertySearchInfo, true, (int)KindEntityProperty.REFERENCE),
            };


            // los siguientes tipos existen en un entitySearch
            // pero para este caso serán agrupados de manera más general.

            var suggestions = GetDictionaryFromRelated(propertySearchInfo, false, (int)KindProperty.SUGGESTION);

            var num64 = GetDictionaryFromRelated(propertySearchInfo, false, (int)KindProperty.NUM64);

            var suggestionNotInString = suggestions.Where(sg => !modelDictionary.StringData.Any(s => s.Key == sg.Key));

            var num64NotInNum = num64.Where(sg => !modelDictionary.NumData.Any(s => s.Key == sg.Key));

            var relLocal = GetDictionaryFromRelated(propertySearchInfo, true, (int)KindEntityProperty.LOCAL_REFERENCE);

            if (suggestionNotInString.Any())
                foreach (var item in suggestionNotInString)
                    modelDictionary.StringData.Add(item.Key, item.Value);

            if (num64NotInNum.Any())
                foreach (var item in num64NotInNum)
                    modelDictionary.NumData.Add(item.Key, item.Value);

            if (relLocal.Any())
                foreach (var item in relLocal)
                    modelDictionary.relData.Add(item.Key, item.Value);



            return modelDictionary;


        }


        /// <summary>
        /// Obtiene un diccionario con la metadata de propiedades
        /// de acuerdo al tipo y al indice.
        /// </summary>
        /// <param name="propSearchInfos">colección de propiedades asociadas a un índice</param>
        /// <param name="isEntity">El resultado a obtener son entidades?</param>
        /// <param name="related">tipo de dato a buscar (str, sug, num32, etc) o entidad (local/ referencia)</param>
        /// <returns></returns>
        private static Dictionary<int, PropertyMetadata> GetDictionaryFromRelated(IEnumerable<PropertySearchInfo> propSearchInfos, bool isEntity, int related)
        {

            // dependiendo, si la solicitud es entidad o valor, si es entidad buscará todas las entidades del elemento. 
            // si es valor (num32, str, etc), usará el related para determinar que propiedades traerá.
            var infos = isEntity?propSearchInfos.Where(s=>s.IsEntity && s.RelatedEntity== (KindEntityProperty)related):propSearchInfos.Where(s => !s.IsEntity && s.Related == (KindProperty)related).ToList();


            return infos.ToDictionary(s => s.Index, g => new PropertyMetadata
            {
                Visible = g.Visible,
                AutoNumeric = g.AutoNumeric,
                NameProp = char.ToLower(g.Name[0]) + g.Name.Substring(1), // First Upper Case.
                isArray = g.IsEnumerable,
                Info = g.Info,
                Required = g.IsRequired,
                Unique = g.IsUnique,
                HasInput = g.HasInput
            });
        }


        /// <summary>
        /// Obtiene la metadata de propiedad de tipo enuneración
        /// </summary>
        /// <param name="propSearchInfos">Colección de metadata, donde se buscará la enumeración</param>
        /// <returns>metadata de las enumeraciones de un elemento.</returns>
        private static Dictionary<int, PropertyMetadadataEnum> GetEnumDictionaryFromRelated(IEnumerable<PropertySearchInfo> propSearchInfos)
        {
            return propSearchInfos.Where(s => s.Related == KindProperty.ENUM).ToDictionary(s => s.Index, g => new PropertyMetadadataEnum
            {
                NameProp = char.ToLower(g.Name[0]) + g.Name.Substring(1),
                isArray = g.IsEnumerable,
                Info = g.Info,
                EnumData = g.Enums
            });
        }


        /// <summary>
        /// Genera modelo, input model y enums para trifenix connect.
        /// </summary>
        /// <param name="models">tipos de modelo</param>
        /// <param name="inputs">tipos de inputs</param>
        /// <param name="mdm_enums">tipos de mdm</param>
        /// <param name="srcFolder">carpeta del código fuente</param>
        public static void GenerateModelStructure(IEnumerable<Type> models, IEnumerable<Type> inputs, IEnumerable<Type> mdm_enums, string srcFolder) {
            // generación del modelo
            var options = new GeneratorOptions
            {
                BaseOutputDirectory = srcFolder,
                CreateIndexFile = true,
                PropertyNameConverters = new MemberNameConverterCollection(new IMemberNameConverter[] { new JsonMemberNameConverter(), new PascalCaseToCamelCaseConverter() }),
                FileNameConverters = new TypeNameConverterCollection(new ITypeNameConverter[] { }),
                SingleQuotes = true,
                
            };


            var gen = new Generator(options);

            // captura de evento
            gen.FileContentGenerated += (s, e) => Colorful.Console.WriteLine($"Generando archivo {new FileInfo(e.FilePath).Name}", Color.DarkGoldenrod);


            // genera typescript, de acuerdo a un spec.
            gen.Generate(new List<GenerationSpec>() { new DataSpec(models, inputs, mdm_enums) });

            FixDataModel(srcFolder);
        }

        public static void FixDataModel(string srcFolder) {

            var lst = new List<string>();

            var modelDir = Path.Combine(srcFolder, "model/");
            var inputDir = Path.Combine(srcFolder, "input-model/");
            var filesModel = Directory.GetFiles(modelDir).ToList();
            var filesInput = Directory.GetFiles(inputDir).ToList();
            lst.AddRange(filesModel);
            lst.AddRange(filesInput);

            var grp = lst.Where(s=>!s.Contains("index.ts")).GroupBy(s => Path.GetFileNameWithoutExtension(s)).Where(s => s.Count() > 1).ToDictionary(s => s.Key);


            foreach (var item in grp)
            {
                var file = item.Key;
                
                var extradirectory = Path.Combine(srcFolder, "extra/");
                Directory.CreateDirectory(extradirectory);
                var i = 0;
                foreach (var itemFile in item.Value)
                {
                    if (i == 0)
                    {
                        File.Copy(itemFile, Path.Combine(extradirectory, $"{file}.ts"));
                    }
                    File.Delete(itemFile);
                    i++;
                }

                // replace reference
                ReplaceReference(modelDir, file);
                ReplaceReference(inputDir, file);
                var indexModel = Path.Combine(modelDir, "index.ts");
                var indexInputModel = Path.Combine(inputDir, "index.ts");
                var linesModel = File.ReadAllLines(indexModel).ToList();
                var linesInputModel = File.ReadAllLines(indexInputModel).ToList();
                linesModel.RemoveAt(GetIndex(linesModel, file));
                linesInputModel.RemoveAt(GetIndex(linesInputModel, file));
                File.WriteAllLines(indexModel, linesModel);
                File.WriteAllLines(indexInputModel, linesInputModel);


            }
        }

        private static int GetIndex(IEnumerable<string> lines, string fileWithoutExtension) => lines.Select((s, i) => new { Str = s, Index = i })
                    .Where(x => x.Str.Contains(fileWithoutExtension) && x.Str.Contains("from"))
                    .Select(x => x.Index).First();

        public static void ReplaceReference(string pathDirectory, string fileWithoutExtension) {
            var files = Directory.GetFiles(pathDirectory);
            foreach (var item in files)
            {
                var lines = File.ReadAllLines(item);
                if (lines.Any(s => s.Contains(fileWithoutExtension) && s.Contains("import")))
                {
                    var index = GetIndex(lines, fileWithoutExtension);

                    var split = lines[index].Split("from");
                    var textToReplace = split[1].Replace($"'./{fileWithoutExtension}'", $"'./../extra/{fileWithoutExtension}'");
                    lines[index] = $"{split[0]} from {textToReplace}";
                    File.WriteAllLines(item, lines);
                }
            }

        }

        /// <summary>
        /// Genera modelo de datos desde una dll de un proyecto
        /// que implemente trifenix connect.
        /// </summary>
        /// <param name="assembly">dll de donde obtendrá los valores</param>
        /// <param name="modelNamespace">namespace donde se encuentra el modelo</param>
        /// <param name="inputNamespace">namespace donde se encuentra el input-model</param>
        /// <param name="index_model_namespace">namespace del diccionario mdm</param>
        /// <param name="documentNamespace">namespace donde se encuentra la implementación de IMDMDocumentation</param>
        /// <param name="gitRepo">repositorio donde se subirá la información</param>
        /// <param name="user">nombre de usuario git</param>
        /// <param name="email">correo git</param>
        /// <param name="branch">rama</param>
        public static void GenerateDataMdm(string assembly, string modelNamespace, string inputNamespace, string index_model_namespace, string documentNamespace, string gitRepo, string user, string email, string branch)
        {

            // assembly
            var assemblyInput = Assembly.LoadFrom(assembly);

            // carpeta temporal, donde se creará el modelo
            var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"mdm-data-{Guid.NewGuid()}")).FullName;

            // carpeta código fuente.
            var srcFolder = Path.Combine(folder, "src");

            // modelo
            var modelTypes = GetTypesFromNameSpace(assemblyInput, modelNamespace);

            // input types
            var inputTypes = GetTypesFromNameSpace(assemblyInput, inputNamespace);

            // enum types

            var enumTypes = GetTypesFromNameSpace(assemblyInput, index_model_namespace);

          


            // documentación
            var documentation = GetDocumentation(assemblyInput, documentNamespace);


            // metadata de los tipos
            var propSearchinfos = modelTypes.Where(s => s.GetTypeInfo().GetCustomAttributes<EntityIndexAttribute>(true).Any()).Select(s => {

                // metadata de la entidad
                var infoHeader = s.GetTypeInfo().GetCustomAttributes<EntityIndexAttribute>(true).FirstOrDefault();

                // busca si existen metadatas de menú.
                var grp = s.GetCustomAttributes(typeof(EntityGroupMenuAttribute), true).Select(s => (EntityGroupMenuAttribute)s).ToList();


                // objeto anónimo con metadata.
                return new
                {
                    index = infoHeader.Index,
                    visible = infoHeader.Visible,
                    pathName = infoHeader.PathName,
                    kindEntity = infoHeader.Kind,
                    propInfos = GetPropertySearchInfo(s, assemblyInput, inputNamespace, documentation),
                    className = s.Name,
                    GroupMenu = grp?.Select(s=>s.Grupo).ToArray()??Array.Empty<GroupMenu>()
                };


            }).ToList();


            // metadata en colección de EntityMetadata
            var modelDict = propSearchinfos.Select(s =>
            {
                var model = GetModel(s.propInfos, documentation,  s.index);
                model.Visible = s.visible;
                model.PathName = s.pathName;
                model.EntityKind = s.kindEntity;
                model.ClassName = s.className;
                model.AutoNumeric = s.propInfos.Any(a => a.AutoNumeric);
                model.Menus = s.GroupMenu;
                return model;

            }).ToList();

            // mdm agro
            var modelDate = new ModelMetaData { Indexes = modelDict.ToArray() };

            // serialización de datos
            var json = JsonConvert.SerializeObject(modelDate, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });



            


            


            // archivo a generar.
            var file =  Path.Combine(srcFolder, "metadata/mdm.ts");



            // commit y envío.
            GenUtil.Git.StageCommitPush(gitRepo, email, user, folder, branch, new Dictionary<string, Func<bool>> {
                { "Eliminando archivos generados anteriormente", ()=>GenUtil.RecursiveDelete(new DirectoryInfo(srcFolder)) },
                { "creando modelo", ()=> {
                    GenerateModelStructure(modelTypes, inputTypes, enumTypes, srcFolder );
                    return true;
                } },
                { "datos cargados", () => {
                 File.WriteAllText(file, $"import {{ ModelMetaData }} from \"@trifenix/mdm\"; \nexport const data:ModelMetaData = {json} as ModelMetaData");
                 return true;
            } } });;


            // genera el json con datos
           




        }


        /// <summary>
        /// Obtiene una colección de metadata desde una entidad.
        /// </summary>
        /// <param name="type">Clase de tipo modelo, donde debe obtener la metadata</param>
        /// <param name="index">índice de la entidad</param>
        /// <param name="inputType">Clase de tipo input, donde podrá obtener la metadata</param>
        /// <param name="docs">Implmentación de IMdmDocumentation para generar la documentación</param>
        /// <returns>Colección de metadata</returns>
        public static PropertySearchInfo[] GetPropertyByIndex(Type type, int index, Type inputType, IMdmDocumentation docs)
        {

            // obtiene el atributo de la clase, que determina el índice de la entidad
            var searchAttributesProps = type.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(BaseIndexAttribute), true));

            
            
            // asigna la metadata de la propiedad, además, si es requerido o si es único. 
            var elemTypeInputProps = inputType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(BaseIndexAttribute), true))
                .Select(s => new { info = s, search = Mdm.Reflection.Attributes.GetAttribute<BaseIndexAttribute>(s), required = Mdm.Reflection.Attributes.GetAttribute<RequiredAttribute>(s), unique = Mdm.Reflection.Attributes.GetAttribute<UniqueAttribute>(s) });

            


            // crea un listado de metadata para la entidad.
            var props = searchAttributesProps.Select(s => {

                // buscando atributos. 
                var searchAttribute = (BaseIndexAttribute)s.GetCustomAttributes(typeof(BaseIndexAttribute), true).FirstOrDefault();
                var searchAttributeInput = elemTypeInputProps.FirstOrDefault(p => p.search.Index == searchAttribute.Index && p.search.IsEntity == searchAttribute.IsEntity);
                var grp = s.GetCustomAttributes(typeof(GroupAttribute), true).Select(s=>(GroupAttribute)s).ToList();

                return new PropertySearchInfo
                {
                    IsEnumerable =  Mdm.Reflection.IsEnumerableProperty(s),
                    Name = s.Name,
                    Index = searchAttribute.Index,
                    Related = (KindProperty)(!searchAttribute.IsEntity?searchAttribute.KindIndex:0),
                    RelatedEntity = (KindEntityProperty)(searchAttribute.IsEntity?searchAttribute.KindIndex:0),
                    Enums = !searchAttribute.IsEntity && searchAttribute.KindIndex == (int)KindProperty.ENUM ?  Mdm.Reflection.GetDescription(s.PropertyType) : new Dictionary<int, string>(),
                    IndexClass = index,
                    Info = searchAttribute.IsEntity? docs.GetInfoFromEntity(index): docs.GetInfoFromProperty((KindProperty)searchAttribute.KindIndex, searchAttribute.Index),
                    IsRequired = searchAttributeInput?.required != null,
                    IsUnique = searchAttributeInput?.unique != null,
                    AutoNumeric = searchAttribute.GetType() == typeof(AutoNumericDependantAttribute),
                    Visible = searchAttribute.Visible,
                    HasInput = searchAttributeInput != null,
                    IsEntity = searchAttribute.IsEntity,
                    Group = grp?.Select(s=>s.Group).ToArray()??Array.Empty<GroupInput>()
                    


                };
            }).ToArray();


            //si existe alguna propiedad que esté en el input y no esté en la entidad.
            if (elemTypeInputProps.Any(s => !props.Any(a => a.Name.Equals(s.info.Name))))
            {
                var extra = elemTypeInputProps.Where(s => !props.Any(a => a.Name.Equals(s.info.Name)));
                var list = props.ToList();
                foreach (var item in extra)
                {
                    list.Add(new PropertySearchInfo
                    {
                        IsEnumerable = Mdm.Reflection.IsEnumerableProperty(item.info),
                        Name = item.info.Name,
                        Index = item.search.Index,
                        Related = (KindProperty)(!item.search.IsEntity ? item.search.KindIndex : 0),
                        RelatedEntity = (KindEntityProperty)(item.search.IsEntity ? item.search.KindIndex : 0),
                        Enums = !item.search.IsEntity && item.search.KindIndex == (int)KindProperty.ENUM ? Mdm.Reflection.GetDescription(item.info.PropertyType) : new Dictionary<int, string>(),
                        IndexClass = index,
                        Info = item.search.IsEntity ? docs.GetInfoFromEntity(item.search.Index) : docs.GetInfoFromProperty((KindProperty)item.search.KindIndex, item.search.Index),
                        IsRequired = item?.required != null,
                        IsUnique = item?.unique != null,
                        IsEntity = item.search.IsEntity,
                        
                    });
                }
                props = list.ToArray();

            }
            return props;
        }


        /// <summary>
        /// Obtiene metadata desde una entidad. 
        /// </summary>
        /// <param name="type">Tipo de entidad</param>
        /// <param name="assemblyInput"></param>
        /// <param name="typeNamespace">namespace del input (se buscará el input vinculado a la clase, a través de los índices)</param>
        /// <param name="docs">Implmentación Documentación</param>
        /// <returns>Colección de metadata</returns>
        public static PropertySearchInfo[] GetPropertySearchInfo(Type type, Assembly assemblyInput, string typeNamespace, IMdmDocumentation docs)
        {
            var classAtribute = Mdm.Reflection.Attributes.GetAttributes<EntityIndexAttribute>(type);
            if (classAtribute == null || !classAtribute.Any())
                return Array.Empty<PropertySearchInfo>();


            var inputType = Mdm.Reflection.GetEntityType(classAtribute.FirstOrDefault().Index, assemblyInput, typeNamespace);
            if (inputType == null) return Array.Empty<PropertySearchInfo>();


            return GetPropertyByIndex(type, classAtribute.FirstOrDefault().Index, inputType, docs);
        }
    }



    
}
