using Microsoft.Extensions.Localization;

namespace Order_Management_API.Common.Localization;


public class SharedResource { }


public class MockStringLocalizer : IStringLocalizer<SharedResource>
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new()
    {
        ["en-US"] = new()
        {
            ["Cat_Fiction"] = "Fiction & Literature",
            ["Cat_NonFiction"] = "Non-Fiction",
            ["Cat_Technical"] = "Technical & Professional",
            ["Cat_Children"] = "Children's Orders",
            ["Status_OutOfStock"] = "Out of Stock",
            ["Status_InStock"] = "In Stock",
            ["Status_Limited"] = "Limited Stock",
            ["Status_LastCopy"] = "Last Copy"
        },
        ["es-ES"] = new()
        {
            ["Cat_Fiction"] = "Ficción y Literatura",
            ["Cat_NonFiction"] = "No Ficción",
            ["Cat_Technical"] = "Técnico y Profesional",
            ["Cat_Children"] = "Pedidos Infantiles",
            ["Status_OutOfStock"] = "Agotado",
            ["Status_InStock"] = "En Stock",
            ["Status_Limited"] = "Stock Limitado",
            ["Status_LastCopy"] = "Última Copia"
        }
    };

    public LocalizedString this[string name]
    {
        get
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
            // Fallback to en-US if culture dictionary doesn't exist
            var dictionary = _resources.ContainsKey(culture) ? _resources[culture] : _resources["en-US"];
            // Fallback to key name if translation doesn't exist
            var value = dictionary.ContainsKey(name) ? dictionary[name] : name;
            return new LocalizedString(name, value, resourceNotFound: !dictionary.ContainsKey(name));
        }
    }

    public LocalizedString this[string name, params object[] arguments] => this[name];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _resources["en-US"].Select(x => new LocalizedString(x.Key, x.Value));
    }
}