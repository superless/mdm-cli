using LibGit2Sharp;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using trifenix.connect.mdm.entity_model;
using trifenix.connect.mdm.enums;
using trifenix.connect.mdm.resources;
using trifenix.connect.mdm.ts_model;
using trifenix.connect.mdm.ts_model.props;
using trifenix.connect.mdm_attributes;
using trifenix.connect.ts_model.enums;
using trifenix.connect.util;
using TypeGen.Core.Converters;
using TypeGen.Core.Extensions;
using TypeGen.Core.Generator;
using TypeGen.Core.SpecGeneration;


namespace mdm_gen
{
    public static class MdmGen
    {

        /// <summary>
        /// Borra recursivamente una carpeta
        /// </summary>
        /// <param name="baseDir">carpeta a borrar</param>
        public static bool RecursiveDelete(DirectoryInfo baseDir)
        {
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



        public static void StageCommitPush(string gitAddress, string email, string username, string folder, string branch, Dictionary<string, Func<bool>> commitMessageFileOperations) {

            

            Colorful.Console.WriteLine($"Clonando repositorio", Color.OrangeRed);


            using (var repo = new Repository(Repository.Clone(gitAddress, folder, new CloneOptions { BranchName = branch })))
            {

                Colorful.Console.WriteLine($"Repositorio clonado", Color.OrangeRed);

                var listTags = repo.Tags;

                var fullTags = listTags.Select(s => {
                    var splt = s.FriendlyName.Split(".", 3);

                    return new
                    {
                        major = splt[0],
                        minor = splt[1],
                        patch = splt[2],
                        full = s.FriendlyName

                    };
                }).Where(s => int.TryParse(s.major, out var res) && int.TryParse(s.minor, out var res2) && int.TryParse(s.patch, out var res3));


                var maxMajor = fullTags.Max(s => s.major);
                var maxMinor = fullTags.Max(s => s.minor);
                var maxPatch = fullTags.Max(s => s.patch);

                var newTag = $"{maxMajor}.{maxMinor}.{int.Parse(maxPatch) + 1}";

                var tg = repo.ApplyTag(newTag);

                Colorful.Console.WriteLine($"Generando nuevo tag : {newTag}", Color.OrangeRed);


                repo.Network.Push(repo.Network.Remotes["origin"], tg.CanonicalName, new PushOptions { });

                var srcFolder = Path.Combine(folder, "src");

                Colorful.Console.WriteLine($"Eliminando archivos generados anteriormente", Color.OrangeRed);
                Commands.Checkout(repo, branch);

                foreach (var actionMessage in commitMessageFileOperations)
                {
                    var commit = actionMessage.Key;

                    if (actionMessage.Value.Invoke())
                    {
                        Commands.Stage(repo, "*");

                        // corregir, falla cuando no hay cambios, igual funciona por ahora.
                        repo.Commit(commit, new Signature(username, email, DateTimeOffset.Now), new Signature(username, email, DateTimeOffset.Now)); 
                    }

                }

                Colorful.Console.WriteLine($"Commit con archivos generados", Color.OrangeRed);

                repo.Network.Push(repo.Network.Remotes["origin"], @$"refs/heads/{branch}", new PushOptions { });
            }
        }

        /// <summary>
        /// Genera las clases en typescript desde el repositorio que se le asigne
        /// para luego actualizar la rama develop del componente
        /// </summary>
        /// <param name="gitAddress">repositorio git donde se actualizará</param>
        /// <param name="email"></param>
        /// <param name="username"></param>
        public static void GenerateMdm(string gitAddress, string email, string username) {

            var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"mdm-gen-{Guid.NewGuid()}")).FullName;

            var srcFolder = Path.Combine(folder, "src");


            StageCommitPush(gitAddress, email, username, folder, "master", new Dictionary<string, Func<bool>> { 
                { "Eliminando archivos generados anteriormente", ()=>RecursiveDelete(new DirectoryInfo(srcFolder)) },
                { "+semver: patch", () => GenerateTsModel(srcFolder) }

            });

            Colorful.Console.WriteLine($"Eliminando archivos generados anteriormente", Color.OrangeRed);

            

            



        }

        

        private static bool GenerateTsModel(string directory) {
            try
            {
                var options = new GeneratorOptions
                {
                    BaseOutputDirectory = directory,
                    CreateIndexFile = true,
                    PropertyNameConverters = new MemberNameConverterCollection(new IMemberNameConverter[] { new JsonMemberNameConverter(), new PascalCaseToCamelCaseConverter() }),
                    FileNameConverters = new TypeNameConverterCollection(new ITypeNameConverter[] { }),
                    SingleQuotes = true
                };

                var gen = new Generator(options);

                gen.FileContentGenerated += Gen_FileContentGenerated;


                gen.Generate(new List<GenerationSpec>() { new ModelSpec() });
                return true;
            }
            catch (Exception)
            {
                return false;
                
            }
            
        }

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



