using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace DotOcrAPI
{
    /// <summary>
    /// An operation filter to correctly represent IFormFile parameters in Swagger UI.
    /// This is necessary because Swashbuckle might not automatically infer the correct schema
    /// for file uploads when using [FromForm] with IFormFile.
    /// </summary>
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParameters = context.ApiDescription.ActionDescriptor.Parameters
                .Where(p => p.ParameterType == typeof(IFormFile))
                .ToList();

            if (!fileParameters.Any())
            {
                return;
            }

            // Ensure the content type is multipart/form-data
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = fileParameters.ToDictionary(
                                p => p.Name,
                                p => new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                })
                        }
                    }
                }
            };
        }
    }
}