[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=kellybirr_zonkey&metric=alert_status)](https://sonarcloud.io/dashboard?id=kellybirr_zonkey)

[![Nuget](https://img.shields.io/nuget/v/zonkey.data?label=NuGet%3A%20Zonkey.Data)](http://www.nuget.org/packages/Zonkey.Data/)
[![Nuget](https://img.shields.io/nuget/v/zonkey.text?label=NuGet%3A%20Zonkey.Text)](http://www.nuget.org/packages/Zonkey.Text/)
[![Nuget](https://img.shields.io/nuget/v/zonkey.droid?label=NuGet%3A%20Zonkey.Droid)](http://www.nuget.org/packages/Zonkey.Droid/)
[![Nuget](https://img.shields.io/nuget/v/zonkey.mocks?label=NuGet%3A%20Zonkey.Mocks)](http://www.nuget.org/packages/Zonkey.Mocks/)

# Zonkey
Zonkey ORM (and then some) libraries for .Net

.

### Upgrading from 4.x and older versions of Zonkey
You can you Visual Studio RegEx Find/Replace with the following values to automatically update the SetFieldValue() calls in your DCs.

**FIND**

SetFieldValue\\(("\w+"), ref (\w+), value\\);

**REPLACE**

SetFieldValue(ref $2, value);