        private static IEnumerable<Type> GetTypesFromNameSpace(Assembly assmbl, string ns) {

            var types = GetLoadableTypes(assmbl).ToList();

            var rtnTypes = types.Where(x => x.FullName.StartsWith($"{ns}."));
            return rtnTypes;
        }



        private static void Gen_FileContentGenerated(object sender, FileContentGeneratedArgs e)
        {
            Colorful.Console.WriteLine($"Generando archivo {new FileInfo(e.FilePath).Name}", Color.DarkGoldenrod);
        }

       

        private static IMdmDocumentation GetDocumentation(Assembly assembly, string ns) {
            var docImplementationType = GetDocumentationType(assembly, ns);
            
            return (IMdmDocumentation)Mdm.Reflection.Collections.CreateEntityInstance(docImplementationType);
        }

              

        private static Type GetDocumentationType(Assembly assembly, string ns) => GetTypesFromNameSpace(assembly, ns).FirstOrDefault(s => s.GetInterface("IMdmDocumentation")!=null);


        public static EntityMetadata GetModel(IEnumerable<PropertySearchInfo> propertySearchInfo, IMdmDocumentation doc, int index)
        {

            

            var modelInfo = doc.GetInfoFromEntity(index);

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

        private static Dictionary<int, PropertyMetadata> GetDictionaryFromRelated(IEnumerable<PropertySearchInfo> propSearchInfos, bool isEntity, int related)
        {
            var infos = isEntity?propSearchInfos.Where(s=>s.IsEntity && s.RelatedEntity== (KindEntityProperty)related):propSearchInfos.Where(s => !s.IsEntity && s.Related == (KindProperty)related).ToList();

            return infos.ToDictionary(s => s.Index, g => new PropertyMetadata
            {
                Visible = g.Visible,
                AutoNumeric = g.AutoNumeric,
                NameProp = char.ToLower(g.Name[0]) + g.Name.Substring(1),
                isArray = g.IsEnumerable,
                Info = g.Info,
                Required = g.IsRequired,
                Unique = g.IsUnique,
                HasInput = g.HasInput
            });
        }

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




        public static void GenerateDataMdm(string assembly, string modelNamespace, string inputNamespace, string documentNamespace, string gitRepo, string user, string email, string branch)
        {
            var assemblyInput = Assembly.LoadFrom(assembly);


            var modelTypes = GetTypesFromNameSpace(assemblyInput, modelNamespace);


            var documentation = GetDocumentation(assemblyInput, documentNamespace);


            var propSearchinfos = modelTypes.Where(s => s.GetTypeInfo().GetCustomAttributes<EntityIndexAttribute>(true).Any()).Select(s => {

                var infoHeader = s.GetTypeInfo().GetCustomAttributes<EntityIndexAttribute>(true).FirstOrDefault();

                var grp = s.GetCustomAttributes(typeof(EntityGroupMenuAttribute), true).Select(s => (EntityGroupMenuAttribute)s).ToList();


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

            var json = JsonConvert.SerializeObject(modelDate, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });


            var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"mdm-data-{Guid.NewGuid()}")).FullName;

            var srcFolder = Path.Combine(folder, "src");
            var file =  Path.Combine(srcFolder, "index.ts");




            StageCommitPush(gitRepo, email, user, folder, branch, new Dictionary<string, Func<bool>> { { "datos cargados", () => {
               
                 File.WriteAllText(file, $"import {{ ModelMetaData }} from \"@fenix/mdm\"; \nexport const data:ModelMetaData = {json} as ModelMetaData");
                 return true;
            } } });


            // genera el json con datos
           




        }

