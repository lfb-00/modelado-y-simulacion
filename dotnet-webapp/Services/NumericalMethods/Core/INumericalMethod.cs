namespace dotnet_webapp.Services;

internal interface INumericalMethod
{
    string MethodKey { get; }
    MethodResult Run(MethodContext context, double first, double? second);
}
