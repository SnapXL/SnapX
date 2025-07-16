// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.Text;

public class PastebinTextUploaderService : TextUploaderService
{
    public override TextDestination EnumValue => TextDestination.Pastebin;
    public override bool CheckConfig(UploadersConfig config) => true;

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        PastebinSettings settings = config.PastebinSettings;

        if (string.IsNullOrEmpty(settings.TextFormat))
        {
            settings.TextFormat = taskInfo.TextFormat;
        }

        return new Pastebin(APIKeys.PastebinKey, settings);
    }
}

public sealed class Pastebin : TextUploader
{
    private string APIKey;

    public PastebinSettings Settings { get; private set; }

    public Pastebin(string apiKey)
    {
        APIKey = apiKey;
        Settings = new PastebinSettings();
    }

    public Pastebin(string apiKey, PastebinSettings settings)
    {
        APIKey = apiKey;
        Settings = settings;
    }

    public bool Login()
    {
        if (string.IsNullOrEmpty(Settings.Username) || string.IsNullOrEmpty(Settings.Password))
        {
            Settings.UserKey = null;
            Errors.Add("Pastebin login failed.");
            return false;
        }

        var loginArgs = new Dictionary<string, string?>
        {
            { "api_dev_key", APIKey },
            { "api_user_name", Settings.Username },
            { "api_user_password", Settings.Password }
        };

        string? loginResponse = SendRequestMultiPart("https://pastebin.com/api/api_login.php", loginArgs);

        if (!string.IsNullOrEmpty(loginResponse) && !loginResponse.StartsWith("Bad API request"))
        {
            Settings.UserKey = loginResponse;
            return true;
        }

        Settings.UserKey = null;
        Errors.Add("Pastebin login failed.");
        return false;
    }


    public override UploadResult UploadText(string? text, string? fileName)
    {
        var ur = new UploadResult();

        if (string.IsNullOrEmpty(text) || Settings == null) return ur;

        var args = new Dictionary<string, string?>
        {
            { "api_dev_key", APIKey }, // Your unique API Developer Key
            { "api_option", "paste" }, // Action set to 'paste' to create a new paste
            { "api_paste_code", text }, // The content of your paste
            { "api_paste_name", Settings.Title }, // Title of the paste
            { "api_paste_format", Settings.TextFormat }, // Syntax highlighting format
            { "api_paste_private", GetPrivacy(Settings.Exposure) }, // Public or private paste
            { "api_paste_expire_date", GetExpiration(Settings.Expiration) } // Expiration date of the paste
        };

        // If a user key is provided (for logged-in users)
        if (!string.IsNullOrEmpty(Settings.UserKey))
        {
            args.Add("api_user_key", Settings.UserKey);
        }

        // Send request to Pastebin API
        ur.Response = SendRequestMultiPart("https://pastebin.com/api/api_post.php", args);

        // If the response is a valid URL
        if (URLHelpers.IsValidURL(ur.Response))
        {
            // If RawURL setting is enabled, return the raw paste URL
            ur.URL = Settings.RawURL
                ? "https://pastebin.com/raw/" + URLHelpers.GetFileName(ur.Response)
                : ur.Response; // Otherwise, return the normal paste URL
        }
        else
        {
            // If the response is not a valid URL, add it to errors
            Errors.Add(ur.Response);
        }

        return ur;
    }


    private string? GetPrivacy(PastebinPrivacy privacy)
    {
        return privacy switch
        {
            PastebinPrivacy.Public => "0",
            PastebinPrivacy.Unlisted => "1",
            PastebinPrivacy.Private => "2",
            _ => throw new ArgumentOutOfRangeException(nameof(privacy), privacy, null)
        };
    }

    private string? GetExpiration(PastebinExpiration expiration) =>
        expiration switch
        {
            PastebinExpiration.N => "N",
            PastebinExpiration.M10 => "10M",
            PastebinExpiration.H1 => "1H",
            PastebinExpiration.D1 => "1D",
            PastebinExpiration.W1 => "1W",
            PastebinExpiration.W2 => "2W",
            PastebinExpiration.M1 => "1M",
            _ => throw new ArgumentOutOfRangeException(nameof(expiration), expiration, null)
        };


