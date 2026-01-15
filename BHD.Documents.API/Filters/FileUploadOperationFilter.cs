using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BHD.Filters;

public class FileUploadOperationFilter : IOperationFilter
{
   public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile))
            .ToList();

        if (!fileParams.Any())
            return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>(),
                        Required = new HashSet<string>()
                    }
                }
            }
        };

        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;

        foreach (var param in context.MethodInfo.GetParameters())
        {
            if (param.ParameterType == typeof(IFormFile))
            {
                schema.Properties[param.Name!] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
                schema.Required.Add(param.Name!);
            }
            else if (param.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.FromFormAttribute), false).Any())
            {
                var paramType = param.ParameterType;
                
                if (paramType.IsEnum)
                {
                    schema.Properties[param.Name!] = new OpenApiSchema
                    {
                        Type = "string",
                        Enum = Enum.GetNames(paramType)
                            .Select(name => new Microsoft.OpenApi.Any.OpenApiString(name) as Microsoft.OpenApi.Any.IOpenApiAny)
                            .ToList()
                    };
                }
                else if (paramType == typeof(string))
                {
                    schema.Properties[param.Name!] = new OpenApiSchema
                    {
                        Type = "string",
                        Nullable = Nullable.GetUnderlyingType(paramType) != null
                    };
                }
                else
                {
                    schema.Properties[param.Name!] = new OpenApiSchema
                    {
                        Type = "string"
                    };
                }

                if (!param.HasDefaultValue && Nullable.GetUnderlyingType(paramType) == null)
                {
                    schema.Required.Add(param.Name!);
                }
            }
        }

        var paramsToRemove = operation.Parameters
            .Where(p => fileParams.Any(fp => fp.Name == p.Name))
            .ToList();

        foreach (var param in paramsToRemove)
        {
            operation.Parameters.Remove(param);
        }
    } 
}