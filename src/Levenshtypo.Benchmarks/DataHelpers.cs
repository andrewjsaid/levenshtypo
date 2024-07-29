using System.IO.Compression;
using System.Reflection;

internal static class DataHelpers
{
    public static IReadOnlyList<string> EnglishWords() => ReadLinesFromEmbeddedResource("english-words.zip");

    public static readonly string Initiate = "initiate";

    public static readonly string[] InitiateDistance3 = [
        "aciliate", "acitate", "agitate", "amitate", "animate", "anisate", "antiae", "antliate",
        "biciliate", "biliate", "bivittate", "ciliate", "citrate", "daimiate", "deitate", "denitrate",
        "digitate", "dimidiate", "dinitrate", "eciliate", "edituate", "enstate", "Entiat", "evitate",
        "evittate", "filiate", "finitive", "ictuate", "ignitible", "ignitive", "imitate", "imitated",
        "imitatee", "imitates", "inactivate", "inactuate", "inanimate", "inaquate", "inaurate",
        "incavate", "incerate", "inchoate", "incitate", "incite", "incitive", "increate", "inctirate",
        "incubate", "incudate", "indagate", "indicate", "indigitate", "indite", "indurate", "indusiate",
        "induviate", "inebriate", "inertiae", "inescate", "inesite", "inexpiate", "infatuate",
        "infiltrate", "infinitate", "infinite", "infirmate", "inflate", "infoliate", "infortiate",
        "infumate", "infuriate", "ingate", "ingeniate", "ingrate", "ingratiate", "inhiate", "inhumate",
        "inyoite", "inital", "initial", "initialed", "initialer", "initialise", "initialize",
        "initialled", "initialler", "initially", "initials", "initiant", "initiary", "initiate",
        "initiated", "initiates", "initiating", "initiation", "initiative", "initiatives", "initiator",
        "initiatory", "initiators", "initiatress", "initiatrix", "initio", "inition", "initis",
        "initive", "inmate", "innate", "innodate", "innovate", "innuate", "inodiate", "inopinate",
        "inornate", "inosite", "inquinate", "insaniate", "insatiate", "insatiated", "insidiate",
        "insinuate", "insite", "insociate", "insolate", "inspirate", "instate", "instated", "instates",
        "instigate", "instigated", "instigates", "institue", "institute", "insulate", "intake", "intice",
        "intimae", "intimate", "intimated", "intimater", "intimates", "intime", "intimidate", "intimiste",
        "intine", "intire", "intitle", "intonate", "intrate", "intricate", "intubate", "inundate",
        "inusitate", "inutile", "invigilate", "invinate", "inviolate", "invirtuate", "inviscate", "invite",
        "invitiate", "invocate", "iridate", "iridiate", "iridite", "irisate", "irritate", "isatate",
        "limitate", "lithate", "lithiate", "litigate", "lituate", "lixiviate", "militate", "militiate",
        "minchiate", "miniate", "Minimite", "ministate", "ministrate", "minutiae", "mitigate", "mitrate",
        "nictate", "nictitate", "niobate", "nitrate", "nitrated", "nitrates", "nitrite", "notate", "novitiate",
        "nutate", "opiniate", "ostiate", "preinitiate", "reinitiate", "sanitate", "satiate", "Scituate",
        "situate", "spatiate", "stibiate", "tertiate", "titilate", "titivate", "titrate", "trinitrate",
        "tritiate", "tritiated", "Uniate", "uninitiate", "uninitiated", "uninnate", "unintimate", "unitage",
        "unitive", "unitize", "unsatiate", "unstate", "unvitiated", "usitate", "viritrate", "visitate",
        "vitiate", "vitiated", "vitiates", "vittate"
        ];
    private static IReadOnlyList<string> ReadLinesFromEmbeddedResource(string name)
    {
        var lines = new List<string>();

        var assembly = Assembly.GetExecutingAssembly();
        var fullName = assembly.GetManifestResourceNames().Single(n => n.EndsWith(name));

        using (var resource = assembly.GetManifestResourceStream(fullName)!)
        using (var zipReader = new ZipArchive(resource))
        using (var reader = new StreamReader(zipReader.GetEntry("words.txt")!.Open()))
        {
            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }
        }

        return lines;
    }
}