        public static PropertySearchInfo[] GetPropertyByIndex(Type type, int index, Type inputType, IMdmDocumentation docs)
        {
            var searchAttributesProps = type.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(BaseIndexAttribute), true));

            

            var elemTypeInputProps = inputType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(BaseIndexAttribute), true))
                .Select(s => new { info = s, search = Mdm.Reflection.Attributes.GetAttribute<BaseIndexAttribute>(s), required = Mdm.Reflection.Attributes.GetAttribute<RequiredAttribute>(s), unique = Mdm.Reflection.Attributes.GetAttribute<UniqueAttribute>(s) });

            

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

    


    public class ModelSpec : GenerationSpec {

        public override void OnBeforeGeneration(OnBeforeGenerationArgs args)
        {
            
            AddInterface<PropertyMetadadataEnum>("data/");
            AddInterface<EntitySearchDisplayInfo>("data/");
            AddInterface<PropertyMetadata>("data/");
            AddInterface<ModelMetaData>("data/");
            AddInterface<EntityMetadata>("data/");
            AddInterface(typeof(EntityBaseSearch<>), "model/main")
                .Member(nameof(EntityBaseSearch<Object>.bl)).Type("BoolProperty", "./BoolProperty")
                .Member(nameof(EntityBaseSearch<Object>.dbl)).Type("DblProperty", "./DblProperty")                
                .Member(nameof(EntityBaseSearch<Object>.enm)).Type("EnumProperty", "./EnumProperty")
                .Member(nameof(EntityBaseSearch<Object>.dt)).Type("DtProperty", "./DtProperty")
                .Member(nameof(EntityBaseSearch<Object>.num32)).Type("Num32Property", "./Num32Property")
                .Member(nameof(EntityBaseSearch<Object>.num64)).Type("Num64Property", "./Num64Property")
                .Member(nameof(EntityBaseSearch<Object>.rel)).Type("RelatedId", "./RelatedId")
                .Member(nameof(EntityBaseSearch<Object>.str)).Type("StrProperty", "./StrProperty")
                .Member(nameof(EntityBaseSearch<Object>.sug)).Type("StrProperty", "./StrProperty")
                ;
            AddInterface<GeoPointTs>("model/main");
            AddInterface(typeof(IProperty<>), "model/main");
            AddInterface<GeographyProperty>("model/main");
            AddInterface<DblProperty>("model/main");
            AddInterface<DtProperty>("model/main");
            AddInterface<EnumProperty>("model/main");
            AddInterface<StrProperty>("model/main");
            AddInterface<Num32Property>("model/main");
            AddInterface<Num64Property>("model/main");
            AddInterface<BoolProperty>("model/main");
            AddInterface(typeof(PropertyBaseFaceTable<>),"model/main");
            AddInterface<IPropertyFaceTable>("model/main");
            AddInterface<RelatedId>("model/main");
            AddInterface<StrProperty>("model/main");
            AddEnum<PhisicalDevice>("model/enums");
            AddEnum<FilterType>("model/enums");
            AddInterface<FilterGlobalEntityInput>("model/filters").Member(nameof(FilterGlobalEntityInput.FilterChilds)).Optional();
            AddInterface<GroupInput>("model/containers")
                .Member(nameof(GroupInput.ColumnProportion)).Optional()                
                .Member(nameof(GroupInput.OrderIndex)).Optional()
                .Member(nameof(GroupInput.Device)).Optional();
            AddInterface<GroupMenu>("model/containers");
            AddInterface<FilterModel>("model/filters")
                .Member(nameof(FilterModel.BoolFilters)).Optional()
                .Member(nameof(FilterModel.DateFilters)).Optional()
                .Member(nameof(FilterModel.DoubleFilters)).Optional()
                .Member(nameof(FilterModel.EnumFilter)).Optional()
                .Member(nameof(FilterModel.FilterEntity)).Optional()
                .Member(nameof(FilterModel.FilterStr)).Optional()
                .Member(nameof(FilterModel.LongFilter)).Optional()
                .Member(nameof(FilterModel.NumFilter)).Optional();


            AddInterface(typeof(FilterBase<>), "model/filters");

            AddInterface<OrderItem>("model/containers");


            AddInterface<Facet>("model/containers");


            AddInterface(typeof(CollectionResult), "model/containers").Member(nameof(CollectionResult.Entities)).Type("EntityBaseSearch<GeoPointTs>", "./../main/EntityBaseSearch")
                .Member(nameof(CollectionResult.IndexPropNames)).Optional()
                .Member(nameof(CollectionResult.Filter)).Optional()
                .Member(nameof(CollectionResult.Facets)).Optional()
                .Member(nameof(CollectionResult.OnlyForTsGeneticEntity)).Type("GeoPointTs", "./../main/GeoPointTs");

        }

        public override void OnBeforeBarrelGeneration(OnBeforeBarrelGenerationArgs args)
        {
            AddBarrel("model/main", BarrelScope.Files);
            AddBarrel("model/enums", BarrelScope.Files);
            AddBarrel("model/filters", BarrelScope.Files | BarrelScope.Directories);
            AddBarrel("model/containers", BarrelScope.Files | BarrelScope.Directories);
            AddBarrel("data", BarrelScope.Files | BarrelScope.Directories);
            AddBarrel("model", BarrelScope.Directories);
            AddBarrel(".", BarrelScope.Directories);

        }
    }

    

    /// <summary>
    /// toma el nombre que se asignó en un atributo jsonProperty y se lo asigna, si existe. de lo contrario
    /// tomará el nombre de la propiedad.
    /// </summary>
    public class JsonMemberNameConverter : IMemberNameConverter
    {
        //TypeNameConverterCollection
        public string Convert(string name, MemberInfo memberInfo)
        {
            var attribute = memberInfo.GetCustomAttribute<JsonPropertyAttribute>();
            
            var jsonname = attribute != null ? attribute.PropertyName : name;

            if (memberInfo.GetType().IsInterface)
            {
                return jsonname.Substring(1);
            }

            return jsonname;
        }
    }

    public class TypeMemberConverter : ITypeNameConverter
    {
        public string Convert(string name, Type type)
        {
            if (type.IsInterface)
            {
                return name.Substring(1);
            }
            return name;
        }
    }



    
}
