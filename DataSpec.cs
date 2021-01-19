using System;
using System.Collections;
using System.Collections.Generic;
using trifenix.connect.input;
using trifenix.connect.model;
using trifenix.mock;
using TypeGen.Core.SpecGeneration;


namespace mdm_gen
{
    public class DataSpec : GenerationSpec {
        private readonly IEnumerable<Type> _models;
        private readonly IEnumerable<Type> _inputs;
        private readonly IEnumerable<Type> _enums;

        public DataSpec(IEnumerable<Type> models, IEnumerable<Type> inputs, IEnumerable<Type> mdm_enums)
        {
            _models = models;
            _inputs = inputs;
            _enums = mdm_enums;

        }

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
