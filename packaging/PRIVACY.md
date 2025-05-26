# SnapX's Privacy Policy

> Authored on 2/8/2025 \
> Last updated on 5/26/2025 10:26 AM EST

By default, SnapX collects telemetry data about how the application is performing in two ways:

- Application usage data (Tells us how many people are using the application on what systems)
- What Operating System you're running and which version you're using, i.e., `Windows 11 24H2 Home` or `macOS Sequoia` or `Fedora Linux 43 (KDE Plasma) x86_64 Linux 6.13.2`
- Application version, i.e., `1.0.0+44e1612` and the assembly you're using, `snapx-ui`
- What CPU your computer has, i.e., `i7-11800H`
- What GPU your computer has, i.e,. `RTX 3060 Laptop GPU`
- How much memory the application is using (Helps to identify memory leaks)
- How much overall memory your computer has to understand better the "headroom space" left.
- Application environment, i.e., running it on Linux with AppImage, Flatpak, Snap, or running the portable version of my application on Windows.
- Application crash data with stack traces that have had any PII removed & anonymized
- General Region, ie, `United States`

All this data helps to improve SnapX as it is [Free software](https://www.gnu.org/licenses/gpl-3.0.en.html).

### Definitions

telemetry - Modern, dynamic distributed systems require comprehensive monitoring to understand software behavior in various situations. Developers face challenges tracking the software’s performance in the field and responding to various modifications. To keep up with continuously changing requirements, it’s essential to have a simple way to collect data from systems the application is running.

stack trace - A stack trace is like a list that shows the order of steps a computer took before something went wrong. It helps us figure out which part of the program caused the problem by showing the path it followed. Just like a treasure map, it guides us to the exact place where the mistake happened.

fingerprinting - Information that can be used to single you out in the data samples, that makes your data unique.

anonymous - Not identified by name; of unknown name.

## What I will not do

- Sell your data
- Violate the GDPR or the CCPA.
- Spy on what you're doing
- Collect non-anonymous data such as your name, computer's name (`Brycen's GamingLaptop`), etc.
- Be evil

## Services Used

All the services used for telemetry are open source. [None of our telemetry code is secret](https://github.com/BrycensRanch/SnapX/blob/2b1843d973424d737c195664efdadc75f21122a4/SnapX.Core/SnapX.cs#L326).

- [Sentry](https://github.com/getsentry/sentry) - Application crash information & traces & performance analytics, i.e., specific code function taking a long time)
- [Aptabase](https://github.com/aptabase/aptabase) - Usage analytics, i.e., how many users are using a specific function, such as uploading)

Sentry's GDPR compliance page can be found [here](https://sentry.io/security/#gdpr).

Sentry's CCPA compliance page can be found [here](https://sentry.io/legal/ccpa/1.0.0/).

For application analytics, we use [Aptabase](https://aptabase.io), and their privacy policy can be found [here](https://aptabase.io/legal/privacy).


## In terms of fingerprinting

- Although your *public* IP address is naturally processed by these network services. It is not saved and thus discarded.

## How do I disable telemetry?

Go into the application's settings menu or config file

- On Linux, its `~/.config/SnapX/ApplicationConfig.json`
- On Windows, its `%USERPROFILE%\Documents\SnapX\ApplicationConfig.json`
- On macOS, its `~/Library/Application Support/SnapX/ApplicationConfig.json`

[Never used JSON before?](https://www.w3schools.com/whatis/whatis_json.asp)

You should be adding a key that looks like this:

```json
{
  "DisableTelemetry": true
}
```

Ensure there are no extra commas at the end of the last item. Do not add the beginning & ending curly braces if the config file already has them.


You can additionally disable it with an environment variable. With `SNAPX_DISABLETELEMETRY=true` or the [failed standard](https://consoledonottrack.com/) `DO_NOT_TRACK=1`

You can also set a registry key for those using SnapX in Windows organizations. `Computer\HKEY_LOCAL_MACHINE\SOFTWARE\BrycensRanch\SnapX\DisableTelemetry` with the value of `1`


Settings are pulled in via the first match from:
````
User Group Policy (HKCU\Policies\{Path})
Computer Group Policy (HKLM\Policies\{Path})
User registry (HKCU\{Path})
Computer registry (HKLM\{Path})
````

After it is disabled, and you restart SnapX, no data will be sent for crash data or application analytics. End of story.


## How do I request that my data be removed?

All data collected is anonymous. So I can't exactly fulfill requests to remove your specific data because the data that is collected is what I'd call... gray. There are no distinct identifiers.

## Final notes

I made SnapX because I have a point to prove. I have not sold my soul to the devil. \
I doubt I'll even get any donations for my work. This is not a transaction, though. \
The data is only useful for development. \
This is all I ask, **keep telemetry on**. Help me ***improve*** SnapX.

The data that I collect is not valuable to anyone else besides me and the community for cool graphs to look at & drive decisions for the project as well.
