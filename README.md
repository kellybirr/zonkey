[![Quality gate](https://sonarcloud.io/api/project_badges/quality_gate?project=kellybirr_zonkey)](https://sonarcloud.io/dashboard?id=kellybirr_zonkey)

# Zonkey
Zonkey ORM (and then some) libraries for .Net

.

### Upgrading from 4.x and older versions of Zonkey
You can you Visual Studio RegEx Find/Replace with the following values to automatically update the SetFieldValue() calls in your DCs.

**FIND**

SetFieldValue\\(("\w+"), ref (\w+), value\\);

**REPLACE**

SetFieldValue(ref $2, value);
