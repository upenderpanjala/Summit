using Summit.VMS.Models.Safety;

namespace Summit.VMS.Services.Safety;

/// <summary>
/// Reference data for the states this deployment covers (Telangana & Andhra
/// Pradesh). District lists are representative of the major districts — replace
/// with the full official list per the latest state gazette before production.
/// Zones are police-internal groupings (commissionerate zones) and are captured
/// as free text at registration since they vary by city.
/// </summary>
public static class Jurisdictions
{
    public static readonly IReadOnlyDictionary<IndianState, string[]> Districts =
        new Dictionary<IndianState, string[]>
        {
            [IndianState.Telangana] = new[]
            {
                "Hyderabad", "Rangareddy", "Medchal-Malkajgiri", "Sangareddy",
                "Warangal", "Karimnagar", "Khammam", "Nizamabad",
                "Nalgonda", "Mahabubnagar", "Siddipet", "Adilabad"
            },
            [IndianState.AndhraPradesh] = new[]
            {
                "Visakhapatnam", "NTR (Vijayawada)", "Krishna", "Guntur",
                "Nellore", "Kurnool", "Ananthapuramu", "YSR Kadapa",
                "Chittoor", "Tirupati", "East Godavari", "West Godavari"
            }
        };

    public static object AsDto() => new
    {
        states = new[]
        {
            new { value = nameof(IndianState.Telangana), label = "Telangana",
                  districts = Districts[IndianState.Telangana] },
            new { value = nameof(IndianState.AndhraPradesh), label = "Andhra Pradesh",
                  districts = Districts[IndianState.AndhraPradesh] }
        }
    };
}
