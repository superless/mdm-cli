using System;
using System.Collections;
using System.Collections.Generic;
using trifenix.connect.input;
using trifenix.connect.model;
using trifenix.mock;
using TypeGen.Core.SpecGeneration;


namespace mdm_gen
{

    /// <summary>
    /// Generador común de modelos de trifenix connect para typescript,
    /// generando 
    /// 1. Modelo, normalmente la base de datos.
    /// 2. Input-model, modelo de ingreso para compartir en front end.
    /// 3. diccionario de la metadata, en este caso serán las enumeraciones de los tipos de datos para entitySearch.
    /// </summary>
    public class DataSpec : GenerationSpec {
        private readonly IEnumerable<Type> _models;
        private readonly IEnumerable<Type> _inputs;
        private readonly IEnumerable<Type> _enums;


        /// <summary>
        /// Toma los tipos de datos para modelo, inputs y enumeraciones, las que convertirá al ser aplicado el spec.w
        /// </summary>
        /// <param name="models">todos los tipos del modelo.</param>
        /// <param name="inputs">todos los inputs.</param>
        /// <param name="mdm_enums">enumeraciones por tipo.</param>
        public DataSpec(IEnumerable<Type> models, IEnumerable<Type> inputs, IEnumerable<Type> mdm_enums)
        {
            _models = models;
            _inputs = inputs;
            _enums = mdm_enums;

        }


        /// <summary>
        /// Creación del modelo en typescript
        /// </summary>
        /// <param name="args">argumentos de la configuración</param>
        public override void OnBeforeGeneration(OnBeforeGenerationArgs args)
        {
            
            AddClass<mdm>("metadata/");

            AddInterface<InputBase>("index-model/");

            AddInterface<DocumentDb>("model/");

            foreach (var item in _enums)
            {
                AddEnum(item, "index-model/");
            }


            foreach (var item in _models)
            {
                AddInterface(item, "model/");
            }

            

            

            foreach (var item in _inputs)
            {
                AddInterface(item, "input-model/");
            }


        }


        /// <summary>
        /// Crea las carpetas y los index, de cada una.
        /// </summary>
        /// <param name="args">argumentos de la configuración.</param>
        public override void OnBeforeBarrelGeneration(OnBeforeBarrelGenerationArgs args)
        {
            AddBarrel("metadata", BarrelScope.Files);
            AddBarrel("index-model", BarrelScope.Files);
            AddBarrel("model", BarrelScope.Files);
            AddBarrel("input-model", BarrelScope.Files);
            AddBarrel(".", BarrelScope.Directories);


        }


    }



    
}
