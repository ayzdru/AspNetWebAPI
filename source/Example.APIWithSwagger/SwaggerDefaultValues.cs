using System.Collections;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
namespace Example.APIWithSwagger
{
    using Microsoft.AspNet.OData;
    using Microsoft.AspNetCore.Mvc.ApiExplorer;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System.Linq;

    /// <summary>
    /// Represents the Swagger/Swashbuckle operation filter used to document the implicit API version parameter.
    /// </summary>
    /// <remarks>This <see cref="IOperationFilter"/> is only required due to bugs in the <see cref="SwaggerGenerator"/>.
    /// Once they are fixed and published, this class can be removed.</remarks>
    public class SwaggerDefaultValues : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to the specified operation using the given context.
        /// </summary>
        /// <param name="operation">The operation to apply the filter to.</param>
        /// <param name="context">The current operation filter context.</param>
        public void Apply( OpenApiOperation operation, OperationFilterContext context )
        {
            var apiDescription = context.ApiDescription;

            operation.Deprecated |= apiDescription.IsDeprecated();

            if ( operation.Parameters == null )
            {
                return;
            }

            // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/412
            // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/pull/413
            foreach ( var parameter in operation.Parameters )
            {
                var description = apiDescription.ParameterDescriptions.First( p => p.Name == parameter.Name );

                if ( parameter.Description == null )
                {
                    parameter.Description = description.ModelMetadata?.Description;
                }

                if ( parameter.Schema.Default == null && description.DefaultValue != null )
                {
                    parameter.Schema.Default = new OpenApiString( description.DefaultValue.ToString() );
                }

                parameter.Required |= description.IsRequired;
            }
            //EnableQueryAttribute


            var queryAttribute = context.MethodInfo.GetCustomAttributes(true)
                .Union(context.MethodInfo.DeclaringType.GetCustomAttributes(true))
                .OfType<EnableQueryAttribute>().FirstOrDefault();
            if (queryAttribute != null)
            {
                void RemoveParameter(AllowedQueryOptions option)
                {
                    if (!queryAttribute.AllowedQueryOptions.HasFlag(option))
                    {
                        var parameter = operation.Parameters.Where(q => q.Name == "$" + option.ToString().ToLower()).SingleOrDefault();
                        if (parameter != null)
                        {
                            operation.Parameters.Remove(parameter);
                        }
                    }
                }
                if (!queryAttribute.AllowedQueryOptions.HasFlag(AllowedQueryOptions.All))
                {

                    RemoveParameter(AllowedQueryOptions.Select);

                    RemoveParameter(AllowedQueryOptions.Expand);

                    // Additional OData query options are available for collections of entities only
                    if (context.MethodInfo.ReturnType.IsArray ||
                        typeof(IQueryable).IsAssignableFrom(context.MethodInfo.ReturnType) ||
                        typeof(IEnumerable).IsAssignableFrom(context.MethodInfo.ReturnType))
                    {
                        RemoveParameter(AllowedQueryOptions.Filter);
                        RemoveParameter(AllowedQueryOptions.OrderBy);
                        RemoveParameter(AllowedQueryOptions.Top);
                        RemoveParameter(AllowedQueryOptions.Count);
                    }
                }

            }
        }
    }
}