    public static List<PastebinSyntaxInfo> GetSyntaxList()
    {
        string syntaxList = @"4cs = 4CS
6502acme = 6502 ACME Cross Assembler
6502kickass = 6502 Kick Assembler
6502tasm = 6502 TASM/64TASS
abap = ABAP
actionscript = ActionScript
actionscript3 = ActionScript 3
ada = Ada
aimms = AIMMS
algol68 = ALGOL 68
apache = Apache Log
applescript = AppleScript
apt_sources = APT Sources
arm = ARM
asm = ASM (NASM)
asp = ASP
asymptote = Asymptote
autoconf = autoconf
autohotkey = Autohotkey
autoit = AutoIt
avisynth = Avisynth
awk = Awk
bascomavr = BASCOM AVR
bash = Bash
basic4gl = Basic4GL
dos = Batch
bibtex = BibTeX
blitzbasic = Blitz Basic
b3d = Blitz3D
bmx = BlitzMax
bnf = BNF
boo = BOO
bf = BrainFuck
c = C
c_winapi = C (WinAPI)
c_mac = C for Macs
cil = C Intermediate Language
csharp = C#
cpp = C++
cpp-winapi = C++ (WinAPI)
cpp-qt = C++ (with Qt extensions)
c_loadrunner = C: Loadrunner
caddcl = CAD DCL
cadlisp = CAD Lisp
ceylon = Ceylon
cfdg = CFDG
chaiscript = ChaiScript
chapel = Chapel
clojure = Clojure
klonec = Clone C
klonecpp = Clone C++
cmake = CMake
cobol = COBOL
coffeescript = CoffeeScript
cfm = ColdFusion
css = CSS
cuesheet = Cuesheet
d = D
dart = Dart
dcl = DCL
dcpu16 = DCPU-16
dcs = DCS
delphi = Delphi
oxygene = Delphi Prism (Oxygene)
diff = Diff
div = DIV
dot = DOT
e = E
ezt = Easytrieve
ecmascript = ECMAScript
eiffel = Eiffel
email = Email
epc = EPC
erlang = Erlang
euphoria = Euphoria
fsharp = F#
falcon = Falcon
filemaker = Filemaker
fo = FO Language
f1 = Formula One
fortran = Fortran
freebasic = FreeBasic
freeswitch = FreeSWITCH
gambas = GAMBAS
gml = Game Maker
gdb = GDB
genero = Genero
genie = Genie
gettext = GetText
go = Go
groovy = Groovy
gwbasic = GwBasic
haskell = Haskell
haxe = Haxe
hicest = HicEst
hq9plus = HQ9 Plus
html4strict = HTML
html5 = HTML 5
icon = Icon
idl = IDL
ini = INI file
inno = Inno Script
intercal = INTERCAL
io = IO
ispfpanel = ISPF Panel Definition
j = J
java = Java
java5 = Java 5
javascript = JavaScript
jcl = JCL
jquery = jQuery
json = JSON
julia = Julia
kixtart = KiXtart
kotlin = Kotlin
latex = Latex
ldif = LDIF
lb = Liberty BASIC
lsl2 = Linden Scripting
lisp = Lisp
llvm = LLVM
locobasic = Loco Basic
logtalk = Logtalk
lolcode = LOL Code
lotusformulas = Lotus Formulas
lotusscript = Lotus Script
lscript = LScript
lua = Lua
m68k = M68000 Assembler
magiksf = MagikSF
make = Make
mapbasic = MapBasic
markdown = Markdown
matlab = MatLab
mirc = mIRC
mmix = MIX Assembler
modula2 = Modula 2
modula3 = Modula 3
68000devpac = Motorola 68000 HiSoft Dev
mpasm = MPASM
mxml = MXML
mysql = MySQL
nagios = Nagios
netrexx = NetRexx
newlisp = newLISP
nginx = Nginx
nimrod = Nimrod
nsis = NullSoft Installer
oberon2 = Oberon 2
objeck = Objeck Programming Langua
objc = Objective C
ocaml-brief = OCalm Brief
ocaml = OCaml
octave = Octave
oorexx = Open Object Rexx
pf = OpenBSD PACKET FILTER
glsl = OpenGL Shading
oobas = Openoffice BASIC
oracle11 = Oracle 11
oracle8 = Oracle 8
oz = Oz
parasail = ParaSail
parigp = PARI/GP
pascal = Pascal
pawn = Pawn
pcre = PCRE
per = Per
perl = Perl
perl6 = Perl 6
php = PHP
php-brief = PHP Brief
pic16 = Pic 16
pike = Pike
pixelbender = Pixel Bender
pli = PL/I
plsql = PL/SQL
postgresql = PostgreSQL
postscript = PostScript
povray = POV-Ray
powershell = Power Shell
powerbuilder = PowerBuilder
proftpd = ProFTPd
progress = Progress
prolog = Prolog
properties = Properties
providex = ProvideX
puppet = Puppet
purebasic = PureBasic
pycon = PyCon
python = Python
pys60 = Python for S60
q = q/kdb+
qbasic = QBasic
qml = QML
rsplus = R
racket = Racket
rails = Rails
rbs = RBScript
rebol = REBOL
reg = REG
rexx = Rexx
robots = Robots
rpmspec = RPM Spec
ruby = Ruby
gnuplot = Ruby Gnuplot
rust = Rust
sas = SAS
scala = Scala
scheme = Scheme
scilab = Scilab
scl = SCL
sdlbasic = SdlBasic
smalltalk = Smalltalk
smarty = Smarty
spark = SPARK
sparql = SPARQL
sqf = SQF
sql = SQL
standardml = StandardML
stonescript = StoneScript
sclang = SuperCollider
swift = Swift
systemverilog = SystemVerilog
tsql = T-SQL
tcl = TCL
teraterm = Tera Term
thinbasic = thinBasic
typoscript = TypoScript
unicon = Unicon
uscript = UnrealScript
upc = UPC
urbi = Urbi
vala = Vala
vbnet = VB.NET
vbscript = VBScript
vedit = Vedit
verilog = VeriLog
vhdl = VHDL
vim = VIM
visualprolog = Visual Pro Log
vb = VisualBasic
visualfoxpro = VisualFoxPro
whitespace = WhiteSpace
whois = WHOIS
winbatch = Winbatch
xbasic = XBasic
xml = XML
xorg_conf = Xorg Config
xpp = XPP
yaml = YAML
z80 = Z80 Assembler
zxbasic = ZXBasic";

        return syntaxList.Lines()
            .Select(x => x.Trim())
            .Where(line => line.Contains('='))
            .Select(line =>
            {
                var index = line.IndexOf('=');
                return new PastebinSyntaxInfo
                {
                    Value = line.Remove(index).Trim(),
                    Name = line.Substring(index + 1).Trim()
                };
            })
            .Prepend(new PastebinSyntaxInfo("None", "text"))
            .ToList();
    }
}

public enum PastebinPrivacy // Localized
{
    Public,
    Unlisted,
    Private
}

public enum PastebinExpiration // Localized
{
    N,
    M10,
    H1,
    D1,
    W1,
    W2,
    M1
}

public class PastebinSyntaxInfo
{
    public string Name { get; set; }
    public string Value { get; set; }

    public PastebinSyntaxInfo()
    {
    }

    public PastebinSyntaxInfo(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public override string ToString()
    {
        return Name;
    }
}

public class PastebinSettings
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public PastebinPrivacy Exposure { get; set; } = PastebinPrivacy.Unlisted;
    public PastebinExpiration Expiration { get; set; } = PastebinExpiration.N;
    public string? Title { get; set; }
    public string? TextFormat { get; set; } = "text";
    public string? UserKey { get; set; }
    public bool RawURL { get; set; }
}

