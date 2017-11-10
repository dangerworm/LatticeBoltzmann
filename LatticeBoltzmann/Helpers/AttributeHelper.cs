using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LatticeBoltzmann.Helpers
{
    public static class AttributeHelper
    {
        public static string GetDescription(this PropertyInfo property)
        {
            var descriptionAttribute = property.CustomAttributes
                .FirstOrDefault(x => x.AttributeType == typeof(DescriptionAttribute));

            if (descriptionAttribute?.ConstructorArguments.FirstOrDefault() == null)
            {
                return property.Name;
            }

            return descriptionAttribute.ConstructorArguments.FirstOrDefault().Value as string;
        }
    }
}
