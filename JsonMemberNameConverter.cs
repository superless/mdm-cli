using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System.Reflection;
using TypeGen.Core.Converters;


namespace mdm_gen
{
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



    
}
