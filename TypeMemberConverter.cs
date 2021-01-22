using System;
using TypeGen.Core.Converters;


namespace mdm_gen
{
    /// <summary>
    /// Retorna todos los elementos como nombre de clase, 
    /// si es interface le saca la "I".
    /// </summary>
    public class TypeMemberConverter : ITypeNameConverter
    {
        /// <summary>
        /// Le quita la I si es interface
        /// </summary>
        /// <param name="name">nombre de la propiedad o clase o interface</param>
        /// <param name="type">tipo</param>
        /// <returns></returns>
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
