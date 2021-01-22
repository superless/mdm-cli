using System;
using trifenix.connect.mdm.entity_model;
using trifenix.connect.mdm.enums;
using trifenix.connect.mdm.ts_model;
using trifenix.connect.mdm.ts_model.props;
using trifenix.connect.ts_model.enums;
using TypeGen.Core.SpecGeneration;


namespace mdm_gen
{
    /// <summary>
    /// Spec de generación de módelo base Mdm.
    /// </summary>
    public class ModelSpec : GenerationSpec {


        /// <summary>
        /// Base de la generación.
        /// </summary>
        /// <param name="args"></param>
        public override void OnBeforeGeneration(OnBeforeGenerationArgs args)
        {
            
            AddInterface<PropertyMetadadataEnum>("data/");
            AddInterface<EntitySearchDisplayInfo>("data/");
            AddInterface<PropertyMetadata>("data/");
            AddInterface<ModelMetaData>("data/");
            AddInterface<EntityMetadata>("data/");
            AddInterface(typeof(EntityBaseSearch<>), "model/main")
                .Member(nameof(EntityBaseSearch<Object>.bl)).Type("BoolProperty[]", "./BoolProperty")
                .Member(nameof(EntityBaseSearch<Object>.dbl)).Type("DblProperty[]", "./DblProperty")                
                .Member(nameof(EntityBaseSearch<Object>.enm)).Type("EnumProperty[]", "./EnumProperty")
                .Member(nameof(EntityBaseSearch<Object>.dt)).Type("DtProperty[]", "./DtProperty")
                .Member(nameof(EntityBaseSearch<Object>.num32)).Type("Num32Property[]", "./Num32Property")
                .Member(nameof(EntityBaseSearch<Object>.num64)).Type("Num64Property[]", "./Num64Property")
                .Member(nameof(EntityBaseSearch<Object>.rel)).Type("RelatedId[]", "./RelatedId")
                .Member(nameof(EntityBaseSearch<Object>.str)).Type("StrProperty[]", "./StrProperty")
                .Member(nameof(EntityBaseSearch<Object>.sug)).Type("StrProperty[]", "./StrProperty")
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


            AddInterface(typeof(CollectionResult), "model/containers").Member(nameof(CollectionResult.Entities)).Type("EntityBaseSearch<GeoPointTs>[]", "./../main/EntityBaseSearch")
                .Member(nameof(CollectionResult.IndexPropNames)).Optional()
                .Member(nameof(CollectionResult.Filter)).Optional()
                .Member(nameof(CollectionResult.Facets)).Optional()
                .Member(nameof(CollectionResult.OnlyForTsGeneticEntity)).Type("GeoPointTs", "./../main/GeoPointTs");

        }

        /// <summary>
        /// Creación de la estructura de carpetas del modelo mdm.
        /// </summary>
        /// <param name="args"></param>
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



    
}
