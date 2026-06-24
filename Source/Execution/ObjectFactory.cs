using System.Collections.ObjectModel;

namespace HttpsRichardy.Mapping.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ObjectFactory
{
    public static readonly Expression EmptyString = Constant(string.Empty);
    public static readonly LockingConcurrentDictionary<Type, Func<object>> CtorCache = new(GenerateConstructor);

    public static object CreateInstance(Type type) => CtorCache.GetOrAdd(type)();

    private static Func<object> GenerateConstructor(Type type) =>
        Lambda<Func<object>>(GenerateConstructorExpression(type, null).ToObject()).Compile();

    public static object CreateInterfaceProxy(Type interfaceType) => CreateInstance(ProxyGenerator.GetProxyType(interfaceType));

    public static Expression GenerateConstructorExpression(Type type, IGlobalConfiguration configuration) => type switch
    {
        { IsValueType: true } => configuration.Default(type),

        Type stringType when stringType == typeof(string) => EmptyString,

        { IsInterface: true } => CreateInterfaceExpression(type),

        { IsAbstract: true } => InvalidType(type, $"Cannot create an instance of abstract type {type}."),

        _ => CallConstructor(type, configuration)
    };

    private static Expression CallConstructor(Type type, IGlobalConfiguration configuration)
    {
        var constructor = type.GetConstructor(Internal.TypeExtensions.InstanceFlags, []);
        if (constructor is not null)
        {
            return New(constructor);
        }

        var constructorsWithParameters = type.GetDeclaredConstructors()
            .Select(constructorCandidate => new
            {
                Constructor = constructorCandidate,
                Parameters = constructorCandidate.GetParameters()
            });

        var constructorWithOptionalParameters = constructorsWithParameters.FirstOrDefault(candidate => candidate.Parameters.All(parameter => parameter.IsOptional));
        if (constructorWithOptionalParameters is null)
        {
            return InvalidType(type, $"{type} needs to have a constructor with 0 args or only optional args. Validate your configuration for details.");
        }

        var defaultArguments = constructorWithOptionalParameters.Parameters
            .Select(parameter => parameter.GetDefaultValue(configuration));

        return New(constructorWithOptionalParameters.Constructor, defaultArguments);
    }

    private static Expression CreateInterfaceExpression(Type type) =>
        type.IsGenericType(typeof(IDictionary<,>)) ? CreateCollection(type, typeof(Dictionary<,>)) :
        type.IsGenericType(typeof(IReadOnlyDictionary<,>)) ? CreateReadOnlyDictionary(type.GenericTypeArguments) :
        type.IsGenericType(typeof(ISet<>)) ? CreateCollection(type, typeof(HashSet<>)) :
        type.IsCollection() ? CreateCollection(type, typeof(List<>), GetIEnumerableArguments(type)) :
        InvalidType(type, $"Cannot create an instance of interface type {type}.");

    private static Expression InvalidType(Type type, string message) => Throw(Constant(new ArgumentException(message, "type")), type);
    private static Type[] GetIEnumerableArguments(Type type) => type.GetIEnumerableType()?.GenericTypeArguments ?? [typeof(object)];

    private static Expression CreateCollection(Type type, Type collectionType, Type[] genericArguments = null) =>
        ToType(New(collectionType.MakeGenericType(genericArguments ?? type.GenericTypeArguments)), type);

    private static Expression CreateReadOnlyDictionary(Type[] typeArguments)
    {
        var ctor = typeof(ReadOnlyDictionary<,>).MakeGenericType(typeArguments).GetConstructors()[0];
        return New(ctor, New(typeof(Dictionary<,>).MakeGenericType(typeArguments)));
    }
 
}